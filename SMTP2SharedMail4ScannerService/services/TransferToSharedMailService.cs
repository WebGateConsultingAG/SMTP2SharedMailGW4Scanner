using System.Buffers;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Microsoft.Graph;
using Azure.Identity;
using MimeKit;

namespace WebGate.SMTPServer4SharedMailboxes;
public class TransferToSharedMailService : MessageStore
{
    private readonly GraphServiceClient _client;
    private readonly string _sharedMailbox;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public TransferToSharedMailService(SMTP2SharedMailConfig config, Microsoft.Extensions.Logging.ILogger logger)
    {
        var options = new TokenCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };
        var clientSecretCredential = new ClientSecretCredential(config.TenantId, config.ApplicationId, config.ClientSecret, options);
        this._client = new GraphServiceClient(clientSecretCredential);
        this._sharedMailbox = config.SharedEmail!;
        this._logger = logger;
    }
    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Start recieving Message");
        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            stream.Write(memory.Span);
        }

        stream.Position = 0;

        var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        this._logger.LogInformation("Message recived. Passing to SharedMailbox");
        await SendViaSharedMailbox(message, cancellationToken);
        this._logger.LogInformation("SharedMailbox processing DONE");

        return SmtpResponse.Ok;
    }

    private async Task SendViaSharedMailbox(MimeKit.MimeMessage message, CancellationToken cancellationToken)
    {
        var bodyElement = ExtractBody(message);
        var outGoing = new Message
        {
            Subject = message.Subject,
            Body = bodyElement,
            ToRecipients = message.To.Select(email =>
            {
                var mbEmail = email as MimeKit.MailboxAddress;
                return new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = mbEmail!.Address
                    }
                };
            }).ToList(),
        };
        _logger.LogInformation("Ready to process Attachments");
        var allAttachments = message.Attachments.Select(attachment =>
        {
            var fileName = "scannerMessage";
            var mimeType = "application/octed-stream";
            using (var stream = new MemoryStream())
            {
                if (attachment is MimeKit.MessagePart)
                {
                    fileName = attachment.ContentDisposition?.FileName;
                    var rfc822 = (MimeKit.MessagePart)attachment;
                    mimeType = rfc822.ContentType.MimeType;
                    rfc822.Message.WriteTo(stream);
                }
                else
                {
                    var part = (MimeKit.MimePart)attachment;
                    fileName = part.FileName;
                    mimeType = part.ContentType.MimeType;
                    part.Content.DecodeTo(stream);
                }
                return new FileAttachment()
                {
                    Name = fileName,
                    ContentType = mimeType,
                    ContentBytes = stream.ToArray()
                };
            }
        });
        MessageAttachmentsCollectionPage attachments = new MessageAttachmentsCollectionPage();
        foreach (var fAttachment in allAttachments)
        {
            attachments.Add(fAttachment);
        }
        outGoing.Attachments = attachments;
        try
        {
            _logger.LogInformation("Execute Send to {sharedMailbox}", _sharedMailbox);
            await _client.Users[_sharedMailbox].SendMail(outGoing).Request().PostAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Try to send message");
        }
    }

    private ItemBody ExtractBody(MimeKit.MimeMessage message)
    {
        string htmlBody = message.HtmlBody;
        if (!String.IsNullOrEmpty(htmlBody))
        {
            return new ItemBody
            {
                ContentType = BodyType.Html,
                Content = htmlBody
            };
        }
        return new ItemBody
        {
            ContentType = BodyType.Text,
            Content = message.TextBody
        };
    }
}
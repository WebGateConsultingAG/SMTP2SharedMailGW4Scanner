using Microsoft.Extensions.Options;
using SmtpServer;
namespace WebGate.SMTPServer4SharedMailboxes;


public class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private readonly SmtpServer.SmtpServer _server;
    public WindowsBackgroundService(ILogger<WindowsBackgroundService> logger, IOptions<SMTP2SharedMailConfig> config)
    {
        _logger = logger;
        _logger.LogInformation("Building SmtpServer");
        var options = new SmtpServerOptionsBuilder()
            .ServerName("SMTP2SharedMailGW")
            .Port(config.Value.Port, !String.IsNullOrEmpty(config.Value.UserName))
            .Build();

        var serviceProvider = new SmtpServer.ComponentModel.ServiceProvider();
        serviceProvider.Add(new SMTPAuthenticationService(config.Value, logger));
        serviceProvider.Add(new TransferToSharedMailService(config.Value, logger));
        _server = new SmtpServer.SmtpServer(options, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SMTP Service");
        try
        {
            return _server.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
            return Task.FromException(ex);
        }
    }
}
public class SMTP2SharedMailConfig
{
    public int Port { set; get; }
    public string? UserName { set; get; }
    public string? Password { set; get; }
    public string? TenantId { set; get; }
    public string? ApplicationId { set; get; }
    public string? ClientSecret { set; get; }
    public string? SharedEmail { set; get; }
}
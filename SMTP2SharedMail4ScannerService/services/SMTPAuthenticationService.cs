using SmtpServer;
using SmtpServer.Authentication;

namespace WebGate.SMTPServer4SharedMailboxes;
public sealed class SMTPAuthenticationService : UserAuthenticator
{
    private readonly SMTP2SharedMailConfig _config;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    public SMTPAuthenticationService(SMTP2SharedMailConfig config, Microsoft.Extensions.Logging.ILogger logger)
    {
        _config = config;
        _logger = logger;
    }
    /// <summary>
    /// Authenticate a user account.
    /// </summary>
    /// <param name="context">The session context.</param>
    /// <param name="user">The user to authenticate.</param>
    /// <param name="password">The password of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>true if the user is authenticated, false if not.</returns>
    public override Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Authentication is configured for: {UserName}", _config.UserName);
        if (String.IsNullOrEmpty(_config.UserName)) {
            return Task.FromResult(true);
        }
        _logger.LogInformation("{user} is requesting Authentication.", user);
        return Task.FromResult(user == _config.UserName && password == _config.Password);
    }
}
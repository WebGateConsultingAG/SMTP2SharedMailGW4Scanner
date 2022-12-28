using WebGate.SMTPServer4SharedMailboxes;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "SMTP2SharedMailboxGW";
    })
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.Configure<SMTP2SharedMailConfig>(configuration.GetSection(nameof(SMTP2SharedMailConfig)));
        LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
        services.AddHostedService<WindowsBackgroundService>();

    })
    .ConfigureLogging((context, logging) =>
    {
        // See: https://github.com/dotnet/runtime/issues/47303
        logging.AddConfiguration(
            context.Configuration.GetSection("Logging"));
    })
    .Build();

await host.RunAsync();

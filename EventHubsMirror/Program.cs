using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventHubsMirror
{
    class Program
    {
        public static IConfiguration Configuration { get; private set; }
        public static ILoggerFactory LoggerFactory { get; private set; }

        private static CancellationTokenSource tokenSource;

        static void Main(string[] args)
        {
            Configuration = BuildConfiguration();
            LoggerFactory = BuildLogger();

            tokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) => { e.Cancel = true; tokenSource.Cancel(); };

            MainAsync(tokenSource.Token).GetAwaiter().GetResult();
        }

        private static ILoggerFactory BuildLogger()
        {
            return new LoggerFactory().AddConsole(Configuration.GetSection("Logging"));
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static async Task MainAsync(CancellationToken token)
        {
            var logger = LoggerFactory.CreateLogger("Program");

            logger.LogInformation("Starting mirror...");

            var eventProcessorHost = new EventProcessorHost(
                Configuration["EventHubName"],
                Configuration["ConsumerGroupName"],
                Configuration.GetConnectionString("EventHubSource"),
                Configuration.GetConnectionString("ConsumerGroupStorage"),
                Configuration["StorageContainer"]);

            await eventProcessorHost.RegisterEventProcessorAsync<MirrorEventProcessor>();

            token.WaitHandle.WaitOne();

            await eventProcessorHost.UnregisterEventProcessorAsync();

            logger.LogInformation("Mirror closed.");
        }

        public static void Close()
        {
            tokenSource.Cancel();
        }
    }
}

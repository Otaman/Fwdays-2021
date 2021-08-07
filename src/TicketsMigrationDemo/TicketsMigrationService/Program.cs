using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Prometheus;

namespace TicketsMigrationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
            {
                {"service", "TicketsMigrationService"}
            });
            
            var metricServer = new MetricServer(port: 2345);
            metricServer.Start();
            MetricsReporter.BeginReporting();


            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<MigrationSettings>(configuration.GetSection("TicketsMigration"));
            services.AddSingleton<TicketsMigrationService>();
            
            var provider = services.BuildServiceProvider();
            var migrationService = provider.GetRequiredService<TicketsMigrationService>();
            
            var userIds = UserIdGenerator.GenerateIds().Take(100000);
            await migrationService.MigrateTickets(userIds);
            
            Console.WriteLine("Finished migration");
        }
    }
    
    // No record support
    class MigrationSettings { public int MaxDegreeOfParallelism { get; set; } }
    
    class TicketsMigrationService : IDisposable
    {
        private readonly IDisposable _subscription;
        private MigrationSettings _settings;
        private CancellationTokenSource _configurationCancellation = new();
        private readonly TicketsClient _ticketsClient = new();
        
        public TicketsMigrationService(IOptionsMonitor<MigrationSettings> optionsMonitor)
        {
            _subscription = optionsMonitor.OnChange(ReconfigureMigration);
            _settings = optionsMonitor.CurrentValue;
        }

        private void ReconfigureMigration(MigrationSettings settings)
        {
            _settings = settings;
            _configurationCancellation?.Cancel();
            _configurationCancellation = new CancellationTokenSource();
            Console.WriteLine("Configuration updated. " +
                              "MaxDegreeOfParallelism = " + _settings.MaxDegreeOfParallelism);
        }

        public async Task MigrateTickets(IEnumerable<Guid> userIds)
        {
            using var enumerator = userIds.GetEnumerator();
            CancellationToken configurationCancellation;
            do
            {
                if (_settings.MaxDegreeOfParallelism == 0)
                {
                    Console.WriteLine("Migration paused");
                    await WaitAndDontThrow(_configurationCancellation.Token);
                    configurationCancellation = _configurationCancellation.Token;
                    continue;
                }

                var actionBlock = new ActionBlock<Guid>(userId => GetAllInfo(userId),
                    new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = 50,
                        MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
                    });

                configurationCancellation = _configurationCancellation.Token;
                while (!configurationCancellation.IsCancellationRequested && enumerator.MoveNext())
                {
                    await actionBlock.SendAsync(enumerator.Current);
                }

                actionBlock.Complete();
                await actionBlock.Completion;
            } while (configurationCancellation.IsCancellationRequested);
        }

        private Task GetAllInfo(Guid userId) => 
            MetricsReporter.Track(() => _ticketsClient.GetAllInfo(userId));

        private async Task WaitAndDontThrow(CancellationToken token)
        {
            try
            {
                await Task.Delay(-1, token); // infinite delay
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _configurationCancellation?.Dispose();
        }
    }
}
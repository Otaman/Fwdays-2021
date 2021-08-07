using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Contracts;
using Prometheus;

namespace UsersEmulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
            {
                {"service", "UsersEmulator"}
            });
            
            var metricServer = new MetricServer(port: 1234);
            metricServer.Start();
            MetricsReporter.BeginReporting();

            var client = new TicketsClient();
            var userIds = UserIdGenerator.GenerateIds().Take(100000).ToArray();
            var random = new Random();
            
            while (true)
            {
                var userId = userIds[random.Next(userIds.Length)];
                try
                {
                    var sw = Stopwatch.StartNew();
                    await MetricsReporter.Track(() => client.GetAllInfo(userId));
                    
                    var waitTime = TimeSpan.FromMilliseconds(300) - sw.Elapsed;
                    if (waitTime > TimeSpan.FromMilliseconds(10))
                        await Task.Delay(waitTime);
                }
                catch (Exception)
                {
                    Console.WriteLine("GetAllInfo failed for user " + userId);
                }
            }
        }
    }
}
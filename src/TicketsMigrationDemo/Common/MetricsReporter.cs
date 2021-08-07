using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;

namespace Contracts
{
    /// <summary>
    /// Reports to both console and prometheus
    /// </summary>
    public static class MetricsReporter
    {
        private static int _processedCount;
        private static int _failedCount;
        private static List<int> _elapsedMilliseconds = new();
        
        private static readonly Summary SupportServiceResponseTime = Metrics.CreateSummary("response_time_ms", "",
            new SummaryConfiguration
            {
                MaxAge = TimeSpan.FromSeconds(10),
                Objectives = new[]
                {
                    new QuantileEpsilonPair(0.5, 0.05),
                    new QuantileEpsilonPair(0.9, 0.05),
                    new QuantileEpsilonPair(0.95, 0.01),
                    new QuantileEpsilonPair(0.99, 0.005),
                }
            });

        private static readonly Counter ProcessedUsersCountProm = Metrics.CreateCounter("processed_users", "");

        public static async Task<T> Track<T>(Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await action();
                sw.Stop();
                IncrementProcessed();
                ProcessedUsersCountProm.Inc();
                return response;
            }
            catch (Exception)
            {
                IncrementFailed();
                throw;
            }
            finally
            {
                SupportServiceResponseTime.Observe(sw.ElapsedMilliseconds);
                TrackElapsedTime(sw.Elapsed);
            }
        }

        private static void IncrementProcessed() => _processedCount++;
        private static void IncrementFailed() => _failedCount++;
        private static void TrackElapsedTime(TimeSpan elapsed) => _elapsedMilliseconds.Add(elapsed.Milliseconds);

        
        private static void Report()
        {
            var processed = _processedCount;
            _processedCount = 0;
            var failed = _failedCount;
            _failedCount = 0;
            var elapsed = _elapsedMilliseconds;
            _elapsedMilliseconds = new List<int>();

            var median = elapsed.Count == 0 ? 0 : elapsed.Sum() / (double) elapsed.Count;
            
            Console.WriteLine($"Processed {processed.ToString().PadLeft(4)}, " +
                              $"elapsed median {median.ToString("N0").PadLeft(4)} ms");
        }

        public static void BeginReporting()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    Report();
                }
                // ReSharper disable once FunctionNeverReturns
            });
        }
    }
}
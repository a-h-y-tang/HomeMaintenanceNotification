using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    public class RetryCircuitBreakerPipelineBuilder
    {
        public static ResiliencePipeline Build(ILogger logger, int maxRetries, int retryDelayMs)
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();
            pipelineBuilder.AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(retryDelayMs),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    logger.LogError($"OnRetry exception: {exception.Message}", exception);
                    return default;
                }
            }); // We are not calling the Build method here because we will do it as a separate step to make the code cleaner.

            pipelineBuilder.AddCircuitBreaker(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 1.0,
                SamplingDuration = TimeSpan.FromSeconds(2),
                MinimumThroughput = 2,
                BreakDuration = TimeSpan.FromSeconds(3),
                OnOpened = args =>
                {
                    var exception = args.Outcome.Exception!;
                    logger.LogWarning($"Breaker logging: Breaking the circuit for {args.BreakDuration.TotalMilliseconds}ms! due to: {exception.Message}");
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Breaker logging: Call OK! Closed the circuit again!");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("Breaker logging: Half-open: Next call is a trial!");
                    return default;
                }
            }); // We are not calling the Build method because we want to add one more strategy to the pipeline.

            // Build the pipeline since we have added all the necessary strategies to it.
            return pipelineBuilder.Build();
        }
    }
}

using DevWiki.Microservices.CircuitBreaker.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;

namespace DevWiki.Microservices.CircuitBreaker.Policies;

public static class PoliticaResiliencia
{
    public static ResiliencePipeline<ResultadoPagamento> CriarPipeline(int maxFalhasParaAbrir = 4,
                                                                       double ratioFalhas = 0.5,
                                                                       TimeSpan? duracaoAbertura = null,
                                                                       int maxRetentativas = 2)
    {
        var breakDuration = duracaoAbertura ?? TimeSpan.FromSeconds(30);

        var builder = new ResiliencePipelineBuilder<ResultadoPagamento>()
            .AddFallback(new FallbackStrategyOptions<ResultadoPagamento>
            {
                ShouldHandle = new PredicateBuilder<ResultadoPagamento>().Handle<BrokenCircuitException>(),
                FallbackAction = _ => ValueTask.FromResult(Outcome.FromResult(ResultadoPagamento.Degradado()))
            });

        if (maxRetentativas >= 1)
        {
            builder.AddRetry(new RetryStrategyOptions<ResultadoPagamento>
            {
                MaxRetryAttempts = maxRetentativas,
                Delay = TimeSpan.FromMilliseconds(10),
                ShouldHandle = new PredicateBuilder<ResultadoPagamento>().Handle<HttpRequestException>()
            });
        }

        return builder
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ResultadoPagamento>
            {
                FailureRatio = ratioFalhas,
                MinimumThroughput = maxFalhasParaAbrir,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = breakDuration,
                ShouldHandle = new PredicateBuilder<ResultadoPagamento>().Handle<HttpRequestException>()
            })
            .Build();
    }
    public static ResiliencePipeline<ResultadoPagamento> CriarPipelineRapido(int minimoThroughput = 3,
                                                                             int maxRetentativas = 0) =>
        CriarPipeline(maxFalhasParaAbrir: minimoThroughput,
                      ratioFalhas: 1.0,
                      duracaoAbertura: TimeSpan.FromMilliseconds(500),
                      maxRetentativas: maxRetentativas);

}
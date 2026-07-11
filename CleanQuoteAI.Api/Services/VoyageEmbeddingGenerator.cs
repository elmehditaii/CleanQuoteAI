using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CleanQuoteAI.Api.Services;

/// <summary>
/// Générateur d'embeddings Voyage AI (voyage-code-3, dimension 1024) implémentant
/// l'abstraction Microsoft.Extensions.AI, avec retry exponentiel sur rate limiting.
/// </summary>
public class VoyageEmbeddingGenerator(HttpClient http, IConfiguration config, ILogger<VoyageEmbeddingGenerator> logger)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    private const int MaxRetries = 5;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = config["Voyage:ApiKey"]
            ?? throw new InvalidOperationException("Clé Voyage manquante : dotnet user-secrets set \"Voyage:ApiKey\" \"...\"");
        var model = config["Voyage:Model"] ?? "voyage-code-3";

        string inputType = "document";
        if (options?.AdditionalProperties?.TryGetValue("input_type", out var it) == true && it is string s)
            inputType = s;

        var payload = new
        {
            input = values.ToArray(),
            model,
            input_type = inputType,
            output_dimension = options?.Dimensions ?? 1024,
        };

        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new("Bearer", apiKey);

            HttpResponseMessage? response = null;
            try
            {
                response = await http.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                    var embeddings = doc.RootElement.GetProperty("data")
                        .EnumerateArray()
                        .Select(item => new Embedding<float>(
                            item.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray()))
                        .ToList();
                    return new GeneratedEmbeddings<Embedding<float>>(embeddings);
                }

                var retryable = response.StatusCode == HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500;
                if (!retryable || attempt >= MaxRetries)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Voyage AI a répondu {(int)response.StatusCode} : {body}");
                }

                var delay = GetRetryDelay(response, attempt);
                logger.LogWarning("Voyage AI {Status} — nouvelle tentative {Attempt}/{Max} dans {Delay:N1}s",
                    (int)response.StatusCode, attempt + 1, MaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < MaxRetries && response is null)
            {
                // Erreur réseau : retry exponentiel
                var delay = GetRetryDelay(null, attempt);
                logger.LogWarning("Erreur réseau Voyage AI — nouvelle tentative {Attempt}/{Max} dans {Delay:N1}s",
                    attempt + 1, MaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage? response, int attempt)
    {
        if (response?.Headers.RetryAfter?.Delta is { } retryAfter)
            return retryAfter;
        var baseDelay = Math.Min(Math.Pow(2, attempt), 30);
        return TimeSpan.FromSeconds(baseDelay + Random.Shared.NextDouble());
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

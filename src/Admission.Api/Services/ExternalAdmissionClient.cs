using System.Net.Http.Json;
using Admission.Api.DTOs.ExternalAdmission;

namespace Admission.Api.Services;

public class ExternalAdmissionClient : IExternalAdmissionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalAdmissionClient> _logger;
    public ExternalAdmissionClient(
    HttpClient httpClient,
    ILogger<ExternalAdmissionClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalAdmissionResponse> CreateApplicationAsync(
    ExternalAdmissionRequest request,
    string idempotencyKey,
    bool simulateTimeout,
    int simulateTransientFailures,
    CancellationToken cancellationToken)
    {
        var endpoint =
            $"api/applications" +
            $"?simulateSlowResponse={simulateTimeout.ToString().ToLowerInvariant()}" +
            $"&failFirstAttempts={simulateTransientFailures}";

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                endpoint);

        httpRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        httpRequest.Content =
            JsonContent.Create(request);

        var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<ExternalAdmissionResponse>(
                    cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "External admission platform returned an empty response.");
        }
        return result;
    }

    public async Task<ExternalAdmissionResponse?> GetApplicationAsync(
    string externalApplicationId,
    CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/applications/{Uri.EscapeDataString(externalApplicationId)}",
            cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ExternalAdmissionResponse>(
                cancellationToken: cancellationToken);
    }
}
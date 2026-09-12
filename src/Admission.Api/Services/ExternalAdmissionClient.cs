using System.Net.Http.Json;
using Admission.Api.DTOs.ExternalAdmission;

namespace Admission.Api.Services;

public class ExternalAdmissionClient : IExternalAdmissionClient
{
    private readonly HttpClient _httpClient;

    public ExternalAdmissionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalAdmissionResponse> CreateApplicationAsync(
    ExternalAdmissionRequest request,
    string idempotencyKey,
    bool simulateTimeout,
    CancellationToken cancellationToken)
    {
        var endpoint = simulateTimeout
            ? "api/applications?simulateSlowResponse=true"
            : "api/applications";

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
}
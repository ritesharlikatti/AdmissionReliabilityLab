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
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/applications",
            request,
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
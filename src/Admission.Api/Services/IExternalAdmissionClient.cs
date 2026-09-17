using Admission.Api.DTOs.ExternalAdmission;

namespace Admission.Api.Services;

public interface IExternalAdmissionClient
{
    Task<ExternalAdmissionResponse> CreateApplicationAsync(
        ExternalAdmissionRequest request,
        string idempotencyKey,
        bool simulateTimeout,
        int simulateTransientFailures,
        CancellationToken cancellationToken);
    Task<ExternalAdmissionResponse?> GetApplicationAsync(
        string externalApplicationId,
        CancellationToken cancellationToken);
}
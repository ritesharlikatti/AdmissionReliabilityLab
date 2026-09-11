using Admission.Api.DTOs.ExternalAdmission;

namespace Admission.Api.Services;

public interface IExternalAdmissionClient
{
    Task<ExternalAdmissionResponse> CreateApplicationAsync(
        ExternalAdmissionRequest request,
        CancellationToken cancellationToken);
}
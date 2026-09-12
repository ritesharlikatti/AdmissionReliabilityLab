using Admission.Api.DTOs.Applications;

namespace Admission.Api.Services;

public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResponse> RetryExternalSubmissionAsync(
        int applicationId,
        CancellationToken cancellationToken);
}
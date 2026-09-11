using Admission.Api.DTOs.Applications;

namespace Admission.Api.Services;

public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken);
}
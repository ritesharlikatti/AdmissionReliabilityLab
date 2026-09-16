using Admission.Api.DTOs.Applications;
using Admission.Api.DTOs.Webhooks;
namespace Admission.Api.Services;


public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResponse> RetryExternalSubmissionAsync(
        int applicationId,
        CancellationToken cancellationToken);

    Task<bool> ProcessAdmissionWebhookAsync(
        AdmissionWebhookRequest request,
        CancellationToken cancellationToken);
}
using Admission.Api.DTOs.Applications;
using Admission.Api.DTOs.Webhooks;
using Admission.Api.Models;
namespace Admission.Api.Services;


public interface IApplicationService
{
    Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResponse> RetryExternalSubmissionAsync(
        int applicationId,
        CancellationToken cancellationToken);

    Task<WebhookProcessingResult> ProcessAdmissionWebhookAsync(
        AdmissionWebhookRequest request,
        CancellationToken cancellationToken);
}
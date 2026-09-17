using Admission.Api.DTOs.Webhooks;
using Admission.Api.Models;
using Admission.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("api/webhooks/admission")]
public class AdmissionWebhooksController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public AdmissionWebhooksController(
        IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
public async Task<IActionResult> Receive(
    AdmissionWebhookRequest request,
    CancellationToken cancellationToken)
{
    var result =
        await _applicationService
            .ProcessAdmissionWebhookAsync(
                request,
                cancellationToken);

        return result switch
        {
            WebhookProcessingResult.Processed =>
                Ok(new
                {
                    message =
                        "Webhook processed successfully."
                }),

            WebhookProcessingResult.AlreadyProcessed =>
                Ok(new
                {
                    message =
                        "Webhook was already processed."
                }),

            WebhookProcessingResult.ApplicationNotFound =>
                NotFound(new ProblemDetails
                {
                    Title = "Application not found",
                    Detail =
                        $"No local application was found for external ID " +
                        $"'{request.ExternalApplicationId}'.",
                    Status =
                        StatusCodes.Status404NotFound
                }),
            _ => throw new InvalidOperationException(
                "Unexpected webhook processing result.")
        };
    
}
}
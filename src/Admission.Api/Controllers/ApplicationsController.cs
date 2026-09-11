using Admission.Api.DTOs.Applications;
using Admission.Api.Exceptions;
using Admission.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(
        IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(
    CreateApplicationRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var application =
                await _applicationService.CreateAsync(
                    request,
                    cancellationToken);

            return Created(
                $"/api/applications/{application.Id}",
                application);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Related resource not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}
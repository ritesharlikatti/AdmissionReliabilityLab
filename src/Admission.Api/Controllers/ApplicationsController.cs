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

    [HttpGet]
    public async Task<ActionResult<List<ApplicationResponse>>> GetAll(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var applications =
                await _applicationService.GetAllAsync(
                    status,
                    cancellationToken);

            return Ok(applications);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid application status",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
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

    [HttpPost("{id:int}/retry-external")]
    public async Task<ActionResult<ApplicationResponse>> RetryExternal(
    int id,
    CancellationToken cancellationToken)
    {
        try
        {
            var application =
                await _applicationService.RetryExternalSubmissionAsync(
                    id,
                    cancellationToken);

            return Ok(application);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Application not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPost("{id:int}/reconcile")]
    public async Task<ActionResult<ReconciliationResponse>> Reconcile(
    int id,
    CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _applicationService.ReconcileAsync(
                    id,
                    cancellationToken);

            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Application not found",
                Detail = ex.Message,
                Status =
                    StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var application =
            await _applicationService.GetByIdAsync(
                id,
                cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(application);
    }
}
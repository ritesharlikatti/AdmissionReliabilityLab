using ExternalAdmission.Api.Data;
using ExternalAdmission.Api.DTOs;
using ExternalAdmission.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExternalAdmission.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly ExternalAdmissionDbContext _db;

    public ApplicationsController(
        ExternalAdmissionDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateExternalApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = new ExternalApplication
        {
            ExternalApplicationId =
                $"EXT-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",

            SourceApplicationNumber =
                request.SourceApplicationNumber,

            StudentNumber =
                request.StudentNumber,

            CourseCode =
                request.CourseCode,

            Status = "Received",

            ReceivedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);

        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            $"/api/applications/{application.ExternalApplicationId}",
            application);
    }
}
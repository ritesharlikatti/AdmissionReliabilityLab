using Admission.Api.Data;
using Admission.Api.DTOs.Applications;
using Admission.Api.Models;
using Admission.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Admission.Api.DTOs.ExternalAdmission;

namespace Admission.Api.Services;

public class ApplicationService : IApplicationService
{
    private readonly AdmissionDbContext _db;
    private readonly IExternalAdmissionClient _externalAdmissionClient;

    public ApplicationService(AdmissionDbContext db, IExternalAdmissionClient externalAdmissionClient)
    {
        _db = db;
        _externalAdmissionClient = externalAdmissionClient;
    }


    public async Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var student = await _db.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(
                 student => student.Id == request.StudentId,
                 cancellationToken);

        if (student is null)
        {
            throw new EntityNotFoundException(
                $"Student {request.StudentId} does not exist.");
        }

        var course = await _db.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                course => course.Id == request.CourseId,
                cancellationToken);

        if (course is null)
        {
            throw new EntityNotFoundException(
                $"Course {request.CourseId} does not exist.");
        }

        var now = DateTime.UtcNow;

        var application = new AdmissionApplication
        {
            ApplicationNumber =
                $"APP-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",

            StudentId = request.StudentId,
            CourseId = request.CourseId,
            Status = ApplicationStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _db.Applications.Add(application);

            await _db.SaveChangesAsync(cancellationToken);

            var audit = new ApplicationAudit
            {
                ApplicationId = application.Id,
                EventType = "ApplicationCreated",
                Message = "Application submitted successfully.",
                CreatedAt = DateTime.UtcNow
            };

            _db.ApplicationAudits.Add(audit);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var externalRequest = new ExternalAdmissionRequest
            {
                SourceApplicationNumber = application.ApplicationNumber,
                StudentNumber = student.StudentNumber,
                CourseCode = course.Code
            };
            var externalResponse =
                await _externalAdmissionClient.CreateApplicationAsync(
                    externalRequest,
                    cancellationToken);

            application.ExternalApplicationId = externalResponse.ExternalApplicationId;            
            application.Status = ApplicationStatus.Processing;
            application.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ApplicationResponse
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            StudentId = application.StudentId,
            CourseId = application.CourseId,
            Status = application.Status.ToString(),
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            ExternalApplicationId = application.ExternalApplicationId
        };
    }
}
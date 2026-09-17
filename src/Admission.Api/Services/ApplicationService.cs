using Admission.Api.Data;
using Admission.Api.DTOs.Applications;
using Admission.Api.Models;
using Admission.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Admission.Api.DTOs.ExternalAdmission;
using Admission.Api.DTOs.Webhooks;

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
            IdempotencyKey = Guid.NewGuid().ToString(),
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
                    application.IdempotencyKey!,
                    request.SimulateExternalTimeout,
                    request.SimulateTransientFailures,
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

    public async Task<ApplicationResponse> RetryExternalSubmissionAsync(
    int applicationId,
    CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .Include(application => application.Student)
            .Include(application => application.Course)
            .SingleOrDefaultAsync(
                application => application.Id == applicationId,
                cancellationToken);

        if (application is null)
        {
            throw new EntityNotFoundException(
                $"Application {applicationId} does not exist.");
        }

        var externalRequest = new ExternalAdmissionRequest
        {
            SourceApplicationNumber = application.ApplicationNumber,
            StudentNumber = application.Student.StudentNumber,
            CourseCode = application.Course.Code
        };

        if (string.IsNullOrWhiteSpace(application.IdempotencyKey))
        {
            throw new InvalidOperationException(
                "This application predates idempotency support and cannot be safely retried using this lab workflow.");
        }

        var externalResponse =
            await _externalAdmissionClient.CreateApplicationAsync(
                externalRequest,
                application.IdempotencyKey!,
                simulateTimeout: false,
                simulateTransientFailures: 0,
                cancellationToken);

        application.ExternalApplicationId =
            externalResponse.ExternalApplicationId;

        application.Status = ApplicationStatus.Processing;
        application.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new ApplicationResponse
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            StudentId = application.StudentId,
            CourseId = application.CourseId,
            Status = application.Status.ToString(),
            ExternalApplicationId = application.ExternalApplicationId,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        };
    }


    public async Task<WebhookProcessingResult>
    ProcessAdmissionWebhookAsync(
        AdmissionWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var alreadyProcessed =
            await _db.AdmissionWebhookEvents
                .AsNoTracking()
                .AnyAsync(
                    webhook =>
                        webhook.EventId == request.EventId,
                    cancellationToken);

        if (alreadyProcessed)
        {
            return WebhookProcessingResult.AlreadyProcessed;
        }

        var application =
            await _db.Applications
                .SingleOrDefaultAsync(
                    application =>
                        application.ExternalApplicationId ==
                        request.ExternalApplicationId,
                    cancellationToken);

        if (application is null)
        {
            return WebhookProcessingResult.ApplicationNotFound;
        }

        if (!Enum.TryParse<ApplicationStatus>(
                request.Status,
                ignoreCase: true,
                out var newStatus))
        {
            throw new InvalidOperationException(
                $"Unsupported application status '{request.Status}'.");
        }

        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;

        var webhookEvent =
            new AdmissionWebhookEvent
            {
                EventId = request.EventId,
                EventType = request.EventType,
                ExternalApplicationId =
                    request.ExternalApplicationId,
                OccurredAt = request.OccurredAt,
                ProcessedAt = DateTime.UtcNow
            };

        _db.AdmissionWebhookEvents.Add(webhookEvent);

        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is Microsoft.Data.SqlClient.SqlException sqlException
                  &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627))
        {
            return WebhookProcessingResult.AlreadyProcessed;
        }

        return WebhookProcessingResult.Processed;
    }
}
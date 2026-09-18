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
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(AdmissionDbContext db, IExternalAdmissionClient externalAdmissionClient, ILogger<ApplicationService> logger)
    {
        _db = db;
        _externalAdmissionClient = externalAdmissionClient;
        _logger = logger;
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

    public async Task<ReconciliationResponse> ReconcileAsync(
    int applicationId,
    CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .SingleOrDefaultAsync(
                application => application.Id == applicationId,
                cancellationToken);

        if (application is null)
        {
            throw new EntityNotFoundException(
                $"Application {applicationId} does not exist.");
        }

        if (string.IsNullOrWhiteSpace(
                application.ExternalApplicationId))
        {
            throw new InvalidOperationException(
                $"Application {applicationId} does not have an external application ID.");
        }

        var localStatusBefore =
            application.Status.ToString();

        var externalApplication =
            await _externalAdmissionClient.GetApplicationAsync(
                application.ExternalApplicationId,
                cancellationToken);

        if (externalApplication is null)
        {
            throw new EntityNotFoundException(
                $"External application '{application.ExternalApplicationId}' does not exist.");
        }

        if (!Enum.TryParse<ApplicationStatus>(
                externalApplication.Status,
                ignoreCase: true,
                out var externalStatus))
        {
            throw new InvalidOperationException(
                $"Unsupported external status '{externalApplication.Status}'.");
        }

        var changed =
            application.Status != externalStatus;

        if (changed)
        {
            application.Status = externalStatus;
            application.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        return new ReconciliationResponse
        {
            ApplicationId = application.Id,
            ApplicationNumber =
                application.ApplicationNumber,

            ExternalApplicationId =
                application.ExternalApplicationId,

            LocalStatusBefore =
                localStatusBefore,

            ExternalStatus =
                externalApplication.Status,

            LocalStatusAfter =
                application.Status.ToString(),

            Changed =
                changed,

            Message =
                changed
                    ? "Local application status was reconciled with the external platform."
                    : "Local and external application statuses were already consistent."
        };
    }

    public async Task<int> ReconcileProcessingApplicationsAsync(
    CancellationToken cancellationToken)
    {
        var applicationIds = await _db.Applications
            .AsNoTracking()
            .Where(application =>
                application.Status == ApplicationStatus.Processing &&
                application.ExternalApplicationId != null)
            .Select(application => application.Id)
            .ToListAsync(cancellationToken);

        var changedCount = 0;

        foreach (var applicationId in applicationIds)
        {
            try
            {
                var result = await ReconcileAsync(
                    applicationId,
                    cancellationToken);

                if (result.Changed)
                {
                    changedCount++;
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Background reconciliation failed for application {ApplicationId}.",
                    applicationId);
            }
        }

        return changedCount;
    }

    public async Task<ApplicationResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _db.Applications
            .AsNoTracking()
            .Where(application => application.Id == id)
            .Select(application => new ApplicationResponse
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                StudentId = application.StudentId,
                CourseId = application.CourseId,
                Status = application.Status.ToString(),
                ExternalApplicationId = application.ExternalApplicationId,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ApplicationResponse>> GetAllAsync(
        string? status,
        CancellationToken cancellationToken)
    {
        var query = _db.Applications
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ApplicationStatus>(
                    status,
                    ignoreCase: true,
                    out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                throw new ArgumentException(
                    $"Unsupported application status '{status}'.",
                    nameof(status));
            }

            query = query.Where(
                application => application.Status == parsedStatus);
        }

        return await query
            .OrderBy(application => application.Id)
            .Select(application => new ApplicationResponse
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                StudentId = application.StudentId,
                CourseId = application.CourseId,
                Status = application.Status.ToString(),
                ExternalApplicationId = application.ExternalApplicationId,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
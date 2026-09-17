using System.Security.Cryptography;
using System.Text;
using ExternalAdmission.Api.Data;
using ExternalAdmission.Api.DTOs;
using ExternalAdmission.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace ExternalAdmission.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly ExternalAdmissionDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly ConcurrentDictionary<string, int> AttemptCounts = new();

    public ApplicationsController(ExternalAdmissionDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }


    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> Create(
    CreateExternalApplicationRequest request,
    [FromHeader(Name = "Idempotency-Key")]
    string? idempotencyKey,
    [FromQuery] bool simulateSlowResponse,
    [FromQuery] int failFirstAttempts,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Missing idempotency key",
                Detail = "An Idempotency-Key header is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }
        var requestHash = CreateRequestHash(request);
        var existing = await _db.Applications.AsNoTracking()
            .SingleOrDefaultAsync(
                app => app.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Idempotency key conflict",
                    Detail = "The same idempotency key was used with a different request.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return Ok(existing);
        }

        if (failFirstAttempts > 0)
        {
            var attempt = AttemptCounts.AddOrUpdate(
                idempotencyKey,
                1,
                (_, currentAttempt) => currentAttempt + 1);

            if (attempt <= failFirstAttempts)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new ProblemDetails
                    {
                        Title = "Simulated temporary outage",
                        Detail =
                            $"External platform simulated failure on attempt {attempt}.",
                        Status = StatusCodes.Status503ServiceUnavailable
                    });
            }
        }

        var application = new ExternalApplication
        {
            ExternalApplicationId = $"EXT-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            SourceApplicationNumber = request.SourceApplicationNumber,
            StudentNumber = request.StudentNumber,
            CourseCode = request.CourseCode,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            Status = "Received",
            ReceivedAt = DateTime.UtcNow
        };        

        _db.Applications.Add(application);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlException &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627))
        {
            // Another request may have inserted the same
            // idempotency key at almost exactly the same time.

            _db.Entry(application).State = EntityState.Detached;

            var winner = await _db.Applications
                .AsNoTracking()
                .SingleAsync(
                    x => x.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (winner.RequestHash != requestHash)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Idempotency key conflict",
                    Detail =
                        "This idempotency key was concurrently used with a different request.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return Ok(winner);
        }

        if (simulateSlowResponse)
        {
            // SIMULATION ONLY:
            // The DB insert has ALREADY succeeded,
            // but we'll delay the HTTP response.
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                CancellationToken.None);
        }

        return Created(
            $"/api/applications/{application.ExternalApplicationId}",
            application);
    }
    private static string CreateRequestHash(
    CreateExternalApplicationRequest request)
    {
        var canonical =
            $"{request.SourceApplicationNumber.Trim().ToUpperInvariant()}|" +
            $"{request.StudentNumber.Trim().ToUpperInvariant()}|" +
            $"{request.CourseCode.Trim().ToUpperInvariant()}";

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(bytes);
    }

    [HttpPost("{externalApplicationId}/simulate-accepted")]
    public async Task<IActionResult> SimulateAccepted(
    string externalApplicationId,
    [FromQuery] string? eventId,
    CancellationToken cancellationToken)
    {
        var application =
            await _db.Applications
                .SingleOrDefaultAsync(
                    application =>
                        application.ExternalApplicationId ==
                        externalApplicationId,
                    cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        application.Status = "Accepted";

        await _db.SaveChangesAsync(
            cancellationToken);

        var webhook = new
        {
            // eventId =
            //     $"EVT-{Guid.NewGuid()
            //         .ToString("N")[..12]
            //         .ToUpperInvariant()}",
            eventId = string.IsNullOrWhiteSpace(eventId) ? $"EVT-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}" : eventId,

            eventType = "admission.status.changed",

            externalApplicationId = application.ExternalApplicationId,

            sourceApplicationNumber = application.SourceApplicationNumber,

            status = application.Status,

            occurredAt = DateTime.UtcNow
        };

        var client =
            _httpClientFactory.CreateClient(
                "AdmissionApi");

        var response =
            await client.PostAsJsonAsync(
                "api/webhooks/admission",
                webhook,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return Ok(new
        {
            application.ExternalApplicationId,
            application.Status,
            webhookDelivered = true
        });
    }

    [HttpGet("{externalApplicationId}")]
    public async Task<IActionResult> GetByExternalId(
    string externalApplicationId,
    CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                application =>
                    application.ExternalApplicationId ==
                    externalApplicationId,
                cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(application);
    }

    [HttpPost("{externalApplicationId}/simulate-accepted-no-webhook")]
    public async Task<IActionResult> SimulateAcceptedWithoutWebhook(
    string externalApplicationId,
    CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .SingleOrDefaultAsync(
                application =>
                    application.ExternalApplicationId ==
                    externalApplicationId,
                cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        application.Status = "Accepted";

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            application.ExternalApplicationId,
            application.Status,
            webhookDelivered = false
        });
    }
}
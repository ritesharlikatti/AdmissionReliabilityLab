using Admission.Api.Data;
using Admission.Api.Models;
using Admission.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Admission.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient<
        IExternalAdmissionClient,
        ExternalAdmissionClient>(
            client =>
            {
                client.BaseAddress = new Uri(
                    builder.Configuration["ExternalAdmission:BaseUrl"]!);
            })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = false;
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
    });    

builder.Services.AddDbContext<AdmissionDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddHostedService<ReconciliationBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        application = "Admission Reliability Lab",
        status = "Running"
    });
});

app.MapControllers();
app.Run();
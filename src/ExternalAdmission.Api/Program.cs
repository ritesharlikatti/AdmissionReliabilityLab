using ExternalAdmission.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExternalAdmissionDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString("DefaultConnection")));

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

app.MapControllers();

app.Run();
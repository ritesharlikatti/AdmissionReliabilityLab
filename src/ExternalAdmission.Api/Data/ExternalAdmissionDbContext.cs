using ExternalAdmission.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExternalAdmission.Api.Data;

public class ExternalAdmissionDbContext : DbContext
{
    public ExternalAdmissionDbContext(
        DbContextOptions<ExternalAdmissionDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExternalApplication> Applications { get; set; }

    protected override void OnModelCreating(
    ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExternalApplication>()
            .HasIndex(application => application.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}
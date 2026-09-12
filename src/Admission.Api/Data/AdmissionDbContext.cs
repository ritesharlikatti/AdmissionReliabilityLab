using Admission.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Admission.Api.Data;

public class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(
        DbContextOptions<AdmissionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }

    public DbSet<Course> Courses { get; set; }

    public DbSet<AdmissionApplication> Applications { get; set; }

    public DbSet<ApplicationAudit> ApplicationAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>()
            .HasIndex(student => student.StudentNumber)
            .IsUnique();

        modelBuilder.Entity<Course>()
            .HasIndex(course => course.Code)
            .IsUnique();

        modelBuilder.Entity<Course>()
            .Property(course => course.Fee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<AdmissionApplication>()
            .HasIndex(application => application.ApplicationNumber)
            .IsUnique();

        modelBuilder.Entity<AdmissionApplication>()
            .Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<AdmissionApplication>()
            .HasOne(application => application.Student)
            .WithMany(student => student.Applications)
            .HasForeignKey(application => application.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionApplication>()
            .HasOne(application => application.Course)
            .WithMany(course => course.Applications)
            .HasForeignKey(application => application.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationAudit>()
            .HasOne<AdmissionApplication>()
            .WithMany()
            .HasForeignKey(audit => audit.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionApplication>()
            .HasIndex(application => application.IdempotencyKey)
            .IsUnique();
            
    }    
}
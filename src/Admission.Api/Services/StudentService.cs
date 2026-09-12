using Admission.Api.Data;
using Admission.Api.DTOs.Students;
using Admission.Api.Models;
using Microsoft.EntityFrameworkCore;
using Admission.Api.Exceptions;
using Microsoft.Data.SqlClient;

namespace Admission.Api.Services;

public class StudentService : IStudentService
{
    private readonly AdmissionDbContext _db;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AdmissionDbContext db, ILogger<StudentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<StudentResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Students
            .AsNoTracking()
            .OrderBy(student => student.Id)
            .Select(student => new StudentResponse
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                StudentNumber = student.StudentNumber,
                CreatedAt = student.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _db.Students
            .AsNoTracking()
            .Where(student => student.Id == id)
            .Select(student => new StudentResponse
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                StudentNumber = student.StudentNumber,
                CreatedAt = student.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StudentResponse> CreateAsync(
    CreateStudentRequest request,
    CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking whether student number {StudentNumber} exists",
            request.StudentNumber);

        var alreadyExists = await _db.Students
            .AnyAsync(
                student =>
                    student.StudentNumber == request.StudentNumber,
                cancellationToken);

        if (alreadyExists)
        {
            _logger.LogWarning(
                "Student number {StudentNumber} was already found during pre-check",
                request.StudentNumber);

            throw new DuplicateStudentNumberException(
                request.StudentNumber);
        }

        _logger.LogInformation(
            "{StudentNumber} passed the application-level duplicate check",
            request.StudentNumber);

        var student = new Student
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            StudentNumber = request.StudentNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.Students.Add(student);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{StudentNumber} successfully inserted with ID {StudentId}",
                student.StudentNumber,
                student.Id);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlException &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627))
        {
            _logger.LogWarning(
                "Database unique constraint rejected duplicate student number {StudentNumber}",
                request.StudentNumber);

            throw new DuplicateStudentNumberException(
                request.StudentNumber);
        }

        return new StudentResponse
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            StudentNumber = student.StudentNumber,
            CreatedAt = student.CreatedAt
        };
    }

}

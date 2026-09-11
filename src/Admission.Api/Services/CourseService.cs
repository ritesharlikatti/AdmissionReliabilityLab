using Admission.Api.Data;
using Admission.Api.DTOs.Courses;
using Admission.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Admission.Api.Services;

public class CourseService : ICourseService
{
    private readonly AdmissionDbContext _db;

    public CourseService(AdmissionDbContext db)
    {
        _db = db;
    }

    public async Task<List<CourseResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .OrderBy(course => course.Id)
            .Select(course => new CourseResponse
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Fee = course.Fee,
                CreatedAt = course.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseResponse> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Code = request.Code,
            Name = request.Name,
            Fee = request.Fee,
            CreatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);

        await _db.SaveChangesAsync(cancellationToken);

        return new CourseResponse
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Fee = course.Fee,
            CreatedAt = course.CreatedAt
        };
    }
}
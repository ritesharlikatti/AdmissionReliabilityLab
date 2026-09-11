using Admission.Api.DTOs.Courses;

namespace Admission.Api.Services;

public interface ICourseService
{
    Task<List<CourseResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<CourseResponse> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken);
}
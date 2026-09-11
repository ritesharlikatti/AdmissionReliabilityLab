using Admission.Api.DTOs.Courses;
using Admission.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var courses =
            await _courseService.GetAllAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpPost]
    public async Task<ActionResult<CourseResponse>> Create(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course =
            await _courseService.CreateAsync(
                request,
                cancellationToken);

        return Created(
            $"/api/courses/{course.Id}",
            course);
    }
}
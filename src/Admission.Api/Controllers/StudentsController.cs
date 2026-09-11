using Admission.Api.DTOs.Students;
using Admission.Api.Services;
using Admission.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var students =
            await _studentService.GetAllAsync(cancellationToken);

        return Ok(students);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var student =
            await _studentService.GetByIdAsync(
                id,
                cancellationToken);

        if (student is null)
        {
            return NotFound();
        }

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<StudentResponse>> Create(
    CreateStudentRequest request,
    CancellationToken cancellationToken)
    {
    try
      {
        var student =
            await _studentService.CreateAsync(
                request,
                cancellationToken);

        return Created(
            $"/api/students/{student.Id}",
            student);
      }
    catch (DuplicateStudentNumberException ex)
      {
        return Conflict(new ProblemDetails
        {
            Title = "Student number already exists",
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict
        });
      }
    }
}
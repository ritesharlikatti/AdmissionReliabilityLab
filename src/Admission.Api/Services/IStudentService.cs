using Admission.Api.DTOs.Students;

namespace Admission.Api.Services;

public interface IStudentService
{
    Task<List<StudentResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<StudentResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<StudentResponse> CreateAsync(
        CreateStudentRequest request,
        CancellationToken cancellationToken);
}
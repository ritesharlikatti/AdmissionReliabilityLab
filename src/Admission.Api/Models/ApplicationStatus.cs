namespace Admission.Api.Models;

public enum ApplicationStatus
{
    Draft,
    Submitted,
    Processing,
    Accepted,
    Rejected,
    Failed,
    Cancelled
}
namespace Admission.Api.Exceptions;

public class DuplicateStudentNumberException : Exception
{
    public DuplicateStudentNumberException(string studentNumber)
        : base($"A student with student number '{studentNumber}' already exists.")
    {
    }
}
using System;

namespace CheckYourEligibility.Domain;
    
public class UseExecutionResult<T>
{
    public bool IsServiceUnavailable { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationErrors { get; set; }
    public T? Response { get; set; }

    public UseExecutionResult()
    {
        IsServiceUnavailable = false;
        IsNotFound = false;
        IsValid = false;
        ValidationErrors = string.Empty;
        Response = default;
    }
    
    public void SetSuccess(T response)
    {
        IsValid = true;
        Response = response;
        ValidationErrors = string.Empty;
    }
    
    public void SetFailure(string validationErrors)
    {
        IsValid = false;
        ValidationErrors = validationErrors;
        Response = default;
    }

    public void SetNotFound(string guid)
    {
        IsNotFound = true;
        ValidationErrors = $"Bulk upload with ID {guid} not found";
        Response = default;
    }

    public void SetServiceUnavailable()
    {
        IsServiceUnavailable = true;
        ValidationErrors = "Service is unavailable";
        Response = default;
    }
}

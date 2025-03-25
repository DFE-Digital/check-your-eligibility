namespace CheckYourEligibility.API.Boundary.Responses;

public class ErrorResponse
{
    public List<Error> Errors { get; set; }
}

public class Error
{
    public string Status { get; set; }
    public string Title { get; set; }
    public string? Detail { get; set; }
}
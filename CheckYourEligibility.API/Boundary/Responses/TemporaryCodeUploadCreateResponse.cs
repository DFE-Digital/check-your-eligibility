namespace CheckYourEligibility.API.Boundary.Responses
{
	public class TemporaryCodeUploadCreateResponse
	{
		public int NumberOfCodes { get; set; }
		public List<string> Errors { get; set; } = new List<string>();
	}
}

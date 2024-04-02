namespace CheckYourEligibility.SystemTests.Utilities.Models
{
    public class FreeSchoolMealsResponseModel
    {
        public DataModel Data { get; set; }
        public LinksModel Links { get; set; }
    }
    public class DataModel
    {
        public string Status { get; set; }
    }

    public class LinksModel
    {
        public string Get_EligibilityCheck { get; set; }
        public string Put_EligibilityCheckProcess { get; set; }
    }


}

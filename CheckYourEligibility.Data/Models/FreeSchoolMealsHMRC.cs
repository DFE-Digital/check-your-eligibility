

namespace CheckYourEligibility.Data.Models
{
    public class FreeSchoolMealsHMRC
    {
        /// <summary>
        /// NINO
        /// </summary>
        public string FreeSchoolMealsHMRCID { get; set; }
        
        public int DataType { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Surname { get; set; }
       
    }
}

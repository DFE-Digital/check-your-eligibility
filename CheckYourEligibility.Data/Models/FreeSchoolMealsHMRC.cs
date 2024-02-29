

using System.ComponentModel.DataAnnotations.Schema;

namespace CheckYourEligibility.Data.Models
{
    public class FreeSchoolMealsHMRC
    {
        /// <summary>
        /// NINO
        /// </summary>
        [Column(TypeName = "varchar(50)")]
        public string FreeSchoolMealsHMRCID { get; set; }
        
        public int DataType { get; set; }

        public DateTime DateOfBirth { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Surname { get; set; }
       
    }
}

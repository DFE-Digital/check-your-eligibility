

using System.ComponentModel.DataAnnotations.Schema;

namespace CheckYourEligibility.Data.Models
{
    public class FreeSchoolMealsHO
    {
        /// <summary>
        /// NASS
        /// </summary>
        [Column(TypeName = "varchar(100)")]
        public string FreeSchoolMealsHOID { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string NASS { get; set; }

        public DateTime DateOfBirth { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; }

    }

}

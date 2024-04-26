using CheckYourEligibility.Data.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.SystemTests.Utilities.Models.Responses
{
    public class Application
    {
        public string id { get; set; }
        public string reference { get; set; }
        public int localAuthority { get; set; }
        public int school { get; set; }
        public string parentFirstName { get; set; }
        public string parentLastName { get; set; }
        public string parentNationalInsuranceNumber { get; set; }
        public object parentNationalAsylumSeekerServiceNumber { get; set; }
        public string parentDateOfBirth { get; set; }
        public string childFirstName { get; set; }
        public string childLastName { get; set; }
        public string childDateOfBirth { get; set; }
    }



    public class Links
    {
        public string get_Application { get; set; }
    }

    public class ApplicationFreeSchoolMeals
    {
        public Application data { get; set; }
        public Links links { get; set; }
    }
}

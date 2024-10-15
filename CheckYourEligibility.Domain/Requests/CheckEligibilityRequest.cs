using CheckYourEligibility.Domain.Enums;
using Newtonsoft.Json.Converters;

namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityRequest_Fsm
    {
        public CheckEligibilityRequestData_Fsm? Data { get; set; }
    }
    public class CheckEligibilityRequestBulk_Fsm
    {
        public IEnumerable<CheckEligibilityRequestData_Fsm> Data { get; set; }
    }

    public class CheckEligibilityRequestDataBase
    {
        protected CheckEligibilityType baseType;
        public CheckEligibilityType Type { get { return baseType; }  }
        public int? Sequence { get; set; }
    }

    public class CheckEligibilityRequestData_Fsm : CheckEligibilityRequestDataBase
    {
        public CheckEligibilityRequestData_Fsm()
        {
            baseType = CheckEligibilityType.FreeSchoolMeals;
        }

        public string? NationalInsuranceNumber { get; set; }

        public string LastName { get; set; }

        public string DateOfBirth { get; set; }

        public string? NationalAsylumSeekerServiceNumber { get; set; }
    }




}

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CheckYourEligibility.API.Domain.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum UserType 
{
    NotSpecified,
    FreeSchoolMealsAdmin,
    FreeSchoolMealsParent,
    ChildcareAdmin,
    EligibilityCheckingEngineSupport,
    API
}
 
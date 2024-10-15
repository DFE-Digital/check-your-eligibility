// Ignore Spelling: Fsm

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace CheckYourEligibility.Domain.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CheckEligibilityType
    {
        None = 0,
        [Description("Free School Meals")]
        FreeSchoolMeals,
    }
}

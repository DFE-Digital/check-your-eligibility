using CsvHelper.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Services.CsvImport
{
    [ExcludeFromCodeCoverage]
    public class HomeOfficeRow
    {
        public string Nas { get; set; }
        public string Dob { get; set; }
        public string Surname { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class HomeOfficeRowMap : ClassMap<HomeOfficeRow>
    {
        public HomeOfficeRowMap()
        {
            Map(m => m.Nas).Index(0);
            Map(m => m.Dob).Index(1);
            Map(m => m.Surname).Index(2);
        }
    }
}

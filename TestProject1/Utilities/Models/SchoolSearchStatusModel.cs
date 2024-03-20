using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.SystemTests.Utilities.Models
{

    public class SchoolSearchStatusModel
    {
        public List<SchoolSearchStatusDataModel> Data { get; set; }
    }
}
public class SchoolSearchStatusDataModel
{
    public int id { get; set; }
    public string name { get; set; }
    public string postcode { get; set; }
    public string street { get; set; }
    public string locality { get; set; }
    public string town { get; set; }
    public string county { get; set; }
    public string la { get; set; }
    public double distance { get; set; }
}



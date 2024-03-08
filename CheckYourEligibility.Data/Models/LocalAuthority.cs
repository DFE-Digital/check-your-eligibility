using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.Data.Models
{

    public class LocalAuthority
    {
        [Key]
        public int LaCode { get; set; }
        public string LaName { get; set; }
    }
}

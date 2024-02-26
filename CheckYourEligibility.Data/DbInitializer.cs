using CheckYourEligibility.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.Data
{
    public static class DbInitializer
    {
        public static void Initialize(EligibilityCheckContext context)
        {
            context.Database.EnsureCreated();

            // Look for any FreeSchoolMealsHMRC.
            if (!context.FreeSchoolMealsHMRC.Any())
            {

                var fsmHmrc = new FreeSchoolMealsHMRC[]
                {
            new FreeSchoolMealsHMRC{FreeSchoolMealsHMRCID="AB123456C",Surname="SIMPSON",DateOfBirth=DateTime.Parse("01-01-1990"),DataType = 1},
            new FreeSchoolMealsHMRC{FreeSchoolMealsHMRCID="AC123456D",Surname="GRIFFIN",DateOfBirth=DateTime.Parse("31-12-2000"),DataType = 1},
                };
                foreach (FreeSchoolMealsHMRC s in fsmHmrc)
                {
                    context.FreeSchoolMealsHMRC.Add(s);
                }
            }

            // Look for any FreeSchoolMealsHO.
            if (!context.FreeSchoolMealsHO.Any())
            {
                var fsmHo = new FreeSchoolMealsHO[]
            {
            new FreeSchoolMealsHO{FreeSchoolMealsHOID=Guid.NewGuid().ToString(),LastName="SIMPSON",DateOfBirth=DateTime.Parse("01-01-1990"),NASS = "AB123456C"},
            new FreeSchoolMealsHO{FreeSchoolMealsHOID=Guid.NewGuid().ToString(),LastName="GRIFFIN",DateOfBirth=DateTime.Parse("31-12-2000"),NASS = "AC123456D"},
            };
                foreach (FreeSchoolMealsHO s in fsmHo)
                {
                    context.FreeSchoolMealsHO.Add(s);
                }

                context.SaveChanges();
            }
        }
    }
}

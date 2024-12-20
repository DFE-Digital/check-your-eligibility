﻿using CsvHelper.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Services.CsvImport
{
    [ExcludeFromCodeCoverage]
    public class EstablishmentRow
    {
        public int Urn { get; set; }
        public int LaCode { get; set; }
        public string LaName { get; set; }
        public string EstablishmentName { get; set; }
        public string Postcode { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class EstablishmentRowMap : ClassMap<EstablishmentRow>
    {
        public EstablishmentRowMap()
        {
            Map(m => m.Urn).Index(0);
            Map(m => m.LaCode).Index(1);
            Map(m => m.LaName).Index(2);
            Map(m => m.EstablishmentName).Index(4);
            Map(m => m.Postcode).Index(64);
            Map(m => m.Street).Index(59);
            Map(m => m.Locality).Index(60);
            Map(m => m.Town).Index(62);
            Map(m => m.County).Index(63);
            Map(m => m.Status).Index(10);
            Map(m => m.Type).Index(6);
        }
    }
}

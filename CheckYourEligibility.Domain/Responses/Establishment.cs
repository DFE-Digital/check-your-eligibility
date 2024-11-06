﻿namespace CheckYourEligibility.Domain.Responses
{
    public class Establishment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Postcode { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string La { get; set; }
        public double? Distance { get; set; }
    }
}

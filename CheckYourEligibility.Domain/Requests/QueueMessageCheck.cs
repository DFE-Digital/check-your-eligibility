﻿namespace CheckYourEligibility.Domain.Requests
{
    public class QueueMessageCheck
    {
        public string Type { get; set; }
        public string Guid { get; set; }

        public string ProcessUrl { get; set; }
        public string SetStatusUrl { get; set; }
    }
}

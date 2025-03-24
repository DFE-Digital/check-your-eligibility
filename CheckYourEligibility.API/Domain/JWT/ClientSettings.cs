using System;

namespace CheckYourEligibility.API.Domain;

public class ClientSettings
{
    public string Secret { get; set; }
    public string Scope { get; set; }
}

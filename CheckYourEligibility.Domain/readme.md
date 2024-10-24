Domain model for consumption by ECE consumers

**[Api Documentation](https://github.com/DFE-Digital/check-your-eligibility-documentation/blob/main/Runbook/System/API/Operations/Readme.md)**

Copy or regenerate api key from 
   https://www.nuget.org/account/apikeys
if generating a key then the Key name is the name of the DLL ie **CheckYourEligibility.Domain**



**Update the version**
in Project properties, increment the Version
Project>Properties>Package General

**Publish**
run a developer cmd shell,
ie View\terminal 
type `cd CheckYourEligibility.Domain\bin\Debug`
this will get you too the package directory


`dotnet nuget push CheckYourEligibility.Domain.**VersionNumnber**.nupkg --api-key **YourApiKey** --source https://api.nuget.org/v3/index.json`

Note the name of the **nupkg** file is the one in the 
	CheckYourEligibility.Domain\bin\Debug directory

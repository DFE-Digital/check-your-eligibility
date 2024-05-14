# check-your-eligibility

help https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=vs

Set folders for migration files
http://stackoverflow.com/questions/8546257/is-it-possible-to-change-the-location-of-the-ef-migrations-migrations-folder

Add-Migration BaseMigration -project CheckYourEligibility.Data.Migrations
Add-Migration establishmentImport -project CheckYourEligibility.Data.Migrations
Add-Migration idxReference -project CheckYourEligibility.Data.Migrations
Add-Migration applicationStatus -project CheckYourEligibility.Data.Migrations
Add-Migration checkHash -project CheckYourEligibility.Data.Migrations
Add-Migration checkHashSource -project CheckYourEligibility.Data.Migrations
Add-Migration checkHashResult -project CheckYourEligibility.Data.Migrations
Add-Migration UserCreate -project CheckYourEligibility.Data.Migrations

--Run Latest migration
update-database -project CheckYourEligibility.Data.Migrations

--List Migrations
Get-Migration

Remove-Migration -Force -project CheckYourEligibility.Data.Migrations

--Run specific migration
update-database -migration BaseMigration -project CheckYourEligibility.Data.Migrations


--MoqDWP
DWP checking:-
firstly the citizen is checked using the supplied details, if found Status200OK then the GUID is returned and a check is made to see if the citizen is eligible or not.
if an error occurs then this is logged and the eligibility check record is set to queuedForProcessing.  Status404NotFound is returned when the resource is not found.
the following are details used to force results.

public static class MogDWPValues
    {
        public static string validUniversalBenefitType = "universal_credit";
        public static string validCitizenEligibleGuid = "58ccfe37-7e43-4682-a412-19ec663ca556";
        public static string validCitizenSurnameEligible = "DWPmoqEligible";
        public static string validCitizenNotEligibleGuid = "48ccfe37-7e43-4682-a412-19ec663ca556";
        public static string validCitizenSurnameNotEligible = "DWPmoqNotEligible";
        public static string validCitizenDob = "1990-01-01";
        public static string validCitizenNino = "AB123456C";
    }

Using a valid check will enforce a valid result changing the surname to 'DWPmoqNotEligible' follows a different path

curl --location 'https://localhost:7117/FreeSchoolMeals' \
--header 'accept: text/plain' \
--header 'Content-Type: application/json' \
--data '{
  "data": {
    "nationalInsuranceNumber": "AB123456C",
    "lastName": "DWPmoqEligible",
    "dateOfBirth": "01/01/1990",
    "nationalAsylumSeekerServiceNumber": null
  }
}'

the moq dwp endpoints are as follows, note the headers.
curl --location 'https://localhost:7117/MoqDWP/v2/citizens' \
--header 'accept: text/plain' \
--header 'instigating-user-id: abcdef1234577890abcdeffghi' \
--header 'policy-id: fsm' \
--header 'correlation-id: 4c6a63f1-1924-4911-b45c-95dbad8b6c37' \
--header 'context: abc-1-ab-x128881' \
--header 'Content-Type: application/json-patch+json' \
--data '{
  "jsonapi": {
    "version": "2.0"
  },
  "data": {
    "type": "Match",
    "attributes": {
      "dateOfBirth": "1990-01-01",
      "ninoFragment": "AB123456C",
      "lastName": "DWPmoqEligible"
    }
  }
}'

curl --location --request GET 'https://localhost:7117/MoqDWP/v2/citizens/58ccfe37-7e43-4682-a412-19ec663ca556/claims?benefitType=universal_credit%27' \
--header 'accept: text/plain' \
--header 'instigating-user-id: abcdef1234577890abcdeffghi' \
--header 'access-level: 1' \
--header 'correlation-id: 4c6a63f1-1924-4911-b45c-95dbad8b6c37' \
--header 'context: abc-1-ab-x128881' \
--header 'Content-Type: application/json-patch+json' \
--data '{
  "jsonapi": {
    "version": "2.0"
  },
  "data": {
    "type": "Match",
    "attributes": {
      "dateOfBirth": "01/01/1990",
      "ninoFragment": "AB123456C",
      "lastName": "DWPmoq"
    }
  }
}'
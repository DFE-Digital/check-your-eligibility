# check-your-eligibility

help https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=vs

Set folders for migration files
http://stackoverflow.com/questions/8546257/is-it-possible-to-change-the-location-of-the-ef-migrations-migrations-folder

Add-Migration BaseMigration -project CheckYourEligibility.Data.Migrations
Add-Migration establishmentImport -project CheckYourEligibility.Data.Migrations
Add-Migration idxReference -project CheckYourEligibility.Data.Migrations


--List Migrations
Get-Migration

Remove-Migration -Force -project CheckYourEligibility.Data.Migrations

--Run specific migration
update-database -migration BaseMigration -project CheckYourEligibility.Data.Migrations

--Run Latest migration
update-database -project CheckYourEligibility.Data.Migrations

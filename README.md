# check-your-eligibility

help https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=vs

Set folders for migration files
http://stackoverflow.com/questions/8546257/is-it-possible-to-change-the-location-of-the-ef-migrations-migrations-folder

Add-Migration BaseMigration -project CheckYourEligibility.Data.Migrations
Add-Migration UpdatedModel -project CheckYourEligibility.Data.Migrations
Add-Migration FsmHMRC -project CheckYourEligibility.Data.Migrations

update-database -migration UpdatedModel
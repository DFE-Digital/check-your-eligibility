# check-your-eligibility

help http://www.entityframeworktutorial.net/efcore/pmc-commands-for-ef-core-migration.aspx

Set folders for migration files
http://stackoverflow.com/questions/8546257/is-it-possible-to-change-the-location-of-the-ef-migrations-migrations-folder


eligibility db
Enable-Migrations -ProjectName Inframon.Domain.Context -ContextTypeName ContextScaffolding -MigrationsDirectory ScaffoldMigrations
Enable-Migrations -ContextTypeName  ItsSorted.Api.EASOnline.DAL.ApplicationEasOnlineDbContext -EnableAutomaticMigrations
Add-Migration -ProjectName ItsSorted.Api.EASOnline -ConfigurationTypeName ItsSorted.Api.EASOnline.DAL.Migrations.EasOnline.Configuration "InitialDatabaseCreation" -force
Add-Migration -ConfigurationTypeName ItsSorted.Api.EASOnline.DAL.Migrations.EasOnline.Configuration  -force

update-Database -ConfigurationTypeName ItsSorted.Api.EASOnline.DAL.Migrations.EasOnline.Configuration
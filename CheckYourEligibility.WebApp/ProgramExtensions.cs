﻿using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using CheckYourEligibility.WebApp.Extensions;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain;

namespace CheckYourEligibility.WebApp
{
    [ExcludeFromCodeCoverage(Justification = "extension of program")]
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("ConnectionString");

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   connectionString,
                   x => x.MigrationsAssembly("CheckYourEligibility.Data.Migrations"))
               // **** note adding this back in will have undesired effects on updates 
               //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
               );
            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }

        public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("QueueConnectionString");
            services.AddAzureClients(builder =>
            {
                builder.AddQueueServiceClient(connectionString);

            });
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddTransient<ICheckEligibility, CheckEligibilityService>();
            services.AddTransient<IApplication, ApplicationService>();
            services.AddTransient<IAdministration, AdministrationService>();
            services.AddTransient<IEstablishmentSearch, EstablishmentSearchService>();
            services.AddTransient<IUsers, UsersService>();
            services.AddTransient<IAudit, AuditService>();
            services.AddTransient<IHash, HashService>();
            return services;
        }

        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IDwpService, DwpService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Dwp:BaseUrl"]);
            });
            return services;
        }

        public static IServiceCollection AddJwtSettings(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = new JwtSettings();
            configuration.GetSection("Jwt").Bind(jwtSettings);
            services.AddSingleton(jwtSettings);
            return services;
        }

        public static IServiceCollection AddAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Issuer"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnForbidden = context =>
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            }
                        };
                    });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.RequireLocalAuthorityScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScopeWithColon(configuration["Jwt:Scopes:local_authority"] ?? "local_authority")));

                options.AddPolicy(PolicyNames.RequireCheckScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:check"] ?? "check")));

                options.AddPolicy(PolicyNames.RequireApplicationScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:application"] ?? "application")));

                options.AddPolicy(PolicyNames.RequireAdminScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:admin"] ?? "admin")));

                /* new policies for "bulk_check","establishment","user" and "engine" */
                options.AddPolicy(PolicyNames.RequireBulkCheckScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:bulk_check"] ?? "bulk_check")));

                options.AddPolicy(PolicyNames.RequireEstablishmentScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:establishment"] ?? "establishment")));

                options.AddPolicy(PolicyNames.RequireUserScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:user"] ?? "user")));

                options.AddPolicy(PolicyNames.RequireEngineScope, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasScope(configuration["Jwt:Scopes:engine"] ?? "engine")));

            });
            return services;
        }
    }
}

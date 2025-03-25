﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CheckYourEligibility.API.Migrations
{
    [DbContext(typeof(EligibilityCheckContext))]
    [Migration("20240229142252_BaseMigration")]
    partial class BaseMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CheckYourEligibility.API.Domain.EligibilityCheck", b =>
                {
                    b.Property<string>("EligibilityCheckID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("NASSNumber")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("NINumber")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("datetime2");

                    b.HasKey("EligibilityCheckID");

                    b.ToTable("EligibilityCheck", (string)null);
                });

            modelBuilder.Entity("CheckYourEligibility.API.Domain.FreeSchoolMealsHMRC", b =>
                {
                    b.Property<string>("FreeSchoolMealsHMRCID")
                        .HasColumnType("varchar(50)");

                    b.Property<int>("DataType")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("Surname")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.HasKey("FreeSchoolMealsHMRCID");

                    b.ToTable("FreeSchoolMealsHMRC");
                });

            modelBuilder.Entity("CheckYourEligibility.API.Domain.FreeSchoolMealsHO", b =>
                {
                    b.Property<string>("FreeSchoolMealsHOID")
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("NASS")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.HasKey("FreeSchoolMealsHOID");

                    b.ToTable("FreeSchoolMealsHO");
                });
#pragma warning restore 612, 618
        }
    }
}

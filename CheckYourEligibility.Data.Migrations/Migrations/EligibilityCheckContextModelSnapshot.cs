﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    [DbContext(typeof(EligibilityCheckContext))]
    partial class EligibilityCheckContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CheckYourEligibility.Data.Models.FreeSchoolMealsHMRC", b =>
                {
                    b.Property<string>("FreeSchoolMealsHMRCID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("DataType")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("Surname")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("FreeSchoolMealsHMRCID");

                    b.ToTable("FreeSchoolMealsHMRC");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.FsmCheckEligibility", b =>
                {
                    b.Property<string>("FsmCheckEligibilityID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NASSNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NINumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.HasKey("FsmCheckEligibilityID");

                    b.ToTable("FsmCheckEligibility", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}

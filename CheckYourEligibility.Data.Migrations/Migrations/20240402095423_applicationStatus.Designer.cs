﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    [DbContext(typeof(EligibilityCheckContext))]
    [Migration("20240402095423_applicationStatus")]
    partial class applicationStatus
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Application", b =>
                {
                    b.Property<string>("ApplicationID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("ChildDateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("ChildFirstName")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ChildLastName")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<int>("LocalAuthorityId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ParentDateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("ParentFirstName")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("ParentLastName")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("ParentNationalAsylumSeekerServiceNumber")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ParentNationalInsuranceNumber")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasColumnType("varchar(8)");

                    b.Property<int>("SchoolId")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("datetime2");

                    b.HasKey("ApplicationID");

                    b.HasIndex("SchoolId");

                    b.HasIndex(new[] { "Status" }, "idx_ApplicationStatus");

                    b.HasIndex(new[] { "Reference" }, "idx_Reference")
                        .IsUnique();

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.ApplicationStatus", b =>
                {
                    b.Property<string>("ApplicationStatusID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ApplicationID")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.HasKey("ApplicationStatusID");

                    b.HasIndex("ApplicationID");

                    b.ToTable("ApplicationStatuses");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.EligibilityCheck", b =>
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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.FreeSchoolMealsHMRC", b =>
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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.FreeSchoolMealsHO", b =>
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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.LocalAuthority", b =>
                {
                    b.Property<int>("LocalAuthorityId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LocalAuthorityId"));

                    b.Property<string>("LaName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("LocalAuthorityId");

                    b.ToTable("LocalAuthorities");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.School", b =>
                {
                    b.Property<int>("SchoolId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("SchoolId"));

                    b.Property<string>("County")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EstablishmentName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LocalAuthorityId")
                        .HasColumnType("int");

                    b.Property<string>("Locality")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Postcode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("StatusOpen")
                        .HasColumnType("bit");

                    b.Property<string>("Street")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Town")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SchoolId");

                    b.HasIndex("LocalAuthorityId");

                    b.ToTable("Schools");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Application", b =>
                {
                    b.HasOne("CheckYourEligibility.Data.Models.School", "School")
                        .WithMany()
                        .HasForeignKey("SchoolId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("School");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.ApplicationStatus", b =>
                {
                    b.HasOne("CheckYourEligibility.Data.Models.Application", "Application")
                        .WithMany("Statuses")
                        .HasForeignKey("ApplicationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Application");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.School", b =>
                {
                    b.HasOne("CheckYourEligibility.Data.Models.LocalAuthority", "LocalAuthority")
                        .WithMany()
                        .HasForeignKey("LocalAuthorityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LocalAuthority");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Application", b =>
                {
                    b.Navigation("Statuses");
                });
#pragma warning restore 612, 618
        }
    }
}

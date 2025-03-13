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
                .HasAnnotation("ProductVersion", "8.0.6")
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

                    b.Property<string>("EligibilityCheckHashID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("EstablishmentId")
                        .HasColumnType("int");

                    b.Property<int>("LocalAuthorityId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ParentDateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("ParentEmail")
                        .IsRequired()
                        .HasColumnType("varchar(1000)");

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

                    b.Property<string>("Status")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("ApplicationID");

                    b.HasIndex("EligibilityCheckHashID");

                    b.HasIndex("EstablishmentId");

                    b.HasIndex("UserId");

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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Audit", b =>
                {
                    b.Property<string>("AuditID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("authentication")
                        .IsRequired()
                        .HasColumnType("varchar(5000)");

                    b.Property<string>("method")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<string>("scope")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("source")
                        .IsRequired()
                        .HasColumnType("varchar(500)");

                    b.Property<string>("typeId")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<string>("url")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.HasKey("AuditID");

                    b.ToTable("Audits");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.EligibilityCheck", b =>
                {
                    b.Property<string>("EligibilityCheckID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CheckData")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<string>("EligibilityCheckHashID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Group")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("Sequence")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("datetime2");

                    b.HasKey("EligibilityCheckID");

                    b.HasIndex("EligibilityCheckHashID");

                    b.ToTable("EligibilityCheck", (string)null);
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.EligibilityCheckHash", b =>
                {
                    b.Property<string>("EligibilityCheckHashID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("varchar(5000)");

                    b.Property<string>("Outcome")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.HasKey("EligibilityCheckHashID");

                    b.HasIndex(new[] { "Hash" }, "idx_EligibilityCheckHash");

                    b.ToTable("EligibilityCheckHashes");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Establishment", b =>
                {
                    b.Property<int>("EstablishmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EstablishmentId"));

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

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.HasKey("EstablishmentId");

                    b.HasIndex("LocalAuthorityId");

                    b.ToTable("Establishments");
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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.User", b =>
                {
                    b.Property<string>("UserID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasColumnType("varchar(1000)");

                    b.HasKey("UserID");

                    b.HasIndex("Email", "Reference")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Application", b =>
                {
                    b.HasOne("CheckYourEligibility.Data.Models.EligibilityCheckHash", "EligibilityCheckHash")
                        .WithMany()
                        .HasForeignKey("EligibilityCheckHashID");

                    b.HasOne("CheckYourEligibility.Data.Models.Establishment", "Establishment")
                        .WithMany()
                        .HasForeignKey("EstablishmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CheckYourEligibility.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("EligibilityCheckHash");

                    b.Navigation("Establishment");

                    b.Navigation("User");
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

            modelBuilder.Entity("CheckYourEligibility.Data.Models.EligibilityCheck", b =>
                {
                    b.HasOne("CheckYourEligibility.Data.Models.EligibilityCheckHash", "EligibilityCheckHash")
                        .WithMany()
                        .HasForeignKey("EligibilityCheckHashID");

                    b.Navigation("EligibilityCheckHash");
                });

            modelBuilder.Entity("CheckYourEligibility.Data.Models.Establishment", b =>
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

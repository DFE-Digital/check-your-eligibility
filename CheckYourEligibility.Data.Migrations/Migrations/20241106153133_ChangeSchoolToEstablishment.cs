using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSchoolToEstablishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Schools_SchoolId",
                table: "Applications");

            
            migrationBuilder.CreateTable(
                name: "Establishments",
                columns: table => new
                {
                    EstablishmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstablishmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusOpen = table.Column<bool>(type: "bit", nullable: false),
                    LocalAuthorityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Establishments", x => x.EstablishmentId);
                    table.ForeignKey(
                        name: "FK_Establishments_LocalAuthorities_LocalAuthorityId",
                        column: x => x.LocalAuthorityId,
                        principalTable: "LocalAuthorities",
                        principalColumn: "LocalAuthorityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Establishments_LocalAuthorityId",
                table: "Establishments",
                column: "LocalAuthorityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Establishments_SchoolId",
                table: "Applications",
                column: "SchoolId",
                principalTable: "Establishments",
                principalColumn: "EstablishmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
SET IDENTITY_INSERT Establishments ON

INSERT INTO [dbo].[Establishments]
           ([EstablishmentId]
           ,[EstablishmentName]
           ,[Postcode]
           ,[Street]
           ,[Locality]
           ,[Town]
           ,[County]
           ,[StatusOpen]
           ,[LocalAuthorityId])
     select [SchoolId]
           ,[EstablishmentName]
           ,[Postcode]
           ,[Street]
           ,[Locality]
           ,[Town]
           ,[County]
           ,[StatusOpen]
           ,[LocalAuthorityId] from Schools

SET IDENTITY_INSERT Establishments ON
");


            migrationBuilder.DropTable(
                name: "Schools");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Establishments_SchoolId",
                table: "Applications");

            migrationBuilder.DropTable(
                name: "Establishments");

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalAuthorityId = table.Column<int>(type: "int", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstablishmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusOpen = table.Column<bool>(type: "bit", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolId);
                    table.ForeignKey(
                        name: "FK_Schools_LocalAuthorities_LocalAuthorityId",
                        column: x => x.LocalAuthorityId,
                        principalTable: "LocalAuthorities",
                        principalColumn: "LocalAuthorityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_LocalAuthorityId",
                table: "Schools",
                column: "LocalAuthorityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Schools_SchoolId",
                table: "Applications",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "SchoolId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

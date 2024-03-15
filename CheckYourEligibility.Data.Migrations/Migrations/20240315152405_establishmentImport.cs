using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class establishmentImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalAuthorities",
                columns: table => new
                {
                    LaCode = table.Column<int>(type: "int", nullable: false),
                    LaName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalAuthorities", x => x.LaCode);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Urn = table.Column<int>(type: "int", nullable: false),
                    EstablishmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Locality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusOpen = table.Column<bool>(type: "bit", nullable: false),
                    LocalAuthorityLaCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Urn);
                    table.ForeignKey(
                        name: "FK_Schools_LocalAuthorities_LocalAuthorityLaCode",
                        column: x => x.LocalAuthorityLaCode,
                        principalTable: "LocalAuthorities",
                        principalColumn: "LaCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_LocalAuthorityLaCode",
                table: "Schools",
                column: "LocalAuthorityLaCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropTable(
                name: "LocalAuthorities");
        }
    }
}

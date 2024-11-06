using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationSchool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Establishments_SchoolId",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "Applications",
                newName: "EstablishmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_SchoolId",
                table: "Applications",
                newName: "IX_Applications_EstablishmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Establishments_EstablishmentId",
                table: "Applications",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "EstablishmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Establishments_EstablishmentId",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "EstablishmentId",
                table: "Applications",
                newName: "SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_EstablishmentId",
                table: "Applications",
                newName: "IX_Applications_SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Establishments_SchoolId",
                table: "Applications",
                column: "SchoolId",
                principalTable: "Establishments",
                principalColumn: "EstablishmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

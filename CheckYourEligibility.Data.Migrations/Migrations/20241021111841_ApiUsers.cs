using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckYourEligibility.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ApiUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemUsers",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "varchar(200)", nullable: false),
                    Password = table.Column<string>(type: "varchar(200)", nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUsers", x => x.UserName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemUsers");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chandiman.HttpServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWebsite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WebsitePath",
                table: "Websites",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "WebsiteId",
                table: "Websites",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Websites",
                newName: "WebsitePath");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Websites",
                newName: "WebsiteId");
        }
    }
}

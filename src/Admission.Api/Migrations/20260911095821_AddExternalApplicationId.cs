using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admission.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalApplicationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalApplicationId",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalApplicationId",
                table: "Applications");
        }
    }
}

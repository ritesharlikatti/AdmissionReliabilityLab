using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admission.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Applications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_IdempotencyKey",
                table: "Applications",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_IdempotencyKey",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Applications");
        }
    }
}

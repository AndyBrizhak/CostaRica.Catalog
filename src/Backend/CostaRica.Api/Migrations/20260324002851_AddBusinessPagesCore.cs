using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CostaRica.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessPagesCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BusinessPages");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "BusinessPages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OldSlugs",
                table: "BusinessPages",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPages_Location",
                table: "BusinessPages",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPages_OldSlugs",
                table: "BusinessPages",
                column: "OldSlugs")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessPages_Location",
                table: "BusinessPages");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPages_OldSlugs",
                table: "BusinessPages");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "BusinessPages");

            migrationBuilder.DropColumn(
                name: "OldSlugs",
                table: "BusinessPages");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BusinessPages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

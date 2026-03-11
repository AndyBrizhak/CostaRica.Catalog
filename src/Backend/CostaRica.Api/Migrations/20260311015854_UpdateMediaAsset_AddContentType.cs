using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CostaRica.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMediaAsset_AddContentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "MediaAssets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "MediaAssets");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CostaRica.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPages_Cities_CityId",
                table: "BusinessPages");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPages_Provinces_ProvinceId",
                table: "BusinessPages");

            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Provinces_ProvinceId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Cities");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPages_Cities_CityId",
                table: "BusinessPages",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPages_Provinces_ProvinceId",
                table: "BusinessPages",
                column: "ProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Provinces_ProvinceId",
                table: "Cities",
                column: "ProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPages_Cities_CityId",
                table: "BusinessPages");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPages_Provinces_ProvinceId",
                table: "BusinessPages");

            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Provinces_ProvinceId",
                table: "Cities");

            migrationBuilder.AddColumn<Point>(
                name: "Center",
                table: "Cities",
                type: "geography(Point, 4326)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPages_Cities_CityId",
                table: "BusinessPages",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPages_Provinces_ProvinceId",
                table: "BusinessPages",
                column: "ProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Provinces_ProvinceId",
                table: "Cities",
                column: "ProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

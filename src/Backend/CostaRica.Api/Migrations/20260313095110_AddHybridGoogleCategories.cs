using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CostaRica.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridGoogleCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessGoogleCategories_BusinessPages_BusinessPageId",
                table: "BusinessGoogleCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessGoogleCategories_GoogleCategories_GoogleCategoriesId",
                table: "BusinessGoogleCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessGoogleCategories",
                table: "BusinessGoogleCategories");

            migrationBuilder.RenameTable(
                name: "BusinessGoogleCategories",
                newName: "BusinessSecondaryCategories");

            migrationBuilder.RenameColumn(
                name: "GoogleCategoriesId",
                table: "BusinessSecondaryCategories",
                newName: "SecondaryCategoriesId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessGoogleCategories_GoogleCategoriesId",
                table: "BusinessSecondaryCategories",
                newName: "IX_BusinessSecondaryCategories_SecondaryCategoriesId");

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryCategoryId",
                table: "BusinessPages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSecondaryCategories",
                table: "BusinessSecondaryCategories",
                columns: new[] { "BusinessPageId", "SecondaryCategoriesId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPages_PrimaryCategoryId",
                table: "BusinessPages",
                column: "PrimaryCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPages_GoogleCategories_PrimaryCategoryId",
                table: "BusinessPages",
                column: "PrimaryCategoryId",
                principalTable: "GoogleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSecondaryCategories_BusinessPages_BusinessPageId",
                table: "BusinessSecondaryCategories",
                column: "BusinessPageId",
                principalTable: "BusinessPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSecondaryCategories_GoogleCategories_SecondaryCateg~",
                table: "BusinessSecondaryCategories",
                column: "SecondaryCategoriesId",
                principalTable: "GoogleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPages_GoogleCategories_PrimaryCategoryId",
                table: "BusinessPages");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSecondaryCategories_BusinessPages_BusinessPageId",
                table: "BusinessSecondaryCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSecondaryCategories_GoogleCategories_SecondaryCateg~",
                table: "BusinessSecondaryCategories");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPages_PrimaryCategoryId",
                table: "BusinessPages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSecondaryCategories",
                table: "BusinessSecondaryCategories");

            migrationBuilder.DropColumn(
                name: "PrimaryCategoryId",
                table: "BusinessPages");

            migrationBuilder.RenameTable(
                name: "BusinessSecondaryCategories",
                newName: "BusinessGoogleCategories");

            migrationBuilder.RenameColumn(
                name: "SecondaryCategoriesId",
                table: "BusinessGoogleCategories",
                newName: "GoogleCategoriesId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessSecondaryCategories_SecondaryCategoriesId",
                table: "BusinessGoogleCategories",
                newName: "IX_BusinessGoogleCategories_GoogleCategoriesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessGoogleCategories",
                table: "BusinessGoogleCategories",
                columns: new[] { "BusinessPageId", "GoogleCategoriesId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessGoogleCategories_BusinessPages_BusinessPageId",
                table: "BusinessGoogleCategories",
                column: "BusinessPageId",
                principalTable: "BusinessPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessGoogleCategories_GoogleCategories_GoogleCategoriesId",
                table: "BusinessGoogleCategories",
                column: "GoogleCategoriesId",
                principalTable: "GoogleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

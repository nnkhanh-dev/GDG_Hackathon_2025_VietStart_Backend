using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietStart_API.Migrations
{
    /// <inheritdoc />
    public partial class FixDBForEmbbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectCategoriesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectRolesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectSkillsEmbadding",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "CategoryEmbedding",
                table: "StartUps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamEmbedding",
                table: "StartUps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryInvests",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RolesInStartup",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryEmbedding",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "TeamEmbedding",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "CategoryInvests",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RolesInStartup",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "ProjectCategoriesEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectRolesEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectSkillsEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietStart_API.Migrations
{
    /// <inheritdoc />
    public partial class removePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamStartUps_Positions_PositionId",
                table: "TeamStartUps");

            migrationBuilder.DropTable(
                name: "CategoryEmbadings");

            migrationBuilder.DropIndex(
                name: "IX_TeamStartUps_PositionId",
                table: "TeamStartUps");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "TeamStartUps");

            migrationBuilder.DropColumn(
                name: "Motivation",
                table: "TeamStartUps");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "TeamStartUps");

            migrationBuilder.DropColumn(
                name: "CategoryEmbedding",
                table: "StartUps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "TeamStartUps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivation",
                table: "TeamStartUps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "TeamStartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategoryEmbedding",
                table: "StartUps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryEmbadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryEmbadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamStartUps_PositionId",
                table: "TeamStartUps",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamStartUps_Positions_PositionId",
                table: "TeamStartUps",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

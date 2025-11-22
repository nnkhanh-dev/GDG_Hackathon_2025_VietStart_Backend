using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietStart_API.Migrations
{
    /// <inheritdoc />
    public partial class removeEmbadding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCategoryEmbadings");

            migrationBuilder.DropTable(
                name: "UserRoleEmbadings");

            migrationBuilder.DropTable(
                name: "UserSkillEmbadings");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "RolesEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillsEmbadding",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoriesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectCategoriesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectRolesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProjectSkillsEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RolesEmbadding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SkillsEmbadding",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "UserCategoryEmbadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryEmBadingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategoryEmbadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCategoryEmbadings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCategoryEmbadings_CategoryEmbadings_CategoryEmBadingId",
                        column: x => x.CategoryEmBadingId,
                        principalTable: "CategoryEmbadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleEmbadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleEmbadingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleEmbadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleEmbadings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleEmbadings_RoleEmbadings_RoleEmbadingId",
                        column: x => x.RoleEmbadingId,
                        principalTable: "RoleEmbadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSkillEmbadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillEmbadingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkillEmbadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSkillEmbadings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSkillEmbadings_SkillEmbadings_SkillEmbadingId",
                        column: x => x.SkillEmbadingId,
                        principalTable: "SkillEmbadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryEmbadings_CategoryEmBadingId",
                table: "UserCategoryEmbadings",
                column: "CategoryEmBadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryEmbadings_UserId",
                table: "UserCategoryEmbadings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleEmbadings_RoleEmbadingId",
                table: "UserRoleEmbadings",
                column: "RoleEmbadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleEmbadings_UserId",
                table: "UserRoleEmbadings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillEmbadings_SkillEmbadingId",
                table: "UserSkillEmbadings",
                column: "SkillEmbadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillEmbadings_UserId",
                table: "UserSkillEmbadings",
                column: "UserId");
        }
    }
}

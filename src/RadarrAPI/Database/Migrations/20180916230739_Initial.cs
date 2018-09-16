using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RadarrAPI.Database.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trakt",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    State = table.Column<Guid>(nullable: false),
                    Target = table.Column<string>(maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trakt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Updates",
                columns: table => new
                {
                    UpdateEntityId = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Version = table.Column<string>(maxLength: 32, nullable: false),
                    ReleaseDate = table.Column<DateTime>(nullable: false),
                    Branch = table.Column<sbyte>(type: "tinyint", nullable: false),
                    New = table.Column<string>(maxLength: 8192, nullable: false),
                    Fixed = table.Column<string>(maxLength: 8192, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Updates", x => x.UpdateEntityId);
                });

            migrationBuilder.CreateTable(
                name: "UpdateFiles",
                columns: table => new
                {
                    UpdateEntityId = table.Column<int>(nullable: false),
                    OperatingSystem = table.Column<sbyte>(type: "tinyint", nullable: false),
                    Filename = table.Column<string>(maxLength: 128, nullable: false),
                    Url = table.Column<string>(maxLength: 255, nullable: false),
                    Hash = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateFiles", x => new { x.UpdateEntityId, x.OperatingSystem });
                    table.ForeignKey(
                        name: "FK_UpdateFiles_Updates_UpdateEntityId",
                        column: x => x.UpdateEntityId,
                        principalTable: "Updates",
                        principalColumn: "UpdateEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trakt_State",
                table: "Trakt",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_Updates_Branch_Version",
                table: "Updates",
                columns: new[] { "Branch", "Version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trakt");

            migrationBuilder.DropTable(
                name: "UpdateFiles");

            migrationBuilder.DropTable(
                name: "Updates");
        }
    }
}

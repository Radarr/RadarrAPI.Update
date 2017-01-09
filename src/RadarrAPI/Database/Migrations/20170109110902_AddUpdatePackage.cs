using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RadarrAPI.Database.Migrations
{
    public partial class AddUpdatePackage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpdatePackages",
                columns: table => new
                {
                    UpdatePackageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Branch = table.Column<string>(nullable: true),
                    Filename = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true),
                    ReleaseDate = table.Column<DateTime>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    VersionStr = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdatePackages", x => x.UpdatePackageId);
                });

            migrationBuilder.CreateTable(
                name: "UpdateChanges",
                columns: table => new
                {
                    UpdateChangesId = table.Column<int>(nullable: false),
                    FixedStr = table.Column<string>(nullable: true),
                    NewStr = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateChanges", x => x.UpdateChangesId);
                    table.ForeignKey(
                        name: "FK_UpdateChanges_UpdatePackages_UpdateChangesId",
                        column: x => x.UpdateChangesId,
                        principalTable: "UpdatePackages",
                        principalColumn: "UpdatePackageId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateChanges");

            migrationBuilder.DropTable(
                name: "UpdatePackages");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageScanner.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    FileCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileLastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTaken = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CameraModel = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ExifDataJson = table.Column<string>(type: "TEXT", nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastProcessed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_FileHash",
                table: "Images",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Name",
                table: "Images",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Path",
                table: "Images",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_FileHash",
                table: "ProcessedFiles",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_Path",
                table: "ProcessedFiles",
                column: "Path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "ProcessedFiles");
        }
    }
}

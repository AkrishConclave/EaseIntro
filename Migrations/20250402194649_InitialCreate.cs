using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ease_intro_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MeetStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetStatus", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Meets",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Location = table.Column<string>(type: "varchar(260)", maxLength: 260, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meets", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_Meets_MeetStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "MeetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "MeetStatus",
                columns: new[] { "Id", "Description", "Title" },
                values: new object[,]
                {
                    { 1, "Встреча запланирована", "Встреча запланирована" },
                    { 2, "Встреча в процессе", "Встреча в процессе" },
                    { 3, "Встреча завершена", "Встреча завершена" },
                    { 4, "Встреча отменена", "Встреча отменена" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meets_StatusId",
                table: "Meets",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meets");

            migrationBuilder.DropTable(
                name: "MeetStatus");
        }
    }
}

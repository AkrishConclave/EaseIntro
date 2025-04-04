using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ease_intro_api.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberQRcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Member",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "MeetStatus",
                columns: new[] { "Id", "Description", "Title" },
                values: new object[] { 5, "Открыто для регистрации", "Открыто для регистрации" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MeetStatus",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Member");
        }
    }
}

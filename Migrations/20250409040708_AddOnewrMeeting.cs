using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ease_intro_api.Migrations
{
    /// <inheritdoc />
    public partial class AddOnewrMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Meets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Meets_OwnerId",
                table: "Meets",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meets_Users_OwnerId",
                table: "Meets",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meets_Users_OwnerId",
                table: "Meets");

            migrationBuilder.DropIndex(
                name: "IX_Meets_OwnerId",
                table: "Meets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Meets");
        }
    }
}

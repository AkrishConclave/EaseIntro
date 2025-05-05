using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ease_intro_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToMemberContactWithMeetUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Member_MeetGuid_Contact",
                table: "Member",
                columns: new[] { "MeetGuid", "Contact" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Member_MeetGuid_Contact",
                table: "Member");

            migrationBuilder.CreateIndex(
                name: "IX_Member_Contact",
                table: "Member",
                column: "Contact",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_MeetGuid",
                table: "Member",
                column: "MeetGuid");
        }
    }
}

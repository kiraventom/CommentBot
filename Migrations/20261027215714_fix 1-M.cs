using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentBot.Migrations
{
    /// <inheritdoc />
    public partial class fix1M : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_UserComments_UserId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_UserId",
                table: "UserComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_UserComments_UserId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_UserId",
                table: "UserComments",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies",
                column: "PostId",
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentBot.Migrations
{
    /// <inheritdoc />
    public partial class fixindexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_UserComments_TelegramId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ChannelId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TelegramId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies");

            migrationBuilder.DropIndex(
                name: "IX_BotReplies_TelegramId",
                table: "BotReplies");

            migrationBuilder.DropIndex(
                name: "IX_BotComments_TelegramId",
                table: "BotComments");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_PostId_TelegramId",
                table: "UserComments",
                columns: new[] { "PostId", "TelegramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ChannelId_TelegramId",
                table: "Posts",
                columns: new[] { "ChannelId", "TelegramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotReplies_PostId_TelegramId",
                table: "BotReplies",
                columns: new[] { "PostId", "TelegramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotComments_PostId_TelegramId",
                table: "BotComments",
                columns: new[] { "PostId", "TelegramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserComments_PostId_TelegramId",
                table: "UserComments");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ChannelId_TelegramId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_BotReplies_PostId_TelegramId",
                table: "BotReplies");

            migrationBuilder.DropIndex(
                name: "IX_BotComments_PostId_TelegramId",
                table: "BotComments");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_PostId",
                table: "UserComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_TelegramId",
                table: "UserComments",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ChannelId",
                table: "Posts",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TelegramId",
                table: "Posts",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotReplies_PostId",
                table: "BotReplies",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_BotReplies_TelegramId",
                table: "BotReplies",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotComments_TelegramId",
                table: "BotComments",
                column: "TelegramId",
                unique: true);
        }
    }
}

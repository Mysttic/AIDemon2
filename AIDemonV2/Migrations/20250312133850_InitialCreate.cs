using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDemonV2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageContent = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalMessage = table.Column<string>(type: "TEXT", nullable: false),
                    AIModel = table.Column<string>(type: "TEXT", nullable: true),
                    ProgrammingLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    Favourite = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplyToMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Messages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    InstructionPrompt = table.Column<string>(type: "TEXT", nullable: true),
                    AIModel = table.Column<string>(type: "TEXT", nullable: true),
                    ProgrammingLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "AIModel", "ApiKey", "CreationDate", "InstructionPrompt", "ModificationDate", "ProgrammingLanguage" },
                values: new object[] { 1, null, "", new DateTime(2025, 3, 12, 13, 38, 49, 702, DateTimeKind.Utc).AddTicks(5133), "You are a helpful assistant.", new DateTime(2025, 3, 12, 13, 38, 49, 702, DateTimeKind.Utc).AddTicks(5135), null });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}

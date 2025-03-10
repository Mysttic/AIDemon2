﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AIDemonV2.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageContent = table.Column<string>(type: "text", nullable: false),
                    OriginalMessage = table.Column<string>(type: "text", nullable: false),
                    AIModel = table.Column<string>(type: "text", nullable: true),
                    ProgrammingLanguage = table.Column<string>(type: "text", nullable: true),
                    RunDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Favourite = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    InstructionPrompt = table.Column<string>(type: "text", nullable: true),
                    AIModel = table.Column<string>(type: "text", nullable: true),
                    ProgrammingLanguage = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "AIModel", "ApiKey", "CreationDate", "InstructionPrompt", "ModificationDate", "ProgrammingLanguage" },
                values: new object[] { 1, null, "", new DateTime(2025, 3, 10, 11, 30, 3, 279, DateTimeKind.Utc).AddTicks(4209), "You are a helpful assistant.", new DateTime(2025, 3, 10, 11, 30, 3, 279, DateTimeKind.Utc).AddTicks(4209), null });
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

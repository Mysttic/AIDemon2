using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDemon2.Migrations
{
    /// <inheritdoc />
    public partial class AddIsUserMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUserMessage",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Wiersze sprzed tej migracji nie mają zapisanego autora. Odtwarzamy go
            // regułą, która obowiązywała wcześniej (brak języka programowania ==
            // wiadomość użytkownika), żeby istniejące rozmowy wyglądały tak samo jak
            // przed aktualizacją. Dla nowych wiadomości autor jest zapisywany wprost.
            migrationBuilder.Sql(
                "UPDATE Messages SET IsUserMessage = 1 " +
                "WHERE ProgrammingLanguage IS NULL OR ProgrammingLanguage = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUserMessage",
                table: "Messages");
        }
    }
}

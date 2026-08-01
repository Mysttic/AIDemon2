using AIDemon2.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class AIDemonDbContext : DbContext
{
	public DbSet<Message> Messages { get; set; }
	public DbSet<Settings> Settings { get; set; }

	/// <summary>Znaczniki czasu wiersza startowego, przepisane z migracji Initial.</summary>
	private static readonly DateTime SeedTimestampCreation =
		new DateTime(2025, 3, 17, 9, 16, 31, 559, DateTimeKind.Utc).AddTicks(4538);

	private static readonly DateTime SeedTimestampModification =
		new DateTime(2025, 3, 17, 9, 16, 31, 559, DateTimeKind.Utc).AddTicks(4539);

	public AIDemonDbContext(DbContextOptions<AIDemonDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Daty MUSZĄ być stałe. Z DateTime.UtcNow model zmieniał się przy każdym
		// uruchomieniu, EF Core zgłaszał PendingModelChangesWarning i ostrzeżenie
		// trzeba było wyciszać — a wyciszone maskowało też realne rozjazdy modelu
		// z migracjami. Wartości pochodzą wprost z migracji 20250317091632_Initial,
		// więc model zgadza się ze snapshotem i nie generuje zmiany do zastosowania.
		modelBuilder.Entity<Settings>().HasData(
			new Settings
			{
				Id = 1,
				ApiKey = string.Empty,
				InstructionPrompt = "You are a helpful assistant.",
				AIModel = null,
				ProgrammingLanguage = null,
				CreationDate = SeedTimestampCreation,
				ModificationDate = SeedTimestampModification
			});

		modelBuilder.Entity<Message>()
			.HasMany(m => m.Replies)
			.WithOne(m => m.ReplyTo)
			.HasForeignKey(m => m.ReplyToMessageId);

		// Kasowanie miękkie egzekwowane przez model, a nie przez pamięć programisty.
		// Wcześniej filtr !Deleted trzeba było dopisywać w każdym zapytaniu osobno
		// i w GetAllFavouriteAsync go zabrakło — usunięte wiadomości wracały
		// użytkownikowi na listę ulubionych.
		modelBuilder.Entity<Message>().HasQueryFilter(m => !m.Deleted);
	}

	// OnConfiguring z ConfigureWarnings(Ignore(PendingModelChangesWarning)) usunięte.
	// Ostrzeżenie było wyciszane, bo seed używał DateTime.UtcNow i model zmieniał się
	// przy każdym uruchomieniu. Po zamianie na stałe znaczniki czasu ostrzeżenie
	// nie występuje, a wyciszone maskowałoby realne rozjazdy modelu z migracjami.
}

public class AIDemonDbContextFactory : IDesignTimeDbContextFactory<AIDemonDbContext>
{
	public AIDemonDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AIDemonDbContext>();
		// Ten sam connection string co aplikacja — łącznie z kluczem. Wcześniej
		// fabryka otwierała bazę bez hasła, więc narzędzia EF albo padały na
		// istniejącej bazie, albo tworzyły obok nieszyfrowaną.
		optionsBuilder.UseSqlite(DatabaseLocation.ConnectionString);

		return new AIDemonDbContext(optionsBuilder.Options);
	}
}
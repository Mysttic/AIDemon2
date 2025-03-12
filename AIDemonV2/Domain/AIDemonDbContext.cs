using AIDemonV2.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class AIDemonDbContext : DbContext
{
	public DbSet<Message> Messages { get; set; }
	public DbSet<Settings> Settings { get; set; }

	public AIDemonDbContext(DbContextOptions<AIDemonDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Settings>().HasData(
			new Settings
			{
				Id = 1,
				ApiKey = string.Empty,
				InstructionPrompt = "You are a helpful assistant.",
				AIModel = null,
				ProgrammingLanguage = null,
				CreationDate = DateTime.UtcNow,
				ModificationDate = DateTime.UtcNow
			});

		modelBuilder.Entity<Message>()
			.HasMany(m => m.Replies)
			.WithOne(m => m.ReplyTo)
			.HasForeignKey(m => m.ReplyToMessageId);
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.ConfigureWarnings(warnings =>
			warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
	}
}

public class AIDemonDbContextFactory : IDesignTimeDbContextFactory<AIDemonDbContext>
{
	public AIDemonDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AIDemonDbContext>();
		// Ścieżka do bazy SQLite
		var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AIDemonV2.db");
		optionsBuilder.UseSqlite($"Data Source={dbPath}");

		return new AIDemonDbContext(optionsBuilder.Options);
	}
}
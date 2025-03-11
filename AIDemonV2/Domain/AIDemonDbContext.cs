using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AIDemonV2.Properties;


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
			.Property(m => m.RunDate)
			.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

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
		optionsBuilder.UseNpgsql(Resources.ConnectionString);

		return new AIDemonDbContext(optionsBuilder.Options);
	}
}
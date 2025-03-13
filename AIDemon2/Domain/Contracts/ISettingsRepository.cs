public interface ISettingsRepository
{
	Task<Settings?> Get();

	Task<Settings> UpdateAsync(Settings entity);
}
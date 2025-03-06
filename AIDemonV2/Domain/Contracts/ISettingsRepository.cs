using System.Threading.Tasks;

public interface ISettingsRepository
{
	Task<Settings?> Get();

	Task<Settings> Update(Settings entity);
}

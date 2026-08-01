namespace AIDemon2.Services.ModelCatalog;

/// <summary>Dostawca listy modeli do wyboru w ustawieniach.</summary>
public interface IModelCatalog
{
	/// <summary>
	/// Zwraca identyfikatory modeli, posortowane. Nigdy nie rzuca ani nie zwraca pustej
	/// listy — brak sieci kończy się listą zapasową, bo okno ustawień musi dać się otworzyć
	/// także wtedy, gdy użytkownik jest offline.
	/// </summary>
	Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken = default);
}

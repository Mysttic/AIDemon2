using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Wspólna podstawa encji.
///
/// Dziedziczy po <see cref="ObservableObject"/> zamiast polegać na atrybucie
/// [AddINotifyPropertyChangedInterface] z PropertyChanged.Fody. Powiadomienia są
/// dzięki temu widoczne w kodzie C#, a nie wplatane w assembly przez weaver —
/// którego usunięcie po cichu psuło bindingi, bez błędu kompilacji.
/// </summary>
public class EntityBase : ObservableObject, IEntityBase
{
	// Id nie jest bindowane w widokach — zwykła właściwość wystarczy.
	public int Id { get; set; }

	private DateTime _creationDate;
	public DateTime CreationDate
	{
		get => _creationDate;
		set => SetProperty(ref _creationDate, value);
	}

	private DateTime _modificationDate;
	public DateTime ModificationDate
	{
		get => _modificationDate;
		set => SetProperty(ref _modificationDate, value);
	}

	/// <summary>
	/// Zegar odczytywany RAZ, a nie osobno dla każdej daty.
	///
	/// DateTime.UtcNow ma na Windows rozdzielczość 100 ns, więc dwa kolejne odczyty
	/// potrafią różnić się o tik. Tyle wystarczało, by ModificationDate wypadło
	/// później niż CreationDate — a więc by IsModified było prawdą dla encji, której
	/// nikt nie tknął. Widok rozmowy pokazywał wtedy pod znacznikiem czasu drugi,
	/// identycznie sformatowany (różnica ginęła w formacie HH:mm:ss).
	/// </summary>
	public EntityBase()
	{
		var teraz = DateTime.UtcNow;
		CreationDate = teraz;
		ModificationDate = teraz;
	}
}

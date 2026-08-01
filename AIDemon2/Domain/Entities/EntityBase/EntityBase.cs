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

	public EntityBase()
	{
		CreationDate = DateTime.UtcNow;
		ModificationDate = DateTime.UtcNow;
	}
}

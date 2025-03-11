using PropertyChanged;
using System;

[AddINotifyPropertyChangedInterface]
public class EntityBase : IEntityBase
{
	public int Id { get; set; }
	public DateTime CreationDate { get; set; }
	public DateTime ModificationDate { get; set; }
	public EntityBase()
	{
		CreationDate = DateTime.UtcNow;
		ModificationDate = DateTime.UtcNow;
	}
}


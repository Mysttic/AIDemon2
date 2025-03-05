using System;

public interface IEntityBase
{
	public int Id { get; set; }
	public DateTime CreationDate { get; set; }
	public DateTime ModificationDate { get; set; }

}


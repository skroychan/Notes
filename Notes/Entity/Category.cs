using System;
using System.Collections.Generic;

namespace skroy.Notes.Entity;

public class Category
{
	public long Id { get; set; }
	public string Name { get; set; }
	public List<Note> Notes { get; set; }
	public string Color { get; set; }
	public DateTime? CreationDate { get; set; }
}

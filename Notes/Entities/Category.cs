using System;
using System.Collections.Generic;

namespace Notes.Entities;

public class Category
{
	public long ID { get; set; }
	public string Name { get; set; }
	public List<Note> Notes { get; set; }
	public string Color { get; set; }
	public DateTime? CreationDate { get; set; }
}

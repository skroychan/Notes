using System;

namespace Notes.Entities;

public class Note
{
	public long ID { get; set; }
	public string Text { get; set; }
	public string Color { get; set; }
	public DateTime? CreationDate { get; set; }
	public DateTime? ArchiveDate { get; set; }
}

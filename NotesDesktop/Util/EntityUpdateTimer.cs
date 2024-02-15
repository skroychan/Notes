using System.Timers;

namespace skroy.NotesDesktop.Util;

internal class NoteUpdateTimer : Timer
{
	public long Id { get; set; }
	public string Text { get; set; }
}

internal class CategoryUpdateTimer : Timer
{
	public long Id { get; set; }
	public string Name { get; set; }
}

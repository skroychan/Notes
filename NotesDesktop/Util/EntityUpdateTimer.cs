using System.Timers;

namespace skroy.NotesDesktop.Util;

internal class NoteUpdateTimer : Timer
{
	public long Id;
	public string Text;
}

internal class CategoryUpdateTimer : Timer
{
	public long Id;
	public string Name;
}

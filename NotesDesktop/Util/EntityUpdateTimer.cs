using Avalonia.Threading;

namespace skroy.NotesDesktop.Util;

internal class NoteUpdateTimer : DispatcherTimer
{
    public long Id { get; set; }
    public string Text { get; set; }
}

internal class CategoryUpdateTimer : DispatcherTimer
{
    public long Id { get; set; }
    public string Name { get; set; }
}

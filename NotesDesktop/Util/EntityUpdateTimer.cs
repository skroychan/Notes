using Avalonia.Threading;

namespace skroy.NotesDesktop.Util;

internal class NoteUpdateTimer : DispatcherTimer
{
    public long Id { get; init; }
    public string Text { get; set; }
    public int Order { get; set; } = -1;
}

internal class CategoryUpdateTimer : DispatcherTimer
{
    public long Id { get; init; }
    public string Name { get; set; }
    public int Order { get; set; } = -1;
}

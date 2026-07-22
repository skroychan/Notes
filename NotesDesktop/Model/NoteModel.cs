using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace skroy.NotesDesktop.Model;

public partial class NoteModel : ObservableObject
{
    [ObservableProperty]
    public partial long Id { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    [ObservableProperty]
    public partial long CategoryId { get; set; }

    [ObservableProperty]
    public partial NoteStorage Storage { get; set; }

    [ObservableProperty]
    public partial long Order { get; set; }

    [ObservableProperty]
    public partial DateTime? CreationDate { get; set; }

    [ObservableProperty]
    public partial DateTime? ModificationDate { get; set; }

    [ObservableProperty]
    public partial DateTime? ArchiveDate { get; set; }
}

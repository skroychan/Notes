using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace skroy.NotesDesktop.Model;

public partial class CategoryModel : ObservableObject
{
    [ObservableProperty]
    public partial long Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<NoteModel> Notes { get; set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    [ObservableProperty]
    public partial long Order { get; set; }

    [ObservableProperty]
    public partial DateTime? CreationDate { get; set; }
}

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace NotesDesktop.Model;

public class CategoryModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<NoteModel> Notes { get; set; }
    public SolidColorBrush Brush { get; set; }
    public DateTime? CreationDate { get; set; }
}

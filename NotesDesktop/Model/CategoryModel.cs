using System;
using System.Collections.Generic;

namespace NotesDesktop.Model;

public class CategoryModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<NoteModel> Notes { get; set; }
    public string Color { get; set; }
    public DateTime? CreationDate { get; set; }
}

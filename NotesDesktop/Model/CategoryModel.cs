using System;
using System.Collections.Generic;

namespace skroy.NotesDesktop.Model;

public class CategoryModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<NoteModel> Notes { get; set; }
    public string Color { get; set; }
    public long Order { get; set; }
    public DateTime? CreationDate { get; set; }
}

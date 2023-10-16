﻿using System;

namespace NotesDesktop.Model;

public class NoteModel
{
    public long Id { get; set; }
    public string Text { get; set; }
    public string Color { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }
    public DateTime? ArchiveDate { get; set; }
}

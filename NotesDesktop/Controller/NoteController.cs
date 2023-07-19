using Notes.Entity;
using Notes.Service;
using NotesDesktop.Model;
using NotesDesktop.Util;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NotesDesktop.Controller;

public class NoteController
{
    private readonly NoteService noteService;

    public NoteController()
    {
        noteService = new NoteService();
    }


    public CategoryModel CreateCategory(string name)
    {
        var category = noteService.CreateCategory(name);

        return ToModel(category);
    }

    public NoteModel CreateNote(long categoryID)
    {
        var note = noteService.CreateNote(categoryID);

        return ToModel(note);
    }

    public IEnumerable<CategoryModel> GetAll()
    {
        return noteService.GetAll().Select(ToModel);
    }

    public IEnumerable<CategoryModel> Search(string query, bool ignoreCase)
    {
        return noteService.Search(query, ignoreCase).Select(ToModel);
    }

    public bool DeleteCategory(long categoryId)
    {
        return noteService.DeleteCategory(categoryId);
    }

    public bool DeleteNote(long noteId)
    {
        return noteService.DeleteNote(noteId);
    }

    public bool ChangeNoteCategory(long noteId, long toCategoryId)
    {
        return noteService.ChangeNoteCategory(noteId, toCategoryId);
    }

    public bool MoveCategory(long categoryId, int newPosition)
    {
        return noteService.MoveCategory(categoryId, newPosition);
    }

    public bool MoveNote(long noteId, int newPosition)
    {
        return noteService.MoveNote(noteId, newPosition);
    }

    public bool UpdateNoteText(long noteId, string newText)
    {
        return noteService.UpdateNoteText(noteId, newText);
    }

    public bool UpdateNoteColor(long noteId, string hexColor)
    {
        return noteService.UpdateNoteColor(noteId, hexColor);
    }

    public bool UpdateCategoryName(long categoryId, string newName)
    {
        return noteService.UpdateCategoryName(categoryId, newName);
    }

    public bool UpdateCategoryColor(long categoryId, string hexColor)
    {
        return noteService.UpdateCategoryColor(categoryId, hexColor);
    }

    public bool ArchiveNote(long noteId)
    {
        return noteService.ArchiveNote(noteId);
    }

    public void Save()
    {
        noteService.Save();
    }


    private CategoryModel ToModel(Category category)
    {
        var brush = WpfUtils.ToBrush(category.Color);
        if (brush != null)
            brush.Opacity = 0.3;
        else
            brush = SystemColors.ControlBrush;

        return new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            CreationDate = category.CreationDate,
            Brush = brush,
            Notes = category.Notes.Select(ToModel).ToList()
        };
    }

    private NoteModel ToModel(Note note)
    {
        var brush = WpfUtils.ToBrush(note.Color);
        if (brush != null)
            brush.Opacity = 0.3;

        return new NoteModel
        {
            Id = note.Id,
            Text = note.Text,
            CreationDate = note.CreationDate,
            ModificationDate = note.ModificationDate,
            ArchiveDate = note.ArchiveDate,
            Brush = brush
        };
    }
}

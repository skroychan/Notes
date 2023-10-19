using Notes.Entity;
using Notes.Service;
using NotesDesktop.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NotesDesktop.Controller;

public class NoteController
{
    private Dictionary<NoteStorage, NoteService> NoteServices { get; } = new();
    private NoteStorage CurrentStorage { get; set; }


	public NoteController()
    {
        NoteServices[NoteStorage.Main] = new NoteService("notes");
		NoteServices[NoteStorage.Archive] = new NoteService("archive");
    }


    public CategoryModel CreateCategory()
    {
        var category = GetService().CreateCategory();

        return ToModel(category);
    }

    public NoteModel CreateNote(long categoryID)
    {
        var note = GetService().CreateNote(categoryID);

        return ToModel(note);
    }

    public IEnumerable<CategoryModel> GetAll()
    {
        return GetService().GetAll().Select(ToModel);
    }

    public IEnumerable<CategoryModel> Search(string query)
    {
        return GetService().Search(query).Select(ToModel);
    }

    public bool DeleteCategory(long categoryId)
    {
        return GetService().DeleteCategory(categoryId);
    }

    public bool DeleteNote(long noteId)
    {
        return GetService().DeleteNote(noteId);
    }

    public bool ChangeNoteCategory(long noteId, long toCategoryId)
    {
        return GetService().ChangeNoteCategory(noteId, toCategoryId);
    }

    public bool MoveCategory(long categoryId, int newPosition)
    {
        return GetService().MoveCategory(categoryId, newPosition);
    }

    public bool MoveNote(long noteId, int newPosition)
    {
        return GetService().MoveNote(noteId, newPosition);
    }

    public bool UpdateNoteText(long noteId, string newText)
    {
        return GetService().UpdateNoteText(noteId, newText);
    }

    public bool UpdateNoteColor(long noteId, string hexColor)
    {
        return GetService().UpdateNoteColor(noteId, hexColor);
    }

    public bool UpdateCategoryName(long categoryId, string newName)
    {
        return GetService().UpdateCategoryName(categoryId, newName);
    }

    public bool UpdateCategoryColor(long categoryId, string hexColor)
    {
        return GetService().UpdateCategoryColor(categoryId, hexColor);
    }

    public bool ChangeNoteStorage(long noteId, NoteStorage targetStorage)
    {
        if (!NoteServices.TryGetValue(targetStorage, out var targetService))
            return false;

        var currentService = GetService();
		var note = currentService.GetNote(noteId);
        var categoryId = currentService.GetNoteCategory(noteId);
        var targetCategory = targetService.GetCategory(categoryId);
        if (targetCategory == null)
		{
			var category = currentService.GetCategory(categoryId);
			targetCategory = targetService.CreateCategory(category);
		}

		return currentService.DeleteNote(note.Id)
            && targetService.CreateNote(targetCategory.Id, note) != null;
    }

    public void SetStorage(NoteStorage mode)
    {
        CurrentStorage = mode;
    }

    public void Save()
    {
		GetService().Save();
    }


    private NoteService GetService()
    {
        if (NoteServices.TryGetValue(CurrentStorage, out var service))
            return service;
           
        throw new ArgumentException($"Storage '{CurrentStorage}' has no corresponding {typeof(NoteService)}");
    }

    private CategoryModel ToModel(Category category)
    {
        if (category.Color == null)
            category.Color = SystemColors.ControlBrush.ToString();

        return new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            CreationDate = category.CreationDate,
            Color = category.Color,
            Notes = category.Notes.Select(ToModel).ToList()
        };
    }

    private NoteModel ToModel(Note note)
    {
        return new NoteModel
        {
            Id = note.Id,
            Text = note.Text,
            CreationDate = note.CreationDate,
            ModificationDate = note.ModificationDate,
            ArchiveDate = note.ArchiveDate,
            Color = note.Color
        };
    }
}


public enum NoteStorage
{
    Main,
    Archive
}

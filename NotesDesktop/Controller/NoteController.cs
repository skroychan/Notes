using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using skroy.Notes.Entity;
using skroy.Notes.Service;
using skroy.NotesDesktop.Model;
using skroy.NotesDesktop.Util;

namespace skroy.NotesDesktop.Controller;

public class NoteController
{
    private NoteService NoteService { get; set; }


    public NoteController()
    {
        NoteService = new NoteService();

        var missingStorages = Enum.GetNames(typeof(NoteStorage)).Except(NoteService.GetAllStorages().Select(x => x.Name));
        foreach (var storage in missingStorages)
            NoteService.CreateStorage(storage);

        NoteService.SetCurrentStorage("Main");
    }


    public CategoryModel CreateCategory()
    {
        return ToModel(NoteService.CreateCategory());
    }

    public NoteModel CreateNote(long categoryId)
    {
        return ToModel(NoteService.CreateNote(categoryId));
    }

    public IEnumerable<CategoryModel> GetAll()
    {
        return SortByOrder(NoteService.GetAll()).Select(ToModel);
    }

    public IEnumerable<CategoryModel> Search(string query)
    {
        return SortByOrder(NoteService.Search(query)).Select(ToModel);
    }

    public bool DeleteCategory(long categoryId)
    {
        return NoteService.DeleteCategory(categoryId);
    }

    public bool DeleteNote(long noteId)
    {
        return NoteService.DeleteNote(noteId);
    }

    public bool ChangeNoteCategory(long noteId, long toCategoryId)
    {
        return NoteService.SetNoteCategory(noteId, toCategoryId);
    }

    public bool ReorderCategory(long categoryId, int newPosition)
    {
        return NoteService.ReorderCategory(categoryId, newPosition);
    }

    public bool ReorderNote(long noteId, int newPosition)
    {
        return NoteService.ReorderNote(noteId, newPosition);
    }

    public bool SetNoteText(long noteId, string newText)
    {
        return NoteService.SetNoteText(noteId, newText);
    }

    public bool SetNoteColor(long noteId, Color color)
    {
        return NoteService.SetNoteColor(noteId, ColorUtils.ToHexString(color));
    }

    public bool SetCategoryName(long categoryId, string newName)
    {
        return NoteService.SetCategoryName(categoryId, newName);
    }

    public bool SetCategoryColor(long categoryId, Color color)
    {
        return NoteService.SetCategoryColor(categoryId, ColorUtils.ToHexString(color));
    }

    public bool ChangeNoteStorage(long noteId, NoteStorage targetStorage)
    {
        return NoteService.SetNoteStorage(noteId, targetStorage.ToString());
    }

    public void SetStorage(NoteStorage storage)
    {
        NoteService.SetCurrentStorage(storage.ToString());
    }

    public void Save()
    {
        NoteService.Save();
    }


    private IEnumerable<Category> SortByOrder(IEnumerable<Category> categories)
    {
        return [.. categories.Select(x => { x.Notes = [.. x.Notes.OrderBy(y => y.Order)]; return x; }).OrderBy(x => x.Order)];
    }

    private CategoryModel ToModel(Category category)
    {
        return new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            CreationDate = category.CreationDate,
            Color = category.Color != null ? Color.Parse(category.Color) : default,
            Order = category.Order,
            Notes = new ObservableCollection<NoteModel>(category.Notes.Select(ToModel))
        };
    }

    private NoteModel ToModel(Note note)
    {
        var storage = NoteService.GetAllStorages().Single(x => x.Id == note.StorageId);
        if (!Enum.TryParse<NoteStorage>(storage.Name, out var noteStorage))
            throw new ArgumentException($"Storage {storage.Name} is not supported.");

        return new NoteModel
        {
            Id = note.Id,
            Text = note.Text,
            Color = note.Color != null ? Color.Parse(note.Color) : default,
            CategoryId = note.CategoryId,
            Storage = noteStorage,
            Order = note.Order,
            CreationDate = note.CreationDate,
            ModificationDate = note.ModificationDate,
            ArchiveDate = note.ArchiveDate
        };
    }
}

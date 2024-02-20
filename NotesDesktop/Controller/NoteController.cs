using skroy.Notes.Entity;
using skroy.Notes.Service;
using skroy.NotesDesktop.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

	public NoteModel CreateNote(long categoryID)
	{
		return ToModel(NoteService.CreateNote(categoryID));
	}

	public IEnumerable<CategoryModel> GetAll()
	{
		return NoteService.GetAll().Select(ToModel);
	}

	public IEnumerable<CategoryModel> Search(string query)
	{
		return NoteService.Search(query).Select(ToModel);
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

	public bool SetNoteColor(long noteId, string hexColor)
	{
		return NoteService.SetNoteColor(noteId, hexColor);
	}

	public bool SetCategoryName(long categoryId, string newName)
	{
		return NoteService.SetCategoryName(categoryId, newName);
	}

	public bool SetCategoryColor(long categoryId, string hexColor)
	{
		return NoteService.SetCategoryColor(categoryId, hexColor);
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


	private CategoryModel ToModel(Category category)
	{
		category.Color ??= SystemColors.ControlBrush.ToString();

		return new CategoryModel
		{
			Id = category.Id,
			Name = category.Name,
			CreationDate = category.CreationDate,
			Color = category.Color,
			Order = category.Order,
			Notes = category.Notes.Select(ToModel).ToList()
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
			Color = note.Color,
			CategoryId = note.CategoryId,
			Storage = noteStorage,
			Order = note.Order,
			CreationDate = note.CreationDate,
			ModificationDate = note.ModificationDate,
			ArchiveDate = note.ArchiveDate
		};
	}
}

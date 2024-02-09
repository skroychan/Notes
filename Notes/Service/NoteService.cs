using skroy.Notes.Entity;
using skroy.Notes.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace skroy.Notes.Service;

public class NoteService
{
	private readonly NoteRepository repository;

	private string CurrentStorage { get; set; }


	public NoteService()
	{
		repository = new NoteRepository();
		CurrentStorage = "Main";
	}


	public Category CreateCategory()
	{
		var newCategory = new Category()
		{
			Name = string.Empty,
			Notes = new List<Note>(),
			CreationDate = DateTime.Now
		};

		if (!repository.CreateCategory(newCategory))
			throw new Exception("Failed to create category.");

		return newCategory;
	}

	public Note CreateNote(long categoryId)
	{
		var newNote = new Note()
		{
			Text = string.Empty,
			CategoryId = categoryId,
			Storage = CurrentStorage,
			CreationDate = DateTime.Now
		};

		if (!repository.CreateNote(newNote))
			throw new Exception("Failed to create note.");

		return newNote;
	}

	public IEnumerable<Category> GetAll()
	{
		return repository.GetAll();
	}

	public IEnumerable<Category> Search(string query, bool ignoreCase = false)
	{
		throw new NotImplementedException();
	}

	public bool DeleteCategory(long categoryId)
	{
		var result = true;
		var category = repository.GetCategory(categoryId);
		var currentStorageNotes = category.Notes.Where(x => x.Storage == CurrentStorage).ToList();
		foreach (var note in currentStorageNotes)
			result &= repository.DeleteNote(note.Id);

		if (result && category.Notes.Count == currentStorageNotes.Count)
			result &= repository.DeleteCategory(categoryId);

		return result;
	}

	public bool DeleteNote(long noteId)
	{
		return repository.DeleteNote(noteId);
	}

	public bool ReorderCategory(long categoryId, int newPosition)
	{
		return false;
	}

	public bool ReorderNote(long noteId, int newPosition)
	{
		return false;
	}

	public bool SetNoteCategory(long noteId, long toCategoryId)
	{
		return repository.UpdateNote(noteId, _ => new Note
		{
			CategoryId = toCategoryId
		});
	}

	public bool SetNoteText(long noteId, string newText)
	{
		return repository.UpdateNote(noteId, _ => new Note
		{
			Text = newText,
			ModificationDate = DateTime.Now
		});
	}

	public bool SetNoteColor(long noteId, string hexColor)
	{
		var note = repository.GetNote(noteId);
		return repository.UpdateNote(noteId, _ => new Note
		{
			Color = hexColor
		});
	}

	public bool SetNoteStorage(long noteId, string targetStorage)
	{
		return repository.UpdateNote(noteId, _ => new Note
		{
			Storage = targetStorage
		});
	}

	public bool SetCategoryName(long categoryId, string newName)
	{
		return repository.UpdateCategory(categoryId, _ => new Category
		{
			Name = newName
		});
	}

	public bool SetCategoryColor(long categoryId, string hexColor)
	{
		return repository.UpdateCategory(categoryId, _ => new Category
		{
			Color = hexColor
		});
	}

	public void SetCurrentStorage(string storage)
	{
		CurrentStorage = storage;
	}
}

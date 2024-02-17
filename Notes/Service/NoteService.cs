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
		var count = repository.GetAll().Count;
		var newCategory = new Category()
		{
			Name = string.Empty,
			Notes = [],
			Order = count,
			CreationDate = DateTime.Now
		};

		if (!repository.CreateCategory(newCategory))
			throw new Exception("Failed to create category.");

		return newCategory;
	}

	public Note CreateNote(long categoryId)
	{
		var count = repository.GetCategory(categoryId).Notes.Count;
		var newNote = new Note()
		{
			Text = string.Empty,
			CategoryId = categoryId,
			Storage = CurrentStorage,
			Order = count,
			CreationDate = DateTime.Now
		};

		if (!repository.CreateNote(newNote))
			throw new Exception("Failed to create note.");

		return newNote;
	}

	public IEnumerable<Category> GetAll()
	{
		var categories = repository.GetAll();
		for (var i = 0; i < categories.Count; i++)
		{
			if (categories[i].Notes.Count == 0)
				continue;

			categories[i].Notes = categories[i].Notes.FindAll(note => note.Storage == CurrentStorage);
			if (categories[i].Notes.Count == 0)
				categories.Remove(categories[i--]);
		}

		return categories;
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
		var result = true;
		var category = repository.GetCategory(categoryId);
		if (category.Order == newPosition)
			return true;

		var categories = repository.GetAll();
		if (newPosition > category.Order)
		{
			categories = categories.Where(x => x.Order > category.Order && x.Order <= newPosition).ToList();
			foreach (var cat in categories)
				result &= repository.UpdateCategory(cat.Id, _ => new Category { Order = cat.Order - 1 });
		}
		else
		{
			categories = categories.Where(x => x.Order >= newPosition && x.Order < category.Order).ToList();
			foreach (var cat in categories)
				result &= repository.UpdateCategory(cat.Id, _ => new Category { Order = cat.Order + 1 });
		}

		result &= repository.UpdateCategory(category.Id, _ => new Category { Order = newPosition });

		return result;
	}

	public bool ReorderNote(long noteId, int newPosition)
	{
		var result = true;
		var note = repository.GetNote(noteId);
		if (note.Order == newPosition)
			return true;

		var categoryNotes = repository.GetCategory(note.CategoryId).Notes;
		if (newPosition > note.Order)
		{
			categoryNotes = categoryNotes.Where(x => x.Order > note.Order && x.Order <= newPosition).ToList();
			foreach (var categoryNote in categoryNotes)
				result &= repository.UpdateNote(categoryNote.Id, _ => new Note { Order = categoryNote.Order - 1 });
		}
		else
		{
			categoryNotes = categoryNotes.Where(x => x.Order >= newPosition && x.Order < note.Order).ToList();
			foreach (var categoryNote in categoryNotes)
				result &= repository.UpdateNote(categoryNote.Id, _ => new Note { Order = categoryNote.Order + 1 });
		}

		result &= repository.UpdateNote(note.Id, _ => new Note { Order = newPosition });

		return result;
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

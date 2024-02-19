using skroy.Notes.Entity;
using skroy.Notes.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace skroy.Notes.Service;

public class NoteService
{
	private readonly NoteRepository repository;

	private long CurrentStorageId { get; set; }


	public NoteService()
	{
		repository = new NoteRepository();
	}


	public Category CreateCategory()
	{
		var count = repository.GetAll().Count();
		var newCategory = new Category
		{
			Name = string.Empty,
			Notes = [],
			Order = count,
			CreationDate = DateTime.Now
		};

		if (!repository.CreateCategory(newCategory))
			return null;

		return newCategory;
	}

	public Note CreateNote(long categoryId)
	{
		var count = repository.GetCategory(categoryId).Notes.Count;
		var newNote = new Note
		{
			Text = string.Empty,
			CategoryId = categoryId,
			StorageId = CurrentStorageId,
			Order = count,
			CreationDate = DateTime.Now
		};

		if (!repository.CreateNote(newNote))
			return null;

		return newNote;
	}

	public Storage CreateStorage(string name)
	{
		var newStorage = new Storage
		{
			Name = name
		};

		if (!repository.CreateStorage(newStorage))
			return null;

		return newStorage;
	}

	public IEnumerable<Category> GetAll()
	{
		var categories = repository.GetAll().ToList();
		for (var i = 0; i < categories.Count; i++)
		{
			if (categories[i].Notes.Count == 0)
				continue;

			categories[i].Notes = categories[i].Notes.FindAll(note => note.StorageId == CurrentStorageId);
			if (categories[i].Notes.Count == 0)
				categories.Remove(categories[i--]);
		}

		return categories;
	}

	public IEnumerable<Storage> GetAllStorages()
	{
		return repository.GetAllStorages();
	}

	public IEnumerable<Category> Search(string query)
	{
		return repository.GetAll()
			.Select(x =>
			{
				x.Notes = x.Notes.FindAll(note => note.Text.Contains(query, StringComparison.InvariantCultureIgnoreCase));
				return x;
			})
			.Where(x => x.Notes.Count > 0);
	}

	public bool DeleteCategory(long categoryId)
	{
		var result = true;
		var category = repository.GetCategory(categoryId);
		var currentStorageNotes = category.Notes.Where(x => x.StorageId == CurrentStorageId).ToList();
		foreach (var note in currentStorageNotes)
			result &= repository.DeleteNote(note.Id);

		if (result && category.Notes.Count == currentStorageNotes.Count)
			result &= repository.DeleteCategory(categoryId);

		if (!result)
			return false;

		foreach (var cat in repository.GetAll().Where(x => x.Order > category.Order))
			result &= repository.UpdateCategory(cat.Id, _ => new Category { Order = cat.Order - 1 });

		return result;
	}

	public bool DeleteNote(long noteId)
	{
		var result = true;
		var note = repository.GetNote(noteId);
		var noteCategory = repository.GetCategory(note.CategoryId);

		result &= repository.DeleteNote(noteId);
		if (!result)
			return false;

		foreach (var otherNote in noteCategory.Notes.Where(x => x.Order > note.Order))
			result &= repository.UpdateNote(otherNote.Id, _ => new Note { Order = otherNote.Order - 1 });

		return result;
	}

	public bool ReorderCategory(long categoryId, int newPosition)
	{
		var result = true;
		var category = repository.GetCategory(categoryId);
		var categories = repository.GetAll().ToList();
		var newGlobalPosition = categories
			.Where(x => x.Notes.Count == 0 || x.Notes.Any(n => n.StorageId == CurrentStorageId))
			.OrderBy(x => x.Order)
			.ElementAt(newPosition)
			.Order;
		if (category.Order == newGlobalPosition)
			return true;

		if (newGlobalPosition > category.Order)
		{
			categories = categories.Where(x => x.Order > category.Order && x.Order <= newGlobalPosition).ToList();
			foreach (var cat in categories)
				result &= repository.UpdateCategory(cat.Id, _ => new Category { Order = cat.Order - 1 });
		}
		else
		{
			categories = categories.Where(x => x.Order >= newGlobalPosition && x.Order < category.Order).ToList();
			foreach (var cat in categories)
				result &= repository.UpdateCategory(cat.Id, _ => new Category { Order = cat.Order + 1 });
		}

		result &= repository.UpdateCategory(category.Id, _ => new Category { Order = newGlobalPosition });

		return result;
	}

	public bool ReorderNote(long noteId, int newPosition)
	{
		var result = true;
		var note = repository.GetNote(noteId);
		if (note.Order == newPosition)
			return true;

		var categoryNotes = repository.GetCategory(note.CategoryId).Notes.Where(x => x.StorageId == CurrentStorageId);
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
		var result = true;
		var note = repository.GetNote(noteId);
		var oldCategory = repository.GetCategory(note.CategoryId);
		var oldOrder = note.Order;
		var newOrder = repository.GetCategory(toCategoryId).Notes.Count(x => x.StorageId == CurrentStorageId);

		result &= repository.UpdateNote(noteId, _ => new Note
		{
			CategoryId = toCategoryId,
			Order = newOrder
		});

		foreach (var otherNote in oldCategory.Notes.Where(x => x.StorageId == CurrentStorageId && x.Order > oldOrder))
			result &= repository.UpdateNote(otherNote.Id, _ => new Note { Order = otherNote.Order - 1 });

		return result;
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
		var result = true;
		var targetStorageId = GetStorageIdByName(targetStorage);
		var note = repository.GetNote(noteId);
		var category = repository.GetCategory(note.CategoryId);
		var oldOrder = note.Order;
		var newOrder = category.Notes.Count(x => x.StorageId == targetStorageId);

		result &= repository.UpdateNote(noteId, _ => new Note
		{
			StorageId = targetStorageId,
			Order = newOrder
		});

		foreach (var otherNote in category.Notes.Where(x => x.StorageId == CurrentStorageId && x.Order > oldOrder))
			result &= repository.UpdateNote(otherNote.Id, _ => new Note { Order = otherNote.Order - 1 });

		return result;
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

	public void SetCurrentStorage(string storageName)
	{
		CurrentStorageId = GetStorageIdByName(storageName);
	}
	

	private long GetStorageIdByName(string storageName)
	{
		return repository.GetAllStorages().Single(x => x.Name == storageName).Id;
	}
}

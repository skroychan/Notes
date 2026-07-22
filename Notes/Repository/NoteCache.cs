using skroy.Notes.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace skroy.Notes.Repository;

internal class NoteCache
{
	private Dictionary<long, Category> CategoriesCache { get; set; }
	private Dictionary<long, Note> NotesCache { get; set; }
	private Dictionary<long, Storage> StoragesCache { get; set; }


	public NoteCache(IEnumerable<Category> categories, IEnumerable<Note> notes, IEnumerable<Storage> storages)
	{
		NotesCache = notes.ToDictionary(x => x.Id, x => x);
		CategoriesCache = categories.ToDictionary(x => x.Id, x => x);
		StoragesCache = storages.ToDictionary(x => x.Id, x => x);
		foreach (var category in CategoriesCache.Values)
			category.Notes = [];
		foreach (var note in NotesCache.Values)
			CategoriesCache[note.CategoryId].Notes.Add(note);
	}


	public IEnumerable<Category> GetAll()
	{
		return CategoriesCache.Values.Select(GetCopy);
	}

	public IEnumerable<Storage> GetStorages()
	{
		return StoragesCache.Values.Select(GetCopy);
	}

	public Category GetCategory(long categoryId)
	{
		if (CategoriesCache.TryGetValue(categoryId, out var category))
			return GetCopy(category);

		return null;
	}

	public Note GetNote(long noteId)
	{
		if (NotesCache.TryGetValue(noteId, out var note))
			return GetCopy(note);

		return null;
	}

	public void AddNote(Note note)
	{
		if (!CategoriesCache.TryGetValue(note.CategoryId, out Category category))
			throw new ArgumentException($"Note with Id={note.Id} references category with Id={note.CategoryId} which does not exist.");

		var newNote = GetCopy(note);
		NotesCache[note.Id] = newNote;
		category.Notes.Add(newNote);
	}

	public void AddCategory(Category category)
	{
		CategoriesCache[category.Id] = GetCopy(category);
	}

	public void AddStorage(Storage storage)
	{
		StoragesCache[storage.Id] = GetCopy(storage);
	}

	public void UpdateNote(Note note)
	{
		if (!NotesCache.TryGetValue(note.Id, out var oldNote))
			throw new ArgumentException($"Cannot find note with Id={note.Id}.");

		var oldCategory = CategoriesCache[oldNote.CategoryId];
		var newNote = GetCopy(note);
		var oldNoteIndex = oldCategory.Notes.FindIndex(x => x.Id == oldNote.Id);
		if (note.CategoryId == oldNote.CategoryId)
			oldCategory.Notes[oldNoteIndex] = newNote;
		else
		{
			if (!CategoriesCache.TryGetValue(note.CategoryId, out Category newCategory))
				throw new ArgumentException($"Note with Id={note.Id} references category with Id={note.CategoryId} which does not exist.");

			oldCategory.Notes.RemoveAt(oldNoteIndex);
			newCategory.Notes.Add(newNote);
		}

		NotesCache[note.Id] = newNote;
	}

	public void UpdateCategory(Category category)
	{
		if (!CategoriesCache.ContainsKey(category.Id))
			throw new ArgumentException($"Cannot find category with Id={category.Id}.");

		CategoriesCache[category.Id] = GetCopy(category);
	}

	public void DeleteNote(long noteId)
	{
		if (!NotesCache.TryGetValue(noteId, out var note))
			throw new ArgumentException($"Cannot find note with Id={noteId}.");
		
		var noteIndex = CategoriesCache[note.CategoryId].Notes.FindIndex(x => x.Id == note.Id);
		if (noteIndex == -1)
			throw new Exception($"Failed to remove note with Id={note.Id} from category with Id={note.CategoryId}.");
		CategoriesCache[note.CategoryId].Notes.RemoveAt(noteIndex);
		
		if (!NotesCache.Remove(noteId))
			throw new Exception($"Failed to remove note with Id={noteId}.");
	}

	public void DeleteCategory(long categoryId)
	{
		if (!CategoriesCache.TryGetValue(categoryId, out var category))
			throw new ArgumentException($"Cannot find category with Id={categoryId}.");
		
		if (category.Notes.Count != 0)
			throw new ArgumentException($"Category with Id={categoryId} has notes.");

		if (!CategoriesCache.Remove(categoryId))
			throw new Exception($"Failed to remove category with Id={categoryId}.");
	}


	private Note GetCopy(Note note)
	{
		return new Note
		{
			Id = note.Id,
			Text = note.Text,
			Color = note.Color,
			CategoryId = note.CategoryId,
			StorageId = note.StorageId,
			Order = note.Order,
			CreationDate = note.CreationDate,
			ModificationDate = note.ModificationDate,
			ArchiveDate = note.ArchiveDate
		};
	}

	private Category GetCopy(Category category)
	{
		return new Category
		{
			Id = category.Id,
			Name = category.Name,
			Notes = category.Notes.Select(GetCopy).ToList(),
			Color = category.Color,
			Order = category.Order,
			CreationDate = category.CreationDate
		};
	}

	private Storage GetCopy(Storage storage)
	{
		return new Storage
		{
			Id = storage.Id,
			Name = storage.Name
		};
	}
}

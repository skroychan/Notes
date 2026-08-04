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
		return CategoriesCache.Values;
	}

	public IEnumerable<Storage> GetStorages()
	{
		return StoragesCache.Values;
	}

	public Category GetCategory(long categoryId)
	{
		return CategoriesCache.GetValueOrDefault(categoryId);
	}

	public Note GetNote(long noteId)
	{
		return NotesCache.GetValueOrDefault(noteId);
	}

	public void AddNote(Note note)
	{
		if (!CategoriesCache.TryGetValue(note.CategoryId, out Category category))
			throw new ArgumentException($"Note with Id={note.Id} references category with Id={note.CategoryId} which does not exist.");

		NotesCache[note.Id] = note;
		category.Notes.Add(note);
	}

	public void AddCategory(Category category)
	{
		CategoriesCache[category.Id] = category;
	}

	public void AddStorage(Storage storage)
	{
		StoragesCache[storage.Id] = storage;
	}

	public void UpdateNote(Note note, long categoryId)
	{
		if (!NotesCache.TryGetValue(note.Id, out var oldNote))
			throw new ArgumentException($"Cannot find note with Id={note.Id}.");

		if (!CategoriesCache.TryGetValue(note.CategoryId, out var newCategory))
			throw new ArgumentException($"Note with Id={note.Id} references category with Id={note.CategoryId} which does not exist.");

		if (note.CategoryId == categoryId)
			return;

		var oldCategory = CategoriesCache[categoryId];
		var oldNoteIndex = oldCategory.Notes.FindIndex(x => x.Id == oldNote.Id);
		oldCategory.Notes.RemoveAt(oldNoteIndex);
		newCategory.Notes.Add(note);
	}

	public void UpdateCategory(Category category)
	{
		if (!CategoriesCache.ContainsKey(category.Id))
			throw new ArgumentException($"Cannot find category with Id={category.Id}.");
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
}

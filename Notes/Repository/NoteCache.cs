using skroy.Notes.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace skroy.Notes.Repository;

internal class NoteCache
{
	private Dictionary<long, Category> CategoriesCache { get; set; }
	private Dictionary<long, Note> NotesCache { get; set; }


	public NoteCache(IEnumerable<Category> categories, IEnumerable<Note> notes)
	{
		NotesCache = notes.ToDictionary(x => x.Id, x => x);
		CategoriesCache = categories.ToDictionary(x => x.Id, x => x);
		foreach (var category in CategoriesCache.Values)
			category.Notes = [];
		foreach (var note in NotesCache.Values)
			CategoriesCache[note.CategoryId].Notes.Add(note);
	}


	public IEnumerable<Category> GetAll()
	{
		return CategoriesCache.Values.Select(GetCopy);
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

	public void UpdateNote(Note note)
	{
		if (!CategoriesCache.TryGetValue(note.CategoryId, out Category category))
			throw new ArgumentException($"Note with Id={note.Id} references category with Id={note.CategoryId} which does not exist.");

		if (!NotesCache.TryGetValue(note.Id, out var oldNote))
			throw new ArgumentException($"Cannot find note with Id={note.Id}.");

		var newNote = GetCopy(note);
		category.Notes[category.Notes.IndexOf(oldNote)] = newNote;
		if (note.CategoryId != oldNote.CategoryId)
		{
			CategoriesCache[oldNote.CategoryId].Notes.Remove(oldNote);
			category.Notes.Add(newNote);
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

		CategoriesCache[note.CategoryId].Notes.Remove(note);

		NotesCache.Remove(noteId);
	}

	public void DeleteCategory(long categoryId)
	{
		if (!CategoriesCache.TryGetValue(categoryId, out var category))
			throw new ArgumentException($"Cannot find category with Id={categoryId}.");

		var notesToDelete = NotesCache.Where(x => x.Value.CategoryId == category.Id);
		foreach (var note in notesToDelete)
			NotesCache.Remove(note.Key);

		CategoriesCache.Remove(categoryId);
	}


	private Note GetCopy(Note note)
	{
		return new Note
		{
			Id = note.Id,
			Text = note.Text,
			Color = note.Color,
			CategoryId = note.CategoryId,
			Storage = note.Storage,
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
			CreationDate = category.CreationDate
		};
	}
}

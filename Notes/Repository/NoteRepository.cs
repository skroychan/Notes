using Notes.Entity;
using skroy.ORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Notes.Repository;

public class NoteRepository
{
	private readonly Database database;

	private Dictionary<long, Category> CategoriesCache { get; set; }
	private Dictionary<long, Note> NotesCache { get; set; }


	public NoteRepository()
	{
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var dbPath = Path.Combine(appDataPath, "skroy", "Notes", "notes.db");
		database = Database.GetSqliteDatabase($"Data Source={dbPath};");

		var categoryMappingBuilder = database.GetMappingBuilder<Category>()
			.Ignore(x => x.Notes);
		var noteMappingBuilder = database.GetMappingBuilder<Note>();

		database.AddMapping(categoryMappingBuilder);
		database.AddMapping(noteMappingBuilder);

		database.Initialize();

		NotesCache = database.Select<Note>().ToDictionary(x => x.Id, x => x);
		CategoriesCache = database.Select<Category>().ToDictionary(x => x.Id, x => x);
		foreach (var category in CategoriesCache.Values)
			category.Notes = new List<Note>();
		foreach (var note in NotesCache.Values)
			CategoriesCache[note.CategoryId].Notes.Add(note);
	}


	public bool CreateNote(Note note)
	{
		if (!CategoriesCache.ContainsKey(note.CategoryId))
			return false;

		var id = database.Insert(note);
		note.Id = (long)id;

		NotesCache[note.Id] = note;
		CategoriesCache[note.CategoryId].Notes.Add(note);

		return true;
	}

	public bool CreateCategory(Category category)
	{
		var id = database.Insert(category);
		category.Id = (long)id;

		CategoriesCache[category.Id] = category;

		return true;
	}

	public List<Category> GetAll()
	{
		return [.. CategoriesCache.Values];
	}

	public Category GetCategory(long categoryId)
	{
		return CategoriesCache[categoryId];
	}

	public Note GetNote(long noteId)
	{
		return NotesCache[noteId];
	}

	public bool UpdateNote(Note note)
	{
		if (!CategoriesCache.TryGetValue(note.CategoryId, out var category))
			return false;

		var affectedRows = database.Update(note);
		if (affectedRows != 1)
			return false;

		var oldCategoryId = NotesCache[note.Id].CategoryId;
		if (oldCategoryId != note.CategoryId)
		{
			CategoriesCache[oldCategoryId].Notes.Remove(note);
			category.Notes.Add(note);
		}

		NotesCache[note.Id] = note;

		return true;
	}

	public bool UpdateNote(long noteId, Expression<Action<Note>> updater)
	{
		var note = GetNote(noteId);
		var oldCategoryId = note.CategoryId;

		updater.Compile()(note);

		if (!CategoriesCache.TryGetValue(note.CategoryId, out var category))
			return false;

		var affectedRows = database.Update(note, updater);
		if (affectedRows != 1)
			return false;

		if (oldCategoryId != note.CategoryId)
		{
			CategoriesCache[oldCategoryId].Notes.RemoveAll(x => x.Id == noteId);
			category.Notes.Add(note);
		}

		NotesCache[note.Id] = note;

		return true;
	}

	public bool UpdateCategory(Category category)
	{
		var affectedRows = database.Update(category);

		CategoriesCache[category.Id] = category;

		return affectedRows == 1;
	}

	public bool UpdateCategory(long categoryId, Expression<Action<Category>> updater)
	{
		var category = GetCategory(categoryId);
		var affectedRows = database.Update(category, updater);

		updater.Compile()(category);

		CategoriesCache[category.Id] = category;

		return affectedRows == 1;
	}

	public bool DeleteNote(long noteId)
	{
		var affectedRows = database.Delete<Note>(x => x.Id == noteId);

		NotesCache.Remove(noteId);

		return affectedRows == 1;
	}

	public bool DeleteCategory(long categoryId)
	{
		var category = CategoriesCache[categoryId];
		var keysToDelete = NotesCache
			.Where(x => x.Value.CategoryId == category.Id)
			.Select(x => x.Key)
			.ToList();
		foreach (var key in keysToDelete)
			NotesCache.Remove(key);

		var affectedRows = database.Delete<Note>(x => x.CategoryId == category.Id);
		if (affectedRows != keysToDelete.Count)
			return false;

		CategoriesCache.Remove(categoryId);

		return database.Delete(category) == 1;
	}
}

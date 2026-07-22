using skroy.Notes.Entity;
using skroy.ORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace skroy.Notes.Repository;

internal class NoteRepository
{
	private readonly Database database;
	private readonly string dbPath;
	private readonly NoteCache cache;


	public NoteRepository()
	{
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		dbPath = Path.Combine(appDataPath, "skroy", "Notes", "notes.db");

		if (!File.Exists(dbPath))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
			File.Create(dbPath).Dispose();
		}

		database = Database.GetSqliteDatabase($"Data Source={dbPath};");
		
		var categoryBuilder = Database.GetMappingBuilder<Category>().Ignore(x => x.Notes);
		database.AddMapping(categoryBuilder);
		database.AddMapping<Note>();
		database.AddMapping<Storage>();

		database.Initialize();

		cache = new NoteCache(database.Select<Category>(), database.Select<Note>(), database.Select<Storage>());		
	}


	public bool CreateNote(Note note)
	{
		if (cache.GetCategory(note.CategoryId) == null)
			return false;

		note.Id = (long)database.Insert(note);

		cache.AddNote(note);

		return true;
	}

	public bool CreateCategory(Category category)
	{
		category.Id = (long)database.Insert(category);

		cache.AddCategory(category);

		return true;
	}

	public bool CreateStorage(Storage storage)
	{
		storage.Id = (long)database.Insert(storage);

		cache.AddStorage(storage);

		return true;
	}

	public IEnumerable<Category> GetAll()
	{
		return cache.GetAll().ToList();
	}

	public IEnumerable<Storage> GetAllStorages()
	{
		return cache.GetStorages();
	}

	public Category GetCategory(long categoryId)
	{
		return cache.GetCategory(categoryId);
	}

	public Note GetNote(long noteId)
	{
		return cache.GetNote(noteId);
	}

	public bool UpdateNote(long noteId, Expression<Action<Note>> updater)
	{
		var note = cache.GetNote(noteId);
		ApplyUpdater(note, updater);

		if (cache.GetCategory(note.CategoryId) == null)
			return false;

		var affectedRows = database.Update(note, updater);
		if (affectedRows != 1)
			return false;

		cache.UpdateNote(note);

		return true;
	}

	public bool UpdateCategory(long categoryId, Expression<Action<Category>> updater)
	{
		var category = GetCategory(categoryId);
		ApplyUpdater(category, updater);

		var affectedRows = database.Update(category, updater);
		if (affectedRows != 1)
			return false;

		cache.UpdateCategory(category);

		return true;
	}

	public bool DeleteNote(long noteId)
	{
		var affectedRows = database.Delete<Note>(x => x.Id == noteId);
		if (affectedRows != 1)
			return false;

		cache.DeleteNote(noteId);

		return true;
	}

	public bool DeleteCategory(long categoryId)
	{
		var affectedRows = database.Delete<Category>(x => x.Id == categoryId);
		if (affectedRows != 1)
			return false;

		cache.DeleteCategory(categoryId);

		return true;
	}

	public void Backup()
		=> File.Copy(dbPath, $"{dbPath}.backup", true);


	private static void ApplyUpdater<T>(T obj, Expression<Action<T>> updater)
	{
		foreach (var binding in ((MemberInitExpression)updater.Body).Bindings)
		{
			var assignment = ((MemberAssignment)binding).Expression;
			var member = binding.Member.Name;
			var value = Expression.Lambda(assignment).Compile().DynamicInvoke();
			typeof(T).GetProperty(member).SetValue(obj, value);
		}
	}
}

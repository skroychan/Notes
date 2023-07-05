using Notes.Entities;
using Notes.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Notes;

	public class NoteManager
	{
		private readonly ArchiveManager Archive;
		private readonly string ResourcesPath;
		private readonly string NotesFilename;

		private List<Category> Categories { get; set; }
		private Dictionary<long, Category> CategoriesDictionary { get; set; }
		private long MaxNoteID { get; set; }
		private long MaxCategoryID { get; set; }


		public NoteManager()
		{
			ResourcesPath = @"C:\notes\";
			NotesFilename = "notes.json";

			Archive = new ArchiveManager(ResourcesPath);

			Load();
		}


		public Category CreateCategory(string name)
		{
			var newCategory = new Category() { ID = ++MaxCategoryID, Name = name, Notes = new List<Note>(), CreationDate = DateTime.Now };
			Categories.Add(newCategory);
			CategoriesDictionary[newCategory.ID] = newCategory;

			return newCategory;
		}

		public Note CreateNote(long categoryID)
		{
			if (CategoriesDictionary.TryGetValue(categoryID, out var category))
			{
				var newNote = new Note() { ID = ++MaxNoteID, Text = string.Empty, CreationDate = DateTime.Now };
				category.Notes.Add(newNote);

				return newNote;
			}

			return null;
		}

		public IEnumerable<Category> GetAll()
		{
			return Categories;
		}

		public IEnumerable<Note> GetByCategory(long categoryID)
		{
			return CategoriesDictionary.TryGetValue(categoryID, out var category)
				? category.Notes
				: new List<Note>();
		}

		public IEnumerable<Category> Find(string query, bool ignoreCase = false)
		{
			return Categories
			.ToDictionary(k => k.ID, v => v)
			.Select(i => new Category()
			{
				ID = i.Key,
				Color = i.Value.Color,
				Name = i.Value.Name,
				Notes = i.Value.Notes.FindAll(note => note.Text.IndexOf(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : default) >= 0)
			})
			.Where(cat => cat.Notes.Count > 0);
		}

		public IEnumerable<Note> FindInCategory(long categoryID, string query)
		{
			return CategoriesDictionary.TryGetValue(categoryID, out var category)
				? category.Notes.FindAll(note => note.Text.Contains(query))
				: new List<Note>();
		}

		public bool DeleteCategory(long categoryID)
		{
		if (!CategoriesDictionary.TryGetValue(categoryID, out var category) || !Categories.Remove(category))
			return false;

		if (categoryID == MaxCategoryID)
			SetMaxCategoryID();

		return true;
		}

		public bool DeleteNote(Note note, long categoryID)
		{
		if (!CategoriesDictionary.TryGetValue(categoryID, out var category) || !category.Notes.Remove(note))
			return false;

		if (note.ID == MaxNoteID)
			SetMaxNoteID();

		return true;
		}

		public bool MoveNote(Note note, long oldCategoryID, long newCategoryID)
		{
			if (CategoriesDictionary.TryGetValue(oldCategoryID, out var oldCategory) && CategoriesDictionary.TryGetValue(newCategoryID, out var newCategory))
			{
				oldCategory.Notes.Remove(note);
				newCategory.Notes.Add(note);

				return true;
			}

			return false;
		}

		public bool ReorderCategory(long categoryID, int newPosition)
		{
			if (CategoriesDictionary.TryGetValue(categoryID, out var category))
			{
				Categories.Remove(category);
				Categories.Insert(newPosition, category);

				return true;
			}

			return false;
		}

		public bool ReorderNote(Note note, long categoryID, int newPosition)
		{
			if (CategoriesDictionary.TryGetValue(categoryID, out var category))
			{
				category.Notes.Remove(note);
				category.Notes.Insert(newPosition, note);

				return true;
			}

			return false;
		}

		public bool ArchiveNote(Note note, long categoryID)
		{
			if (CategoriesDictionary.TryGetValue(categoryID, out var category))
			{
				Archive.ArchiveNote(note, category);
				return true;
			}

			return false;
		}

		public void Merge(IEnumerable<Category> categories)
		{
			var archive = Archive.GetAll();
			var existingCategoryIDs = new HashSet<long>();
			var existingNoteIDs = new HashSet<long>();

			existingCategoryIDs.UnionWith(archive.Select(category => category.ID));
			existingCategoryIDs.UnionWith(Categories.Select(category => category.ID));
			existingNoteIDs.UnionWith(archive.SelectMany(category => category.Notes.Select(note => note.ID)));
			existingNoteIDs.UnionWith(Categories.SelectMany(category => category.Notes.Select(note => note.ID)));

			foreach (var category in categories)
			{
				while (existingCategoryIDs.Contains(category.ID))
					category.ID++;

				foreach (var note in category.Notes)
				{
					while (existingNoteIDs.Contains(note.ID))
						note.ID++;
				}
			}

			Categories.AddRange(categories);
			Save();
		}

		public void Save(IEnumerable<Category> categories = null)
		{
			if (categories != null)
			{
				Categories = categories.ToList();
				CategoriesDictionary = Categories.ToDictionary(k => k.ID, v => v);
				MaxNoteID = Categories.SelectMany(cat => cat.Notes).Max(note => note.ID);
				MaxCategoryID = Categories.Max(cat => cat.ID);
			}

			Json.WriteJson(ResourcesPath + NotesFilename, Categories);
		}


		private void Load()
		{
			var json = Json.ReadJson<List<Category>>(ResourcesPath + NotesFilename);

			if (json == null || !json.Any())
			{
				Categories = new List<Category>();
				CategoriesDictionary = new Dictionary<long, Category>();
			}
			else
			{
				Categories = json;
				CategoriesDictionary = Categories.ToDictionary(k => k.ID, v => v);
			SetMaxNoteID();
			SetMaxCategoryID();
		}
	}

	private void SetMaxNoteID()
	{
				MaxNoteID = Categories.SelectMany(cat => cat.Notes).Max(note => note.ID);
	}

	private void SetMaxCategoryID()
	{
				MaxCategoryID = Categories.Max(cat => cat.ID);
			}
		}

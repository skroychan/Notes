using Notes.Entity;
using Notes.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Notes.Service;

public class NoteService
{
    private readonly ArchiveService archiveService;
    private readonly string resourcesPath;

    private List<Category> Categories { get; set; }
    private Dictionary<long, Category> CategoriesCache { get; set; }
    private Dictionary<long, (Note note, long categoryId)> NotesCache { get; set; }
    private long MaxNoteId { get; set; }
    private long MaxCategoryId { get; set; }


    public NoteService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        resourcesPath = Path.Combine(appDataPath, "skroy", "Notes", "notes.json");

        archiveService = new ArchiveService(Path.Combine(appDataPath, "skroy", "Notes", "archive.json"));

        Load();
    }


    public Category CreateCategory(string name)
    {
        var newCategory = new Category()
        {
            Id = ++MaxCategoryId,
            Name = name,
            Notes = new List<Note>(),
            CreationDate = DateTime.Now
        };

        Categories.Add(newCategory);
        CategoriesCache[newCategory.Id] = newCategory;

        return newCategory;
    }

    public Note CreateNote(long categoryId)
    {
        if (!CategoriesCache.TryGetValue(categoryId, out var category))
            throw new ArgumentException($"Category with id={categoryId} doesn't exist");

        var newNote = new Note()
        {
            Id = ++MaxNoteId,
            Text = string.Empty,
            CreationDate = DateTime.Now
        };

        category.Notes.Add(newNote);
        NotesCache[newNote.Id] = (newNote, category.Id);

        return newNote;
    }

    public IEnumerable<Category> GetAll()
    {
        return Categories;
    }

    public IEnumerable<Category> Search(string query, bool ignoreCase = false)
    {
        return CategoriesCache
            .Select(i => new Category()
            {
                Id = i.Key,
                Color = i.Value.Color,
                Name = i.Value.Name,
                Notes = i.Value.Notes.FindAll(note => note.Text.Contains(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : default))
            })
            .Where(cat => cat.Notes.Count > 0);
    }

    public bool DeleteCategory(long categoryId)
    {
        if (!CategoriesCache.TryGetValue(categoryId, out var category) || !Categories.Remove(category))
            return false;

        UpdateCache();

        if (categoryId == MaxCategoryId)
            SetMaxCategoryId();

        return true;
    }

    public bool DeleteNote(long noteId)
    {
        if (!NotesCache.TryGetValue(noteId, out var note)
            || !CategoriesCache.TryGetValue(note.categoryId, out var category)
            || !category.Notes.Remove(note.note))
            return false;

        NotesCache.Remove(note.note.Id);

        if (noteId == MaxNoteId)
            SetMaxNoteID();

        return true;
    }

    public bool ChangeNoteCategory(long noteId, long toCategoryId)
    {
        if (!NotesCache.TryGetValue(noteId, out var note)
            || !CategoriesCache.TryGetValue(note.categoryId, out var oldCategory)
            || !CategoriesCache.TryGetValue(toCategoryId, out var newCategory)
            || !oldCategory.Notes.Remove(note.note))
            return false;

        newCategory.Notes.Add(note.note);
        note.categoryId = newCategory.Id;

        return true;
    }

    public bool MoveCategory(long categoryId, int newPosition)
    {
        if (!CategoriesCache.TryGetValue(categoryId, out var category))
            return false;
        
        Categories.Remove(category);
        Categories.Insert(newPosition, category);

        return true;
    }

    public bool MoveNote(long noteId, int newPosition)
    {
        if (!NotesCache.TryGetValue(noteId, out var note) || !CategoriesCache.TryGetValue(note.categoryId, out var category))
            return false;

        category.Notes.Remove(note.note);
        category.Notes.Insert(newPosition, note.note);

        return true;
    }

    public bool UpdateNoteText(long noteId, string newText)
    {
        if (!NotesCache.TryGetValue(noteId, out var note))
            return false;

        note.note.Text = newText;
        note.note.ModificationDate = DateTime.Now;

        return true;
    }

    public bool UpdateNoteColor(long noteId, string hexColor)
    {
        if (!NotesCache.TryGetValue(noteId, out var note))
            return false;

        note.note.Color = hexColor;

        return true;
    }

    public bool UpdateCategoryName(long categoryId, string newName)
    {
        if (!CategoriesCache.TryGetValue(categoryId, out var category))
            return false;

        category.Name = newName;

        return true;
    }

    public bool UpdateCategoryColor(long categoryId, string hexColor)
    {
        if (!CategoriesCache.TryGetValue(categoryId, out var category))
            return false;

        category.Color = hexColor;

        return true;
    }

    public bool ArchiveNote(long noteId)
    {
        if (!NotesCache.TryGetValue(noteId, out var note) || !CategoriesCache.TryGetValue(note.categoryId, out var category))
            return false;

        archiveService.ArchiveNote(note.note, category);
        DeleteNote(note.note.Id);

        return true;
    }

    public void Save()
    {
        Json.WriteJson(resourcesPath, Categories);
    }


    private void Load()
    {
        var json = Json.ReadJson<List<Category>>(resourcesPath);

        if (json == null || !json.Any())
        {
            Categories = new List<Category>();
            CategoriesCache = new Dictionary<long, Category>();
        }
        else
        {
            Categories = json;
            UpdateCache();
            SetMaxNoteID();
            SetMaxCategoryId();
        }
    }

    private void UpdateCache()
    {
        CategoriesCache = Categories.ToDictionary(k => k.Id, v => v);
        NotesCache = Categories.SelectMany(category => category.Notes.Select(note => (note, category.Id))).ToDictionary(k => k.note.Id, v => (v.note, v.Id));
    }

    private void SetMaxNoteID()
    {
        MaxNoteId = NotesCache.Keys.Count > 0 ? NotesCache.Keys.Max() : -1;
    }

    private void SetMaxCategoryId()
    {
        MaxCategoryId = Categories.Count > 0 ? Categories.Max(cat => cat.Id) : -1;
    }
}

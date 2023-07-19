using Notes.Entity;
using Notes.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Notes;

public class ArchiveService
{
	private readonly string resourcesPath;

	private List<Category> Archive { get; set; }
	private Dictionary<long, Category> ArchiveDictionary { get; set; }


	public ArchiveService(string resourcesPath)
	{
		this.resourcesPath = resourcesPath;

		Load();
	}


	public void ArchiveNote(Note note, Category category)
	{
		if (!ArchiveDictionary.TryGetValue(category.Id, out var archiveCategory))
			archiveCategory = CreateCategory(category);

		note.ArchiveDate = DateTime.Now;
		archiveCategory.Notes.Add(note);
		Save();
	}

	public List<Category> GetAll()
	{
		return Archive;
	}


	private Category CreateCategory(Category category)
	{
		var newCategory = new Category() { Id = category.Id, Name = category.Name, Notes = new List<Note>() };
		Archive.Add(newCategory);
		ArchiveDictionary[newCategory.Id] = newCategory;

		return newCategory;
	}

	private void Save()
	{
		Json.WriteJson(resourcesPath, Archive);
	}

	private void Load()
	{
		var json = Json.ReadJson<List<Category>>(resourcesPath);

		Archive = json ?? new List<Category>();
		ArchiveDictionary = Archive.ToDictionary(k => k.Id, v => v);
	}
}

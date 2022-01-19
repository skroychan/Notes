using Notes.Entities;
using Notes.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Notes
{
	public class ArchiveManager
	{
		private readonly string ResourcesPath;
		private readonly string ArchiveFilename;

		private List<Category> Archive { get; set; }
		private Dictionary<long, Category> ArchiveDictionary { get; set; }


		public ArchiveManager(string resourcesPath)
		{
			ResourcesPath = resourcesPath;
			ArchiveFilename = "archive.json";

			Load();
		}


		public void ArchiveNote(Note note, Category category)
		{
			if (!ArchiveDictionary.TryGetValue(category.ID, out var archiveCategory))
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
			var newCategory = new Category() { ID = category.ID, Name = category.Name, Notes = new List<Note>() };
			Archive.Add(newCategory);
			ArchiveDictionary[newCategory.ID] = newCategory;

			return newCategory;
		}

		private void Save()
		{
			Json.WriteJson(ResourcesPath + ArchiveFilename, Archive);
		}

		private void Load()
		{
			var json = Json.ReadJson<List<Category>>(ResourcesPath + ArchiveFilename);

			Archive = json ?? new List<Category>();
			ArchiveDictionary = Archive.ToDictionary(k => k.ID, v => v);
		}
	}
}

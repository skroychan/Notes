using Notes;
using Notes.Entities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace CalendarConverter
{
	public class Program
	{
		public static void Main(string[] args)
		{
			List<Note> notes;
			while (true)
			{
				Console.Write("Enter the .db file location: ");

				var path = Console.ReadLine();
				if (File.Exists(path))
				{
					try
					{
						notes = GetNotes(path);
						break;
					}
					catch (FormatException)
					{
						Console.WriteLine("Invalid date.");
					}
					catch (Exception)
					{
						Console.WriteLine("Invalid file.");
					}
				}
				else
				{
					Console.WriteLine("File doesn't exist.");
				}
			}

			if (notes == null)
			{
				Console.WriteLine("Something went wrong...");
				return;
			}

			var noteManager = new NoteManager();
			var category = new Category { Name = "UNSORTED", Notes = notes };
			noteManager.Merge(new Category[] { category });
		}


		private static List<Note> GetNotes(string path)
		{
			var result = new List<Note>();
			using (var connection = new SQLiteConnection($"Data Source={path}"))
			{
				connection.Open();

				var command = connection.CreateCommand();
				command.CommandText = @"select _id, title from Events";

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						result.Add(new Note
						{
							ID = long.Parse(reader["_id"].ToString()),
							Text = reader["title"].ToString()
						});
					}
				}
			}

			return result;
		}
	}
}

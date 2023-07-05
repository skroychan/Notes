using System.IO;
using System.Text.Json;

namespace Notes.Utils;

public static class Json
{
	public static T ReadJson<T>(string path)
	{
		return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
	}

	public static void WriteJson<T>(string path, T obj)
	{
		var jsonOptions = new JsonSerializerOptions() { WriteIndented = true };
		File.WriteAllText(path, JsonSerializer.Serialize(obj, jsonOptions));
	}
}

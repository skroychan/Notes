using System.IO;
using System.Text.Json;

namespace Notes.Util;

public static class Json
{
	public static T ReadJson<T>(string path)
	{
		if (File.Exists(path))
			return JsonSerializer.Deserialize<T>(File.ReadAllText(path));

		return default;
	}

	public static void WriteJson<T>(string path, T obj)
	{
        new FileInfo(path).Directory.Create();
		var jsonOptions = new JsonSerializerOptions() { WriteIndented = true };
		File.WriteAllText(path, JsonSerializer.Serialize(obj, jsonOptions));
	}
}

namespace skroy.NotesDesktop.Util;

internal static class StringExtensions
{
	public static string Truncate(this string source, int maxLength)
	{
		if (source == null)
			return null;

		return source.Length > maxLength ? source[..(maxLength - 3)] + "..." : source;
	}
}

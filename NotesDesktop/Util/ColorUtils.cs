using Avalonia.Media;

namespace skroy.NotesDesktop.Util;

internal static class ColorUtils
{
    public static string ToHexString(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

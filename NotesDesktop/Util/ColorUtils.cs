using System.Windows.Media;

namespace skroy.NotesDesktop.Util;

internal static class ColorUtils
{
    public static SolidColorBrush ToBrush(string hexColor)
    {
        if (hexColor == null)
            return null;

        return (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
    }

    public static SolidColorBrush ToBrush(System.Drawing.Color color)
    {
		return ToBrush(ToHexString(color));
    }

    public static string ToHexString(SolidColorBrush brush)
    {
        return $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    }

    public static string ToHexString(System.Drawing.Color color) 
    {
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}

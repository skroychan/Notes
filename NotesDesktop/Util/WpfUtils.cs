using System.Windows.Media;

namespace NotesDesktop.Util;

public static class WpfUtils
{
    public static SolidColorBrush ToBrush(string hexColor)
    {
        if (hexColor == null)
            return null;

        return (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
    }

    public static SolidColorBrush ToBrush(System.Drawing.Color color)
    {
        var colorHexString = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        return ToBrush(colorHexString);
    }

    public static string ToHexString(SolidColorBrush brush)
    {
        return $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    }
}

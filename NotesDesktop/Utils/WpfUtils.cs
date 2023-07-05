using System.Windows;
using System.Windows.Media;

namespace NotesDesktop.Utils;

public static class WpfUtils
{
    public static SolidColorBrush ToBrush(string color)
    {
        if (color == null)
            return SystemColors.ControlBrush;

        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(color);
        brush.Opacity = 0.3;
        return brush;
    }
}

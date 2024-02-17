using System.Windows;
using System.Windows.Media;

namespace skroy.NotesDesktop.Util;

internal static class ControlExtensions
{
	public static T FindChild<T>(this DependencyObject parent) where T : DependencyObject
	{
		T result = null;
		var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < childrenCount; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T tChild)
			{
				result = tChild;
				break;
			}
			else
			{
				result = FindChild<T>(child);
				if (result != null)
					break;
			}
		}

		return result;
	}
}

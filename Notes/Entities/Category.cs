using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Notes.Entities
{
	public class Category
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public List<Note> Notes { get; set; }
		public string Color { get; set; }
		public DateTime? CreationDate { get; set; }

		[JsonIgnore]
		public SolidColorBrush Brush
		{
			get
			{
				if (Color == null)
					return SystemColors.ControlBrush;

				var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(Color);
				brush.Opacity = 0.3;
				return brush;
			}
		}
	}
}

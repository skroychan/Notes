using System;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Notes.Entities
{
	public class Note
	{
		public long ID { get; set; }
		public string Text { get; set; }
		public string Color { get; set; }
		public DateTime? CreationDate { get; set; }
		public DateTime? ArchiveDate { get; set; }

		[JsonIgnore]
		public SolidColorBrush Brush
		{
			get
			{
				if (Color == null)
					return null;

				var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(Color);
				brush.Opacity = 0.3;
				return brush;
			}
		}
	}
}

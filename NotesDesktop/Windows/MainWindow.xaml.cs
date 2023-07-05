using Notes;
using Notes.Entities;
using NotesDesktop.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotesDesktop;

	public partial class MainWindow : Window
	{
		private NoteManager NoteManager { get; set; }
		private Category SelectedCategory { get; set; }
		private Note SelectedNote { get; set; }
		private bool IsSearch { get; set; }

		public List<Category> Categories { get; set; }


		public MainWindow()
		{
			NoteManager = new NoteManager();

			Categories = NoteManager.GetAll().ToList();

			InitializeComponent();

			SetTitle();
		}


		private void WindowClosing(object sender, CancelEventArgs e)
		{
		Save(false);
		}

		private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		if (!(e.Source is TabControl))
			return;

		SelectedCategory = (Category)TabControl.SelectedItem;
				UnselectNote();
		Save();
				SetCategoryNoteCount();
				SetCategoryColor();
			}

		private void SelectNote(object sender, KeyboardFocusChangedEventArgs e)
		{
			var item = (ListViewItem)sender;
			SelectedNote = (Note)item.Content;
			SelectedID.Content = SelectedNote.ID;
		MoveToButton.IsEnabled = SelectedNote != null;
			SetNoteColor();
		}

		private void UnselectNote()
		{
			SelectedNote = null;
			SelectedID.Content = string.Empty;
		MoveToButton.IsEnabled = false;
			SetNoteColor();
		}

		private void SetTitle()
		{
			Title = $"{Categories.SelectMany(c => c.Notes).Count()} notes in {Categories.Count} categories";
		}

		private void SetCategoryNoteCount()
		{
			if (SelectedCategory != null)
				NotesInCategory.Content = SelectedCategory.Notes.Count + " notes";
		}

		private void AddCategoryClick(object sender, RoutedEventArgs e)
		{
			var newCategory = NoteManager.CreateCategory("New");
			if (newCategory == null)
				return;

			var selectedIndex = Categories.IndexOf(SelectedCategory);
			if (selectedIndex <= Categories.Count - 1)
				NoteManager.ReorderCategory(newCategory.ID, selectedIndex + 1);

			Categories = NoteManager.GetAll().ToList();
			if (!IsSearch)
				TabControl.ItemsSource = Categories;
			MoveDestination.ItemsSource = Categories;
			UnselectNote();
			TabControl.Items.Refresh();
			TabControl.SelectedIndex = selectedIndex + 1;
			MoveDestination.Items.Refresh();
		}

		private void RemoveCategoryClick(object sender, RoutedEventArgs e)
		{
			if (SelectedCategory == null)
				return;

			if (SelectedCategory.Notes.Any())
			{
				var result = MessageBox.Show($"Are you sure you want to delete '{SelectedCategory.Name}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Stop);
				if (result != MessageBoxResult.Yes)
					return;
			}

			NoteManager.DeleteCategory(SelectedCategory.ID);

			Categories = NoteManager.GetAll().ToList();
			if (!IsSearch)
				TabControl.ItemsSource = Categories;
			MoveDestination.ItemsSource = Categories;
			UnselectNote();
			TabControl.Items.Refresh();
			MoveDestination.Items.Refresh();
			SetTitle();
		}

		private void MoveCategoryUpClick(object sender, RoutedEventArgs e)
		{
			var selectedIndex = TabControl.SelectedIndex;
			if (selectedIndex <= 0 || IsSearch)
				return;

		Categories.RemoveAt(selectedIndex);
			Categories.Insert(selectedIndex - 1, SelectedCategory);
			TabControl.Items.Refresh();
			MoveDestination.Items.Refresh();
		}

		private void MoveCategoryDownClick(object sender, RoutedEventArgs e)
		{
			var selectedIndex = TabControl.SelectedIndex;
			if (selectedIndex == -1 || selectedIndex >= TabControl.Items.Count - 1 || IsSearch)
				return;

		Categories.RemoveAt(selectedIndex);
			Categories.Insert(selectedIndex + 1, SelectedCategory);
			TabControl.Items.Refresh();
			MoveDestination.Items.Refresh();
		}

		private void MoveCategoryTopClick(object sender, RoutedEventArgs e)
		{
			var selectedIndex = TabControl.SelectedIndex;
			if (selectedIndex <= 0 || IsSearch)
				return;

		Categories.RemoveAt(selectedIndex);
			Categories.Insert(0, SelectedCategory);
			TabControl.Items.Refresh();
			MoveDestination.Items.Refresh();
		}

		private void MoveCategoryBottomClick(object sender, RoutedEventArgs e)
		{
			var selectedIndex = TabControl.SelectedIndex;
			if (selectedIndex == -1 || selectedIndex >= TabControl.Items.Count - 1 || IsSearch)
				return;

		Categories.RemoveAt(selectedIndex);
			Categories.Insert(TabControl.Items.Count, SelectedCategory);
			TabControl.Items.Refresh();
			MoveDestination.Items.Refresh();
		}

		private void AddNoteClick(object sender, RoutedEventArgs e)
		{
			var newNote = NoteManager.CreateNote(SelectedCategory.ID);
			if (newNote == null)
				return;

			var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
			if (selectedIndex != SelectedCategory.Notes.Count - 1)
				NoteManager.ReorderNote(newNote, SelectedCategory.ID, selectedIndex + 1);

			TabControl.Items.Refresh();
			SetTitle();
			SetCategoryNoteCount();
		}

		private void RemoveNoteClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || SelectedCategory == null)
				return;

			if (!string.IsNullOrEmpty(SelectedNote.Text))
			{
				var truncated = SelectedNote.Text.Length > 60 ? SelectedNote.Text.Substring(0, 60) + "..." : SelectedNote.Text;
				var result = MessageBox.Show($"Are you sure you want to delete '{truncated}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
				if (result != MessageBoxResult.Yes)
					return;
			}

			NoteManager.DeleteNote(SelectedNote, SelectedCategory.ID);

			UnselectNote();
			TabControl.Items.Refresh();
			SetTitle();
			SetCategoryNoteCount();
		}

		private void ArchiveNoteClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || SelectedCategory == null)
				return;

			if (!string.IsNullOrEmpty(SelectedNote.Text))
			{
				var truncated = SelectedNote.Text.Length > 60 ? SelectedNote.Text.Substring(0, 60) + "..." : SelectedNote.Text;
				var result = MessageBox.Show($"Are you sure you want to archive '{truncated}'?", "Confirm archiving", MessageBoxButton.YesNo, MessageBoxImage.Warning);
				if (result != MessageBoxResult.Yes)
					return;
			}

			if (!NoteManager.ArchiveNote(SelectedNote, SelectedCategory.ID))
				return;
			NoteManager.DeleteNote(SelectedNote, SelectedCategory.ID);

			UnselectNote();
			TabControl.Items.Refresh();
			SetTitle();
			SetCategoryNoteCount();
		}

		private void MoveNoteUpClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || IsSearch)
				return;

			var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
			if (selectedIndex <= 0)
				return;

		SelectedCategory.Notes.RemoveAt(selectedIndex);
			SelectedCategory.Notes.Insert(selectedIndex - 1, SelectedNote);
			TabControl.Items.Refresh();
		}

		private void MoveNoteDownClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || IsSearch)
				return;

			var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
			if (selectedIndex >= SelectedCategory.Notes.Count - 1)
				return;

		SelectedCategory.Notes.RemoveAt(selectedIndex);
			SelectedCategory.Notes.Insert(selectedIndex + 1, SelectedNote);
			TabControl.Items.Refresh();
		}

		private void MoveNoteTopClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || IsSearch)
				return;

			var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
			if (selectedIndex <= 0)
				return;

		SelectedCategory.Notes.RemoveAt(selectedIndex);
			SelectedCategory.Notes.Insert(0, SelectedNote);
			TabControl.Items.Refresh();
		}

		private void MoveNoteBottomClick(object sender, RoutedEventArgs e)
		{
			if (SelectedNote == null || IsSearch)
				return;

			var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
			if (selectedIndex >= SelectedCategory.Notes.Count - 1)
				return;

		SelectedCategory.Notes.RemoveAt(selectedIndex);
			SelectedCategory.Notes.Insert(SelectedCategory.Notes.Count, SelectedNote);
			TabControl.Items.Refresh();
		}

		private void ChangeNoteCategoryClick(object sender, RoutedEventArgs e)
		{
			if (SelectedCategory == null || SelectedNote == null || MoveDestination.SelectedIndex == -1)
				return;

			var destinationCategory = (Category)MoveDestination.SelectedItem;
			var currentCategory = SelectedCategory;

			NoteManager.MoveNote(SelectedNote, currentCategory.ID, destinationCategory.ID);

			if (IsSearch)
			{
				var searchResults = (IEnumerable<Category>)TabControl.ItemsSource;
				searchResults.Single(c => c.ID == destinationCategory.ID).Notes.Add(SelectedNote);
				searchResults.Single(c => c.ID == currentCategory.ID).Notes.Remove(SelectedNote);
			}

			UnselectNote();
			TabControl.Items.Refresh();
			SetCategoryNoteCount();
		}

		private void SearchTextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(SearchBox.Text))
			{
				TabControl.ItemsSource = Categories;
				SetTitle();
				IsSearch = false;
			}
			else
			{
				var searchResults = NoteManager.Find(SearchBox.Text, ignoreCase: true);
				TabControl.ItemsSource = searchResults;
				Title = $"Found {searchResults.SelectMany(c => c.Notes).Count()} notes in {searchResults.Count()} categories";
				IsSearch = true;
			}
		}

		private void NoteColorClick(object sender, RoutedEventArgs e)
		{
			var colorDialog = new System.Windows.Forms.ColorDialog();
			if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var colorHexString = $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
				SelectedNote.Color = colorHexString;
				SetNoteColor();
				TabControl.Items.Refresh();
			}
		}

		private void CategoryColorClick(object sender, RoutedEventArgs e)
		{
			var colorDialog = new System.Windows.Forms.ColorDialog();
			if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var colorHexString = $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
				SelectedCategory.Color = colorHexString;
				SetCategoryColor();
				TabControl.Items.Refresh();
			}
		}

		private void SetCategoryColor()
		{
		CategoryColor.Background = WpfUtils.ToBrush(SelectedCategory?.Color);
		}

		private void SetNoteColor()
		{
		NoteColor.Background = WpfUtils.ToBrush(SelectedNote?.Color);
		}

	private void Save()
	{
		NoteManager.Save();
	}
}

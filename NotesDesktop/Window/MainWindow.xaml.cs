using skroy.NotesDesktop.Controller;
using skroy.NotesDesktop.Model;
using skroy.NotesDesktop.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace skroy.NotesDesktop;

public partial class MainWindow : Window
{
	private readonly NoteController controller;

	private ListView ListView { get; set; }
	private CategoryModel SelectedCategory { get; set; }
	private NoteModel SelectedNote { get; set; }
	private Dictionary<long, CategoryUpdateTimer> CategoryUpdateTimers { get; set; }
	private Dictionary<long, NoteUpdateTimer> NoteUpdateTimers { get; set; }

	public List<CategoryModel> Categories { get; set; }


	public MainWindow()
	{
		controller = new NoteController();

		CategoryUpdateTimers = [];
		NoteUpdateTimers = [];

		Categories = GetAll();

		InitializeComponent();

		UpdateWindowTitle();
	}


	#region Buttons

	private void AddCategoryClick(object sender, RoutedEventArgs e)
	{
		var newCategory = controller.CreateCategory();
		if (newCategory == null)
			return;

		if (controller.SetCategoryName(newCategory.Id, "New"))
			newCategory.Name = "New";

		var selectedIndex = Categories.IndexOf(SelectedCategory);
		if (selectedIndex <= Categories.Count - 1)
			controller.ReorderCategory(newCategory.Id, selectedIndex + 1);
		Categories.Insert(selectedIndex + 1, newCategory);

		TabControl.SelectedIndex = selectedIndex + 1;

		OnCategoryListUpdated();
		DeselectNote();
	}

	private void RemoveCategoryClick(object sender, RoutedEventArgs e)
	{
		if (SelectedCategory == null)
			return;

		if (SelectedCategory.Notes.Count != 0)
		{
			var result = MessageBox.Show($"Are you sure you want to delete '{SelectedCategory.Name}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Stop);
			if (result != MessageBoxResult.Yes)
				return;
		}

		CategoryUpdateTimers.Remove(SelectedCategory.Id, out var categoryTimer);
		categoryTimer?.Stop();
		foreach (var note in SelectedCategory.Notes)
		{
			NoteUpdateTimers.Remove(note.Id, out var noteTimer);
			noteTimer?.Stop();
		}

		if (!controller.DeleteCategory(SelectedCategory.Id))
			return;

		var selectedIndex = Categories.IndexOf(SelectedCategory);
		Categories.Remove(SelectedCategory);

		OnCategoryListUpdated();
		DeselectNote();

		TabControl.SelectedIndex = Math.Max(0, selectedIndex - 1);
	}

	private void MoveCategoryUpClick(object sender, RoutedEventArgs e)
		=> ReorderCategory(TabControl.SelectedIndex - 1);

	private void MoveCategoryDownClick(object sender, RoutedEventArgs e)
		=> ReorderCategory(TabControl.SelectedIndex + 1);

	private void MoveCategoryTopClick(object sender, RoutedEventArgs e)
		=> ReorderCategory(0);

	private void MoveCategoryBottomClick(object sender, RoutedEventArgs e)
		=> ReorderCategory(TabControl.Items.Count - 1);

	private void AddNoteClick(object sender, RoutedEventArgs e)
	{
		if (SelectedCategory == null)
			return;

		var newNote = controller.CreateNote(SelectedCategory.Id);
		if (newNote == null)
			return;

		var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
		controller.ReorderNote(newNote.Id, selectedIndex + 1);
		SelectedCategory.Notes.Insert(selectedIndex + 1, newNote);

		OnNoteListUpdated();
		FocusOnNote(newNote);
	}

	private void RemoveNoteClick(object sender, RoutedEventArgs e)
	{
		if (SelectedNote == null || SelectedCategory == null)
			return;

		var noteText = SelectedNote.Text.Truncate(60);
		if (!string.IsNullOrEmpty(noteText))
		{
			var result = MessageBox.Show($"Are you sure you want to permanently delete '{noteText}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

		NoteUpdateTimers.Remove(SelectedNote.Id, out var noteTimer);
		noteTimer?.Stop();

		if (!controller.DeleteNote(SelectedNote.Id))
			return;

		var lastSelectedIndex = ListView.SelectedIndex;
		SelectedCategory.Notes.Remove(SelectedNote);

		OnNoteListUpdated();
		FocusOnNoteIfAny(lastSelectedIndex);
	}

	private void ArchiveNoteClick(object sender, RoutedEventArgs e)
	{
		if (SelectedNote == null || SelectedCategory == null)
			return;

		var noteText = SelectedNote.Text.Truncate(60);
		if (!string.IsNullOrEmpty(noteText))
		{
			var result = MessageBox.Show($"Are you sure you want to archive '{noteText}'?", "Confirm archival", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

		ChangeNoteStorage(NoteStorage.Archive);
	}

	private void UnarchiveNoteClick(object sender, RoutedEventArgs e)
	{
		if (SelectedNote == null || SelectedCategory == null)
			return;

		var noteText = SelectedNote.Text.Truncate(60);
		if (!string.IsNullOrEmpty(noteText))
		{
			var result = MessageBox.Show($"Are you sure you want to restore '{noteText}'?", "Confirm unarchival", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

		ChangeNoteStorage(NoteStorage.Main);
	}

	private void MoveNoteUpClick(object sender, RoutedEventArgs e)
		=> ReorderNote(SelectedCategory.Notes.IndexOf(SelectedNote) - 1);

	private void MoveNoteDownClick(object sender, RoutedEventArgs e)
		=> ReorderNote(SelectedCategory.Notes.IndexOf(SelectedNote) + 1);

	private void MoveNoteTopClick(object sender, RoutedEventArgs e)
		=> ReorderNote(0);

	private void MoveNoteBottomClick(object sender, RoutedEventArgs e)
		=> ReorderNote(SelectedCategory.Notes.Count - 1);

	private void ChangeNoteCategoryClick(object sender, RoutedEventArgs e)
	{
		if (SelectedCategory == null || SelectedNote == null || MoveDestination.SelectedIndex == -1)
			return;

		var destinationCategory = (CategoryModel)MoveDestination.SelectedItem;
		controller.ChangeNoteCategory(SelectedNote.Id, destinationCategory.Id);

		var lastSelectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
		SelectedCategory.Notes.Remove(SelectedNote);
		destinationCategory.Notes.Add(SelectedNote);

		DeselectNote();
		OnNoteListUpdated();
		UpdateCategoryNoteCount();
		FocusOnNoteIfAny(lastSelectedIndex);
	}

	private void NoteColorClick(object sender, RoutedEventArgs e)
	{
		var colorDialog = new System.Windows.Forms.ColorDialog();
		if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			SelectedNote.Color = ColorUtils.ToHexString(colorDialog.Color);
			controller.SetNoteColor(SelectedNote.Id, SelectedNote.Color);
			SetNoteColor();
			OnNoteUpdated();
		}
	}

	private void CategoryColorClick(object sender, RoutedEventArgs e)
	{
		var colorDialog = new System.Windows.Forms.ColorDialog();
		if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			SelectedCategory.Color = ColorUtils.ToHexString(colorDialog.Color);
			controller.SetCategoryColor(SelectedCategory.Id, SelectedCategory.Color);
			SetCategoryColor();
			OnCategoryUpdated();
		}
	}

	private void MainModeClick(object sender, RoutedEventArgs e)
	{
		MainMode.Visibility = Visibility.Hidden;
		ArchiveMode.Visibility = Visibility.Visible;
		UnarchiveNote.Visibility = Visibility.Hidden;
		ArchiveNote.Visibility = Visibility.Visible;

		ChangeStorage(NoteStorage.Main);
	}

	private void ArchiveModeClick(object sender, RoutedEventArgs e)
	{
		MainMode.Visibility = Visibility.Visible;
		ArchiveMode.Visibility = Visibility.Hidden;
		UnarchiveNote.Visibility = Visibility.Visible;
		ArchiveNote.Visibility = Visibility.Hidden;

		ChangeStorage(NoteStorage.Archive);
	}

	#endregion

	#region Events

	private void WindowClosing(object sender, CancelEventArgs e)
	{
		foreach (var timer in CategoryUpdateTimers.Values)
			controller.SetCategoryName(timer.Id, timer.Name);

		foreach (var timer in NoteUpdateTimers.Values)
			controller.SetNoteText(timer.Id, timer.Text);
	}

	private void ListViewLoaded(object sender, RoutedEventArgs e)
	{
		ListView = (ListView)sender;
	}

	private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.OriginalSource != TabControl)
			return;

		SelectedCategory = (CategoryModel)TabControl.SelectedItem;
		UpdateCategoryNoteCount();
		DeselectNote();
		SetCategoryColor();
	}

	private void SearchTextChanged(object sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(SearchBox.Text))
		{
			TabControl.ItemsSource = Categories;
			UpdateWindowTitle();
			ToggleSearchControls(false);
		}
		else
		{
			var searchResults = controller.Search(SearchBox.Text);
			TabControl.ItemsSource = searchResults;
			Title = $"Found {searchResults.Sum(c => c.Notes.Count)} notes in {searchResults.Count()} categories";
			ToggleSearchControls(true);
		}
	}

	private void NoteTextChanged(object sender, TextChangedEventArgs e)
	{
		if (SelectedNote == null)
			return;

		UpdateNote();
		SelectedNote.ModificationDate = DateTime.Now;
		UpdateLastModifiedText();
	}

	private void CategoryNameChanged(object sender, DataTransferEventArgs e)
	{
		if (SelectedCategory == null)
			return;

		UpdateCategory();
	}

	private void NoteSelected(object sender, MouseEventArgs e)
	{
		var item = (ListViewItem)sender;
		item.IsSelected = true;
		SelectNote((NoteModel)item.Content);
	}

	#endregion

	private void SelectNote(NoteModel item)
	{
		SelectedNote = item;
		SelectedID.Content = $"#{SelectedNote.Id}";
		ChangeNoteCategory.IsEnabled = true;
		UpdateLastModifiedText();
		SetNoteColor();
	}

	private void DeselectNote()
	{
		SelectedNote = null;
		SelectedID.Content = string.Empty;
		LastModified.Content = string.Empty;
		ChangeNoteCategory.IsEnabled = false;
		SetNoteColor();
	}

	private void FocusOnNote(NoteModel note)
	{
		ListView.SelectedItem = note;
		SelectNote(note);
		ListView.UpdateLayout();
		var listViewItem = (ListViewItem)ListView.ItemContainerGenerator.ContainerFromItem(ListView.SelectedItem);
		listViewItem.FindChild<TextBox>().Focus();
	}

	private void FocusOnNoteIfAny(int lastSelectedIndex)
	{
		if (SelectedCategory.Notes.Count != 0)
		{
			var newIndex = Math.Clamp(lastSelectedIndex, 0, ListView.Items.Count - 1);
			FocusOnNote((NoteModel)ListView.Items[newIndex]);
		}
		else
			DeselectNote();
	}

	private void UpdateWindowTitle()
	{
		Title = $"{Categories.SelectMany(c => c.Notes).Count()} notes in {Categories.Count} categories";
	}

	private void UpdateCategoryNoteCount()
	{
		NotesInCategory.Content = SelectedCategory != null ? SelectedCategory.Notes.Count + " notes" : "";
	}

	private void UpdateLastModifiedText()
	{
		if (SelectedNote == null)
		{
			LastModified.Content = "";
			LastModified.ToolTip = null;
			return;
		}

		string tooltip = null;
		var info = new List<string>();
		if (SelectedNote.CreationDate != null)
			info.Add($"Created: {SelectedNote.CreationDate}");
		if (SelectedNote.ModificationDate != null)
			info.Add($"Edited: {SelectedNote.ModificationDate}");
		if (info.Count != 0)
			tooltip = string.Join('\n', info);

		LastModified.Content = (SelectedNote.CreationDate ?? SelectedNote.ModificationDate).ToString() ?? "";
		LastModified.ToolTip = tooltip;
	}

	private void ReorderCategory(int newPosition)
	{
		if (SelectedCategory == null)
			return;

		if (newPosition < 0 || newPosition >= TabControl.Items.Count)
			return;

		controller.ReorderCategory(SelectedCategory.Id, newPosition);

		Categories.RemoveAt(TabControl.SelectedIndex);
		Categories.Insert(newPosition, SelectedCategory);
		OnCategoryUpdated();
	}

	private void ReorderNote(int newPosition)
	{
		if (SelectedNote == null || SelectedCategory == null)
			return;

		if (newPosition < 0 || newPosition >= SelectedCategory.Notes.Count)
			return;

		controller.ReorderNote(SelectedNote.Id, newPosition);

		SelectedCategory.Notes.Remove(SelectedNote);
		SelectedCategory.Notes.Insert(newPosition, SelectedNote);
		OnNoteUpdated();
		FocusOnNote(SelectedNote);
	}

	private void ChangeNoteStorage(NoteStorage targetStorage)
	{
		NoteUpdateTimers.Remove(SelectedNote.Id, out var noteTimer);
		if (noteTimer != null)
			UpdateNoteCallback(noteTimer);

		if (!controller.ChangeNoteStorage(SelectedNote.Id, targetStorage))
			return;

		var lastSelectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
		SelectedCategory.Notes.Remove(SelectedNote);
		OnNoteListUpdated();
		FocusOnNoteIfAny(lastSelectedIndex);
	}

	private void ChangeStorage(NoteStorage targetStorage)
	{
		controller.SetStorage(targetStorage);

		Reload();
	}

	private void SetCategoryColor()
	{
		CategoryColor.Background = ColorUtils.ToBrush(SelectedCategory?.Color);
	}

	private void SetNoteColor()
	{
		NoteColor.Background = ColorUtils.ToBrush(SelectedNote?.Color);
	}

	private void OnCategoryListUpdated()
	{
		TabControl.ItemsSource = Categories;
		MoveDestination.ItemsSource = Categories;
		OnCategoryUpdated();
		UpdateWindowTitle();
	}

	private void OnCategoryUpdated()
	{
		OnNoteUpdated();
		MoveDestination.Items.Refresh();
	}

	private void OnNoteListUpdated()
	{
		OnNoteUpdated();
		UpdateWindowTitle();
		UpdateCategoryNoteCount();
	}

	private void OnNoteUpdated()
	{
		TabControl.Items.Refresh();
	}

	private void ToggleSearchControls(bool isSearch)
	{
		AddCategory.IsEnabled = !isSearch;
		RemoveCategory.IsEnabled = !isSearch;
		MoveCategoryUp.IsEnabled = !isSearch;
		MoveCategoryDown.IsEnabled = !isSearch;
		MoveCategoryTop.IsEnabled = !isSearch;
		MoveCategoryBottom.IsEnabled = !isSearch;
		AddNote.IsEnabled = !isSearch;
		RemoveNote.IsEnabled = !isSearch;
		ArchiveNote.IsEnabled = !isSearch;
		MoveNoteUp.IsEnabled = !isSearch;
		MoveNoteDown.IsEnabled = !isSearch;
		MoveNoteTop.IsEnabled = !isSearch;
		MoveNoteBottom.IsEnabled = !isSearch;
		NoteColor.IsEnabled = !isSearch;
		CategoryColor.IsEnabled = !isSearch;
		ChangeNoteCategory.IsEnabled = !isSearch;
		MoveDestination.IsEnabled = !isSearch;
	}

	private void Reload()
	{
		Categories = GetAll();
		OnCategoryListUpdated();
		OnNoteListUpdated();
	}

	private List<CategoryModel> GetAll()
	{
		return [.. controller.GetAll().Select(x => { x.Notes = [.. x.Notes.OrderBy(x => x.Order)]; return x; }).OrderBy(x => x.Order)];
	}

	private void UpdateNote()
	{
		if (NoteUpdateTimers.TryGetValue(SelectedNote.Id, out var existingTimer))
		{
			existingTimer.Text = SelectedNote.Text;
			existingTimer.Stop();
			existingTimer.Start();
			return;
		}

		var timer = new NoteUpdateTimer
		{
			Interval = 3000,
			Id = SelectedNote.Id,
			Text = SelectedNote.Text
		};
		timer.Elapsed += (sender, _) => UpdateNoteCallback((NoteUpdateTimer)sender);
		NoteUpdateTimers[SelectedNote.Id] = timer;
	}

	private void UpdateCategory()
	{
		if (CategoryUpdateTimers.TryGetValue(SelectedCategory.Id, out var existingTimer))
		{
			existingTimer.Name = SelectedCategory.Name;
			existingTimer.Stop();
			existingTimer.Start();
			return;
		}

		var timer = new CategoryUpdateTimer
		{
			Interval = 3000,
			Id = SelectedCategory.Id,
			Name = SelectedCategory.Name
		};
		timer.Elapsed += (sender, _) => UpdateCategoryCallback((CategoryUpdateTimer)sender);
		CategoryUpdateTimers[SelectedCategory.Id] = timer;
	}

	private void UpdateNoteCallback(NoteUpdateTimer timer)
	{
		controller.SetNoteText(timer.Id, timer.Text);
		timer.Stop();
	}

	private void UpdateCategoryCallback(CategoryUpdateTimer timer)
	{
		controller.SetCategoryName(timer.Id, timer.Name);
		timer.Stop();
	}
}

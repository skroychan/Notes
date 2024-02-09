using skroy.NotesDesktop.Controller;
using skroy.NotesDesktop.Model;
using skroy.NotesDesktop.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace skroy.NotesDesktop;

public partial class MainWindow : Window
{
    private readonly NoteController controller;

	private CategoryModel SelectedCategory { get; set; }
	private NoteModel SelectedNote { get; set; }

	public List<CategoryModel> Categories { get; set; }


	public MainWindow()
	{
		controller = new NoteController();

        Categories = controller.GetAll().ToList();

		InitializeComponent();

        SetTitle();
	}

    #region Buttons

    private void AddCategoryClick(object sender, RoutedEventArgs e)
    {
        var newCategory = controller.CreateCategory();
        if (newCategory == null)
            return;

        controller.UpdateCategoryName(newCategory.Id, "New");

        var selectedIndex = Categories.IndexOf(SelectedCategory);
        if (selectedIndex <= Categories.Count - 1)
            controller.MoveCategory(newCategory.Id, selectedIndex + 1);

        Categories.Insert(selectedIndex + 1, newCategory);
        UnselectNote();
        UpdateCategories();
        TabControl.SelectedIndex = selectedIndex + 1;
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

        controller.DeleteCategory(SelectedCategory.Id);

        Categories.Remove(SelectedCategory);
        UnselectNote();
        UpdateCategories();
    }

    private void MoveCategoryUpClick(object sender, RoutedEventArgs e)
    {
        MoveCategory(TabControl.SelectedIndex - 1);
    }

    private void MoveCategoryDownClick(object sender, RoutedEventArgs e)
    {
        MoveCategory(TabControl.SelectedIndex + 1);
    }

    private void MoveCategoryTopClick(object sender, RoutedEventArgs e)
    {
        MoveCategory(0);
    }

    private void MoveCategoryBottomClick(object sender, RoutedEventArgs e)
    {
        MoveCategory(TabControl.Items.Count - 1);
    }

    private void AddNoteClick(object sender, RoutedEventArgs e)
    {
        if (SelectedCategory == null)
            return;

        var newNote = controller.CreateNote(SelectedCategory.Id);
        if (newNote == null)
            return;

        var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
        controller.MoveNote(newNote.Id, selectedIndex + 1);

        SelectedCategory.Notes.Insert(selectedIndex + 1, newNote);
        TabControl.Items.Refresh();
        SetTitle();
        SetCategoryNoteCount();
    }

    private void RemoveNoteClick(object sender, RoutedEventArgs e)
    {
        if (SelectedNote == null || SelectedCategory == null)
            return;

        var truncated = SelectedNote.Text.Truncate(60);
        if (!string.IsNullOrEmpty(truncated))
        {
            var result = MessageBox.Show($"Are you sure you want to permanently delete '{truncated}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }

        if (!controller.DeleteNote(SelectedNote.Id))
            return;

        SelectedCategory.Notes.Remove(SelectedNote);
        UnselectNote();
        UpdateNotes();
    }

	private void ArchiveNoteClick(object sender, RoutedEventArgs e)
	{
		var truncated = SelectedNote.Text.Truncate(60);
		if (!string.IsNullOrEmpty(truncated))
		{
			var result = MessageBox.Show($"Are you sure you want to archive '{truncated}'?", "Confirm archival", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

        ChangeNoteStorage(NoteStorage.Archive);
	}

	private void UnarchiveNoteClick(object sender, RoutedEventArgs e)
	{
		var truncated = SelectedNote.Text.Truncate(60);
		if (!string.IsNullOrEmpty(truncated))
		{
			var result = MessageBox.Show($"Are you sure you want to restore '{truncated}'?", "Confirm unarchival", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

		ChangeNoteStorage(NoteStorage.Main);
	}

	private void MoveNoteUpClick(object sender, RoutedEventArgs e)
    {
        MoveNote(SelectedCategory.Notes.IndexOf(SelectedNote) - 1);
    }

    private void MoveNoteDownClick(object sender, RoutedEventArgs e)
    {
        MoveNote(SelectedCategory.Notes.IndexOf(SelectedNote) + 1);
    }

    private void MoveNoteTopClick(object sender, RoutedEventArgs e)
    {
        MoveNote(0);
    }

    private void MoveNoteBottomClick(object sender, RoutedEventArgs e)
    {
        MoveNote(SelectedCategory.Notes.Count - 1);
    }

    private void ChangeNoteCategoryClick(object sender, RoutedEventArgs e)
    {
        if (SelectedCategory == null || SelectedNote == null || MoveDestination.SelectedIndex == -1)
            return;

        var destinationCategory = (CategoryModel)MoveDestination.SelectedItem;
        controller.ChangeNoteCategory(SelectedNote.Id, destinationCategory.Id);

        SelectedCategory.Notes.Remove(SelectedNote);
        destinationCategory.Notes.Add(SelectedNote);
        UnselectNote();
        RefreshNotes();
        SetCategoryNoteCount();
    }

    private void NoteColorClick(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SelectedNote.Color = ColorUtils.ToHexString(colorDialog.Color);
            controller.UpdateNoteColor(SelectedNote.Id, SelectedNote.Color);
            SetNoteColor();
            RefreshNotes();
        }
    }

    private void CategoryColorClick(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SelectedCategory.Color = ColorUtils.ToHexString(colorDialog.Color);
			controller.UpdateCategoryColor(SelectedCategory.Id, SelectedCategory.Color);
            SetCategoryColor();
            RefreshCategories();
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
        //Save();
    }

    private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource != TabControl)
            return;

        SelectedCategory = (CategoryModel)TabControl.SelectedItem;
        UnselectNote();
        SetCategoryNoteCount();
        SetCategoryColor();
    }

    private void SearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            TabControl.ItemsSource = Categories;
            SetTitle();
            ToggleSearch(false);
        }
        else
        {
            var searchResults = controller.Search(SearchBox.Text);
            TabControl.ItemsSource = searchResults;
            Title = $"Found {searchResults.SelectMany(c => c.Notes).Count()} notes in {searchResults.Count()} categories";
            ToggleSearch(true);
        }
	}

	private void NoteTextChanged(object sender, TextChangedEventArgs e)
    {
        if (SelectedNote == null)
            return;

        controller.UpdateNoteText(SelectedNote.Id, SelectedNote.Text);
        SelectedNote.ModificationDate = DateTime.Now;
        SetLastModifiedText();
    }

    private void CategoryNameChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || SelectedCategory == null)
            return;

        controller.UpdateCategoryName(SelectedCategory.Id, SelectedCategory.Name);
    }

    private void NoteSelected(object sender, MouseEventArgs e)
    {
        var item = (ListViewItem)sender;
        SelectNote((NoteModel)item.Content);
    }

    #endregion

    private void SelectNote(NoteModel item)
    { 
		SelectedNote = item;
		SelectedID.Content = $"#{SelectedNote.Id}";
		SetLastModifiedText();
        ChangeNoteCategory.IsEnabled = SelectedNote != null;
		SetNoteColor();
	}

    private void UnselectNote()
	{
		SelectedNote = null;
		SelectedID.Content = string.Empty;
        LastModified.Content = string.Empty;
        ChangeNoteCategory.IsEnabled = false;
		SetNoteColor();
	}

	private void SetTitle()
	{
		Title = $"{Categories.SelectMany(c => c.Notes).Count()} notes in {Categories.Count} categories";
	}

	private void SetCategoryNoteCount()
	{
		NotesInCategory.Content = SelectedCategory != null ? SelectedCategory.Notes.Count + " notes" : "";
	}

    private void SetLastModifiedText()
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
		if (info.Any())
			tooltip = string.Join('\n', info);

        LastModified.Content = (SelectedNote.CreationDate ?? SelectedNote.ModificationDate).ToString() ?? "";
        LastModified.ToolTip = tooltip;
    }

	private void MoveCategory(int newPosition)
	{
        if (SelectedCategory == null)
            return;

        if (newPosition < 0 || newPosition >= TabControl.Items.Count)
            return;

        controller.MoveCategory(SelectedCategory.Id, newPosition);

        Categories.RemoveAt(TabControl.SelectedIndex);
        Categories.Insert(newPosition, SelectedCategory);
        RefreshCategories();
    }

	private void MoveNote(int newPosition)
	{
        if (SelectedNote == null || SelectedCategory == null)
            return;

        if (newPosition < 0 || newPosition >= SelectedCategory.Notes.Count)
            return;

		controller.MoveNote(SelectedNote.Id, newPosition);

        SelectedCategory.Notes.Remove(SelectedNote);
        SelectedCategory.Notes.Insert(newPosition, SelectedNote);
		RefreshNotes();
	}

	private void ChangeNoteStorage(NoteStorage targetStorage)
	{
		if (SelectedNote == null || SelectedCategory == null)
			return;

		if (!controller.ChangeNoteStorage(SelectedNote.Id, targetStorage))
			return;

		SelectedCategory.Notes.Remove(SelectedNote);
		UnselectNote();
		UpdateNotes();
	}

	private void ChangeStorage(NoteStorage targetStorage)
	{
        controller.SetStorage(targetStorage);

		Reload();
	}

	private void SetCategoryColor()
	{
		CategoryColor.Background = ColorUtils.ToBrush(SelectedCategory?.Color);
        if (CategoryColor.Background != null)
            CategoryColor.Background.Opacity = 0.3;
	}

	private void SetNoteColor()
	{
		NoteColor.Background = ColorUtils.ToBrush(SelectedNote?.Color);
        if (NoteColor.Background != null)
			NoteColor.Background.Opacity = 0.3;
	}

	private void UpdateCategories()
	{
		TabControl.ItemsSource = Categories;
		MoveDestination.ItemsSource = Categories;
		RefreshCategories();
        SetTitle();
    }

	private void RefreshCategories()
	{
        RefreshNotes();
        MoveDestination.Items.Refresh();
    }

	private void UpdateNotes()
	{
        RefreshNotes();
		SetTitle();
        SetCategoryNoteCount();
    }

	private void RefreshNotes()
	{
        TabControl.Items.Refresh();
    }

	private void ToggleSearch(bool isSearch)
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
		Categories = controller.GetAll().ToList();
        UpdateCategories();
		UpdateNotes();
	}
}

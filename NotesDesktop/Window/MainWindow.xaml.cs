using NotesDesktop.Controller;
using NotesDesktop.Model;
using NotesDesktop.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NotesDesktop;

public partial class MainWindow : Window
{
    private readonly NoteController NoteController;

	private CategoryModel SelectedCategory { get; set; }
	private NoteModel SelectedNote { get; set; }
	private Timer Timer { get; set; }

	public List<CategoryModel> Categories { get; set; }


	public MainWindow()
	{
		NoteController = new NoteController();

		Categories = NoteController.GetAll().ToList();

		InitializeComponent();

		Timer = new Timer(5000);
		Timer.Stop();
        Timer.Elapsed += (_, _) => NoteController.Save();

        SetTitle();
	}

    #region Buttons

    private void AddCategoryClick(object sender, RoutedEventArgs e)
    {
        var newCategory = NoteController.CreateCategory("New");
        if (newCategory == null)
            return;

        var selectedIndex = Categories.IndexOf(SelectedCategory);
        if (selectedIndex <= Categories.Count - 1)
            NoteController.MoveCategory(newCategory.Id, selectedIndex + 1);

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

        NoteController.DeleteCategory(SelectedCategory.Id);

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

        var newNote = NoteController.CreateNote(SelectedCategory.Id);
        if (newNote == null)
            return;

        var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
        NoteController.MoveNote(newNote.Id, selectedIndex + 1);

        SelectedCategory.Notes.Insert(selectedIndex + 1, newNote);
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
            var truncated = SelectedNote.Text.Length > 60 ? SelectedNote.Text[..60] + "..." : SelectedNote.Text;
            var result = MessageBox.Show($"Are you sure you want to delete '{truncated}'?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }

        if (!NoteController.DeleteNote(SelectedNote.Id))
            return;

        SelectedCategory.Notes.Remove(SelectedNote);
        UnselectNote();
        UpdateNotes();
    }

    private void ArchiveNoteClick(object sender, RoutedEventArgs e)
    {
        if (SelectedNote == null || SelectedCategory == null)
            return;

        if (!string.IsNullOrEmpty(SelectedNote.Text))
        {
            var truncated = SelectedNote.Text.Length > 60 ? SelectedNote.Text[..60] + "..." : SelectedNote.Text;
            var result = MessageBox.Show($"Are you sure you want to archive '{truncated}'?", "Confirm archival", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }

        if (!NoteController.ArchiveNote(SelectedNote.Id))
            return;

        SelectedCategory.Notes.Remove(SelectedNote);
        UnselectNote();
        UpdateNotes();
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
        NoteController.ChangeNoteCategory(SelectedNote.Id, destinationCategory.Id);

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
            SelectedNote.Color = colorDialog.Color.ToString();
            NoteController.UpdateNoteColor(SelectedNote.Id, SelectedNote.Color);
            SetNoteColor();
            RefreshNotes();
        }
    }

    private void CategoryColorClick(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SelectedCategory.Color = colorDialog.Color.ToString();
            NoteController.UpdateCategoryColor(SelectedCategory.Id, SelectedCategory.Color);
            SetCategoryColor();
            RefreshCategories();
        }
    }

    #endregion

    #region Events

    private void WindowClosing(object sender, CancelEventArgs e)
    {
        Save();
    }

    private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!(e.Source is TabControl))
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
            var searchResults = NoteController.Search(SearchBox.Text, ignoreCase: true);
            TabControl.ItemsSource = searchResults;
            Title = $"Found {searchResults.SelectMany(c => c.Notes).Count()} notes in {searchResults.Count()} categories";
            ToggleSearch(true);
        }
    }

    private void NoteTextChanged(object sender, TextChangedEventArgs e)
    {
        if (SelectedNote == null)
            return;

        NoteController.UpdateNoteText(SelectedNote.Id, SelectedNote.Text);
        SelectedNote.ModificationDate = DateTime.Now;
        SetLastModifiedText();

        Save(true);
    }

    private void CategoryNameChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || SelectedCategory == null)
            return;

        NoteController.UpdateCategoryName(SelectedCategory.Id, SelectedCategory.Name);
        Save(true);
    }

    private void NoteSelected(object sender, KeyboardFocusChangedEventArgs e)
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

        LastModified.Content = SelectedNote.CreationDate.ToString() ?? SelectedNote.ModificationDate.ToString() ?? "";
        LastModified.ToolTip = tooltip;
    }

	private void MoveCategory(int newPosition)
	{
        if (SelectedCategory == null)
            return;

        if (newPosition < 0 || newPosition >= TabControl.Items.Count)
            return;

        NoteController.MoveCategory(SelectedCategory.Id, newPosition);

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

		NoteController.MoveNote(SelectedNote.Id, newPosition);

        SelectedCategory.Notes.Remove(SelectedNote);
        SelectedCategory.Notes.Insert(newPosition, SelectedNote);
		RefreshNotes();
    }

	private void SetCategoryColor()
	{
		CategoryColor.Background = WpfUtils.ToBrush(SelectedCategory?.Color);
        if (CategoryColor.Background != null)
            CategoryColor.Background.Opacity = 0.3;
	}

	private void SetNoteColor()
	{
		NoteColor.Background = WpfUtils.ToBrush(SelectedNote?.Color);
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
        TabControl.Items.Refresh();
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

    private void Save(bool delayed = false)
	{
		if (!delayed)
		{
            NoteController.Save();
			return;
		}

		Timer.Stop();
		Timer.Start();		
    }

	private SolidColorBrush ToBrush(System.Drawing.Color color)
	{
        var brush = WpfUtils.ToBrush(color);
        brush.Opacity = 0.3;
		return brush;
    }
}

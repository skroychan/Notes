using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using skroy.NotesDesktop.Controller;
using skroy.NotesDesktop.Model;
using skroy.NotesDesktop.Util;
using static Avalonia.VisualTree.VisualExtensions;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;

namespace skroy.NotesDesktop.Window;

public partial class MainWindow : Avalonia.Controls.Window
{
    private readonly TimeSpan defaultTimerInterval = TimeSpan.FromSeconds(3);
    private readonly NoteController controller;

    private ListBox ListBox { get; set; }
    private TextBox SelectedTextBox { get; set; }
    private Dictionary<long, CategoryUpdateTimer> CategoryUpdateTimers { get; } = [];
    private Dictionary<long, NoteUpdateTimer> NoteUpdateTimers { get; } = [];

    public CategoryModel SelectedCategory { get; set => SetAndRaise(SelectedCategoryProperty, ref field, value); }
    public NoteModel SelectedNote { get; set => SetAndRaise(SelectedNoteProperty, ref field, value); }

    public ObservableCollection<CategoryModel> Categories { get; set => SetAndRaise(CategoriesProperty, ref field, value); } = [];

    private static readonly DirectProperty<MainWindow, ObservableCollection<CategoryModel>> CategoriesProperty =
        AvaloniaProperty.RegisterDirect<MainWindow, ObservableCollection<CategoryModel>>(nameof(Categories), x => x.Categories, (x, v) => x.Categories = v);

    private static readonly DirectProperty<MainWindow, CategoryModel> SelectedCategoryProperty =
        AvaloniaProperty.RegisterDirect<MainWindow, CategoryModel>(nameof(SelectedCategory), x => x.SelectedCategory, (x, v) => x.SelectedCategory = v);

    private static readonly DirectProperty<MainWindow, NoteModel> SelectedNoteProperty =
        AvaloniaProperty.RegisterDirect<MainWindow, NoteModel>(nameof(SelectedNote), x => x.SelectedNote, (x, v) => x.SelectedNote = v);

    public MainWindow()
    {
        controller = new NoteController();

        LoadData();

        InitializeComponent();

        UpdateWindowTitle();
    }

    #region Buttons

    private void AddCategoryClick(object sender, RoutedEventArgs e)
    {
        var newCategory = controller.CreateCategory();
        if (newCategory == null)
            throw new Exception("Failed to create category.");

        if (controller.SetCategoryName(newCategory.Id, "New"))
            newCategory.Name = "New";

        var selectedIndex = Categories.IndexOf(SelectedCategory);
        if (!controller.ReorderCategory(newCategory.Id, selectedIndex + 1))
            throw new Exception($"Failed to reorder category to position={selectedIndex + 1}.");
        Categories.Insert(selectedIndex + 1, newCategory);

        TabControl.SelectedIndex = selectedIndex + 1;

        UpdateWindowTitle();
    }

    private async void RemoveCategoryClick(object sender, RoutedEventArgs e)
    {
        if (SelectedCategory.Notes.Count != 0)
        {
            if (!await ShowMessageBoxConfirm("Confirm deletion", $"Are you sure you want to delete '{SelectedCategory.Name}'?"))
                return;
        }

        RemoveCategoryTimer(SelectedCategory.Id);
        foreach (var note in SelectedCategory.Notes)
            RemoveNoteTimer(note.Id);

        if (!controller.DeleteCategory(SelectedCategory.Id))
            throw new Exception("Failed to delete category.");

        var selectedIndex = Categories.IndexOf(SelectedCategory);
        Categories.Remove(SelectedCategory);

        UpdateWindowTitle();

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
        var newNote = controller.CreateNote(SelectedCategory.Id);
        if (newNote == null)
            throw new Exception("Failed to create note.");

        var selectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
        if (!controller.ReorderNote(newNote.Id, selectedIndex + 1))
            throw new Exception($"Failed to reorder note to position={selectedIndex + 1}.");
        SelectedCategory.Notes.Insert(selectedIndex + 1, newNote);

        UpdateWindowTitle();
        FocusOnNote(newNote);
    }

    private async void RemoveNoteClick(object sender, RoutedEventArgs e)
    {
        var noteText = SelectedNote.Text.Truncate(60);
        if (!string.IsNullOrEmpty(noteText))
        {
            var msgBoxText = $"Are you sure you want to permanently delete '{noteText}'?";
            if (!await ShowMessageBoxConfirm("Confirm deletion", msgBoxText))
                return;
        }

        RemoveNoteTimer(SelectedNote.Id);

        if (!controller.DeleteNote(SelectedNote.Id))
            throw new Exception("Failed to delete note.");

        var lastSelectedIndex = ListBox.SelectedIndex;
        SelectedCategory.Notes.Remove(SelectedNote);

        UpdateWindowTitle();
        FocusOnNoteIfAny(lastSelectedIndex - 1);
    }

    private async void ArchiveNoteClick(object sender, RoutedEventArgs e)
    {
        var noteText = SelectedNote.Text.Truncate(60);
        if (!string.IsNullOrEmpty(noteText))
        {
            if (!await ShowMessageBoxConfirm("Confirm archiving", $"Are you sure you want to archive '{noteText}'?"))
                return;
        }

        ChangeNoteStorage(NoteStorage.Archive);
    }

    private async void UnarchiveNoteClick(object sender, RoutedEventArgs e)
    {
        var noteText = SelectedNote.Text.Truncate(60);
        if (!string.IsNullOrEmpty(noteText))
        {
            if (!await ShowMessageBoxConfirm("Confirm unarchiving", $"Are you sure you want to restore '{noteText}'?"))
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
        // TODO bugged
        if (MoveDestination.SelectedItem == null || MoveDestination.SelectedItem == SelectedCategory)
            return;

        var destinationCategory = (CategoryModel)MoveDestination.SelectedItem;
        if (!controller.ChangeNoteCategory(SelectedNote.Id, destinationCategory.Id))
            throw new Exception("Failed to change note's category.");

        var lastSelectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
        SelectedCategory.Notes.Remove(SelectedNote);
        destinationCategory.Notes.Add(SelectedNote);

        UpdateWindowTitle();
        FocusOnNoteIfAny(lastSelectedIndex);
    }

    private void NoteColorChanged(object sender, RoutedEventArgs e)
    {
        if (!controller.SetNoteColor(SelectedNote.Id, SelectedNote.Color))
            throw new Exception("Failed to set note's color.");
    }

    private void CategoryColorChanged(object sender, RoutedEventArgs e)
    {
        if (!controller.SetCategoryColor(SelectedCategory.Id, SelectedCategory.Color))
            throw new Exception("Failed to set category's color.");
    }

    private void MainModeClick(object sender, RoutedEventArgs e)
    {
        MainMode.IsVisible = false;
        ArchiveMode.IsVisible = true;
        UnarchiveNote.IsVisible = false;
        ArchiveNote.IsVisible = true;

        ChangeStorage(NoteStorage.Main);
    }

    private void ArchiveModeClick(object sender, RoutedEventArgs e)
    {
        MainMode.IsVisible = true;
        ArchiveMode.IsVisible = false;
        UnarchiveNote.IsVisible = true;
        ArchiveNote.IsVisible = false;

        ChangeStorage(NoteStorage.Archive);
    }

    #endregion

    #region Events

    protected async void WindowClosing(object sender, WindowClosingEventArgs e)
    {
        foreach (var timer in CategoryUpdateTimers.Values)
            if (!controller.SetCategoryName(timer.Id, timer.Name))
                await ShowMessageBox("Error", $"Failed to save category Id={timer.Id}.");

        foreach (var timer in NoteUpdateTimers.Values)
            if (!controller.SetNoteText(timer.Id, timer.Text))
                await ShowMessageBox("Error", $"Failed to save note Id={timer.Id}.");

        controller.Save();
    }

    [RelayCommand]
    private void SearchSelected()
    {
        if (SelectedCategory == null || SelectedNote == null)
            return;

        var selectedText = SelectedTextBox.SelectedText;
        if (!string.IsNullOrEmpty(selectedText))
            SearchBox.Text = selectedText;
    }

    private void ListViewLoaded(object sender, RoutedEventArgs e)
    {
        ListBox = (ListBox)sender;
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
            var searchResults = controller.Search(SearchBox.Text).ToList();
            TabControl.ItemsSource = searchResults;
            Title = $"Found {searchResults.Sum(c => c.Notes.Count)} notes in {searchResults.Count()} categories";
            ToggleSearchControls(true);
        }
    }

    private void NoteTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateNoteTextTimer();
        SelectedNote.ModificationDate = DateTime.Now;
    }

    private void SelectedCategoryNameChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCategoryNameTimer();
    }

    private void NoteTextBoxSelected(object sender, FocusChangedEventArgs e)
    {
        var textBox = e.NewFocusedElement as TextBox;
        var listBoxItem = textBox?.Parent as ContentPresenter;
        var note = listBoxItem?.Content as NoteModel;
        SelectedNote = note ?? throw new Exception("No note was selected.");
    }

    #endregion

    private void FocusOnNote(NoteModel note)
    {
        ListBox.SelectedItem = note;
        ListBox.UpdateLayout();
        var container = ListBox.ContainerFromItem(note) as ListBoxItem;
        var textBox = container?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        textBox?.Focus();
        textBox?.CaretIndex = note.Text.Length;
    }

    private void FocusOnNoteIfAny(int selectedIndex)
    {
        if (SelectedCategory.Notes.Count != 0)
        {
            var newIndex = Math.Clamp(selectedIndex, 0, ListBox.Items.Count - 1);
            FocusOnNote((NoteModel)ListBox.Items[newIndex]);
        }
    }

    private void UpdateWindowTitle()
    {
        Title = $"{Categories.SelectMany(c => c.Notes).Count()} notes in {Categories.Count} categories";
    }

    private void ReorderCategory(int newPosition)
    {
        if (SelectedCategory == null)
            return;

        if (newPosition < 0 || newPosition >= TabControl.Items.Count)
            return;

        if (!controller.ReorderCategory(SelectedCategory.Id, newPosition))
            throw new Exception($"Failed to reorder category to position={newPosition}.");

        Categories.Move(TabControl.SelectedIndex, newPosition);
        TabControl.SelectedIndex = newPosition;
    }

    private void ReorderNote(int newPosition)
    {
        if (SelectedNote == null || SelectedCategory == null)
            return;

        if (newPosition < 0 || newPosition >= SelectedCategory.Notes.Count)
            return;

        if (!controller.ReorderNote(SelectedNote.Id, newPosition))
            throw new Exception($"Failed to reorder note to position={newPosition}.");

        SelectedCategory.Notes.Move(SelectedCategory.Notes.IndexOf(SelectedNote), newPosition);
        FocusOnNote(SelectedNote);
    }

    private void ChangeNoteStorage(NoteStorage targetStorage)
    {
        if (NoteUpdateTimers.Remove(SelectedNote.Id, out var noteTimer))
            UpdateNoteTextCallback(noteTimer);

        if (!controller.ChangeNoteStorage(SelectedNote.Id, targetStorage))
            throw new Exception($"Failed to move note to storage={targetStorage}.");

        var lastSelectedIndex = SelectedCategory.Notes.IndexOf(SelectedNote);
        SelectedCategory.Notes.Remove(SelectedNote);
        UpdateWindowTitle();
        FocusOnNoteIfAny(lastSelectedIndex);
    }

    private void ChangeStorage(NoteStorage targetStorage)
    {
        foreach (var timer in CategoryUpdateTimers.Values)
            UpdateCategoryNameCallback(timer);
        foreach (var timer in NoteUpdateTimers.Values)
            UpdateNoteTextCallback(timer);

        controller.SetStorage(targetStorage);

        LoadData();
        UpdateWindowTitle();
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

    private void LoadData()
    {
        Categories = new ObservableCollection<CategoryModel>(controller.GetAll());
    }

    private void UpdateNoteTextTimer()
    {
        var timer = GetOrCreateNoteTimer(SelectedNote.Id);
        timer.Text = SelectedNote.Text;

        if (timer.IsEnabled)
            timer.Stop();
        else
            timer.Tick += (sender, _) => UpdateNoteTextCallback((NoteUpdateTimer)sender);

        timer.Start();

        NoteUpdateTimers[SelectedNote.Id] = timer;
    }

    private void UpdateCategoryNameTimer()
    {
        var timer = GetOrCreateCategoryTimer(SelectedCategory.Id);
        timer.Name = SelectedCategory.Name;

        if (timer.IsEnabled)
            timer.Stop();
        else
            timer.Tick += (sender, _) => UpdateCategoryNameCallback((CategoryUpdateTimer)sender);

        timer.Start();

        CategoryUpdateTimers[SelectedCategory.Id] = timer;
    }

    private NoteUpdateTimer GetOrCreateNoteTimer(long noteId)
    {
        if (NoteUpdateTimers.TryGetValue(noteId, out var existingTimer))
            return existingTimer;

        var timer = new NoteUpdateTimer
        {
            Interval = defaultTimerInterval,
            Id = noteId
        };

        return timer;
    }

    private CategoryUpdateTimer GetOrCreateCategoryTimer(long categoryId)
    {
        if (CategoryUpdateTimers.TryGetValue(categoryId, out var existingTimer))
            return existingTimer;

        var timer = new CategoryUpdateTimer
        {
            Interval = defaultTimerInterval,
            Id = categoryId
        };

        return timer;
    }

    private async void UpdateNoteTextCallback(NoteUpdateTimer timer)
    {
        if (!controller.SetNoteText(timer.Id, timer.Text))
            throw new Exception($"Failed to update note Id={timer.Id}.");
        timer.Stop();
    }

    private async void UpdateCategoryNameCallback(CategoryUpdateTimer timer)
    {
        if (!controller.SetCategoryName(timer.Id, timer.Name))
            throw new Exception($"Failed to update category Id={timer.Id}.");
        timer.Stop();
    }

    private void RemoveNoteTimer(long noteId)
    {
        NoteUpdateTimers.Remove(noteId, out var noteTimer);
        noteTimer?.Stop();
    }

    private void RemoveCategoryTimer(long categoryId)
    {
        CategoryUpdateTimers.Remove(categoryId, out var categoryTimer);
        categoryTimer?.Stop();
    }

    private static async Task ShowMessageBox(string title, string text)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.Ok, MsgBoxIcon.Info);

        await messageBox.ShowAsync();
    }

    private static async Task<bool> ShowMessageBoxConfirm(string title, string text)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.YesNo, MsgBoxIcon.Warning);

        var result = await messageBox.ShowAsync();
        return result == ButtonResult.Yes;
    }
}

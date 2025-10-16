using DesktopHelper.Models.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DesktopHelper.Commands;

namespace DesktopHelper.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Service to load and save tasks
        private readonly TaskService _taskService;
        private readonly CalendarImportService _calendarImportService;

        // Holds the list of tasks displayed in the UI
        public static ObservableCollection<TaskItem> _tasks;

        // Backing field for the displayed timer text
        private string _timerDisplay = "25:00";

        // Backing field for enabling/disabling the helper bubble feature
        private bool _isHelperEnabled = true;

        // Indicates whether the helper feature is enabled (bound to ToggleButton in the UI)
        // When set, it updates the global flag used by the rendering logic
        public bool IsHelperEnabled
        {
            get => _isHelperEnabled;
            set
            {
                if (_isHelperEnabled != value)
                {
                    _isHelperEnabled = value;
                    OnPropertyChanged();

                    // Update the global helper flag to reflect current toggle state
                    MainHelper.HelperEnable = _isHelperEnabled;
                }
            }
        }

        // The text displayed in the focus timer
        public string TimerDisplay
        {
            get => _timerDisplay;
            set
            {
                if (_timerDisplay != value)
                {
                    _timerDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        // The text on the "Start" button
        public string StartButtonText { get; set; } = "Start";

        // The text on the "Reset" button
        public string ResetButtonText { get; set; } = "Reset";

        // The text on the "Settings" button
        public string SettingsButtonText { get; set; } = "Settings";

        // The text on the "Add" button
        public string AddButtonText { get; set; } = "Add";

        // The text on the "Import" button
        private string _importButtonText = "Import from Google";

        public string ImportButtonText
        {
            get => _importButtonText;
            private set
            {
                if (_importButtonText != value)
                {
                    _importButtonText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _importStatusMessage = "Connect your Google account to import events.";

        public string ImportStatusMessage
        {
            get => _importStatusMessage;
            private set
            {
                if (_importStatusMessage != value)
                {
                    _importStatusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Command to start the focus timer
        public ICommand StartTimerCommand { get; }

        // Command to reset the focus timer
        public ICommand ResetTimerCommand { get; }

        // Command to add a new task
        public ICommand AddTaskCommand { get; }

        // Command to delete an existing task
        public ICommand DeleteTaskCommand { get; }

        // Command to import tasks from a calendar URL
        public ICommand ImportCalendarCommand { get; }

        private bool _isImportingCalendar;

        public bool IsImportingCalendar
        {
            get => _isImportingCalendar;
            private set
            {
                if (_isImportingCalendar != value)
                {
                    _isImportingCalendar = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsImportButtonEnabled));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsImportButtonEnabled => !IsImportingCalendar;

        public MainViewModel()
        {
            _taskService = new TaskService();
            _calendarImportService = new CalendarImportService();
            Tasks = new ObservableCollection<TaskItem>();

            // Bind commands to their methods
            AddTaskCommand = new RelayCommand(AddTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask, CanDeleteTask);
            StartTimerCommand = new RelayCommand(StartTimer);
            ResetTimerCommand = new RelayCommand(ResetTimer);
            ImportCalendarCommand = new RelayCommand(ImportCalendar, CanImportCalendar);

            // Load tasks from file on startup
            LoadTasks();
        }

        // Currently does nothing to the timer text
        // Implementation will be added later
        private void StartTimer()
        {
            // Timer functionality not implemented yet
        }

        // Resets the timer text to its default state
        private void ResetTimer()
        {
            TimerDisplay = "25:00";
        }

        // Holds all tasks shown in the DataGrid
        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set
            {
                // Detach event handlers from old list
                if (_tasks != null)
                {
                    _tasks.CollectionChanged -= Tasks_CollectionChanged;
                    foreach (var task in _tasks)
                    {
                        task.PropertyChanged -= Task_PropertyChanged;
                    }
                }

                _tasks = value;

                // Attach event handlers to new list
                if (_tasks != null)
                {
                    foreach (var task in _tasks)
                    {
                        task.PropertyChanged += Task_PropertyChanged;
                    }
                    _tasks.CollectionChanged += Tasks_CollectionChanged;
                }

                OnPropertyChanged(nameof(Tasks));
            }
        }

        // Loads tasks from an external file asynchronously
        private async void LoadTasks()
        {
            var loadedTasks = await _taskService.LoadFromFileAsync();
            if (loadedTasks != null)
            {
                Tasks = new ObservableCollection<TaskItem>(loadedTasks);
            }
        }

        // Creates a new task, adds it to the list, and saves
        private void AddTask()
        {
            var newTask = new TaskItem { TaskName = "New Task", DueDate = null, HasReminder = false };
            Tasks.Add(newTask);
            SaveTasks();
        }

        // Deletes a task from the list, if found, and saves
        private void DeleteTask(TaskItem task)
        {
            if (task != null && Tasks.Contains(task))
            {
                Tasks.Remove(task);
                SaveTasks();
            }
        }

        // Saves current tasks to an external file asynchronously
        public async void SaveTasks()
        {
            if (Tasks != null)
            {
                await _taskService.SaveToFileAsync(new List<TaskItem>(Tasks));
            }
        }

        // Determines if a task can be deleted
        private bool CanDeleteTask(TaskItem task) => task != null;

        // Determines if importing can occur
        private bool CanImportCalendar() => !IsImportingCalendar;

        // Imports tasks from a calendar URL and merges them into the task list
        private async void ImportCalendar()
        {
            if (IsImportingCalendar)
            {
                return;
            }

            IsImportingCalendar = true;
            ImportButtonText = "Importing...";
            ImportStatusMessage = "Signing in to Google Calendar...";

            try
            {
                var importedTasks = await _calendarImportService.ImportFromGoogleCalendarAsync();
                if (importedTasks == null || importedTasks.Count == 0)
                {
                    ImportStatusMessage = "No events were returned from Google Calendar.";
                    return;
                }

                if (Tasks == null)
                {
                    Tasks = new ObservableCollection<TaskItem>();
                }

                int addedCount = 0;
                foreach (var importedTask in importedTasks)
                {
                    if (importedTask == null)
                    {
                        continue;
                    }

                    bool alreadyExists = false;

                    if (!string.IsNullOrWhiteSpace(importedTask.ExternalId))
                    {
                        alreadyExists = Tasks.Any(existingTask =>
                            existingTask != null &&
                            string.Equals(existingTask.ExternalId, importedTask.ExternalId, StringComparison.OrdinalIgnoreCase));
                    }

                    if (!alreadyExists)
                    {
                        alreadyExists = Tasks.Any(existingTask =>
                            existingTask != null &&
                            string.Equals(existingTask.TaskName?.Trim(), importedTask.TaskName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            Nullable.Equals(existingTask.DueDate?.Date, importedTask.DueDate?.Date));
                    }

                    if (alreadyExists)
                    {
                        continue;
                    }

                    importedTask.TaskName = string.IsNullOrWhiteSpace(importedTask.TaskName)
                        ? "Calendar Event"
                        : importedTask.TaskName.Trim();

                    Tasks.Add(importedTask);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    SaveTasks();
                    ImportStatusMessage = $"Imported {addedCount} event{(addedCount == 1 ? string.Empty : "s")} from Google Calendar.";
                }
                else
                {
                    ImportStatusMessage = "No new events to import.";
                }
            }
            catch (FileNotFoundException)
            {
                ImportStatusMessage = "Place your google-credentials.json file next to the app and try again.";
            }
            catch (OperationCanceledException)
            {
                ImportStatusMessage = "Google Calendar authorization was canceled.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to import calendar: {ex}");
                ImportStatusMessage = "Unable to import Google Calendar events. Check the log for details.";
            }
            finally
            {
                IsImportingCalendar = false;
                ImportButtonText = "Import from Google";
            }
        }

        // Called whenever any task changes, triggering a save
        private void Task_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveTasks();
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is TaskItem task)
                    {
                        task.PropertyChanged += Task_PropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is TaskItem task)
                    {
                        task.PropertyChanged -= Task_PropertyChanged;
                    }
                }
            }
        }
    }
}

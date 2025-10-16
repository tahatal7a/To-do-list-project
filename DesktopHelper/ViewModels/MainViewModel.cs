using DesktopHelper.Models.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DesktopHelper.Commands;
using System.Collections.Generic;

namespace DesktopHelper.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Service to load and save tasks
        private readonly TaskService _taskService;

        // Holds the list of tasks displayed in the UI
        public static ObservableCollection<TaskItem> _tasks;

        // Backing field for the displayed timer text
        private string _timerDisplay = "25:00";

        // Backing field for the Calendar URL input
        private string _calendarUrl = "";

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

        // The label that appears beside the Calendar URL text box
        public string CalendarUrlLabel { get; set; } = "Calendar URL:";

        // The URL used for calendar import
        public string CalendarUrl
        {
            get => _calendarUrl;
            set
            {
                if (_calendarUrl != value)
                {
                    _calendarUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        // The text on the "Import" button
        public string ImportButtonText { get; set; } = "Import";

        // Command to start the focus timer
        public ICommand StartTimerCommand { get; }

        // Command to reset the focus timer
        public ICommand ResetTimerCommand { get; }

        // Command to add a new task
        public ICommand AddTaskCommand { get; }

        // Command to delete an existing task
        public ICommand DeleteTaskCommand { get; }

        public MainViewModel()
        {
            _taskService = new TaskService();
            Tasks = new ObservableCollection<TaskItem>();

            // Bind commands to their methods
            AddTaskCommand = new RelayCommand(AddTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask, CanDeleteTask);
            StartTimerCommand = new RelayCommand(StartTimer);
            ResetTimerCommand = new RelayCommand(ResetTimer);

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

        // Called whenever any task changes, triggering a save
        private void Task_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveTasks();
        }
    }
}

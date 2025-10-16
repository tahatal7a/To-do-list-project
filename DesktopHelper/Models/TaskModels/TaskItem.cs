using DesktopHelper.ViewModels;
using System;

public class TaskItem : BaseViewModel
{
    private string _taskName;
    private DateTime? _dueDate;
    private bool _hasReminder;

    public string TaskName
    {
        get => _taskName;
        set
        {
            _taskName = value;
            OnPropertyChanged();
        }
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set
        {
            _dueDate = value;
            OnPropertyChanged();
        }
    }

    public bool HasReminder
    {
        get => _hasReminder;
        set
        {
            _hasReminder = value;
            OnPropertyChanged();
        }
    }
}

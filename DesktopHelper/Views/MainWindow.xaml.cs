using DesktopHelper.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Threading;

namespace DesktopHelper.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Thread t = new Thread(HelperThread);
            t.Start();
        }
        static void HelperThread()
        {
            HelperWindow.HelperMain();
        }
        private void TaskListGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.SaveTasks();
            }
        }
    }
}
using System;
using System.Windows;
using System.Windows.Input;

namespace FlashSearch.Viewer.Utilities
{
    public class MinimizeWindowCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is Window window)
                window.WindowState = WindowState.Minimized;
        }

        public event EventHandler CanExecuteChanged;
    }
    
    public class CloseWindowCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is Window window) 
                window.Close();
        }

        public event EventHandler CanExecuteChanged;
    }
}
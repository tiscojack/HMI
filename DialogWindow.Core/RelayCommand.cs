using System;
using System.Windows.Input;

namespace DialogWindow.Core
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Action Action { get; }


        public RelayCommand(Action action)
        {
            this.Action = action ?? throw new ArgumentNullException(nameof(action), "Action cannot be null");
        }

        public bool CanExecute(object parameter)
        {
            return true; // always executable; could create new implementation that can check this
        }

        public void Execute(object parameter)
        {
            this.Action();
        }
    }
}
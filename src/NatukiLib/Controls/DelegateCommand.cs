namespace NatukiLib.Controls
{
    using System;
    using System.Windows.Input;

    public sealed class DelegateCommand : ICommand
    {
        public DelegateCommand(Action<object?> action, Func<object?, bool>? canExecuteFunc = null)
        {
            Action = action;
            CanExecuteFunc = canExecuteFunc;
        }

        public event EventHandler? CanExecuteChanged;

        private bool IsExecuting { get; set; }

        public Action<object?> Action { get; init; }

        public Func<object?, bool>? CanExecuteFunc { get; init; }

        public bool CanExecute(object? parameter) => IsExecuting ? false : CanExecuteFunc?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
        {
            try
            {
                IsExecuting = true;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                Action.Invoke(parameter);
            }
            finally
            {
                IsExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

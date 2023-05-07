namespace NatukiLib.Controls
{
    using System.Windows.Input;

    public sealed class AsyncDelegateCommand : ICommand
    {
        public AsyncDelegateCommand(Func<object?, Task> action, Func<object?, bool>? canExecuteFunc = null)
        {
            Action = action;
            CanExecuteFunc = canExecuteFunc;
        }

        public event EventHandler? CanExecuteChanged;

        private bool IsExecuting { get; set; }

        public Func<object?, Task> Action { get; init; }

        public Func<object?, bool>? CanExecuteFunc { get; init; }

        public bool CanExecute(object? parameter) => IsExecuting ? false : CanExecuteFunc?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            try
            {
                IsExecuting = true;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                await Action.Invoke(parameter);
            }
            finally
            {
                IsExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

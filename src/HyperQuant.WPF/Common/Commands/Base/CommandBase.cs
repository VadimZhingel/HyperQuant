using System.Windows.Input;

namespace HyperQuant.WPF.Common.Commands.Base
{
    internal abstract class CommandBase : ICommand
    {
        event EventHandler? ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested += value;
        }

        bool ICommand.CanExecute(object? parameter) => CanExecute(parameter);

        void ICommand.Execute(object? parameter)
        {
            if (((ICommand)this).CanExecute(parameter))
                Execute(parameter);
        }

        protected virtual bool CanExecute(object? p) => true;

        protected abstract void Execute(object? p);

        public static void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

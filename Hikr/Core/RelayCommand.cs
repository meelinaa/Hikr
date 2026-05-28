using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hikr.Core;

/// <summary>
/// A command implementation that delegates its execution logic to actions.
/// </summary>
public class RelayCommand : ICommand, IAsyncCommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Event triggered when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the RelayCommand class.
    /// </summary>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

    /// <summary>
    /// Executes the command.
    /// </summary>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    public async Task ExecuteAsync(object? parameter)
    {
        _execute();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Raises the CanExecuteChanged event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A generic command implementation that delegates execution logic taking a parameter of type T.
/// </summary>
public class RelayCommand<T> : ICommand, IAsyncCommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    /// <summary>
    /// Event triggered when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the RelayCommand class with a generic parameter.
    /// </summary>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        if (parameter == null && typeof(T).IsValueType)
        {
            return _canExecute == null || _canExecute(default);
        }
        return _canExecute == null || _canExecute((T?)parameter);
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    public void Execute(object? parameter)
    {
        if (parameter == null && typeof(T).IsValueType)
        {
            _execute(default);
        }
        else
        {
            _execute((T?)parameter);
        }
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    public async Task ExecuteAsync(object? parameter)
    {
        Execute(parameter);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Raises the CanExecuteChanged event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Interface representing an asynchronous command execution.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    Task ExecuteAsync(object? parameter);
}

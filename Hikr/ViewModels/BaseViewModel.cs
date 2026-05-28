using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hikr.ViewModels;

/// <summary>
/// Abstract base class for all ViewModels, implementing INotifyPropertyChanged.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Event triggered when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Helper method to set a property and notify bindings of its change.
    /// </summary>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", System.Action? onChanged = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Triggers the PropertyChanged event.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

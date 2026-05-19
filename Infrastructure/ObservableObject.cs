using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MasselGUARD.Infrastructure
{
    /// <summary>
    /// Base class for all ViewModels and observable Models.
    /// Provides INotifyPropertyChanged and a SetField helper.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Sets the backing field and raises PropertyChanged only when the value changed.
        /// Returns true when a change occurred.
        /// </summary>
        protected bool SetField<T>(ref T field, T value,
            [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}

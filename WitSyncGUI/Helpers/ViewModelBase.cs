using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSyncGUI.Helpers
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        // cannot be instantiated
        protected ViewModelBase() { }

        /// <summary>
        /// The PropertyChanged Event to raise to any UI object
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The PropertyChanged Event to raise to any UI object
        /// The event is only invoked if data binding is used
        /// </summary>
        /// <param name="propertyName">The property name that is changing</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);

                // Raise the PropertyChanged event.
                handler(this, args);
            }
        }
    }
}

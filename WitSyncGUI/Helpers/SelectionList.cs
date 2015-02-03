using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WitSyncGUI.Helpers
{
    public class SelectableListItem<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isSelected;
        private T item;

        public SelectableListItem()
        { }

        public SelectableListItem(T item, bool isChecked = false)
        {
            this.item = item;
            this.isSelected = isChecked;
        }

        public T Item
        {
            get { return item; }
            set
            {
                item = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Item"));
            }
        }


        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsSelected"));
            }
        }
    }
    
    public class SelectionList<T> : ObservableCollection<SelectableListItem<T>>
    {
    }
}

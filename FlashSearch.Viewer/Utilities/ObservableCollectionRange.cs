using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FlashSearch.Viewer.Utilities
{
    public class ObservableCollectionRange<T> : ObservableCollection<T>
    {
        private bool _deferNotification = false;

        public ObservableCollectionRange()
        {
            
        }

        public ObservableCollectionRange(IEnumerable<T> init)
            : base(init)
        {
            
        }
        
        public void AddRange(IEnumerable<T> range)
        {
            _deferNotification = true;
            foreach (T v in range)
            {
                Items.Add(v);
            }
            _deferNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void ResetWith(IEnumerable<T> values)
        {
            _deferNotification = true;
            Items.Clear();
            foreach (T value in values)
            {
                Items.Add(value);
            }
            _deferNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_deferNotification)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
namespace NatukiLib.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public RangeObservableCollection()
            : base() { }

        public RangeObservableCollection(IEnumerable<T> collection)
            : base(collection) { }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection is null) throw new ArgumentNullException("collection");

            foreach (var item in collection)
                Items.Add(item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection is null) throw new ArgumentNullException("collection");

            foreach (var item in collection)
                Items.Remove(item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Replace(T item) => ReplaceRange(new T[] { item });

        public void ReplaceRange(IEnumerable<T> collection)
        {
            if (collection is null) throw new ArgumentNullException("collection");

            Items.Clear();
            foreach (var item in collection)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

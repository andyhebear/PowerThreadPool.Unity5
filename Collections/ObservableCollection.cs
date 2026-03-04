#define NET_2_0
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
//#if !NET_2_0
//using System.Runtime.CompilerServices;
//#endif

namespace PowerThreadPool_Net20.Collections
{
	/*
#if NET_2_0
	[Serializable]
#if !NET_2_0 && !MOBILE
	//[TypeForwardedFrom (Consts.WindowsBase_3_0)]
#endif
	public class ObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged {
		[Serializable]
#if !NET_2_0 && !MOBILE
		//[TypeForwardedFrom (Consts.WindowsBase_3_0)]
#endif
		sealed class SimpleMonitor : IDisposable {
			private int _busyCount;

			public SimpleMonitor()
			{
			}

			public void Enter()
			{
				_busyCount++;
			}

			public void Dispose()
			{
				_busyCount--;
			}

			public bool Busy
			{
				get { return _busyCount > 0; }
			}
		}

		private SimpleMonitor _monitor = new SimpleMonitor ();

		public ObservableCollection ()
		{
		}

		public ObservableCollection (IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException ("collection");

			foreach (var item in collection)
				Add (item);
		}

		public ObservableCollection (List<T> list)
			: base (list != null ? new List<T> (list) : null)
		{
		}

		[field:NonSerialized]
		public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
		[field:NonSerialized]
		protected virtual event PropertyChangedEventHandler PropertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
			add { this.PropertyChanged += value; }
			remove { this.PropertyChanged -= value; }
		}

		protected IDisposable BlockReentrancy ()
		{
			_monitor.Enter ();
			return _monitor;
		}

		protected void CheckReentrancy ()
		{
			NotifyCollectionChangedEventHandler eh = CollectionChanged;

			// Only have a problem if we have more than one event listener.
			if (_monitor.Busy && eh != null && eh.GetInvocationList ().Length > 1)
				throw new InvalidOperationException ("Cannot modify the collection while reentrancy is blocked.");
		}

		protected override void ClearItems ()
		{
			CheckReentrancy ();

			base.ClearItems ();

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
			OnPropertyChanged (new PropertyChangedEventArgs ("Count"));
			OnPropertyChanged (new PropertyChangedEventArgs ("Item[]"));
		}

		protected override void InsertItem (int index, T item)
		{
			CheckReentrancy ();

			base.InsertItem (index, item);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item, index));
			OnPropertyChanged (new PropertyChangedEventArgs ("Count"));
			OnPropertyChanged (new PropertyChangedEventArgs ("Item[]"));
		}

		public void Move (int oldIndex, int newIndex)
		{
			MoveItem (oldIndex, newIndex);
		}

		protected virtual void MoveItem (int oldIndex, int newIndex)
		{
			CheckReentrancy ();

			T item = Items [oldIndex];
			base.RemoveItem (oldIndex);
			base.InsertItem (newIndex, item);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
			OnPropertyChanged (new PropertyChangedEventArgs ("Item[]"));
		}

		protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler eh = CollectionChanged;

			if (eh != null) {
				// Make sure that the invocation is done before the collection changes,
				// Otherwise there's a chance of data corruption.
				using (BlockReentrancy ()) {
					eh (this, e);
				}
			}
		}

		protected virtual void OnPropertyChanged (PropertyChangedEventArgs e)
		{
			PropertyChangedEventHandler eh = PropertyChanged;

			if (eh != null)
				eh (this, e);
		}

		protected override void RemoveItem (int index)
		{
			CheckReentrancy ();

			T item = Items [index];

			base.RemoveItem (index);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, item, index));
			OnPropertyChanged (new PropertyChangedEventArgs ("Count"));
			OnPropertyChanged (new PropertyChangedEventArgs ("Item[]"));
		}

		protected override void SetItem (int index, T item)
		{
			CheckReentrancy ();

			T oldItem = Items [index];

			base.SetItem (index, item);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Replace, item, oldItem, index));
			OnPropertyChanged (new PropertyChangedEventArgs ("Item[]"));
		}
	
	}
	
#endif

	*/
}
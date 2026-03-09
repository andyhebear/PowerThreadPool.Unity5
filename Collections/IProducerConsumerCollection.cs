using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PowerThreadPool_Net20.Collections
{
	public interface IProducerConsumerCollection<T> : IEnumerable<T>, ICollection, IEnumerable
	{
		bool TryAdd(T item);
		bool TryTake(out T item);
		T[] ToArray();
		void CopyTo(T[] array,int index);
	}
}

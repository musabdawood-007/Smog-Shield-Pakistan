using System;
using System.Collections.Generic;

namespace SmogShield_Pakistan.DSA.Queue
{
    public class FifoQueue<T>
    {
        private readonly Queue<T> _queue = new();

        public int Count => _queue.Count;

        public void Enqueue(T item) => _queue.Enqueue(item);

        public T? Dequeue()
        {
            return _queue.Count > 0 ? _queue.Dequeue() : default;
        }

        public T? Peek()
        {
            return _queue.Count > 0 ? _queue.Peek() : default;
        }

        public List<T> Drain()
        {
            var result = new List<T>();
            while (_queue.Count > 0) result.Add(_queue.Dequeue());
            return result;
        }

        public List<T> ToList()
        {
            return new List<T>(_queue);
        }
    }
}

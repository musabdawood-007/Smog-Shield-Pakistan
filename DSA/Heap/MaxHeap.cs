using System;
using System.Collections.Generic;

namespace SmogShield_Pakistan.DSA.Heap
{
    public class MaxHeap<T>
    {
        private readonly List<T> _items = new();
        private readonly Func<T, double> _priority;

        public int Count => _items.Count;

        public MaxHeap(Func<T, double> priority)
        {
            _priority = priority;
        }

        public void Push(T item)
        {
            _items.Add(item);
            HeapifyUp(_items.Count - 1);
        }

        public T? Pop()
        {
            if (_items.Count == 0) return default;
            T root = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);
            if (_items.Count > 0) HeapifyDown(0);
            return root;
        }

        public T? Peek()
        {
            return _items.Count > 0 ? _items[0] : default;
        }

        public List<T> PopAll()
        {
            var result = new List<T>();
            while (_items.Count > 0) result.Add(Pop()!);
            return result;
        }

        public List<T> ToSortedList()
        {
            var copy = new List<T>(_items);
            var result = new List<T>();
            var tempHeap = new MaxHeap<T>(_priority);
            foreach (var item in copy) tempHeap.Push(item);
            while (tempHeap.Count > 0) result.Add(tempHeap.Pop()!);
            return result;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_priority(_items[index]) <= _priority(_items[parent])) break;
                (_items[index], _items[parent]) = (_items[parent], _items[index]);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int count = _items.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int largest = index;

                if (left < count && _priority(_items[left]) > _priority(_items[largest]))
                    largest = left;
                if (right < count && _priority(_items[right]) > _priority(_items[largest]))
                    largest = right;
                if (largest == index) break;

                (_items[index], _items[largest]) = (_items[largest], _items[index]);
                index = largest;
            }
        }
    }
}

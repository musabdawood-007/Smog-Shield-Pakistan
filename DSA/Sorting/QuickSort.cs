using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Sorting
{
    public static class QuickSort
    {
        public static void Sort(List<Reading> list, Func<Reading, double> key, bool descending = false)
        {
            if (list.Count <= 1) return;
            SortInternal(list, 0, list.Count - 1, key, descending);
        }

        private static void SortInternal(List<Reading> list, int low, int high,
            Func<Reading, double> key, bool descending)
        {
            if (low < high)
            {
                int pivotIndex = Partition(list, low, high, key, descending);
                SortInternal(list, low, pivotIndex - 1, key, descending);
                SortInternal(list, pivotIndex + 1, high, key, descending);
            }
        }

        private static int Partition(List<Reading> list, int low, int high,
            Func<Reading, double> key, bool descending)
        {
            double pivot = key(list[high]);
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                double current = key(list[j]);
                bool shouldSwap = descending ? current >= pivot : current <= pivot;
                if (shouldSwap)
                {
                    i++;
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            (list[i + 1], list[high]) = (list[high], list[i + 1]);
            return i + 1;
        }
    }
}

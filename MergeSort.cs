using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Sorting
{
    public static class MergeSort
    {
        public static void Sort(List<Reading> list, Func<Reading, double> key, bool descending = false)
        {
            if (list.Count <= 1) return;
            Reading[] temp = new Reading[list.Count];
            SortInternal(list, temp, 0, list.Count - 1, key, descending);
        }

        private static void SortInternal(List<Reading> list, Reading[] temp, int left, int right,
            Func<Reading, double> key, bool descending)
        {
            if (left >= right) return;
            int mid = left + (right - left) / 2;
            SortInternal(list, temp, left, mid, key, descending);
            SortInternal(list, temp, mid + 1, right, key, descending);
            Merge(list, temp, left, mid, right, key, descending);
        }

        private static void Merge(List<Reading> list, Reading[] temp, int left, int mid, int right,
            Func<Reading, double> key, bool descending)
        {
            for (int i = left; i <= right; i++) temp[i] = list[i];

            int l = left, r = mid + 1, k = left;
            while (l <= mid && r <= right)
            {
                double a = key(temp[l]);
                double b = key(temp[r]);
                bool takeLeft = descending ? a >= b : a <= b;
                list[k++] = takeLeft ? temp[l++] : temp[r++];
            }
            while (l <= mid) list[k++] = temp[l++];
            while (r <= right) list[k++] = temp[r++];
        }
    }
}

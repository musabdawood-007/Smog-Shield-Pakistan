using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Searching
{
    public static class BinarySearch
    {
        public static int FindByCity(List<Reading> sortedByCity, string city)
        {
            int low = 0, high = sortedByCity.Count - 1;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                int cmp = string.Compare(sortedByCity[mid].City, city, StringComparison.OrdinalIgnoreCase);
                if (cmp == 0) return mid;
                if (cmp < 0) low = mid + 1;
                else high = mid - 1;
            }
            return -1;
        }

        public static Reading? FindRecord(List<Reading> sortedByCity, string city)
        {
            int index = FindByCity(sortedByCity, city);
            return index >= 0 ? sortedByCity[index] : null;
        }

        public static int LowerBoundByPm25(List<Reading> sortedByPm25, double value)
        {
            int low = 0, high = sortedByPm25.Count;
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                if (sortedByPm25[mid].Pm25 < value) low = mid + 1;
                else high = mid;
            }
            return low;
        }
    }
}

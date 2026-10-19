using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Sorting
{
    public static class InsertionSort
    {
        public static void Sort(List<Reading> list, Func<Reading, double> key, bool descending = false)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Reading current = list[i];
                double currentKey = key(current);
                int j = i - 1;

                while (j >= 0)
                {
                    double prevKey = key(list[j]);
                    bool shouldShift = descending ? prevKey < currentKey : prevKey > currentKey;
                    if (!shouldShift) break;
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = current;
            }
        }
    }
}

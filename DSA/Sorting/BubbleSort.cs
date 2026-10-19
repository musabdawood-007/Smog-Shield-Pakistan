using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Sorting
{
    public static class BubbleSort
    {
        public static void Sort(List<Reading> list, Func<Reading, double> key, bool descending = false)
        {
            int n = list.Count;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    double a = key(list[j]);
                    double b = key(list[j + 1]);
                    bool shouldSwap = descending ? a < b : a > b;
                    if (shouldSwap)
                    {
                        (list[j], list[j + 1]) = (list[j + 1], list[j]);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
        }
    }
}

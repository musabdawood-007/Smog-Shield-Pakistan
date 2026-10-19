using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Searching
{
    public static class LinearSearch
    {
        public static Reading? FindByCity(List<Reading> list, string city)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].City, city, StringComparison.OrdinalIgnoreCase))
                    return list[i];
            }
            return null;
        }

        public static List<Reading> FindAbove(List<Reading> list, double threshold)
        {
            var result = new List<Reading>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Pm25 > threshold)
                    result.Add(list[i]);
            }
            return result;
        }

        public static int IndexOf(List<Reading> list, string city)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].City, city, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}

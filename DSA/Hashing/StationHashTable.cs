using System;
using System.Collections.Generic;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.DSA.Hashing
{
    public class StationHashTable
    {
        private readonly Dictionary<string, Reading> _byCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Reading>> _byCity = new(StringComparer.OrdinalIgnoreCase);

        public int Count => _byCode.Count;

        public void Insert(Reading reading)
        {
            _byCode[reading.Code] = reading;

            if (!_byCity.TryGetValue(reading.City, out var list))
            {
                list = new List<Reading>();
                _byCity[reading.City] = list;
            }
            list.Add(reading);
        }

        public void InsertRange(IEnumerable<Reading> readings)
        {
            foreach (var r in readings) Insert(r);
        }

        public Reading? GetByCode(string code)
        {
            return _byCode.TryGetValue(code, out var r) ? r : null;
        }

        public List<Reading> GetByCity(string city)
        {
            return _byCity.TryGetValue(city, out var list) ? list : new List<Reading>();
        }

        public List<string> AllCities()
        {
            return new List<string>(_byCity.Keys);
        }

        public bool Remove(string code)
        {
            if (!_byCode.TryGetValue(code, out var r)) return false;
            _byCode.Remove(code);
            if (_byCity.TryGetValue(r.City, out var list))
            {
                list.Remove(r);
                if (list.Count == 0) _byCity.Remove(r.City);
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace SmogShield_Pakistan.DSA.Graph
{
    public class StationGraph
    {
        private readonly Dictionary<string, List<(string Station, double DistanceKm)>> _adj =
            new(StringComparer.OrdinalIgnoreCase);

        public void AddStation(string station)
        {
            if (!_adj.ContainsKey(station))
                _adj[station] = new List<(string, double)>();
        }

        public void AddEdge(string from, string to, double distanceKm)
        {
            AddStation(from);
            AddStation(to);
            _adj[from].Add((to, distanceKm));
            _adj[to].Add((from, distanceKm));
        }

        public List<string> Neighbors(string station)
        {
            return _adj.TryGetValue(station, out var list)
                ? list.Select(n => n.Station).ToList()
                : new List<string>();
        }

        public List<(string Station, double DistanceKm)> NeighborsWithDistance(string station)
        {
            return _adj.TryGetValue(station, out var list)
                ? new List<(string, double)>(list)
                : new List<(string, double)>();
        }

        public List<List<string>> BfsLevels(string start)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
            var levels = new List<List<string>>();
            var queue = new Queue<string>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int levelSize = queue.Count;
                var currentLevel = new List<string>();
                for (int i = 0; i < levelSize; i++)
                {
                    string station = queue.Dequeue();
                    currentLevel.Add(station);
                    foreach (var neighbor in Neighbors(station))
                    {
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }
                levels.Add(currentLevel);
            }
            return levels;
        }

        public List<string> BfsOrder(string start)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
            var order = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                string station = queue.Dequeue();
                order.Add(station);
                foreach (var neighbor in Neighbors(station))
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
            return order;
        }

        public List<string> AllStations()
        {
            return new List<string>(_adj.Keys);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.Services
{
    public static class CsvDataService
    {
        private static readonly string CsvPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "sample_readings.csv");

        public static List<Reading> LoadSampleCsv()
        {
            var result = new List<Reading>();
            try
            {
                if (!File.Exists(CsvPath)) return result;

                var lines = File.ReadAllLines(CsvPath);
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = SplitCsvLine(lines[i]);
                    if (parts.Length < 9) continue;

                    if (!double.TryParse(parts[4], out double pm25)) continue;
                    if (!double.TryParse(parts[5], out double pm10)) continue;
                    if (!double.TryParse(parts[6], out double aqi)) continue;
                    if (!DateTime.TryParse(parts[8], out DateTime observed)) observed = DateTime.Now;

                    result.Add(new Reading
                    {
                        Code = parts[0],
                        City = parts[1],
                        Station = parts[2],
                        Location = parts[3],
                        Pm25 = pm25,
                        Pm10 = pm10,
                        Aqi = aqi,
                        Source = parts[7],
                        ObservedAt = observed
                    });
                }
            }
            catch
            {
            }
            return result;
        }

        public static void ExportReadings(IReadOnlyList<Reading> readings, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Code,City,Station,Location,Pm25,Pm10,Aqi,Source,ObservedAt");
                foreach (var r in readings)
                {
                    sb.AppendLine($"{Escape(r.Code)},{Escape(r.City)},{Escape(r.Station)}," +
                                  $"{Escape(r.Location)},{r.Pm25},{r.Pm10},{r.Aqi}," +
                                  $"{Escape(r.Source)},{r.ObservedAt:yyyy-MM-dd HH:mm:ss}");
                }
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public static void ExportVisitors(IReadOnlyList<VisitorLog> visitors, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,Name,Email,Purpose,Page,VisitCount,VisitedAt");
                foreach (var v in visitors)
                {
                    sb.AppendLine($"{v.Id},{Escape(v.Name)},{Escape(v.Email)}," +
                                  $"{Escape(v.Details)},{Escape(v.Page)},{v.VisitCount},{v.VisitedAt:yyyy-MM-dd HH:mm:ss}");
                }
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                            inQuotes = false;
                    }
                    else
                        current.Append(c);
                }
                else
                {
                    if (c == '"')
                        inQuotes = true;
                    else if (c == ',')
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else
                        current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}

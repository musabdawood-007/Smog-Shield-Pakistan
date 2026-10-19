using System;
using System.IO;
using System.Text.Json;

namespace SmogShield_Pakistan.Services
{
    public enum DisplayUnit { UgM3, Aqi }

    public static class AppSettings
    {
        private sealed class Persisted
        {
            public DisplayUnit Unit { get; set; } = DisplayUnit.UgM3;
            public int Threshold { get; set; } = 75;
        }

        private static readonly string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmogShield", "settings.json");

        public static DisplayUnit Unit { get; private set; } = DisplayUnit.UgM3;
        public static int Threshold { get; private set; } = 75;

        public static string VisitorName { get; set; } = "Visitor";
        public static string VisitorEmail { get; set; } = "";
        public static string VisitorPurpose { get; set; } = "";

        public static event Action? Changed;

        public static string UnitLabel => Unit == DisplayUnit.UgM3 ? "µg/m³" : "US AQI";
        public static string ShortUnit => Unit == DisplayUnit.UgM3 ? "µg/m³" : "AQI";

        public static string Initials
        {
            get
            {
                var parts = (VisitorName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "V";
                string first = parts[0].Substring(0, 1).ToUpperInvariant();
                string last = parts.Length > 1 ? parts[^1].Substring(0, 1).ToUpperInvariant() : "";
                return first + last;
            }
        }

        public static string RoleText =>
            string.IsNullOrWhiteSpace(VisitorPurpose) ? "Guest · Visitor" : VisitorPurpose + " · Visitor";

        public static double Num(double pm25, double aqi) =>
            Unit == DisplayUnit.UgM3 ? pm25 : aqi;

        public static string Str(double pm25, double aqi) =>
            Unit == DisplayUnit.UgM3 ? pm25.ToString("0.0") : aqi.ToString("0");

        static AppSettings()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var p = JsonSerializer.Deserialize<Persisted>(File.ReadAllText(FilePath));
                    if (p is not null)
                    {
                        Unit = p.Unit;
                        Threshold = Math.Clamp(p.Threshold, 0, 500);
                    }
                }
            }
            catch { }
        }

        public static void SetUnit(DisplayUnit unit)
        {
            if (Unit == unit) return;
            Unit = unit;
            Save();
            Changed?.Invoke();
        }

        public static void SetThreshold(int threshold)
        {
            threshold = Math.Clamp(threshold, 0, 500);
            if (Threshold == threshold) return;
            Threshold = threshold;
            Save();
            Changed?.Invoke();
        }

        public static void SetSession(string name, string email, string purpose)
        {
            VisitorName = string.IsNullOrWhiteSpace(name) ? "Visitor" : name.Trim();
            VisitorEmail = email?.Trim() ?? "";
            VisitorPurpose = purpose?.Trim() ?? "";
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(new Persisted
                {
                    Unit = Unit,
                    Threshold = Threshold
                }));
            }
            catch { }
        }
    }
}

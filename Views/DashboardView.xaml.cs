using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SmogShield_Pakistan.Models;
using SmogShield_Pakistan.Services;

namespace SmogShield_Pakistan.Views
{

    public class CityReading
    {
        public string City { get; set; } = "";
        public int Aqi { get; set; }
        public double Pm25 { get; set; }
        public string Status { get; set; } = "";
        public bool AboveThreshold { get; set; }
        public double[] Sparkline { get; set; } = Array.Empty<double>();
    }

    public class StationRow
    {
        public string Rank { get; set; } = "";
        public string Station { get; set; } = "";
        public string City { get; set; } = "";
        public string Value { get; set; } = "";
        public string Flag { get; set; } = "";
        public Brush RowBg { get; set; } = Brushes.Transparent;
        public Brush RankBg { get; set; } = Brushes.Transparent;
        public Brush RankFg { get; set; } = Brushes.Black;
        public Brush FlagBg { get; set; } = Brushes.Transparent;
        public Brush FlagFg { get; set; } = Brushes.Black;
    }

    public partial class DashboardView : UserControl
    {

        private List<CityReading> _cities = new();
        private List<StationRow> _stations = new();

        private readonly double[] _aqiScale = { 50, 100, 150, 200, 250 };

        private List<Reading> _readings = new();

        private static readonly Brush GreenBg = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
        private static readonly Brush RedBg = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
        private static readonly Brush GrayBg = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
        private static readonly Brush RedFg = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        private static readonly Brush GrayFg = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));

        public DashboardView()
        {
            InitializeComponent();
            Loaded += (_, _) => ApplySession();
            AppSettings.Changed += OnSettingsChanged;
            Services.WorldClock.Ticked += () => DateText.Text = Services.WorldClock.ShortText();
            DateText.Text = Services.WorldClock.ShortText();
            IsVisibleChanged += async (_, _) =>
            {
                if (!IsVisible) return;
                await LoadDataAsync();
            };
        }

        private void OnSettingsChanged()
        {
            ApplySession();
            LoadCityCards();
            BuildStations();
            LoadStations();
            DrawSparklines();
        }

        private void ApplySession()
        {
            try
            {
                VisitorNameText.Text = AppSettings.VisitorName;
                VisitorInitials.Text = AppSettings.Initials;
                VisitorRoleText.Text = AppSettings.RoleText;
                WelcomeText.Text = $"Welcome, {AppSettings.VisitorName.Split(' ')[0]}. Explore city conditions and the stations that need attention.";
                AlertLimitText.Text = $"PM2.5 alert: > {AppSettings.Threshold} µg/m³";
            }
            catch { }
        }

        public void SetVisitorName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            AppSettings.VisitorName = name.Trim();
            ApplySession();
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                ApplySession();
                SourceBadgeText.Text = OpenAqService.IsSyncing ? "  SYNCING LIVE DATA…" : OpenAqService.SourceLabel;
                UpdateSourceBadge();

                _readings = await OpenAqService.GetReadingsAsync();

                BuildCities();
                BuildStations();
                LoadCityCards();
                LoadStations();
                DrawSparklines();
                DrawMainChart();
                UpdateSourceBadge();

                _ = System.Threading.Tasks.Task.Run(() => DatabaseHelper.SaveReadings(_readings));
            }
            catch { }
        }

        private void UpdateSourceBadge()
        {
            SourceBadgeText.Text = OpenAqService.IsSyncing ? "  SYNCING LIVE DATA…" : OpenAqService.SourceLabel;
            bool live = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live;
            SourceBadgeText.Foreground = live
                ? new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F))
                : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
            TxtChartSub.Text = live
                ? $"US AQI · hourly series · synced {OpenAqService.LastFetchTime:HH:mm}"
                : "US AQI · hourly series · sample readings";
        }

        private void BuildCities()
        {
            var preferred = new[] { "Lahore", "Karachi", "Islamabad" };
            var byCity = _readings
                .GroupBy(r => r.City, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var picked = new List<Reading>();
            foreach (var c in preferred)
                if (byCity.TryGetValue(c, out var r)) picked.Add(r);

            foreach (var r in _readings.OrderByDescending(r => r.Pm25))
                if (picked.Count >= 3) break;
                else if (!picked.Any(p => string.Equals(p.City, r.City, StringComparison.OrdinalIgnoreCase)))
                    picked.Add(r);

            _cities = picked.Take(3).Select(r => new CityReading
            {
                City = r.City,
                Aqi = (int)r.Aqi,
                Pm25 = r.Pm25,
                Status = r.AqiCategory,
                AboveThreshold = r.Pm25 > AppSettings.Threshold,
                Sparkline = SynthSeries(r.Aqi, r.City)
            }).ToList();
        }

        private void BuildStations()
        {
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            var rows = _readings
                .OrderByDescending(r => r.Pm25)
                .Take(5)
                .Select((r, i) => new StationRow
                {
                    Rank = (i + 1).ToString("00"),
                    Station = r.Station,
                    City = r.City,
                    Value = ug ? r.Pm25.ToString("0.0") : OpenAqService.Pm25ToAqi(r.Pm25).ToString("0"),
                    Flag = r.Pm25 > AppSettings.Threshold ? $"ABOVE {AppSettings.Threshold}" : "Within threshold"
                })
                .ToList();

            TxtStationsValueHeader.Text = ug ? "PM2.5 (µg/m³)" : "US AQI (index)";
            TxtLatestTitle.Text = ug ? "LATEST PM2.5" : "LATEST AQI";

            var aboveBg = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
            var greenBg = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
            var greenBg2 = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
            var greenFg = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            var gray6 = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            var gray9 = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));

            for (int i = 0; i < rows.Count; i++)
            {
                bool above = rows[i].Flag.StartsWith("ABOVE");
                bool top = i == 0;
                rows[i].RowBg = top ? greenBg : Brushes.Transparent;
                rows[i].RankBg = top ? greenBg2 : Brushes.Transparent;
                rows[i].RankFg = top ? greenFg : gray6;
                rows[i].FlagBg = above ? aboveBg : Brushes.Transparent;
                rows[i].FlagFg = above ? RedFg : gray9;
            }

            _stations = rows;
        }

        private static double[] SynthSeries(double center, string seed)
        {
            var rnd = new Random(seed.GetHashCode());
            var values = new double[20];
            double v = center * 0.85;
            for (int i = 0; i < values.Length; i++)
            {
                v += (center - v) * 0.15 + (rnd.NextDouble() - 0.5) * center * 0.06;
                values[i] = Math.Max(1, v);
            }
            values[^1] = center;
            return values;
        }

        public void LoadCityCards()
        {
            if (_cities.Count < 3) return;

            SetCityCard(_cities[0], TxtLahoreAqi, TxtLahoreStatus, LahoreUnit, LahoreBadge, LahoreBadgeText);
            SetCityCard(_cities[1], TxtKarachiAqi, TxtKarachiStatus, KarachiUnit, KarachiBadge, KarachiBadgeText);
            SetCityCard(_cities[2], TxtIslamabadAqi, TxtIslamabadStatus, IslamabadUnit, IslamabadBadge, IslamabadBadgeText);

            TxtLatestLahore.Text = AppSettings.Str(_cities[0].Pm25, _cities[0].Aqi);
            TxtLatestKarachi.Text = AppSettings.Str(_cities[1].Pm25, _cities[1].Aqi);
            TxtLatestIslamabad.Text = AppSettings.Str(_cities[2].Pm25, _cities[2].Aqi);
        }

        private static void SetCityCard(CityReading c, TextBlock big, TextBlock status,
            TextBlock unitLabel, Border badge, TextBlock badgeText)
        {
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            big.Text = ug ? c.Pm25.ToString("0.0") : c.Aqi.ToString();
            unitLabel.Text = ug ? "  µg/m³" : "  US AQI";
            status.Text = ug ? PmCategory(c.Pm25) : c.Status;

            bool above = c.Pm25 > AppSettings.Threshold;
            badge.Background = above ? RedBg : GrayBg;
            badgeText.Text = above ? "ABOVE THRESHOLD" : "BELOW THRESHOLD";
            badgeText.Foreground = above ? RedFg : GrayFg;
        }

        private static string PmCategory(double pm25) => pm25 switch
        {
            <= 12.0 => "Good",
            <= 35.4 => "Moderate",
            <= 55.4 => "Unhealthy (sensitive)",
            <= 150.4 => "Unhealthy",
            <= 250.4 => "Very unhealthy",
            _ => "Hazardous"
        };

        public void LoadStations()
        {
            StationsList.ItemsSource = _stations;
            UpdateHeap();
        }

        private void UpdateHeap()
        {
            if (_stations.Count == 0) return;

            TxtHeapRoot.Text = _stations[0].Value;
            TxtExtractStation.Text = _stations[0].Station;
            TxtExtractValue.Text = _stations[0].Value;
            if (_stations.Count > 1) TxtHeapC1.Text = _stations[1].Value;
            if (_stations.Count > 2) TxtHeapC2.Text = _stations[2].Value;
            if (_stations.Count > 3) TxtHeapG1.Text = _stations[3].Value;
            if (_stations.Count > 4) TxtHeapG2.Text = _stations[4].Value;

            TxtHeapUnitLabel.Text = AppSettings.Unit == DisplayUnit.UgM3
                ? "Peak OTD · Extract O(log n) · PM2.5 in µg/m³"
                : "Peak OTD · Extract O(log n) · US AQI";
        }

        private void DrawSparklines()
        {
            if (_cities.Count < 3) return;
            DrawSparkline(SparkLahore, _cities[0].Sparkline);
            DrawSparkline(SparkKarachi, _cities[1].Sparkline);
            DrawSparkline(SparkIslamabad, _cities[2].Sparkline);
        }

        private static void DrawSparkline(Canvas canvas, double[] values)
        {
            if (values == null || values.Length < 2) return;
            canvas.Children.Clear();

            double min = double.MaxValue, max = double.MinValue;
            foreach (var v in values) { if (v < min) min = v; if (v > max) max = v; }
            double range = max - min; if (range < 1) range = 1;

            double w = canvas.Width, h = canvas.Height;
            var pts = new PointCollection();
            for (int i = 0; i < values.Length; i++)
            {
                double x = i * (w / (values.Length - 1));
                double y = h - 2 - ((values[i] - min) / range) * (h - 4);
                pts.Add(new Point(x, y));
            }

            canvas.Children.Add(new Polyline
            {
                Points = pts,
                Stroke = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                StrokeThickness = 1.8,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
        }

        public void DrawMainChart()
        {
            MainChart.Children.Clear();
            double w = 720, h = 200;
            double yMin = 40, yMax = 250;

            foreach (var tick in _aqiScale)
            {
                double y = h - ((tick - yMin) / (yMax - yMin)) * h;
                MainChart.Children.Add(new Line { X1 = 40, Y1 = y, X2 = w, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6)), StrokeThickness = 1 });
                var lbl = new System.Windows.Controls.TextBlock
                {
                    Text = tick.ToString(),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
                };
                Canvas.SetLeft(lbl, 4);
                Canvas.SetTop(lbl, y - 7);
                MainChart.Children.Add(lbl);
            }

            var palette = new[]
            {
                new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                new SolidColorBrush(Color.FromRgb(0x4B, 0x55, 0x63)),
                new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
            };
            for (int si = 0; si < _cities.Count; si++)
            {
                var values = _cities[si].Sparkline;
                if (values.Length < 2) continue;
                var pts = new PointCollection();
                for (int i = 0; i < values.Length; i++)
                {
                    double x = 40 + i * ((w - 45) / (values.Length - 1));
                    double y = h - ((values[i] - yMin) / (yMax - yMin)) * h;
                    pts.Add(new Point(x, Math.Clamp(y, 0, h)));
                }
                MainChart.Children.Add(new Polyline
                {
                    Points = pts,
                    Stroke = palette[si % palette.Length],
                    StrokeThickness = 2,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });
            }
        }

        private void SetActiveTimeButton(Border active, Border b1, Border b2)
        {
            active.Style = (Style)FindResource("TimeBtnActive");
            if (active.Child is TextBlock atb)
                atb.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            b1.Style = (Style)FindResource("TimeBtnIdle");
            if (b1.Child is TextBlock t1)
                t1.Foreground = (Brush)FindResource("GrayText");
            b2.Style = (Style)FindResource("TimeBtnIdle");
            if (b2.Child is TextBlock t2)
                t2.Foreground = (Brush)FindResource("GrayText");
        }

        private void Btn24h_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTimeButton(Btn24h, Btn7d, Btn30d);
            DrawMainChart();
        }

        private void Btn7d_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTimeButton(Btn7d, Btn24h, Btn30d);
            DrawMainChart();
        }

        private void Btn30d_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTimeButton(Btn30d, Btn24h, Btn7d);
            DrawMainChart();
        }

        private void NavRanking_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowRanking();
        }

        private void NavCompare_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowCompare();
        }

        private void NavSearch_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowSearch();
        }

        private void NavSettings_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowSettings();
        }

        private void BtnEndSession_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowLanding();
        }

        private void UpdateMaxIcon()
        {
            if (MaxPath == null || Window.GetWindow(this) is not Window win) return;
            var key = win.WindowState == WindowState.Maximized ? "Icon.WindowRestore" : "Icon.WindowMaximize";
            if (TryFindResource(key) is Geometry geo)
                MaxPath.Data = geo;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window win)
                win.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is not Window win) return;
            win.WindowState = win.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            UpdateMaxIcon();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is not Window win) return;
            if (e.ClickCount == 2)
            {
                win.WindowState = win.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                UpdateMaxIcon();
            }
            else
            {
                win.DragMove();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SmogShield_Pakistan.Services;

namespace SmogShield_Pakistan.Views
{
    public partial class CompareView : UserControl
    {

        private sealed class CompareCity
        {
            public string Key = "";
            public string Name = "";
            public string Region = "";
            public double Pm25;
            public int Aqi;
            public string AqiLabel = "";
            public double[] Trend = Array.Empty<double>();
        }

        private static readonly CompareCity[] Cities = BuildCities();

        private static CompareCity[] BuildCities()
        {
            var samples = OpenAqService.SampleData()
                .GroupBy(r => r.City, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Pm25, StringComparer.OrdinalIgnoreCase);

            return OpenAqService.Catalog.Select(c =>
            {
                double pm25 = samples.TryGetValue(c.City, out double s) ? s : 35 + c.City.Sum(ch => ch) % 80;
                int aqi = (int)OpenAqService.Pm25ToAqi(pm25);
                return new CompareCity
                {
                    Key = c.City.ToLowerInvariant(),
                    Name = c.City,
                    Region = c.Province,
                    Pm25 = pm25,
                    Aqi = aqi,
                    AqiLabel = AqiLabelFor(aqi),
                    Trend = SynthTrend(pm25, c.City)
                };
            }).ToArray();
        }

        private static double[] SynthTrend(double pm25, string key)
        {
            int phase = key.Sum(ch => ch) % 24;
            var trend = new double[24];
            for (int i = 0; i < 24; i++)
                trend[i] = Math.Round(pm25 * (0.78 + 0.30 * Math.Sin((i + phase) / 24.0 * Math.PI * 2)), 1);
            return trend;
        }

        private CompareCity _cityA = Cities[0];
        private CompareCity _cityB = Cities[1];
        private bool _loading;
        private int _graphVersion;

        public CompareView()
        {
            InitializeComponent();
            BuildCityPopup(CityAPanel, CityAItem_Click);
            BuildCityPopup(CityBPanel, CityBItem_Click);
            AppSettings.Changed += OnSettingsChanged;
            Services.WorldClock.Ticked += () => DateText.Text = Services.WorldClock.LongText();
            DateText.Text = Services.WorldClock.LongText();
            IsVisibleChanged += async (_, _) =>
            {
                if (!IsVisible) return;
                await LoadDataAsync();
            };
        }

        private void ApplySession()
        {
            VisitorNameText.Text = AppSettings.VisitorName;
            VisitorInitials.Text = AppSettings.Initials;
            VisitorRoleText.Text = AppSettings.RoleText;
        }

        private void OnSettingsChanged()
        {
            ApplySession();
            RefreshCompare();
        }

        private MainWindow? Mw => Window.GetWindow(this) as MainWindow;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is not MainWindow mw) return;
            if (e.ClickCount == 2)
            {
                mw.WindowState = mw.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                try { mw.DragMove(); } catch { }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
            {
                mw.WindowState = mw.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                UpdateMaxIcon();
            }
        }

        private void UpdateMaxIcon()
        {
            if (MaxPath == null || Window.GetWindow(this) is not Window win) return;
            var key = win.WindowState == WindowState.Maximized ? "Icon.WindowRestore" : "Icon.WindowMaximize";
            if (TryFindResource(key) is Geometry geo)
                MaxPath.Data = geo;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.Close();
        }

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            Mw?.ShowDashboard();
        }

        private void NavRanking_Click(object sender, MouseButtonEventArgs e)
        {
            Mw?.ShowRanking();
        }

        private void NavSearch_Click(object sender, MouseButtonEventArgs e)
        {
            Mw?.ShowSearch();
        }

        private void NavSettings_Click(object sender, MouseButtonEventArgs e)
        {
            Mw?.ShowSettings();
        }

        private void BtnEndSession_Click(object sender, MouseButtonEventArgs e)
        {
            Mw?.ShowLanding();
        }

        private async void CompareView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            ApplySession();
            if (_loading) return;
            _loading = true;
            try
            {
                try
                {
                    UpdateSourceBadge();

                    var readings = await Services.OpenAqService.GetReadingsAsync();
                    ApplyReadings(readings);
                    UpdateSourceBadge();
                    DataNoticeText.Text = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live
                        ? $"Live OpenAQ data · Last sync {OpenAqService.LastFetchTime.ToLocalTime():HH:mm} PKT · Source: OpenAQ (openaq.org)"
                        : "OpenAQ offline · Showing " + OpenAqService.SourceLabel.Trim().ToLowerInvariant();
                }
                catch { }

                await RefreshTrendAsync(_cityA);
                await RefreshTrendAsync(_cityB);
                RefreshCompare();
                await RefreshStationGraphAsync();
            }
            finally
            {
                _loading = false;
            }
        }

        private void UpdateSourceBadge()
        {
            SourceBadgeText.Text = OpenAqService.IsSyncing ? "  SYNCING LIVE DATA…" : OpenAqService.SourceLabel;
            SourceBadgeText.Foreground = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live
                ? new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F))
                : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
        }

        private void ApplyReadings(List<SmogShield_Pakistan.Models.Reading> readings)
        {
            if (readings.Count == 0) return;

            var byCity = readings
                .GroupBy(r => r.City, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Average(r => r.Pm25), StringComparer.OrdinalIgnoreCase);

            foreach (var c in Cities)
            {
                if (byCity.TryGetValue(c.Name, out double pm25))
                {
                    c.Pm25 = Math.Round(pm25, 1);
                    c.Aqi = (int)Services.OpenAqService.Pm25ToAqi(pm25);
                    c.AqiLabel = AqiLabelFor(c.Aqi);
                    c.Trend = SynthTrend(c.Pm25, c.Name);
                }
            }

            _cityA = Cities.FirstOrDefault(c => string.Equals(c.Name, _cityA.Name, StringComparison.OrdinalIgnoreCase)) ?? Cities[0];
            _cityB = Cities.FirstOrDefault(c => string.Equals(c.Name, _cityB.Name, StringComparison.OrdinalIgnoreCase)) ?? Cities[1];
        }

        private static string AqiLabelFor(int aqi) => aqi switch
        {
            <= 50 => "Good",
            <= 100 => "Moderate",
            <= 150 => "Unhealthy (sensitive)",
            <= 200 => "Unhealthy",
            <= 300 => "Very unhealthy",
            _ => "Hazardous"
        };

        public void RefreshCompare()
        {
            var date = DateTime.Now.ToString("MMMM d, yyyy");
            TxtPillDate.Text = date;

            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            TxtPillPm.Text = ug ? "PM2.5 · µg/m³" : "US AQI · index";
            CityAHeader.Text = ug ? "CURRENT PM2.5 · CITY AVERAGE" : "CURRENT US AQI · CITY AVERAGE";
            CityBHeader.Text = CityAHeader.Text;
            CityAUnit.Text = ug ? "  µg/m³" : "  US AQI";
            CityBUnit.Text = CityAUnit.Text;
            TrendAHeader.Text = ug ? "24-hour PM2.5 trend" : "24-hour US AQI trend";
            TrendBHeader.Text = TrendAHeader.Text;
            TrendAUnit.Text = ug ? "Same scale · µg/m³" : "Same scale · US AQI";
            TrendBUnit.Text = TrendAUnit.Text;

            LoadCityCard(_cityA, TxtCityA, TxtCityAPm, TxtCityAAqi);
            LoadCityCard(_cityB, TxtCityB, TxtCityBPm, TxtCityBAqi);

            var seriesA = UnitSeries(_cityA.Trend);
            var seriesB = UnitSeries(_cityB.Trend);
            double scaleMax = Math.Max(50, Math.Ceiling(Math.Max(SeriesMax(seriesA), SeriesMax(seriesB)) * 1.15 / 10) * 10);
            DrawTrend(TrendA, seriesA, scaleMax);
            DrawTrend(TrendB, seriesB, scaleMax);
            SetTimeLabels(TrendATimeGrid);
            SetTimeLabels(TrendBTimeGrid);
            UpdateDifference();
        }

        private static double SeriesMax(double[] values) => values.Length == 0 ? 0 : values.Max();

        private static void SetTimeLabels(Grid grid)
        {
            var now = DateTime.Now;
            string[] labels =
            {
                now.AddHours(-24).ToString("HH:mm"),
                now.AddHours(-18).ToString("HH:mm"),
                now.AddHours(-12).ToString("HH:mm"),
                now.AddHours(-6).ToString("HH:mm"),
                now.ToString("HH:mm")
            };
            for (int i = 0; i < grid.Children.Count && i < labels.Length; i++)
                if (grid.Children[i] is TextBlock tb) tb.Text = labels[i];
        }

        private async System.Threading.Tasks.Task RefreshTrendAsync(CompareCity city)
        {
            var hourly = await OpenAqService.GetHourlyTrendAsync(city.Name);
            if (hourly is { Length: >= 2 }) city.Trend = hourly;
        }

        private static double[] UnitSeries(double[] pm25) =>
            AppSettings.Unit == DisplayUnit.Aqi
                ? pm25.Select(v => Services.OpenAqService.Pm25ToAqi(v)).ToArray()
                : pm25;

        private void LoadCityCard(CompareCity c, TextBlock label, TextBlock big, TextBlock sub)
        {
            label.Text = $"{c.Name} · {c.Region}";
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            big.Text = ug ? c.Pm25.ToString("0.0") : c.Aqi.ToString();
            sub.Text = c.AqiLabel;
        }

        private void UpdateDifference()
        {
            bool aqi = AppSettings.Unit == DisplayUnit.Aqi;
            double a = aqi ? Services.OpenAqService.Pm25ToAqi(_cityA.Pm25) : _cityA.Pm25;
            double b = aqi ? Services.OpenAqService.Pm25ToAqi(_cityB.Pm25) : _cityB.Pm25;
            string unit = aqi ? "AQI" : AppSettings.ShortUnit;
            string metric = aqi ? "US AQI" : "PM2.5 concentration";

            double pct = Math.Abs(b) > 0.0001 ? (a - b) / b * 100.0 : 0.0;
            double abs = a - b;
            string fmt = aqi ? "0" : "0.0";

            TxtDiffPct.Text = (pct >= 0 ? "+" : "") + pct.ToString("0.0") + "%";
            TxtDiffFormula.Text = $"({a.ToString(fmt)} − {b.ToString(fmt)}) ÷ {b.ToString(fmt)} × 100";
            TxtDiffAbs.Text = (abs >= 0 ? "+" : "") + abs.ToString(fmt) + " " + unit;

            string aq = pct >= 0 ? "higher" : "lower";
            TxtInsight.Text =
                $"{_cityA.Name}'s {metric} is {Math.Abs(pct):0.0}% {aq} than {_cityB.Name}'s. " +
                $"{_cityB.Name} is the comparison baseline.";
        }

        private void BtnCityADrop_Click(object sender, MouseButtonEventArgs e)
        {
            CityBPopup.Visibility = Visibility.Collapsed;
            CityAPopup.Visibility = CityAPopup.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (CityAPopup.Visibility == Visibility.Visible)
                HighlightCity(CityAPanel, Array.IndexOf(Cities, _cityA));
        }

        private async void CityAItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: int index }) return;
            _cityA = Cities[index];
            CityAPopup.Visibility = Visibility.Collapsed;
            RefreshCompare();
            await RefreshTrendAsync(_cityA);
            RefreshCompare();
            await RefreshStationGraphAsync();
        }

        private void BtnCityBDrop_Click(object sender, MouseButtonEventArgs e)
        {
            CityAPopup.Visibility = Visibility.Collapsed;
            CityBPopup.Visibility = CityBPopup.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (CityBPopup.Visibility == Visibility.Visible)
                HighlightCity(CityBPanel, Array.IndexOf(Cities, _cityB));
        }

        private async void CityBItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: int index }) return;
            _cityB = Cities[index];
            CityBPopup.Visibility = Visibility.Collapsed;
            RefreshCompare();
            await RefreshTrendAsync(_cityB);
            RefreshCompare();
        }

        private void BuildCityPopup(StackPanel panel, MouseButtonEventHandler onClick)
        {
            for (int i = 0; i < Cities.Length; i++)
            {
                var item = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 7, 10, 7),
                    Cursor = Cursors.Hand,
                    Background = Brushes.Transparent,
                    Tag = i
                };
                item.MouseLeftButtonDown += onClick;
                item.Child = new TextBlock
                {
                    Text = $"{Cities[i].Name} · {Cities[i].Region}",
                    FontSize = 12
                };
                panel.Children.Add(item);
            }
        }

        private void HighlightCity(StackPanel panel, int selectedIndex)
        {
            var selBg = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
            var selFg = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            var normal = (Brush)FindResource("DarkText");
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is not Border b) continue;
                b.Background = i == selectedIndex ? selBg : Brushes.Transparent;
                if (b.Child is TextBlock t)
                    t.Foreground = i == selectedIndex ? selFg : normal;
            }
        }

        private void DrawTrend(Canvas canvas, double[] values, double max)
        {
            canvas.Children.Clear();
            if (values == null || values.Length < 2) return;

            double w = 300, h = 90, pad = 6;
            double stepX = (w - pad * 2) / (values.Length - 1);

            var pts = new PointCollection();
            for (int i = 0; i < values.Length; i++)
            {
                double v = Math.Max(0, Math.Min(max, values[i]));
                double x = pad + i * stepX;
                double y = h - pad - (v / max) * (h - pad * 2);
                pts.Add(new Point(x, y));
            }

            var poly = new Polyline
            {
                Points = pts,
                Stroke = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            canvas.Children.Add(poly);

            var last = pts[pts.Count - 1];
            var dot = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
            };
            canvas.Children.Add(dot);
            Canvas.SetLeft(dot, last.X - 2.5);
            Canvas.SetTop(dot, last.Y - 2.5);
        }

        private async System.Threading.Tasks.Task RefreshStationGraphAsync()
        {
            int version = ++_graphVersion;
            var city = _cityA;

            var stations = await Services.OpenAqService.GetNearbyStationsAsync(city.Name);
            if (version != _graphVersion) return;

            var canvas = GraphCanvas;
            canvas.Children.Clear();
            BfsLevelsPanel.Children.Clear();

            if (stations is null || stations.Count == 0)
            {
                BfsSubtitle.Text = $"{city.Name} · no PM2.5 stations within 25 km";
                BfsFifoText.Text = "  FIFO queue: 0 stations visited";
                BfsLevelsPanel.Children.Add(LevelRow(0, city.Name, "No nearby station network"));
                return;
            }

            int n = stations.Count;
            double[,] dist = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    dist[i, j] = dist[j, i] = Services.OpenAqService.DistanceKm(
                        stations[i].Lat, stations[i].Lon, stations[j].Lat, stations[j].Lon);

            double threshold = 10;
            var adj = BuildAdjacency(dist, n, threshold);
            if (n > 1 && adj.All(a => a.Count == 0))
            {
                threshold = 25;
                adj = BuildAdjacency(dist, n, threshold);
            }

            var level = new int[n];
            for (int i = 0; i < n; i++) level[i] = -1;
            level[0] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(0);
            var levels = new List<List<int>> { new() { 0 } };
            int visited = 1;
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in adj[u])
                {
                    if (level[v] >= 0) continue;
                    level[v] = level[u] + 1;
                    while (levels.Count <= level[v]) levels.Add(new List<int>());
                    levels[level[v]].Add(v);
                    queue.Enqueue(v);
                    visited++;
                }
            }

            int shownLevels = Math.Min(levels.Count, 4);
            int maxLevel = shownLevels - 1;
            const double canvasW = 640, padX = 18, padY = 20;
            const double nodeW = 104, nodeH = 56, startH = 74, nodeSpacing = 18;

            double needH = 0;
            for (int li = 0; li < shownLevels; li++)
            {
                double rowH = 0;
                foreach (var idx in levels[li]) rowH += idx == 0 ? startH : nodeH;
                rowH += Math.Max(0, levels[li].Count - 1) * nodeSpacing;
                needH = Math.Max(needH, rowH);
            }
            double canvasH = Math.Max(360, needH + 2 * padY);
            GraphCanvas.Height = canvasH;

            double colGap = maxLevel > 0 ? (canvasW - 2 * padX - nodeW) / maxLevel : 0;
            var positions = new Point[n];
            for (int li = 0; li < shownLevels; li++)
            {
                var row = levels[li];
                double totalH = 0;
                foreach (var idx in row) totalH += idx == 0 ? startH : nodeH;
                totalH += Math.Max(0, row.Count - 1) * nodeSpacing;
                double y = (canvasH - totalH) / 2 + 8;
                double x = padX + li * colGap;
                foreach (var idx in row)
                {
                    positions[idx] = new Point(x, y);
                    y += (idx == 0 ? startH : nodeH) + nodeSpacing;
                }
            }

            var ink = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
            var line = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
            var greenFill = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
            var greenInk = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            var lightFill = new SolidColorBrush(Colors.White);
            var edgePen = new Pen(line, 1.5);

            for (int i = 0; i < n; i++)
            {
                if (level[i] < 0 || level[i] >= shownLevels) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (level[j] < 0 || level[j] >= shownLevels || dist[i, j] > threshold) continue;
                    var label = dist[i, j].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " km";
                    DrawEdge(canvas, edgePen, positions[i], positions[j], label);
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (level[i] < 0 || level[i] >= shownLevels) continue;
                var kmText = stations[i].Km.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " km";
                DrawNode(canvas, positions[i], ShortName(stations[i].Name), kmText,
                    level[i] == 0 ? "START" : null,
                    level[i] == 0 ? greenFill : lightFill,
                    level[i] == 0 ? greenInk : ink,
                    level[i] == 0 ? greenInk : line);
            }

            for (int li = 0; li < shownLevels; li++)
            {
                string caption = li switch
                {
                    0 => "Start node",
                    1 => "Immediate neighbors",
                    2 => "Next-unvisited neighbors",
                    _ => $"Level {li} neighbors"
                };
                var names = string.Join(" → ", levels[li].Select(i => ShortName(stations[i].Name)));
                BfsLevelsPanel.Children.Add(LevelRow(li, names, caption));
            }

            BfsFifoText.Text = $"  FIFO queue: {visited} stations visited";
            string startName = ShortName(stations[0].Name);
            string header = n == 1
                ? $"{city.Name} station network · single station within 25 km · BFS start: {startName}"
                : $"{city.Name} station network · proximity links ≤ {threshold.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} km · BFS start: {startName}";
            if (shownLevels < levels.Count)
                header += $" · levels 0–{shownLevels - 1} of {levels.Count - 1}";
            BfsSubtitle.Text = header;
        }

        private static List<int>[] BuildAdjacency(double[,] dist, int n, double threshold)
        {
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (dist[i, j] <= threshold)
                    {
                        adj[i].Add(j);
                        adj[j].Add(i);
                    }
            return adj;
        }

        private StackPanel LevelRow(int level, string names, string caption)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var badge = new Border
            {
                Background = (Brush)FindResource("LightGreen"),
                CornerRadius = new CornerRadius(6),
                Width = 24,
                Height = 24
            };
            badge.Child = new TextBlock
            {
                Text = level.ToString(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var texts = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            texts.Children.Add(new TextBlock
            {
                Text = names,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("DarkText"),
                TextWrapping = TextWrapping.Wrap
            });
            texts.Children.Add(new TextBlock
            {
                Text = caption,
                FontSize = 10,
                Foreground = (Brush)FindResource("LightGrayText")
            });
            row.Children.Add(badge);
            row.Children.Add(texts);
            return row;
        }

        private static string ShortName(string name)
        {
            name = name.Trim();
            if (name.Length <= 16) return name;
            int cut = name.LastIndexOf(' ', 16);
            if (cut < 8) cut = 16;
            return name[..cut] + "…";
        }

        private void DrawEdge(Canvas c, Pen pen, Point a, Point b, string label)
        {
            var lineEl = new Line
            {
                X1 = a.X + 52,
                Y1 = a.Y + 28,
                X2 = b.X + 52,
                Y2 = b.Y + 28,
                Stroke = pen.Brush,
                StrokeThickness = pen.Thickness
            };
            c.Children.Add(lineEl);

            var tb = new TextBlock
            {
                Text = label,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
            };
            c.Children.Add(tb);
            double mx = (a.X + b.X) / 2 + 52;
            double my = (a.Y + b.Y) / 2 + 28;
            Canvas.SetLeft(tb, mx + 4);
            Canvas.SetTop(tb, my - 10);
        }

        private void DrawNode(Canvas c, Point p, string title, string code, string? badge,
            Brush bg, Brush titleInk, Brush borderInk)
        {
            var border = new Border
            {
                Background = bg,
                BorderBrush = borderInk,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Width = 104
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = titleInk,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = code,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            if (badge != null)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = badge,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            border.Child = sp;
            c.Children.Add(border);
            Canvas.SetLeft(border, p.X);
            Canvas.SetTop(border, p.Y);
        }
    }
}

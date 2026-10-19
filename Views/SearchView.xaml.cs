using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SmogShield_Pakistan.Services;

namespace SmogShield_Pakistan.Views
{
    public partial class SearchView : UserControl
    {

        public sealed class StationRow
        {
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public string Location { get; set; } = "";
            public double Pm25 { get; set; }
            public double Aqi { get; set; }
            public string Observed { get; set; } = "";
            public string ValueText { get; set; } = "";
        }

        private static readonly StationRow[] DefaultStations =
        {
            new() { Name="Township",   Code="LHR-001", Location="Lahore · 31.450, 74.300", Pm25=158.4, Observed="10:00 PKT" },
            new() { Name="Gulberg",    Code="LHR-002", Location="Lahore · 31.520, 74.350", Pm25=132.8, Observed="10:00 PKT" },
            new() { Name="Jail Road",  Code="LHR-003", Location="Lahore · 31.535, 74.338", Pm25=125.0, Observed="10:00 PKT" },
            new() { Name="Model Town", Code="LHR-004", Location="Lahore · 31.460, 74.320", Pm25=118.0, Observed="10:00 PKT" },
            new() { Name="DHA",        Code="LHR-005", Location="Lahore · 31.480, 74.400", Pm25=108.8, Observed="10:00 PKT" },
            new() { Name="Clifton",    Code="KHI-001", Location="Karachi · 24.810, 67.010", Pm25=62.1,  Observed="10:00 PKT" },
            new() { Name="Gulshan",    Code="KHI-002", Location="Karachi · 24.910, 67.080", Pm25=84.5,  Observed="10:00 PKT" },
            new() { Name="Blue Area",  Code="ISB-001", Location="Islamabad · 33.710, 73.050", Pm25=41.3, Observed="10:00 PKT" },
        };

        private readonly List<StationRow> AllStations = new(DefaultStations);

        private readonly Dictionary<string, StationRow> _byCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<StationRow>> _byCity = new(StringComparer.OrdinalIgnoreCase);

        private string[] SortedCities =
            { "Faisalabad", "Islamabad", "Karachi", "Lahore", "Multan" };

        private string _cityFilter = "Lahore";

        public SearchView()
        {
            InitializeComponent();
            BuildIndex();
            ApplyUnitToRows();
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
            ApplyUnitToRows();
            RefreshResults();
        }

        private void ApplyUnitToRows()
        {
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            foreach (var s in AllStations)
                s.ValueText = ug ? s.Pm25.ToString("0.0") : Math.Round(s.Aqi > 0 ? s.Aqi : Services.OpenAqService.Pm25ToAqi(s.Pm25)).ToString("0");
        }

        private void BuildIndex()
        {
            _byCode.Clear();
            _byCity.Clear();
            foreach (var s in AllStations)
            {
                _byCode[s.Code] = s;
                var city = s.Location.Split('·')[0].Trim();
                if (!_byCity.TryGetValue(city, out var list))
                {
                    list = new List<StationRow>();
                    _byCity[city] = list;
                }
                list.Add(s);
            }
        }

        private MainWindow? Mw => Window.GetWindow(this) as MainWindow;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is not MainWindow mw) return;
            if (e.ClickCount == 2)
                mw.WindowState = mw.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else
            { try { mw.DragMove(); } catch { } }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
            {
                mw.WindowState = mw.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
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

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowDashboard();
        private void NavRanking_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowRanking();
        private void NavCompare_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowCompare();
        private void NavSettings_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowSettings();
        private void BtnEndSession_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowLanding();

        private async void SearchView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                ApplySession();
                UpdateSourceBadge();
                RefreshResults();

                var readings = await Services.OpenAqService.GetReadingsAsync();
                ApplyReadings(readings);
                UpdateSourceBadge();
                DataNoticeText.Text = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live
                    ? $"Live OpenAQ data · {AllStations.Count} stations · Last sync {OpenAqService.LastFetchTime.ToLocalTime():HH:mm} PKT · Source: OpenAQ (openaq.org)"
                    : "OpenAQ offline · Showing " + OpenAqService.SourceLabel.Trim().ToLowerInvariant();
            }
            catch { }

            ApplyUnitToRows();
            RefreshResults();
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

            var rows = readings.Select(r => new StationRow
            {
                Name = r.Station,
                Code = r.Code,
                Location = $"{r.City} · {r.Location}",
                Pm25 = r.Pm25,
                Aqi = r.Aqi,
                Observed = r.ObservedAt.ToString("HH:mm") + " PKT"
            }).ToList();

            if (rows.Count < 3) return;

            AllStations.Clear();
            AllStations.AddRange(rows);
            BuildIndex();
            ApplyUnitToRows();

            SortedCities = AllStations
                .Select(s => s.Location.Split('·')[0].Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public void RefreshResults()
        {
            string q = (TxtSearch.Text ?? "").Trim();
            double min = SliderMin.Value;
            double max = SliderMax.Value;

            IEnumerable<StationRow> query;

            if (q.Length > 0 && _byCode.TryGetValue(q, out var exact))
            {
                query = new[] { exact };
            }
            else if (q.Length > 0 && _byCity.TryGetValue(q, out var byCity))
            {
                query = byCity;
            }
            else if (_byCity.TryGetValue(_cityFilter, out var defaultCity))
            {
                query = defaultCity;
            }
            else
            {
                query = Array.Empty<StationRow>();
            }

            var filtered = query
                .Where(s => s.Pm25 >= min && s.Pm25 <= max)
                .OrderByDescending(s => s.Pm25)
                .ToList();

            ResultsList.ItemsSource = filtered;
            TxtFoundCount.Text = $"{filtered.Count} STATIONS FOUND";
            TxtEmpty.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            string label = q.Length > 0 ? q : _cityFilter;
            bool live = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live;
            TxtResultsSub.Text = live
                ? $"Live results for “{label}” · {OpenAqService.SourceLabel.Trim()}"
                : $"Backup results for “{label}” · {OpenAqService.SourceLabel.Trim()}";
            TxtChipCity.Text = _cityFilter;
            TxtChipRange.Text = RangeLabel(min, max);
            TxtMinVal.Text = ValLabel(min);
            TxtMaxVal.Text = ValLabel(max);
            TxtValueHeader.Text = AppSettings.Unit == DisplayUnit.UgM3 ? "PM2.5 · µg/m³" : "US AQI · index";
            TxtScaleEndMin.Text = ValLabel(250);
            TxtScaleEndMax.Text = ValLabel(250);
            TxtTableFooter.Text = $"{DateTime.Now:dd MMM yyyy} · All {filtered.Count} results shown · {OpenAqService.SourceLabel.Trim()}";
        }

        private static string RangeLabel(double min, double max)
        {
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            return ug ? $"{(int)min}-{(int)max} µg/m³"
                      : $"{(int)OpenAqService.Pm25ToAqi(min)}-{(int)OpenAqService.Pm25ToAqi(max)} AQI";
        }

        private static string ValLabel(double pm25)
        {
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            return ug ? $"{(int)pm25} µg/m³" : $"{(int)OpenAqService.Pm25ToAqi(pm25)} AQI";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string q = (TxtSearch.Text ?? "").Trim();
            if (_byCity.TryGetValue(q, out _)) _cityFilter = q;
            RefreshResults();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) RefreshResults();
        }

        private void ChipCityClear_Click(object sender, MouseButtonEventArgs e)
        {
            TxtSearch.Text = "";
            _cityFilter = "Lahore";
            RefreshResults();
        }

        private void SliderMin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            if (SliderMin.Value > SliderMax.Value) SliderMax.Value = SliderMin.Value;
            RefreshResults();
        }

        private void SliderMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            if (SliderMax.Value < SliderMin.Value) SliderMin.Value = SliderMax.Value;
            RefreshResults();
        }

        private void BtnApplyFilters_Click(object sender, MouseButtonEventArgs e)
        {
            RefreshResults();
        }

        private void BtnResetFilters_Click(object sender, MouseButtonEventArgs e)
        {
            SliderMin.Value = 0;
            SliderMax.Value = 250;
            TxtSearch.Text = "";
            _cityFilter = "Lahore";
            HighlightBSChip(null);
            RefreshResults();
        }

        private void HighlightBSChip(string? target)
        {
            BSChipLahore.Style = (Style)FindResource(target == "Lahore" ? "CityChipActive" : "CityChip");
            BSChipLahoreText.FontWeight = target == "Lahore" ? FontWeights.SemiBold : FontWeights.Normal;
            BSChipLahoreText.Foreground = target == "Lahore"
                ? new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F))
                : new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        }

        private bool TryBinarySearch(string target)
        {
            int lo = 0, hi = SortedCities.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                int cmp = string.Compare(SortedCities[mid], target, StringComparison.OrdinalIgnoreCase);
                if (cmp == 0) return true;
                if (cmp < 0) lo = mid + 1;
                else hi = mid - 1;
            }
            return false;
        }

        private void BS_Faisalabad(object sender, MouseButtonEventArgs e) => BS_Select("Faisalabad");
        private void BS_Islamabad(object sender, MouseButtonEventArgs e) => BS_Select("Islamabad");
        private void BS_Karachi(object sender, MouseButtonEventArgs e) => BS_Select("Karachi");
        private void BS_Lahore(object sender, MouseButtonEventArgs e) => BS_Select("Lahore");
        private void BS_Multan(object sender, MouseButtonEventArgs e) => BS_Select("Multan");

        private void BS_Select(string city)
        {
            if (!TryBinarySearch(city)) return;
            _cityFilter = city;
            HighlightBSChip(city);
            TxtSearch.Text = city;
            RefreshResults();
        }
    }
}

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

    public class CityRank
    {
        public string City { get; set; } = "";
        public string Region { get; set; } = "";
        public double Pm25 { get; set; }
        public double Pm10 { get; set; }

        public string Rank { get; set; } = "";
        public string Pollutant { get; set; } = "PM2.5";
        public string ValueDisplay { get; set; } = "";
        public string Unit { get; set; } = "µg/m³";
        public double BarWidth { get; set; }
        public Brush RowBg { get; set; } = Brushes.Transparent;
        public Brush RankBg { get; set; } = Brushes.Transparent;
        public Brush RankFg { get; set; } = Brushes.Black;
        public Brush BarColor { get; set; } = Brushes.Gray;
        public Brush Top5Bg { get; set; } = Brushes.Transparent;
        public Brush Top5Fg { get; set; } = Brushes.Gray;
        public Visibility Top5Vis { get; set; } = Visibility.Collapsed;
    }

    public partial class RankingView : UserControl
    {

        private readonly List<CityRank> _cities = new()
        {
            new() { City="Lahore",      Region="Punjab",                    Pm25=128.6, Pm10=210.5 },
            new() { City="Faisalabad",  Region="Punjab",                    Pm25=112.3, Pm10=185.2 },
            new() { City="Multan",      Region="Punjab",                    Pm25=98.7,  Pm10=160.8 },
            new() { City="Gujranwala",  Region="Punjab",                    Pm25=91.5,  Pm10=148.3 },
            new() { City="Peshawar",    Region="Khyber Pakhtunkhwa",        Pm25=84.2,  Pm10=135.7 },
            new() { City="Karachi",     Region="Sindh",                     Pm25=78.4,  Pm10=128.9 },
            new() { City="Rawalpindi",  Region="Punjab",                    Pm25=62.8,  Pm10=102.4 },
            new() { City="Quetta",      Region="Balochistan",               Pm25=51.6,  Pm10=85.1  },
            new() { City="Hyderabad",   Region="Sindh",                     Pm25=42.3,  Pm10=70.6  },
            new() { City="Islamabad",   Region="Islamabad Capital Territory", Pm25=35.2, Pm10=58.3 },
        };

        private bool _descending = true;
        private string _pollutant = "PM2.5";

        private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
        private static readonly Brush GreenDark = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
        private static readonly Brush GreenBg = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
        private static readonly Brush GreenRow = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
        private static readonly Brush Gray6 = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        private static readonly Brush Gray9 = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));

        public RankingView()
        {
            InitializeComponent();
            AppSettings.Changed += OnSettingsChanged;
            Services.WorldClock.Ticked += () => DateText.Text = Services.WorldClock.ShortText();
            DateText.Text = Services.WorldClock.ShortText();
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
            RefreshRanking();
        }

        private async void RankingView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {

                ApplySession();
                UpdateSourceBadge();
                RefreshRanking();

                var readings = await Services.OpenAqService.GetReadingsAsync();
                ApplyReadings(readings);
                RefreshRanking();
                UpdateSourceBadge();
                DataNoticeText.Text = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live
                    ? $"Live OpenAQ data · { _cities.Count } cities · Last sync {OpenAqService.LastFetchTime.ToLocalTime():HH:mm} PKT · Source: OpenAQ (openaq.org)"
                    : "OpenAQ offline · Showing " + OpenAqService.SourceLabel.Trim().ToLowerInvariant();
            }
            catch { RefreshRanking(); }
        }

        private void UpdateSourceBadge()
        {
            SourceBadgeText.Text = OpenAqService.IsSyncing ? "  SYNCING LIVE DATA…" : OpenAqService.SourceLabel;
            SourceBadgeText.Foreground = OpenAqService.SourceKind == OpenAqService.DataSourceKind.Live
                ? GreenDark
                : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
        }

        private void ApplyReadings(List<SmogShield_Pakistan.Models.Reading> readings)
        {
            if (readings.Count == 0) return;

            var grouped = readings
                .GroupBy(r => r.City)
                .Select(g => new
                {
                    City = g.Key,
                    Pm25 = g.Average(r => r.Pm25),
                    Pm10 = g.Average(r => r.Pm10)
                })
                .ToList();

            if (grouped.Count < 3) return;

            _cities.Clear();
            foreach (var g in grouped)
            {
                _cities.Add(new CityRank
                {
                    City = g.City,
                    Region = RegionFor(g.City),
                    Pm25 = Math.Round(g.Pm25, 1),
                    Pm10 = Math.Round(g.Pm10, 1)
                });
            }
        }

        private static string RegionFor(string city) => OpenAqService.ProvinceOf(city);

        private static void MergeSort(List<CityRank> list, bool descending)
        {
            if (list.Count <= 1) return;
            var temp = new List<CityRank>(list.Count);
            MergeSortHelper(list, temp, 0, list.Count - 1, descending);
        }

        private static void MergeSortHelper(List<CityRank> list, List<CityRank> temp, int left, int right, bool desc)
        {
            if (left >= right) return;
            int mid = left + (right - left) / 2;
            MergeSortHelper(list, temp, left, mid, desc);
            MergeSortHelper(list, temp, mid + 1, right, desc);
            Merge(list, temp, left, mid, right, desc);
        }

        private static void Merge(List<CityRank> list, List<CityRank> temp, int left, int mid, int right, bool desc)
        {
            temp.Clear();
            int i = left, j = mid + 1;
            while (i <= mid && j <= right)
            {
                double a = Val(list[i]), b = Val(list[j]);
                bool takeA = desc ? a >= b : a <= b;
                temp.Add(takeA ? list[i++] : list[j++]);
            }
            while (i <= mid) temp.Add(list[i++]);
            while (j <= right) temp.Add(list[j++]);
            for (int k = 0; k < temp.Count; k++)
                list[left + k] = temp[k];
        }

        private static void QuickSort(List<CityRank> list, bool descending)
        {
            if (list.Count <= 1) return;
            QuickSortHelper(list, 0, list.Count - 1, descending);
        }

        private static void QuickSortHelper(List<CityRank> list, int low, int high, bool desc)
        {
            if (low < high)
            {
                int pi = Partition(list, low, high, desc);
                QuickSortHelper(list, low, pi - 1, desc);
                QuickSortHelper(list, pi + 1, high, desc);
            }
        }

        private static int Partition(List<CityRank> list, int low, int high, bool desc)
        {
            double pivot = Val(list[high]);
            int i = low - 1;
            for (int j = low; j < high; j++)
            {
                double v = Val(list[j]);
                bool cond = desc ? v >= pivot : v <= pivot;
                if (cond)
                {
                    i++;
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            (list[i + 1], list[high]) = (list[high], list[i + 1]);
            return i + 1;
        }

        private static double Val(CityRank c) => c.Pm25;

        private void RefreshRanking()
        {

            var work = _cities.Select(c => new CityRank
            {
                City = c.City, Region = c.Region, Pm25 = c.Pm25, Pm10 = c.Pm10
            }).ToList();

            if (_pollutant == "PM10")
            {
                foreach (var c in work) { c.Pm25 = c.Pm10; }
            }

            if (_descending) MergeSort(work, true);
            else QuickSort(work, false);

            double max = work.Max(c => c.Pm25);
            double min = work.Min(c => c.Pm25);
            double range = max - min; if (range < 1) range = 1;

            for (int i = 0; i < work.Count; i++)
            {
                var c = work[i];
                c.Rank = (i + 1).ToString("00");
                c.Pollutant = _pollutant;
                bool asAqi = AppSettings.Unit == DisplayUnit.Aqi;
                c.ValueDisplay = asAqi
                    ? Services.OpenAqService.Pm25ToAqi(c.Pm25).ToString("0")
                    : c.Pm25.ToString("0.0");
                c.Unit = asAqi ? "US AQI" : "µg/m³";
                double scaleMax = asAqi ? Math.Max(50, Services.OpenAqService.Pm25ToAqi(max)) : max;
                c.BarWidth = 20 + ((asAqi ? Services.OpenAqService.Pm25ToAqi(c.Pm25) : c.Pm25) / scaleMax) * 55;

                bool top5 = i < 5;
                c.Top5Vis = top5 ? Visibility.Visible : Visibility.Collapsed;
                c.Top5Bg = top5 ? GreenBg : Brushes.Transparent;
                c.Top5Fg = top5 ? GreenDark : Gray9;

                if (top5)
                {
                    c.RowBg = GreenRow;
                    c.RankBg = GreenBg;
                    c.RankFg = GreenDark;
                    c.BarColor = Green;
                }
                else
                {
                    c.RowBg = Brushes.Transparent;
                    c.RankBg = Brushes.Transparent;
                    c.RankFg = Gray6;
                    c.BarColor = Gray9;
                }
            }

            RankingList.ItemsSource = work;

            bool showAqi = AppSettings.Unit == DisplayUnit.Aqi;
            string cardVal(double pm25) => showAqi
                ? Services.OpenAqService.Pm25ToAqi(pm25).ToString("0")
                : pm25.ToString("0.0");
            string cardUnit = showAqi ? "US AQI" : "µg/m³";
            TxtTotalCities.Text = work.Count.ToString();
            TxtHighestLabel.Text = "Highest " + _pollutant;
            TxtHighestVal.Text = cardVal(work.First().Pm25);
            TxtHighestCity.Text = work.First().City;
            TxtHighestUnit.Text = cardUnit;
            TxtLowestLabel.Text = "Lowest " + _pollutant;
            TxtLowestVal.Text = cardVal(work.Last().Pm25);
            TxtLowestCity.Text = work.Last().City;
            TxtLowestUnit.Text = cardUnit;

            TxtShowing.Text = $"Showing 1–{work.Count} of {work.Count} cities · City averages · {OpenAqService.SourceLabel.Trim()}";
            TxtOrder.Text = _descending ? "DESCENDING ORDER" : "ASCENDING ORDER";

            TxtAlgorithm.Text = _descending
                ? "  Sorted via Merge Sort Algorithm (O(n log n))"
                : "  Sorted via Quick Sort Algorithm (avg O(n log n))";
            TxtAlgoDetail.Text = _pollutant == "PM2.5"
                ? "Merge Sort: O(n log n) · Quick Sort: average O(n log n). PM2.5 and PM10 are ranked separately; this view shows PM2.5 only."
                : "Merge Sort: O(n log n) · Quick Sort: average O(n log n). PM2.5 and PM10 are ranked separately; this view shows PM10 only.";
        }

        private void BtnSortDropdown_Click(object sender, MouseButtonEventArgs e)
        {
            SortPopup.Visibility = SortPopup.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
            PollutantPopup.Visibility = Visibility.Collapsed;
        }

        private void SortOption_HighLow(object sender, MouseButtonEventArgs e)
        {
            _descending = true;
            TxtSortValue.Text = "Value - High to Low";
            SortPopup.Visibility = Visibility.Collapsed;
            RefreshRanking();
        }

        private void SortOption_LowHigh(object sender, MouseButtonEventArgs e)
        {
            _descending = false;
            TxtSortValue.Text = "Value - Low to High";
            SortPopup.Visibility = Visibility.Collapsed;
            RefreshRanking();
        }

        private void BtnPollutantDropdown_Click(object sender, MouseButtonEventArgs e)
        {
            PollutantPopup.Visibility = PollutantPopup.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
            SortPopup.Visibility = Visibility.Collapsed;
        }

        private void PollutantOption_PM25(object sender, MouseButtonEventArgs e)
        {
            _pollutant = "PM2.5";
            TxtPollutantValue.Text = "PM2.5";
            OptPm25.Background = GreenBg;
            OptPm10.Background = Brushes.Transparent;
            PollutantPopup.Visibility = Visibility.Collapsed;
            RefreshRanking();
        }

        private void PollutantOption_PM10(object sender, MouseButtonEventArgs e)
        {
            _pollutant = "PM10";
            TxtPollutantValue.Text = "PM10";
            OptPm10.Background = GreenBg;
            OptPm25.Background = Brushes.Transparent;
            PollutantPopup.Visibility = Visibility.Collapsed;
            RefreshRanking();
        }

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowDashboard();
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

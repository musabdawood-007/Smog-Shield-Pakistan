using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SmogShield_Pakistan.Services;

namespace SmogShield_Pakistan.Views
{
    public partial class SettingsView : UserControl
    {

        private int _draftLimit;

        private sealed class FlagCity
        {
            public string Name = "";
            public double Pm25;
        }

        private readonly FlagCity[] FlagCities =
        {
            new() { Name = "Lahore", Pm25 = 128.6 },
            new() { Name = "Karachi", Pm25 = 78.4 },
            new() { Name = "Islamabad", Pm25 = 35.2 },
        };

        public SettingsView()
        {
            InitializeComponent();
            Services.WorldClock.Ticked += () => DateText.Text = Services.WorldClock.LongText();
            DateText.Text = Services.WorldClock.LongText();
            IsVisibleChanged += async (_, _) =>
            {
                if (!IsVisible) return;
                await LoadDataAsync();
            };
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
        private void NavSearch_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowSearch();
        private void NavCompare_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowCompare();
        private void BtnEndSession_Click(object sender, MouseButtonEventArgs e) => Mw?.ShowLanding();

        private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            ApplySession();
            _draftLimit = AppSettings.Threshold;
            TxtLimit.Text = _draftLimit.ToString();
            SetUnit(AppSettings.Unit == DisplayUnit.UgM3);
            UpdateSourceBadge();

            try
            {
                var readings = await Services.OpenAqService.GetReadingsAsync();
                foreach (var fc in FlagCities)
                {
                    var match = readings.FirstOrDefault(r =>
                        string.Equals(r.City, fc.Name, StringComparison.OrdinalIgnoreCase));
                    if (match != null) fc.Pm25 = match.Pm25;
                }
            }
            catch { }

            UpdateSourceBadge();
            DataNoticeText.Text = Services.OpenAqService.SourceKind == Services.OpenAqService.DataSourceKind.Live
                ? "Live OpenAQ data · Last sync " + Services.OpenAqService.LastFetchTime.ToLocalTime().ToString("HH:mm") + " PKT · Source: OpenAQ (openaq.org)"
                : "OpenAQ offline · Showing " + Services.OpenAqService.SourceLabel.Trim().ToLowerInvariant();
            RefreshPreview();
        }

        private void ApplySession()
        {
            string name = AppSettings.VisitorName;
            VisitorNameBig.Text = name;
            VisitorNameText.Text = name;
            VisitorInitials.Text = AppSettings.Initials;
            ProfileInitials.Text = AppSettings.Initials;
            VisitorRoleText.Text = AppSettings.RoleText;
            ProfileRoleText.Text = AppSettings.RoleText + " · Logged-in session";
        }

        private void UpdateSourceBadge()
        {
            SourceBadgeText.Text = Services.OpenAqService.IsSyncing
                ? "  SYNCING LIVE DATA…"
                : Services.OpenAqService.SourceLabel;
            SourceBadgeText.Foreground = Services.OpenAqService.SourceKind == Services.OpenAqService.DataSourceKind.Live
                ? new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F))
                : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
        }

        public void RefreshPreview()
        {
            int saved = AppSettings.Threshold;
            string unit = AppSettings.ShortUnit;
            bool ug = AppSettings.Unit == DisplayUnit.UgM3;
            TxtSavedLimit.Text = $"Current saved limit: > {saved} µg/m³";
            TxtDraftLimit.Text = $"Draft limit: > {_draftLimit} µg/m³";
            TxtPreviewSub.Text = $"Effect of your draft limit on current readings. These flags use PM2.5 > {_draftLimit} µg/m³ ({unit} display)";
            TxtLahoreVal.Text = PreviewVal(FlagCities[0].Pm25, ug);
            TxtKarachiVal.Text = PreviewVal(FlagCities[1].Pm25, ug);
            TxtIslamabadVal.Text = PreviewVal(FlagCities[2].Pm25, ug);

            int changes = _draftLimit == saved ? 0 : 1;
            TxtChangeNote.Text = changes == 0
                ? "No unsaved changes"
                : $"{changes} threshold change · Not saved yet";
            TxtUnsavedBadge.Text = changes == 0 ? "SAVED STATE" : "UNSAVED EXAMPLE";

            int flaggedNow = 0, flaggedBefore = 0;
            int lahoreFlag = 0, karachiFlag = 0, islamabadFlag = 0;

            foreach (var c in FlagCities)
            {
                bool now = c.Pm25 > _draftLimit;
                bool before = c.Pm25 > saved;
                if (now) flaggedNow++;
                if (before) flaggedBefore++;
                if (c.Name == "Lahore") lahoreFlag = now ? 1 : 0;
                if (c.Name == "Karachi") karachiFlag = now ? 1 : 0;
                if (c.Name == "Islamabad") islamabadFlag = now ? 1 : 0;
            }

            ApplyBadge(BadgeLahore, TxtLahoreBadge, lahoreFlag == 1);
            ApplyBadge(BadgeKarachi, TxtKarachiBadge, karachiFlag == 1);
            ApplyBadge(BadgeIslamabad, TxtIslamabadBadge, islamabadFlag == 1);

            bool karachiClears = FlagCities[1].Pm25 > saved && FlagCities[1].Pm25 <= _draftLimit;
            TxtKarachiNote.Text = karachiClears ? "Flag clears" : "";

            TxtFlaggedCount.Text = flaggedBefore == flaggedNow
                ? $"  Flagged cities: {flaggedNow} in this preview"
                : $"  Flagged cities: {flaggedBefore} → {flaggedNow} in this preview";
        }

        private static string PreviewVal(double pm25, bool ug) =>
            ug ? pm25.ToString("0.0") + " µg/m³" : Services.OpenAqService.Pm25ToAqi(pm25).ToString("0") + " AQI";

        private void ApplyBadge(Border badge, TextBlock text, bool above)
        {
            if (above)
            {
                badge.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2));
                text.Text = "ABOVE THRESHOLD";
                text.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
            }
            else
            {
                badge.Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
                text.Text = "BELOW THRESHOLD";
                text.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            }
        }

        private void TxtLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (int.TryParse(TxtLimit.Text, out int v) && v >= 0 && v <= 500)
            {
                _draftLimit = v;
                RefreshPreview();
            }
        }

        private void UnitUgm_Click(object sender, MouseButtonEventArgs e)
        {
            AppSettings.SetUnit(DisplayUnit.UgM3);
            SetUnit(true);
            RefreshPreview();
        }

        private void UnitAqi_Click(object sender, MouseButtonEventArgs e)
        {
            AppSettings.SetUnit(DisplayUnit.Aqi);
            SetUnit(false);
            RefreshPreview();
        }

        private void SetUnit(bool isUgm)
        {
            UnitUgm.Style = (Style)FindResource(isUgm ? "UnitBtnActive" : "UnitBtn");
            UnitAqi.Style = (Style)FindResource(isUgm ? "UnitBtn" : "UnitBtnActive");
            UnitUgm.Child = isUgm ? BuildUnitContent("µg/m³") : BuildPlainUnit("µg/m³");
            UnitAqi.Child = isUgm ? BuildPlainUnit("AQI") : BuildUnitContent("AQI");
        }

        private UIElement BuildUnitContent(string label)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Path
            {
                Data = (Geometry)FindResource("Icon.Check"),
                Fill = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F)),
                Stretch = Stretch.Uniform,
                Width = 12,
                Height = 12,
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = "  " + label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F)),
                VerticalAlignment = VerticalAlignment.Center
            });
            return sp;
        }

        private static UIElement BuildPlainUnit(string label) => new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
        };

        private void BtnSave_Click(object sender, MouseButtonEventArgs e)
        {
            AppSettings.SetThreshold(_draftLimit);
            RefreshPreview();
        }

        private void BtnCancel_Click(object sender, MouseButtonEventArgs e)
        {
            _draftLimit = AppSettings.Threshold;
            TxtLimit.Text = _draftLimit.ToString();
            RefreshPreview();
        }
    }
}

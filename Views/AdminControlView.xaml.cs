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
    public class VisitorRow
    {
        public int Id { get; set; }
        public string TimeText { get; set; } = "";
        public string DateText { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Page { get; set; } = "";
        public string VisitText { get; set; } = "";
        public Brush RowBg { get; set; } = Brushes.Transparent;
    }

    public partial class AdminControlView : UserControl
    {
        public AdminControlView()
        {
            InitializeComponent();
            Services.WorldClock.Ticked += OnClockTick;
            OnClockTick();
            IsVisibleChanged += async (_, _) =>
            {
                if (!IsVisible) return;
                await ActivateAsync();
            };
        }

        private void OnClockTick()
        {
            DateText.Text = Services.WorldClock.ShortText();
            UpdateSyncCountdown();
        }

        private void UpdateSyncCountdown()
        {
            try
            {
                if (Services.OpenAqService.LastFetchTime <= DateTime.MinValue)
                {
                    SyncDateText.Text = "Waiting for first fetch";
                    return;
                }

                var t = Services.OpenAqService.LastFetchTime.ToLocalTime();
                var next = t.AddMinutes(15);
                var left = next - DateTime.Now;
                SyncDateText.Text = left.TotalSeconds > 0
                    ? t.ToString("dd MMM yyyy") + " · Next sync in " +
                      (int)left.TotalMinutes + ":" + left.Seconds.ToString("00")
                    : t.ToString("dd MMM yyyy") + " · Sync due";
            }
            catch { }
        }

        private async void AdminControlView_Loaded(object sender, RoutedEventArgs e)
        {
            await ActivateAsync();
        }

        private async System.Threading.Tasks.Task ActivateAsync()
        {
            LoadVisitors();
            await RefreshSystemHealthAsync();
        }

        private async System.Threading.Tasks.Task RefreshSystemHealthAsync()
        {
            try
            {
                await Services.OpenAqService.GetReadingsAsync();
            }
            catch { }

            try
            {
                bool live = Services.OpenAqService.SourceKind == Services.OpenAqService.DataSourceKind.Live;

                ApiStatusText.Text = live ? "Online" : "Fallback";
                ApiStatusDot.Fill = live
                    ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
                    : new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                ApiStatusSub.Text = live
                    ? "OpenAQ API · Connection healthy"
                    : "OpenAQ offline · Using " + Services.OpenAqService.SourceLabel.Trim();

                if (Services.OpenAqService.LastFetchTime > DateTime.MinValue)
                {
                    var t = Services.OpenAqService.LastFetchTime.ToLocalTime();
                    SyncTimeText.Text = t.ToString("HH:mm:ss") + " PKT";
                }
                else
                {
                    SyncTimeText.Text = "Not synced yet";
                }
                UpdateSyncCountdown();

                DataSourceText.Text = Services.OpenAqService.SourceLabel.Trim();
                DataSourceSub.Text = Services.OpenAqService.IsSyncing
                    ? "Sync in progress…"
                    : (live ? "Fetched " + Services.OpenAqService.LastFetchTime.ToLocalTime().ToString("HH:mm") + " PKT · Cached 15 min"
                            : "Backup CSV · Available on standby");

                SystemStatusBadge.Text = live ? "  ALL SYSTEMS OPERATIONAL" : "  FALLBACK MODE ACTIVE";
                SystemStatusBadge.Foreground = live
                    ? new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F))
                    : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
                SystemStatusDot.Fill = live
                    ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
                    : new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                SystemStatusBadgeBorder.Background = live
                    ? (Brush)FindResource("LightGreen")
                    : new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB));
            }
            catch { }
        }

        private void LoadVisitors()
        {
            try
            {
                var logs = DatabaseHelper.GetVisitors(50);
                var rows = new List<VisitorRow>();
                var highlight = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));

                for (int i = 0; i < logs.Count; i++)
                {
                    var v = logs[i];
                    rows.Add(new VisitorRow
                    {
                        Id = v.Id,
                        TimeText = v.VisitedAt.ToString("HH:mm:ss") + " PKT",
                        DateText = v.VisitedAt.ToString("dd MMM yyyy"),
                        Name = v.Name,
                        Email = string.IsNullOrWhiteSpace(v.Email) ? v.Details : v.Email,
                        Purpose = v.Details,
                        Page = v.Page,
                        VisitText = v.VisitCount > 1 ? $"{v.VisitCount} visits" : "1 visit",
                        RowBg = i == 0 ? highlight : Brushes.Transparent
                    });
                }

                VisitorsList.ItemsSource = rows;
                EntriesBadgeText.Text = logs.Count == 0
                    ? "NO ENTRIES"
                    : $"{logs.Count} ENTR{(logs.Count == 1 ? "Y" : "IES")}";
                int totalVisits = logs.Sum(v => Math.Max(1, v.VisitCount));
                VisitorsFooterText.Text = $"Showing {rows.Count} visitors · {totalVisits} total visits · Newest first · Times in PKT";
            }
            catch
            {
                VisitorsList.ItemsSource = Array.Empty<VisitorRow>();
            }
        }

        private void BtnDeleteVisitor_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not int id) return;
            if (MessageBox.Show("Delete this visitor entry?", "SmogShield",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            System.Threading.Tasks.Task.Run(() => DatabaseHelper.DeleteVisitor(id));
            LoadVisitors();
        }

        private async void BtnForceRefresh_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Services.OpenAqService.GetReadingsAsync(forceRefresh: true);
            }
            catch { }
            await RefreshSystemHealthAsync();
        }

        private void BtnClearCache_Click(object sender, MouseButtonEventArgs e)
        {
            Services.OpenAqService.ClearCache();
            _ = RefreshSystemHealthAsync();
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

        private void BtnLogout_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowLanding();
        }

        private void NavSystemStatus_Click(object sender, MouseButtonEventArgs e)
        {
            SystemHealthCard.Visibility = Visibility.Visible;
            VisitorLogsCard.Visibility = Visibility.Collapsed;

            NavSystemStatus.Style = (Style)FindResource("NavActive");
            NavVisitorLogs.Style = (Style)FindResource("NavIdle");

            NavSystemStatusIcon.Fill = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            NavSystemStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            NavSystemStatusText.FontWeight = FontWeights.SemiBold;

            NavVisitorLogsIcon.Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            NavVisitorLogsText.Foreground = (Brush)FindResource("GrayText");
            NavVisitorLogsText.FontWeight = FontWeights.Normal;
        }

        private void NavVisitorLogs_Click(object sender, MouseButtonEventArgs e)
        {
            SystemHealthCard.Visibility = Visibility.Collapsed;
            VisitorLogsCard.Visibility = Visibility.Visible;

            LoadVisitors();

            NavVisitorLogs.Style = (Style)FindResource("NavActive");
            NavSystemStatus.Style = (Style)FindResource("NavIdle");

            NavVisitorLogsIcon.Fill = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            NavVisitorLogsText.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x74, 0x3F));
            NavVisitorLogsText.FontWeight = FontWeights.SemiBold;

            NavSystemStatusIcon.Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            NavSystemStatusText.Foreground = (Brush)FindResource("GrayText");
            NavSystemStatusText.FontWeight = FontWeights.Normal;
        }

        private void BtnExportCsv_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var visitors = DatabaseHelper.GetVisitors(500);
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "visitor_logs.csv",
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    CsvDataService.ExportVisitors(visitors, dialog.FileName);
                    MessageBox.Show($"Exported {visitors.Count} visitor records to CSV.",
                        "SmogShield", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }
    }
}

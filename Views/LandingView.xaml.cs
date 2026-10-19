using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SmogShield_Pakistan.Views
{
    public partial class LandingView : UserControl
    {
        private readonly (string Image, string Headline, string Subtext)[] _cities =
        {
            ("pack://application:,,,/Assets/Images/Misty Aerial View of Badshahi Mosque.png",
             "Lahore, beneath the haze.", "Badshahi Mosque · Punjab, Pakistan"),

            ("pack://application:,,,/Assets/Images/Karachi Landmarks in Smoggy Haze.png",
             "Karachi, lost in the smog.", "Mazar-e-Quaid · Sindh, Pakistan"),

            ("pack://application:,,,/Assets/Images/Hazy Golden Dawn Over Faisal Mosque.png",
             "Islamabad, veiled in mist.", "Faisal Mosque · Islamabad Capital Territory"),

            ("pack://application:,,,/Assets/Images/Misty Mughal Mosque Overlooking a Bustling Bazaar.png",
             "Peshawar, beneath the dust.", "Mahabat Khan Mosque · Khyber Pakhtunkhwa"),

            ("pack://application:,,,/Assets/Images/Misty Multan Mausoleum and Fort Ruins.png",
             "Multan, shrouded in history.", "Tomb of Shah Rukn-e-Alam · Punjab, Pakistan"),

            ("pack://application:,,,/Assets/Images/Misty Mountain Reservoir Panorama Quetta.png",
             "Quetta, hidden in the valley.", "Hanna Lake · Balochistan, Pakistan"),

            ("pack://application:,,,/Assets/Images/Historic Fort Overlooking Hazy Cityscape hydarabaad.png",
             "Hyderabad, fading in the fog.", "Pacco Qillo Fort · Sindh, Pakistan"),
        };

        private int _currentIndex;
        private bool _useImgA = true;
        private bool _isAnimating;
        private readonly DispatcherTimer _timer;

        public LandingView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += Timer_Tick;

            Services.AppSettings.Changed += () => _ = RefreshReadingsAsync();
            IsVisibleChanged += (_, _) => { if (IsVisible) _ = RefreshReadingsAsync(); };
        }

        private async void LandingView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSlide(0, animate: false);
                _timer.Start();
            }
            catch { }

            await RefreshReadingsAsync();
        }

        private async System.Threading.Tasks.Task RefreshReadingsAsync()
        {
            try
            {
                var readings = await Services.OpenAqService.GetReadingsAsync();
                var badge = Services.OpenAqService.SourceLabel;
                bool live = Services.OpenAqService.SourceKind == Services.OpenAqService.DataSourceKind.Live;

                Fill(LahCardBadge, LahCardValue, LahCardUnit, LahCardNote, "Lahore", badge, live, readings);
                Fill(KhiCardBadge, KhiCardValue, KhiCardUnit, KhiCardNote, "Karachi", badge, live, readings);
                Fill(IsbCardBadge, IsbCardValue, IsbCardUnit, IsbCardNote, "Islamabad", badge, live, readings);
            }
            catch { }
        }

        private static void Fill(TextBlock badge, TextBlock value, TextBlock unit, TextBlock note,
            string city, string sourceLabel, bool live, System.Collections.Generic.List<Models.Reading> readings)
        {
            var r = readings.FirstOrDefault(x => x.City == city);
            if (r == null) return;

            badge.Text = live ? "LIVE" : sourceLabel;
            badge.Foreground = live
                ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
                : new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));

            if (Services.AppSettings.Unit == Services.DisplayUnit.Aqi)
            {
                value.Text = Services.OpenAqService.Pm25ToAqi(r.Pm25).ToString("0");
                unit.Text = " AQI";
            }
            else
            {
                value.Text = r.Pm25.ToString("0.0");
                unit.Text = " µg/m³";
            }

            note.Text = r.ObservedAt.ToLocalTime().ToString("dd MMM HH:mm");
        }

        private void LandingView_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        public void PauseSlideshow() => _timer.Stop();
        public void ResumeSlideshow() => _timer.Start();

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _currentIndex = (_currentIndex + 1) % _cities.Length;
                LoadSlide(_currentIndex, animate: true);
            }
            catch { _timer.Stop(); }
        }

        private void LoadSlide(int index, bool animate)
        {
            if (index < 0 || index >= _cities.Length) return;

            var (imgPath, headline, subtext) = _cities[index];

            BitmapImage bitmap;
            try
            {
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imgPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch
            {
                return;
            }

            var incoming = _useImgA ? ImgB : ImgA;
            var outgoing = _useImgA ? ImgA : ImgB;

            incoming.Source = bitmap;

            HeadlineText.Text = headline;
            SubtextText.Text = subtext;
            UpdateDots(index);

            if (!animate)
            {
                incoming.Opacity = 1;
                outgoing.Opacity = 0;
                _useImgA = !_useImgA;
                return;
            }

            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                var dur = TimeSpan.FromMilliseconds(400);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

                var inT = new TranslateTransform();
                incoming.RenderTransform = inT;
                var outT = new TranslateTransform();
                outgoing.RenderTransform = outT;

                var animInX = new DoubleAnimation(60, 0, dur) { EasingFunction = ease };
                var animInOp = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
                var animOutX = new DoubleAnimation(0, -60, dur) { EasingFunction = ease };
                var animOutOp = new DoubleAnimation(1, 0, dur) { EasingFunction = ease };

                Storyboard.SetTarget(animInX, incoming);
                Storyboard.SetTargetProperty(animInX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTarget(animInOp, incoming);
                Storyboard.SetTargetProperty(animInOp, new PropertyPath("(UIElement.Opacity)"));
                Storyboard.SetTarget(animOutX, outgoing);
                Storyboard.SetTargetProperty(animOutX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTarget(animOutOp, outgoing);
                Storyboard.SetTargetProperty(animOutOp, new PropertyPath("(UIElement.Opacity)"));

                var sb = new Storyboard();
                sb.Children.Add(animInX);
                sb.Children.Add(animInOp);
                sb.Children.Add(animOutX);
                sb.Children.Add(animOutOp);

                sb.Completed += (_, _) =>
                {
                    outgoing.Opacity = 0;
                    incoming.Opacity = 1;
                    _useImgA = !_useImgA;
                    _isAnimating = false;
                };

                sb.Begin(this, HandoffBehavior.SnapshotAndReplace);
            }
            catch
            {
                incoming.Opacity = 1;
                outgoing.Opacity = 0;
                _useImgA = !_useImgA;
                _isAnimating = false;
            }
        }

        private void UpdateDots(int active)
        {
            for (int i = 0; i < 7; i++)
            {
                if (FindName($"Dot{i}") is Border dot)
                {
                    dot.Background = i == active
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromArgb(102, 255, 255, 255));
                }
            }
        }

        private void UpdateMaxIcon()
        {
            if (MaxPath == null || Window.GetWindow(this) is not Window win) return;
            var key = win.WindowState == WindowState.Maximized ? "Icon.WindowRestore" : "Icon.WindowMaximize";
            if (TryFindResource(key) is System.Windows.Media.Geometry geo)
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

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName?.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail?.Text) ||
                CmbPurpose?.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in your name, email and purpose to continue.",
                    "SmogShield", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = TxtName.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string purpose = CmbPurpose?.SelectedItem is System.Windows.Controls.ComboBoxItem item
                ? item.Content?.ToString() ?? "Visitor" : "Visitor";

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Please enter a valid email address (e.g. name@example.com).",
                    "SmogShield", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            Services.AppSettings.SetSession(name, email, purpose);

            System.Threading.Tasks.Task.Run(() =>
                Services.DatabaseHelper.InsertVisitor(name, email, "Dashboard", purpose));

            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowDashboard(name);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
                return false;

            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@'))
                return false;

            string local = email[..at];
            string domain = email[(at + 1)..];

            if (local.Length == 0 || domain.Length < 3 || !domain.Contains('.'))
                return false;

            int dot = domain.LastIndexOf('.');
            if (dot <= 0 || dot == domain.Length - 1)
                return false;

            string tld = domain[(dot + 1)..];
            if (tld.Length < 2 || tld.Length > 24)
                return false;

            foreach (char c in local)
                if (!(char.IsLetterOrDigit(c) || c is '.' or '_' or '%' or '+' or '-'))
                    return false;

            foreach (char c in domain)
                if (!(char.IsLetterOrDigit(c) || c is '.' or '-'))
                    return false;

            return true;
        }

        private void BtnAdminLogin_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ShowAdminLogin();
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

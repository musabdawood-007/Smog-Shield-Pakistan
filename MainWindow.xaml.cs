using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SmogShield_Pakistan
{
    public partial class MainWindow : Window
    {
        private FrameworkElement? _currentView;

        public MainWindow()
        {
            InitializeComponent();
            _currentView = LandingView;
        }

        private void HideAllViews()
        {
            LandingView.Visibility = Visibility.Collapsed;
            AdminLoginView.Visibility = Visibility.Collapsed;
            AdminControlView.Visibility = Visibility.Collapsed;
            DashboardView.Visibility = Visibility.Collapsed;
            RankingView.Visibility = Visibility.Collapsed;
            CompareView.Visibility = Visibility.Collapsed;
            SearchView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
        }

        private void NavigateTo(FrameworkElement view, Action? beforeShow = null)
        {
            if (_currentView == view)
            {
                beforeShow?.Invoke();
                return;
            }

            if (_currentView == LandingView) LandingView.PauseSlideshow();
            if (view == LandingView) LandingView.ResumeSlideshow();

            _currentView = view;
            beforeShow?.Invoke();
            HideAllViews();
            view.Visibility = Visibility.Visible;
            PlayEnterAnimation(view);
        }

        private static void PlayEnterAnimation(FrameworkElement view)
        {
            var transform = new TranslateTransform(0, 16);
            view.RenderTransform = transform;
            view.Opacity = 0;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slide = new DoubleAnimation(16, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.YProperty, slide);
            view.BeginAnimation(OpacityProperty, fade);
        }

        public void ShowAdminLogin()
        {
            HideAllViews();
            LandingView.Visibility = Visibility.Visible;
            AdminLoginView.Visibility = Visibility.Visible;
            _currentView = LandingView;
        }

        public void HideAdminLogin()
        {
            AdminLoginView.Visibility = Visibility.Collapsed;
        }

        public void ShowAdminControl()
        {
            NavigateTo(AdminControlView);
        }

        public void ShowDashboard(string visitorName = "")
        {
            NavigateTo(DashboardView, () =>
            {
                if (!string.IsNullOrWhiteSpace(visitorName))
                    DashboardView.SetVisitorName(visitorName);
            });
        }

        public void ShowRanking()
        {
            NavigateTo(RankingView);
        }

        public void ShowCompare()
        {
            NavigateTo(CompareView);
        }

        public void ShowSearch()
        {
            NavigateTo(SearchView);
        }

        public void ShowSettings()
        {
            NavigateTo(SettingsView);
        }

        public void ShowLanding()
        {
            NavigateTo(LandingView);
        }
    }
}

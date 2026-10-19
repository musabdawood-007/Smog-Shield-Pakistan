using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmogShield_Pakistan.Views
{
    public partial class AdminLoginView : UserControl
    {
        private bool _passwordVisible;

        public AdminLoginView()
        {
            InitializeComponent();
        }

        private void AdminLoginView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtUsername.Text = "";
                TxtPassword.Password = "";
                TxtPasswordPlain.Text = "";
                TxtPassword.Visibility = Visibility.Visible;
                TxtPasswordPlain.Visibility = Visibility.Collapsed;
                _passwordVisible = false;
                EyeIcon.Data = (Geometry)FindResource("Icon.EyeOff");
                HideError();
                TxtUsername.Focus();
            }
            catch { }
        }

        private void Overlay_Click(object sender, MouseButtonEventArgs e)
        {
            CloseToLanding();
        }

        private void BtnClose_Click(object sender, MouseButtonEventArgs e)
        {
            CloseToLanding();
        }

        private void BtnBack_Click(object sender, MouseButtonEventArgs e)
        {
            CloseToLanding();
        }

        private void BtnTogglePass_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _passwordVisible = !_passwordVisible;
                if (_passwordVisible)
                {
                    TxtPasswordPlain.Text = TxtPassword.Password;
                    TxtPassword.Visibility = Visibility.Collapsed;
                    TxtPasswordPlain.Visibility = Visibility.Visible;
                    EyeIcon.Data = (Geometry)FindResource("Icon.Eye");
                }
                else
                {
                    TxtPassword.Password = TxtPasswordPlain.Text;
                    TxtPasswordPlain.Visibility = Visibility.Collapsed;
                    TxtPassword.Visibility = Visibility.Visible;
                    EyeIcon.Data = (Geometry)FindResource("Icon.EyeOff");
                }
            }
            catch { }
        }

        private void BtnLogin_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var user = (TxtUsername.Text ?? "").Trim();
                var pass = _passwordVisible ? (TxtPasswordPlain.Text ?? "") : (TxtPassword.Password ?? "");

                if (user == "admin.demo" && pass == "admin123")
                {
                    HideError();
                    if (Window.GetWindow(this) is MainWindow mw)
                        mw.ShowAdminControl();
                }
                else
                {
                    ShowError();
                }
            }
            catch { }
        }

        private void ShowError()
        {
            ErrorBanner.Visibility = Visibility.Visible;
            UserInputBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
            PassInputBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        }

        private void HideError()
        {
            ErrorBanner.Visibility = Visibility.Collapsed;
            UserInputBorder.BorderBrush = (Brush)FindResource("BorderGray");
            PassInputBorder.BorderBrush = (Brush)FindResource("BorderGray");
        }

        private void CloseToLanding()
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.HideAdminLogin();
        }
    }
}

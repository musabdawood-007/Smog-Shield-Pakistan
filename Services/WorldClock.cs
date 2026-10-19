using System;
using System.Windows.Threading;

namespace SmogShield_Pakistan.Services
{
    public static class WorldClock
    {
        private static readonly DispatcherTimer Timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public static event Action? Ticked;

        public static void Start()
        {
            Timer.Tick -= OnTick;
            Timer.Tick += OnTick;
            if (!Timer.IsEnabled) Timer.Start();
        }

        private static void OnTick(object? sender, EventArgs e) => Ticked?.Invoke();

        public static string ShortText() =>
            DateTime.Now.ToString("dd MMM yyyy") + " · " + DateTime.Now.ToString("HH:mm:ss") + " PKT";

        public static string LongText() =>
            DateTime.Now.ToString("MMMM d, yyyy") + " · " + DateTime.Now.ToString("HH:mm:ss") + " PKT";
    }
}

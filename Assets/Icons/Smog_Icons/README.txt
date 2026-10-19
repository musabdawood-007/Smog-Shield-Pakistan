SmogShield icons
================
00_Window_Controls  -> minimize / maximize / restore / close (12x12, dark #333333 and _white for red close-hover)
01..08 page folders -> icons used on each Figma page
_all_icons          -> every icon in one place
Icons.xaml          -> all icons as WPF Geometry resources (ready to merge in App.xaml)

Colors: green #16743F, grey #758078, dark #333333, red #C74444

WPF tips
--------
1) App.xaml -> Application.Resources -> ResourceDictionary.MergedDictionaries:
     <ResourceDictionary Source="Icons.xaml"/>
2) Use as a Path (colour comes from Fill, so one icon = any colour):
     <Path Data="{StaticResource Icon.ShieldCheck}" Fill="#16743F" Stretch="Uniform" Width="20" Height="20"/>
3) If you paste a path yourself, start Data with "F1 " (NonZero), otherwise WPF's default EvenOdd shows half-filled shapes.
4) Custom title bar: WindowStyle="None" + System.Windows.Shell.WindowChrome, then three Buttons using
   Icon.WindowMinimize / Icon.WindowMaximize (or WindowRestore) / Icon.WindowClose.

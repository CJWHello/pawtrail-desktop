using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

[assembly: AssemblyVersion("1.0.5.0")]
[assembly: AssemblyFileVersion("1.0.5.0")]

namespace PawTrail
{
    internal static class PetIcon
    {
        static Stream Open()
        {
            return typeof(PetIcon).Assembly.GetManifestResourceStream("PawTrail.ico");
        }

        internal static Icon ForTray()
        {
            using (Stream stream = Open())
            using (var icon = new Icon(stream, Forms.SystemInformation.SmallIconSize))
                return (Icon)icon.Clone();
        }

        internal static BitmapFrame ForDisplay(int targetPixels)
        {
            using (Stream stream = Open())
            {
                var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapFrame closest = decoder.Frames[0];
                foreach (BitmapFrame frame in decoder.Frames)
                {
                    int distance = Math.Abs(frame.PixelWidth - targetPixels);
                    int best = Math.Abs(closest.PixelWidth - targetPixels);
                    if (distance < best || (distance == best && frame.PixelWidth > closest.PixelWidth))
                        closest = frame;
                }
                closest.Freeze();
                return closest;
            }
        }
    }

    public sealed class Settings
    {
        public int Skin { get; set; }
        public int Size { get; set; }
        public bool InputEnabled { get; set; }
        public bool MouseOnLeft { get; set; }
        public bool HasPosition { get; set; }
        public int LeftPx { get; set; }
        public int TopPx { get; set; }

        public Settings()
        {
            Size = 1;
            InputEnabled = true;
        }

        internal static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PawTrail", "settings.ini"); }
        }

        internal static Settings Load(string path)
        {
            Settings result = new Settings();
            int? left = null, top = null;
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] pair = line.Split(new char[] { '=' }, 2);
                    if (pair.Length != 2) continue;
                    int index;
                    bool flag;
                    if (pair[0] == "inputEnabled" && bool.TryParse(pair[1], out flag)) result.InputEnabled = flag;
                    if (pair[0] == "mouseOnLeft" && bool.TryParse(pair[1], out flag)) result.MouseOnLeft = flag;
                    if (!int.TryParse(pair[1], out index)) continue;
                    if (pair[0] == "leftPx") left = index;
                    if (pair[0] == "topPx") top = index;
                    if (index < 0 || index > 2) continue;
                    if (pair[0] == "skin") result.Skin = index;
                    if (pair[0] == "size") result.Size = index;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            if (left.HasValue && top.HasValue)
            {
                result.HasPosition = true;
                result.LeftPx = left.Value;
                result.TopPx = top.Value;
            }
            return result;
        }

        internal void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (StreamWriter writer = File.CreateText(temp))
            {
                writer.WriteLine("skin=" + Skin);
                writer.WriteLine("size=" + Size);
                writer.WriteLine("inputEnabled=" + InputEnabled);
                writer.WriteLine("mouseOnLeft=" + MouseOnLeft);
                if (HasPosition)
                {
                    writer.WriteLine("leftPx=" + LeftPx);
                    writer.WriteLine("topPx=" + TopPx);
                }
            }
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }

    internal sealed class PetApp : Application
    {
        private const string InstanceName = @"Local\PawTrail.85df219f";
        private readonly Settings settings = Settings.Load(Settings.FilePath);
        private DesktopPet pet;
        private Studio studio;
        private Forms.NotifyIcon tray;
        private Forms.ContextMenuStrip trayMenu;
        private Icon icon;
        private bool exiting;
        private bool saveErrorReported;

        private readonly bool startQuietly;

        private PetApp(bool startQuietly)
        {
            this.startQuietly = startQuietly;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        [STAThread]
        public static void Main(string[] args)
        {
            bool startQuietly = Array.Exists(args, arg => string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));
            bool created;
            using (EventWaitHandle activate = new EventWaitHandle(false, EventResetMode.AutoReset, InstanceName, out created))
            {
                if (!created)
                {
                    if (!startQuietly) activate.Set();
                    return;
                }
                PetApp app = new PetApp(startQuietly);
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                RegisteredWaitHandle listener = ThreadPool.RegisterWaitForSingleObject(activate, delegate
                {
                    if (!app.Dispatcher.HasShutdownStarted)
                        app.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(app.ShowStudio));
                }, null, Timeout.Infinite, false);
                try { app.Run(); }
                finally { listener.Unregister(null); }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            icon = PetIcon.ForTray();
            trayMenu = new Forms.ContextMenuStrip();
            trayMenu.Items.Add("打开桌宠工作室", null, delegate { ShowStudio(); });
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add("退出爪印小屋", null, delegate { ExitApp(); });
            tray = new Forms.NotifyIcon { Icon = icon, Text = "爪印小屋 · 双击打开工作室", ContextMenuStrip = trayMenu, Visible = true };
            tray.DoubleClick += delegate { ShowStudio(); };
            pet = new DesktopPet(settings);
            pet.OpenStudio += ShowStudio;
            pet.Changed += ApplyChanges;
            pet.ExitRequested += ExitApp;
            pet.Show();
            if (!startQuietly) ShowStudio();
        }

        private void ShowStudio()
        {
            if (exiting) return;
            if (studio == null)
            {
                studio = new Studio(settings) { CopyPose = pet.CopyPoseTo };
                studio.Changed += ApplyChanges;
                studio.Closed += delegate { studio = null; };
                studio.Show();
            }
            if (studio.WindowState == WindowState.Minimized) studio.WindowState = WindowState.Normal;
            studio.Activate();
            SetForegroundWindow(new WindowInteropHelper(studio).Handle);
        }

        private void ApplyChanges()
        {
            pet.RefreshState();
            if (studio != null) studio.RefreshState();
            try { settings.Save(Settings.FilePath); }
            catch (IOException) { ReportSaveError(); }
            catch (UnauthorizedAccessException) { ReportSaveError(); }
        }

        private void ReportSaveError()
        {
            if (saveErrorReported) return;
            saveErrorReported = true;
            tray.ShowBalloonTip(3500, "爪印小屋", "设置已生效。当前无法写入偏好文件，下次启动时会读取上次保存的设置。", Forms.ToolTipIcon.Info);
        }

        private void ExitApp()
        {
            if (exiting) return;
            exiting = true;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            exiting = true;
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            if (trayMenu != null) trayMenu.Dispose();
            if (icon != null) icon.Dispose();
            base.OnExit(e);
        }
    }
}

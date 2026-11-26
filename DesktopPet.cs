using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace PawTrail
{
    // Only the current animation state is retained. IDs refer to drawn physical keys.
    internal sealed class PetInputPose
    {
        private readonly bool[] down = new bool[KeyboardLayout.Count];
        private readonly long[] priority = new long[KeyboardLayout.Count];
        private readonly double[] lightUntil = new double[KeyboardLayout.Count];
        private readonly long[] mouseHeld = new long[3];
        private long sequence;
        private bool leftHeld, rightHeld, buttonHeld;
        private double leftRelease = -10, rightRelease = -10, buttonRelease = -10;
        private double targetX, targetY, lastMove = -10, scrollAt = -10, scrollDirection;

        public int LeftPawKey = -1, RightPawKey = -1, MouseButton;
        public double LeftPawPress, RightPawPress, Click, MouseX, MouseY, Scroll;
        public double Petting, Doze, Reach;
        public double LastActivity { get; private set; }
        public ulong LowKeys, HighKeys;

        public void ObserveInput(double now)
        {
            LastActivity = Math.Max(now, LastActivity);
        }

        public bool ReadyToSleep(double now) { return now - LastActivity >= 180; }

        public void AdvanceInteraction(double now, double dt, bool nearKeyboard, bool petting, bool interacting)
        {
            if (petting || interacting || leftHeld || rightHeld || buttonHeld) ObserveInput(now);
            bool sleeping = ReadyToSleep(now);
            Doze = Approach(Doze, sleeping, sleeping ? 1.4 : 14, dt);
            Petting = Approach(Petting, petting, 12, dt);
            Reach = Approach(Reach, nearKeyboard && !sleeping && !petting && !interacting, 8, dt);
        }

        private static double Approach(double value, bool active, double rate, double dt)
        {
            double target = active ? 1 : 0;
            double next = target + (value - target) * Math.Exp(-dt * rate);
            return Math.Abs(target - next) < 0.001 ? target : next;
        }

        public void Key(int id, bool pressed, double now)
        {
            ObserveInput(now);
            if (id < 0 || id >= down.Length || id >= 128 || down[id] == pressed) return;
            down[id] = pressed;
            priority[id] = pressed ? ++sequence : 0;
            if (pressed) lightUntil[id] = now + .09;
            UpdateTypingPaws(now);
        }

        // Standard touch-typing boundaries follow the stagger of each row.
        internal static bool IsRightHandKey(KeyCap key)
        {
            if (key.Label == "Space") return true;
            if (key.Y == 2.25) return key.X >= 6.5;  // T / Y
            if (key.Y == 3.25) return key.X >= 6.75; // G / H
            if (key.Y == 4.25) return key.X >= 7.25; // B / N
            return key.X >= 6;                      // 5 / 6 and the other rows
        }

        private void UpdateTypingPaws(double now)
        {
            int newest = -1, previous = -1;
            for (int i = 0; i < down.Length; i++)
            {
                if (!down[i]) continue;
                if (newest < 0 || priority[i] > priority[newest])
                {
                    previous = newest;
                    newest = i;
                }
                else if (previous < 0 || priority[i] > priority[previous]) previous = i;
            }

            int left = -1, right = -1;
            if (previous >= 0)
            {
                // Two-key chords use both paws, including chords on one half.
                // Keep their order stable so the forelegs do not cross.
                KeyCap a = KeyboardLayout.Keys[newest], b = KeyboardLayout.Keys[previous];
                double ax = a.X + a.W * .5, bx = b.X + b.W * .5;
                bool aOnLeft = ax < bx || (ax == bx && a.Id < b.Id);
                left = aOnLeft ? newest : previous;
                right = aOnLeft ? previous : newest;
            }
            else if (newest >= 0)
            {
                // When one key of a chord is released, leave the other paw in place.
                bool keepRight = rightHeld && RightPawKey == newest;
                bool keepLeft = leftHeld && LeftPawKey == newest;
                if (keepRight || (!keepLeft && IsRightHandKey(KeyboardLayout.Keys[newest]))) right = newest;
                else left = newest;
            }
            SetPaw(left, right, now, ref LeftPawKey, ref leftHeld, ref leftRelease);
            SetPaw(right, left, now, ref RightPawKey, ref rightHeld, ref rightRelease);
        }

        private static void SetPaw(int target, int otherTarget, double now, ref int displayed, ref bool held, ref double released)
        {
            if (target >= 0) displayed = target;
            else if (held) released = now;
            if (target < 0 && displayed == otherTarget) displayed = -1;
            held = target >= 0;
        }

        public void Move(double dx, double dy, double now)
        {
            if (dx == 0 && dy == 0) return;
            ObserveInput(now);
            targetX = Clamp(targetX + dx / 140.0);
            targetY = Clamp(targetY + dy / 110.0);
            lastMove = now;
        }

        public void Button(int button, bool pressed, double now)
        {
            ObserveInput(now);
            if (button < 0 || button >= mouseHeld.Length || (mouseHeld[button] != 0) == pressed) return;
            mouseHeld[button] = pressed ? ++sequence : 0;
            if (pressed)
            {
                MouseButton = button;
                buttonHeld = true;
            }
            else if (button == MouseButton)
            {
                int remaining = -1;
                for (int i = 0; i < mouseHeld.Length; ++i)
                    if (mouseHeld[i] != 0 && (remaining < 0 || mouseHeld[i] > mouseHeld[remaining])) remaining = i;
                buttonHeld = remaining >= 0;
                if (buttonHeld) MouseButton = remaining;
                else buttonRelease = now;
            }
        }

        public void Wheel(int delta, double now)
        {
            if (delta == 0) return;
            ObserveInput(now);
            scrollDirection = Math.Sign(delta);
            scrollAt = now;
        }

        public void Advance(double now, double dt)
        {
            LowKeys = HighKeys = 0;
            for (int i = 0; i < down.Length && i < 128; ++i)
            {
                if (!down[i] && lightUntil[i] <= now)
                {
                    lightUntil[i] = 0;
                    continue;
                }
                if (i < 64) LowKeys |= 1UL << i;
                else HighKeys |= 1UL << (i - 64);
            }
            LeftPawPress = leftHeld ? 1 : Rebound(now - leftRelease, .16);
            RightPawPress = rightHeld ? 1 : Rebound(now - rightRelease, .16);
            Click = buttonHeld ? 1 : Rebound(now - buttonRelease, 0.14);
            Scroll = scrollDirection * Rebound(now - scrollAt, 0.28);
            if (LeftPawPress == 0) LeftPawKey = -1;
            if (RightPawPress == 0) RightPawKey = -1;
            if (now - lastMove > 0.28)
            {
                double recenter = Math.Exp(-dt * 3.5);
                targetX *= recenter;
                targetY *= recenter;
            }
            double ease = 1 - Math.Exp(-dt * 20);
            MouseX += (targetX - MouseX) * ease;
            MouseY += (targetY - MouseY) * ease;
        }

        private static double Clamp(double value) { return Math.Max(-1, Math.Min(1, value)); }
        private static double Rebound(double age, double duration) { return Math.Max(0, Math.Min(1, 1 - age / duration)); }
    }

    public sealed class DesktopPet : Window
    {
        public event Action OpenStudio;
        public event Action Changed;
        public event Action ExitRequested;

        private readonly Settings settings;
        private readonly PetArt art = new PetArt();
        private PetInputPose pose = new PetInputPose();
        private readonly MenuItem syncItem;
        private readonly MenuItem leftMouseItem;
        private readonly MenuItem[] skinItems = new MenuItem[3];
        private readonly DispatcherTimer timer;
        private readonly Stopwatch clock = new Stopwatch();
        private IntPtr handle, rawBuffer;
        private HwndSource source;
        private bool registered, inputEnabled, placed, reposition, trackingDrag, moved, haveAbsolute, pettingHead;
        private double stamp;
        private int dragLeft, dragTop;
        private ScreenPoint dragStart, lastAbsolute;
        private static readonly double[] Widths = { 240, 320, 420 };
        private static readonly uint HeaderSize = (uint)Marshal.SizeOf(typeof(RawHeader));

        public DesktopPet(Settings s)
        {
            settings = s;
            Title = "爪印小屋";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Focusable = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.WorkArea.Left;
            Top = SystemParameters.WorkArea.Top;
            Content = art;

            ContextMenu = new ContextMenu();
            ContextMenu.Opened += delegate { pose.ObserveInput(clock.Elapsed.TotalSeconds); };
            ContextMenu.Closed += delegate { pose.ObserveInput(clock.Elapsed.TotalSeconds); };
            AddCommand(ContextMenu, "打开桌宠工作室", delegate { Raise(OpenStudio); });
            var skins = new MenuItem { Header = "切换皮肤" };
            string[] names = { "暖阳三花", "海盐水手", "草莓围巾" };
            for (int i = 0; i < names.Length; ++i)
            {
                int skin = i;
                var item = new MenuItem { Header = names[i], IsCheckable = true };
                item.Click += delegate { settings.Skin = skin; ApplyChange(); };
                skinItems[i] = item;
                skins.Items.Add(item);
            }
            ContextMenu.Items.Add(skins);
            syncItem = new MenuItem { Header = "同步键鼠动作", IsCheckable = true };
            syncItem.Click += delegate { settings.InputEnabled = !settings.InputEnabled; ApplyChange(); };
            ContextMenu.Items.Add(syncItem);
            leftMouseItem = new MenuItem { Header = "鼠标放在左侧", IsCheckable = true };
            leftMouseItem.Click += delegate { settings.MouseOnLeft = !settings.MouseOnLeft; ApplyChange(); };
            ContextMenu.Items.Add(leftMouseItem);
            ContextMenu.Items.Add(new Separator());
            AddCommand(ContextMenu, "退出桌宠", delegate { Raise(ExitRequested); });

            timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Tick;
            SourceInitialized += InitNative;
            Loaded += delegate { clock.Start(); Tick(this, EventArgs.Empty); timer.Start(); };
            SizeChanged += delegate { reposition = true; };
            Closed += Cleanup;
            MouseLeftButtonDown += Press;
            MouseLeftButtonUp += delegate { if (trackingDrag) ReleaseMouseCapture(); };
            MouseMove += Drag;
            LostMouseCapture += delegate { FinishDrag(); };
            RefreshState();
        }

        public void RefreshState()
        {
            Width = Widths[Math.Max(0, Math.Min(2, settings.Size))];
            Height = Width * 0.75;
            art.Skin = Math.Max(0, Math.Min(2, settings.Skin));
            art.MouseOnLeft = settings.MouseOnLeft;
            syncItem.IsChecked = settings.InputEnabled;
            leftMouseItem.IsChecked = settings.MouseOnLeft;
            for (int i = 0; i < skinItems.Length; ++i) skinItems[i].IsChecked = i == settings.Skin;
            if (inputEnabled != settings.InputEnabled)
            {
                inputEnabled = settings.InputEnabled;
                ResetPose();
                UpdateRegistration();
            }
        }

        public void CopyPoseTo(PetArt target)
        {
            target.MouseOnLeft = settings.MouseOnLeft;
            target.Phase = clock.Elapsed.TotalSeconds * 2;
            target.LookX = pose.MouseX;
            target.LookY = pose.MouseY;
            target.MouseX = pose.MouseX;
            target.MouseY = pose.MouseY;
            target.Click = pose.Click;
            target.MouseButton = pose.MouseButton;
            target.Scroll = pose.Scroll;
            target.Reach = pose.Reach;
            target.Petting = pose.Petting;
            target.Doze = pose.Doze;
            target.SetKeyboardPose(pose.LeftPawKey, pose.LeftPawPress, pose.RightPawKey, pose.RightPawPress, pose.LowKeys, pose.HighKeys);
        }

        private static void AddCommand(ContextMenu target, string label, Action action)
        {
            var item = new MenuItem { Header = label };
            item.Click += delegate { action(); };
            target.Items.Add(item);
        }

        private static void Raise(Action action) { if (action != null) action(); }
        private void ApplyChange() { RefreshState(); Raise(Changed); }
        private void ResetPose()
        {
            pose = new PetInputPose();
            pose.ObserveInput(clock.Elapsed.TotalSeconds);
            haveAbsolute = false;
            CopyPoseTo(art);
        }

        private void InitNative(object sender, EventArgs e)
        {
            handle = new WindowInteropHelper(this).Handle;
            SetWindowLong(handle, -20, GetWindowLong(handle, -20) | 0x08000000 | 0x00000080);
            source = HwndSource.FromHwnd(handle);
            source.AddHook(WindowMessage);
            rawBuffer = Marshal.AllocHGlobal(128);
            UpdateRegistration();
        }

        private void UpdateRegistration()
        {
            if (handle == IntPtr.Zero) return;
            if (inputEnabled && !registered) registered = RegisterInput(0x2100, handle); // INPUTSINK | DEVNOTIFY
            if (!inputEnabled && registered && RegisterInput(1, IntPtr.Zero)) registered = false;
        }

        private bool RegisterInput(uint flags, IntPtr target)
        {
            var devices = new RawDevice[] {
                new RawDevice { Page = 1, Usage = 6, Flags = flags, Target = target },
                new RawDevice { Page = 1, Usage = 2, Flags = flags, Target = target }
            };
            return RegisterRawInputDevices(devices, 2, (uint)Marshal.SizeOf(typeof(RawDevice)));
        }

        private IntPtr WindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x21)
            {
                handled = true;
                return new IntPtr(3); // Keep the current application's keyboard focus.
            }
            if (msg == 0x7E || msg == 0x1A || msg == 0x02E0) reposition = true;
            // A removed device and a suspended computer can both lose key-up packets.
            if ((msg == 0xFE && wParam.ToInt64() == 2) ||
                (msg == 0x218 && (wParam.ToInt64() == 7 || wParam.ToInt64() == 18)))
                ResetPose();
            if (msg != 0xFF || !inputEnabled || rawBuffer == IntPtr.Zero) return IntPtr.Zero;
            uint size = 128;
            uint read = GetRawInputData(lParam, 0x10000003, rawBuffer, ref size, HeaderSize);
            if (read == uint.MaxValue || read < HeaderSize + 8) return IntPtr.Zero;
            int type = Marshal.ReadInt32(rawBuffer);
            int data = (int)HeaderSize;
            double now = clock.Elapsed.TotalSeconds;
            if (type == 1)
            {
                int scan = (ushort)Marshal.ReadInt16(rawBuffer, data);
                int flags = (ushort)Marshal.ReadInt16(rawBuffer, data + 2);
                int vk = (ushort)Marshal.ReadInt16(rawBuffer, data + 6);
                if (vk != 0xFF && scan != 0xFF)
                {
                    int id = KeyboardLayout.Resolve(scan, flags, vk);
                    bool pressed = (flags & 1) == 0;
                    pose.Key(id, pressed, now);
                    // Some Pause keys send only a make sequence.
                    if (pressed && vk == 0x13) pose.Key(id, false, now);
                }
            }
            else if (type == 0 && read >= HeaderSize + 20)
            {
                int flags = (ushort)Marshal.ReadInt16(rawBuffer, data + 4);
                if (flags != 0) pose.ObserveInput(now);
                if ((Marshal.ReadInt16(rawBuffer, data) & 1) == 0)
                    pose.Move(Marshal.ReadInt32(rawBuffer, data + 12), Marshal.ReadInt32(rawBuffer, data + 16), now);
                else
                {
                    ScreenPoint cursor;
                    if (GetCursorPos(out cursor))
                    {
                        if (haveAbsolute) pose.Move(cursor.X - lastAbsolute.X, cursor.Y - lastAbsolute.Y, now);
                        lastAbsolute = cursor;
                        haveAbsolute = true;
                    }
                }
                for (int button = 0; button < 3; ++button)
                {
                    if ((flags & (1 << (2 * button))) != 0) pose.Button(button, true, now);
                    if ((flags & (2 << (2 * button))) != 0) pose.Button(button, false, now);
                }
                if ((flags & 0x0C00) != 0) pose.Wheel(Marshal.ReadInt16(rawBuffer, data + 6), now);
            }
            return IntPtr.Zero;
        }

        private void Press(object sender, MouseButtonEventArgs e)
        {
            pose.ObserveInput(clock.Elapsed.TotalSeconds);
            if (e.ClickCount == 2)
            {
                Raise(OpenStudio);
                e.Handled = true;
                return;
            }
            NativeRect frame;
            if (!GetCursorPos(out dragStart) || !GetWindowRect(handle, out frame)) return;
            dragLeft = frame.Left;
            dragTop = frame.Top;
            moved = false;
            trackingDrag = CaptureMouse();
            pettingHead = trackingDrag && art.HitHead(e.GetPosition(art));
            e.Handled = true;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (!trackingDrag) return;
            ScreenPoint cursor;
            NativeRect frame, work;
            if (!GetCursorPos(out cursor) || !GetWindowRect(handle, out frame) || !WorkAt(cursor, out work)) return;
            int dx = cursor.X - dragStart.X, dy = cursor.Y - dragStart.Y;
            if (!moved && Math.Abs(dx) < GetSystemMetrics(68) && Math.Abs(dy) < GetSystemMetrics(69)) return;
            moved = true;
            pettingHead = false;
            MoveWithin(frame, work, (long)dragLeft + dx, (long)dragTop + dy);
        }

        private void FinishDrag()
        {
            if (!trackingDrag) return;
            pettingHead = false;
            trackingDrag = false;
            pose.ObserveInput(clock.Elapsed.TotalSeconds);
            NativeRect frame;
            if (!moved || !GetWindowRect(handle, out frame)) return;
            settings.HasPosition = true;
            settings.LeftPx = frame.Left;
            settings.TopPx = frame.Top;
            Raise(Changed);
        }

        private void Tick(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds;
            double dt = Math.Min(0.12, Math.Max(0, now - stamp));
            stamp = now;
            if ((!placed || reposition) && !trackingDrag) PlaceWindow();
            if (inputEnabled) pose.Advance(now, dt);
            if (!registered || !inputEnabled)
            {
                var info = new LastInputInfo { Size = 8 };
                if (GetLastInputInfo(ref info))
                {
                    uint idleMs = unchecked((uint)Environment.TickCount - info.Time);
                    pose.ObserveInput(now - idleMs / 1000.0);
                }
            }
            // A button may already be down at startup, or pressed while sync is paused.
            // Check only at sleep onset. The aggregate result is discarded immediately.
            if (pose.Doze == 0 && pose.ReadyToSleep(now) && AnyButtonDown()) pose.ObserveInput(now);
            ScreenPoint cursor;
            bool near = placed && art.IsLoaded && GetCursorPos(out cursor) &&
                        art.NearKeyboard(art.PointFromScreen(new Point(cursor.X, cursor.Y)));
            pose.AdvanceInteraction(now, dt, near, pettingHead, trackingDrag || ContextMenu.IsOpen);
            CopyPoseTo(art);
        }

        private static bool AnyButtonDown()
        {
            for (int vk = 1; vk < 255; ++vk)
                if (GetAsyncKeyState(vk) < 0) return true;
            return false;
        }

        private void PlaceWindow()
        {
            NativeRect frame, work;
            if (!GetWindowRect(handle, out frame)) return;
            var point = new ScreenPoint { X = settings.LeftPx, Y = settings.TopPx };
            if (!WorkAt(settings.HasPosition ? point : new ScreenPoint(), out work)) return;
            long x = settings.HasPosition ? settings.LeftPx : work.Right - frame.Width - 22;
            long y = settings.HasPosition ? settings.TopPx : work.Bottom - frame.Height - 18;
            MoveWithin(frame, work, x, y);
            placed = true;
            reposition = false;
        }

        private void MoveWithin(NativeRect frame, NativeRect work, long x, long y)
        {
            int px = (int)Math.Max(work.Left, Math.Min(work.Right - frame.Width, x));
            int py = (int)Math.Max(work.Top, Math.Min(work.Bottom - frame.Height, y));
            if (px != frame.Left || py != frame.Top)
                SetWindowPos(handle, IntPtr.Zero, px, py, 0, 0, 0x15);
        }

        private static bool WorkAt(ScreenPoint point, out NativeRect work)
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            bool ok = GetMonitorInfo(MonitorFromPoint(point, 2), ref info);
            work = info.Work;
            return ok;
        }

        private void Cleanup(object sender, EventArgs e)
        {
            timer.Stop();
            if (registered) RegisterInput(1, IntPtr.Zero);
            if (source != null && !source.IsDisposed) source.RemoveHook(WindowMessage);
            if (rawBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rawBuffer);
            rawBuffer = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)] private struct ScreenPoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }
        [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Bounds, Work; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] private struct RawDevice { public ushort Page, Usage; public uint Flags; public IntPtr Target; }
        [StructLayout(LayoutKind.Sequential)] private struct RawHeader { public uint Type, Size; public IntPtr Device, WParam; }
        [StructLayout(LayoutKind.Sequential)] private struct LastInputInfo { public uint Size, Time; }

        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out ScreenPoint point);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(ScreenPoint point, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
        [DllImport("user32.dll")] private static extern bool RegisterRawInputDevices(RawDevice[] devices, uint count, uint size);
        [DllImport("user32.dll")] private static extern uint GetRawInputData(IntPtr handle, uint command, IntPtr buffer, ref uint size, uint headerSize);
    }
}

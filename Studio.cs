using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PawTrail
{
    public class Studio : Window
    {
        static readonly string[] Names = { "暖阳三花", "海盐水手", "草莓围巾" };
        static readonly string[] Washes = { "#F6E8D1", "#DDEBEB", "#F6E0DC" };
        static readonly string[] InteractionNames = { "打字联动", "手搭键盘", "摸头互动", "犯困睡觉", "小猫玩球", "乌鸦展翅" };
        static readonly string[] InteractionHints = { "左右前爪分工敲键", "指针靠近，双爪搭键盘", "按住猫头，闭眼享受", "闲置三分钟后趴睡", "依次敲下 c a t", "依次敲下 b i r d" };
        static readonly string[] InteractionIcons = { "keyboard", "reach", "petting", "sleep", "kitten", "crow" };
        static readonly int[] SampleKeys = {
            KeyboardLayout.Resolve(0x32, 0, 0x4D), KeyboardLayout.Resolve(0x12, 0, 0x45),
            KeyboardLayout.Resolve(0x18, 0, 0x4F), KeyboardLayout.Resolve(0x11, 0, 0x57)
        };
        const string Ink = "#263E32", Muted = "#59695B", Paper = "#FEFBF3", Line = "#E1DBCA";
        readonly Settings settings;
        readonly Border[] frames = new Border[3], marks = new Border[3];
        readonly Button[] cardButtons = new Button[3];
        readonly PetArt[] thumbnails = new PetArt[3];
        readonly RadioButton[] mouseChoices = new RadioButton[2], sizeChoices = new RadioButton[3];
        readonly PetArt preview = new PetArt();
        readonly TextBlock currentLook = Label("", 12, "#526546", FontWeights.SemiBold);
        readonly Border knob = new Border(), track = new Border();
        readonly DispatcherTimer animation = new DispatcherTimer();
        readonly Stopwatch demoClock = new Stopwatch();
        readonly Border[] demoCards = new Border[InteractionNames.Length];
        readonly ScrollViewer scroller = new ScrollViewer();
        int activeDemo = -1;
        double demoStartPhase;
        Button syncButton;
        public event Action Changed;
        public Action<PetArt> CopyPose { get; set; }

        public Studio(Settings s)
        {
            settings = s;
            Title = "爪印小屋 · 桌宠工作室";
            FontFamily = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = B(Paper);
            Width = Math.Min(760, SystemParameters.WorkArea.Width);
            Height = Math.Min(691, SystemParameters.WorkArea.Height);
            BorderThickness = new Thickness(1);
            BorderBrush = B("#BAC1B5");
            Grid page = new Grid();
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition());
            page.Children.Add(Header());
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroller.Focusable = false;
            Grid.SetRow(scroller, 1);
            page.Children.Add(scroller);
            StackPanel body = new StackPanel { Margin = new Thickness(25, 5, 25, 18) };
            scroller.Content = body;
            body.Children.Add(Featured());
            TextBlock choose = Label("皮肤衣橱", 18, Ink, FontWeights.Bold);
            choose.Margin = new Thickness(1, 17, 0, 10);
            body.Children.Add(choose);
            Grid gallery = new Grid();
            for (int i = 0; i < Names.Length; i++)
            {
                gallery.ColumnDefinitions.Add(new ColumnDefinition());
                Button card = Card(i);
                Grid.SetColumn(card, i);
                gallery.Children.Add(card);
            }
            body.Children.Add(gallery);
            body.Children.Add(Options());
            body.Children.Add(InteractionGuide());
            Content = page;
            RefreshState();
            animation.Interval = TimeSpan.FromMilliseconds(32);
            animation.Tick += AnimatePreview;
            IsVisibleChanged += delegate {
                if (IsVisible) animation.Start();
                else animation.Stop();
            };
            Closed += delegate { animation.Stop(); animation.Tick -= AnimatePreview; };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            };
        }

        UIElement Header()
        {
            Grid head = new Grid { Height = 57, Background = B(Paper) };
            head.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
            StackPanel brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(25, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            const int emblemSize = 32;
            var emblem = new Image {
                Width = emblemSize, Height = emblemSize, SnapsToDevicePixels = true,
                Source = PetIcon.ForDisplay((int)Math.Round(emblemSize * VisualTreeHelper.GetDpi(this).DpiScaleX))
            };
            RenderOptions.SetBitmapScalingMode(emblem, BitmapScalingMode.NearestNeighbor);
            brand.Children.Add(emblem);
            TextBlock title = Label("爪印小屋", 18, Ink, FontWeights.Bold);
            title.Margin = new Thickness(9, 0, 12, 0);
            brand.Children.Add(title);
            brand.Children.Add(new Border { Background = B(Line), Width = 1, Height = 15, VerticalAlignment = VerticalAlignment.Center });
            TextBlock subtitle = Label("桌宠工作室", 12, Muted, FontWeights.Normal);
            subtitle.Margin = new Thickness(12, 0, 0, 0);
            brand.Children.Add(subtitle);
            head.Children.Add(brand);
            Button close = ActionButton("收起工作室", new Symbol("close", Ink));
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            close.Margin = new Thickness(0, 0, 17, 0);
            close.Width = close.Height = 32;
            close.ToolTip = "收起工作室 (Esc)";
            close.Click += delegate { Close(); };
            head.Children.Add(close);
            return head;
        }

        UIElement Featured()
        {
            Border rim = new Border { BorderThickness = new Thickness(1), BorderBrush = B("#CCCFB9"), CornerRadius = new CornerRadius(16), Height = 221 };
            Grid stage = new Grid { Background = B("#EDF0D8") };
            ClipRounded(stage, 15);
            rim.Child = stage;
            stage.Children.Add(new DeskScene());
            Grid layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.47, GridUnitType.Star) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.53, GridUnitType.Star) });
            stage.Children.Add(layout);
            StackPanel intro = new StackPanel { Margin = new Thickness(24, 0, 1, 0), VerticalAlignment = VerticalAlignment.Center };
            intro.Children.Add(currentLook);
            TextBlock name = Label("长毛三花猫", 38, Ink, FontWeights.Bold);
            name.Margin = new Thickness(-2, 0, 0, 3);
            intro.SizeChanged += delegate { name.FontSize = Math.Min(38, Math.Max(21, (intro.ActualWidth - 2) / 5)); };
            intro.Children.Add(name);
            TextBlock typing = Label("两只前爪跟你敲键，尾巴同步操作鼠标。", 13, "#485A44", FontWeights.Normal);
            typing.TextWrapping = TextWrapping.Wrap;
            intro.Children.Add(typing);
            TextBlock dragging = Label("拖动猫咪身体可以调整桌宠位置。", 13, "#485A44", FontWeights.Normal);
            dragging.TextWrapping = TextWrapping.Wrap;
            dragging.Margin = new Thickness(0, 3, 0, 0);
            intro.Children.Add(dragging);
            StackPanel cta = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            cta.Children.Add(Label("回到桌面", 13, "#FFFDF1", FontWeights.SemiBold));
            cta.Children.Add(new Symbol("arrow", "#FFFDF1") { Width = 16, Height = 16, Margin = new Thickness(8, 0, 0, 0) });
            Button home = ActionButton("回到桌面", new Border { CornerRadius = new CornerRadius(9), Background = B(Ink), Child = cta });
            home.Width = 111; home.Height = 34; home.Margin = new Thickness(0, 14, 0, 0); home.HorizontalAlignment = HorizontalAlignment.Left;
            home.Click += delegate { Close(); };
            intro.Children.Add(home);
            layout.Children.Add(intro);
            preview.Width = 259; preview.Height = 194; preview.VerticalAlignment = VerticalAlignment.Bottom;
            preview.Margin = new Thickness(0, 0, 3, 9); preview.IsHitTestVisible = false;
            Grid.SetColumn(preview, 1);
            layout.Children.Add(preview);
            return rim;
        }

        Button Card(int index)
        {
            Grid inside = new Grid();
            inside.RowDefinitions.Add(new RowDefinition());
            inside.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
            Grid portrait = new Grid { Background = B(Washes[index]) };
            portrait.Children.Add(new Specimen(index));
            thumbnails[index] = new PetArt { Skin = index, Width = 124, Height = 93, VerticalAlignment = VerticalAlignment.Center };
            portrait.Children.Add(thumbnails[index]);
            inside.Children.Add(portrait);
            Grid caption = new Grid { Background = B("#FFFDF7") };
            TextBlock text = Label(Names[index], 13, Ink, FontWeights.SemiBold);
            text.Margin = new Thickness(12, 0, 0, 0);
            caption.Children.Add(text);
            marks[index] = new Border { Width = 17, Height = 17, CornerRadius = new CornerRadius(10), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), BorderThickness = new Thickness(1) };
            caption.Children.Add(marks[index]);
            Grid.SetRow(caption, 1); inside.Children.Add(caption);
            ClipRounded(inside, 9);
            frames[index] = new Border { CornerRadius = new CornerRadius(11), BorderThickness = new Thickness(2), Child = inside };
            Button button = ActionButton("选择" + Names[index] + "皮肤", frames[index]);
            button.Height = 132;
            button.Margin = new Thickness(index == 0 ? 0 : 5, 0, index == 2 ? 0 : 5, 0);
            button.Click += delegate { if (settings.Skin != index) { settings.Skin = index; Apply(); } };
            cardButtons[index] = button;
            return button;
        }

        UIElement Options()
        {
            Border box = new Border { Margin = new Thickness(0, 17, 0, 0), Padding = new Thickness(6, 0, 6, 0),
                BorderThickness = new Thickness(1), BorderBrush = B(Line), CornerRadius = new CornerRadius(11), Background = B("#F6F5EC") };
            UniformGrid groups = new UniformGrid { Rows = 1, Columns = 3 };
            box.Child = groups;
            track.Width = 37; track.Height = 22; track.CornerRadius = new CornerRadius(13);
            knob.Width = knob.Height = 16; knob.Background = B("#FFFDF7"); knob.CornerRadius = new CornerRadius(10);
            knob.VerticalAlignment = VerticalAlignment.Center; knob.Margin = new Thickness(3, 0, 3, 0); track.Child = knob;
            syncButton = ActionButton("键鼠同步", track);
            syncButton.Width = 44; syncButton.Height = 33;
            syncButton.Click += delegate { settings.InputEnabled = !settings.InputEnabled; Apply(); };
            groups.Children.Add(SettingGroup("keyboard", "键鼠同步", syncButton));
            Border side = ChoiceStrip("鼠标位置", new[] { "左侧", "右侧" }, mouseChoices, 104, delegate(int index) {
                bool left = index == 0;
                if (settings.MouseOnLeft != left) { settings.MouseOnLeft = left; Apply(); }
            });
            groups.Children.Add(SettingGroup("mouse", "鼠标位置", side));
            Border size = ChoiceStrip("桌宠大小", new[] { "小", "中", "大" }, sizeChoices, 104, delegate(int value) {
                if (settings.Size != value) { settings.Size = value; Apply(); }
            });
            groups.Children.Add(SettingGroup("size", "桌宠大小", size));
            return box;
        }

        static StackPanel SettingGroup(string icon, string title, FrameworkElement control)
        {
            StackPanel group = new StackPanel { Margin = new Thickness(4, 9, 4, 9) };
            StackPanel heading = SettingTitle(icon, title);
            heading.HorizontalAlignment = HorizontalAlignment.Center;
            group.Children.Add(heading);
            control.HorizontalAlignment = HorizontalAlignment.Center;
            control.Margin = new Thickness(0, 5, 0, 0);
            group.Children.Add(control);
            return group;
        }

        static Border ChoiceStrip(string name, string[] labels, RadioButton[] buttons, double width, Action<int> choose)
        {
            UniformGrid segments = new UniformGrid { Rows = 1, Columns = labels.Length };
            KeyboardNavigation.SetDirectionalNavigation(segments, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetTabNavigation(segments, KeyboardNavigationMode.Once);
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            ControlTemplate template = new ControlTemplate(typeof(RadioButton)) { VisualTree = border };
            Trigger selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            selected.Setters.Add(new Setter(Control.BackgroundProperty, B(Ink)));
            selected.Setters.Add(new Setter(Control.ForegroundProperty, B("#FFFDF7")));
            template.Triggers.Add(selected);
            for (int i = 0; i < labels.Length; i++)
            {
                int value = i;
                RadioButton button = new RadioButton { GroupName = name, Content = labels[i], Template = template,
                    FontSize = 12, FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand };
                AutomationProperties.SetName(button, name + "：" + labels[i]);
                button.Checked += delegate { choose(value); };
                button.KeyDown += delegate(object sender, KeyEventArgs e) {
                    int step = e.Key == Key.Right ? 1 : e.Key == Key.Left ? -1 : 0;
                    if (step == 0) return;
                    int next = (value + step + buttons.Length) % buttons.Length;
                    buttons[next].Focus(); choose(next); e.Handled = true;
                };
                segments.Children.Add(button);
                buttons[i] = button;
            }
            return new Border { Child = segments, Padding = new Thickness(2), Background = B("#E8E9DD"), CornerRadius = new CornerRadius(8),
                Width = width, Height = 30 };
        }

        static StackPanel SettingTitle(string icon, string text)
        {
            StackPanel line = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            line.Children.Add(new Symbol(icon, "#4E634C") { Width = 20, Height = 20, Margin = new Thickness(0, 0, 8, 0) });
            line.Children.Add(Label(text, 13, Ink, FontWeights.SemiBold));
            return line;
        }

        UIElement InteractionGuide()
        {
            StackPanel guide = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
            Grid heading = new Grid();
            heading.Children.Add(Label("互动图鉴", 18, Ink, FontWeights.Bold));
            TextBlock hint = Label("点选预演", 12, Muted, FontWeights.Normal);
            hint.HorizontalAlignment = HorizontalAlignment.Right;
            heading.Children.Add(hint);
            guide.Children.Add(heading);
            UniformGrid cards = new UniformGrid { Columns = 3, Margin = new Thickness(-3, 6, -3, 0) };
            cards.SizeChanged += delegate { cards.Columns = cards.ActualWidth >= 595 ? 3 : 2; };
            for (int i = 0; i < InteractionNames.Length; i++)
            {
                int index = i;
                StackPanel body = new StackPanel { Margin = new Thickness(10, 6, 5, 6), VerticalAlignment = VerticalAlignment.Center };
                StackPanel title = new StackPanel { Orientation = Orientation.Horizontal };
                title.Children.Add(new Symbol(InteractionIcons[i], "#926132") { Width = 20, Height = 20, Margin = new Thickness(0,0,6,0) });
                title.Children.Add(Label(InteractionNames[i], 14, "#58422F", FontWeights.SemiBold));
                body.Children.Add(title);
                TextBlock text = Label(InteractionHints[i], 11, "#716048", FontWeights.Normal);
                text.TextWrapping = TextWrapping.Wrap;
                text.Margin = new Thickness(0, 3, 0, 0);
                body.Children.Add(text);
                demoCards[i] = new Border { Background = B("#FBF2DE"), BorderBrush = B("#E4D7B9"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Child = body };
                Button button = ActionButton("预演" + InteractionNames[i], demoCards[i]);
                button.Margin = new Thickness(3); button.MinHeight = 56;
                AutomationProperties.SetHelpText(button, InteractionHints[i]);
                button.Click += delegate { StartDemo(index); };
                cards.Children.Add(button);
            }
            guide.Children.Add(cards);
            return guide;
        }

        void StartDemo(int index)
        {
            if (activeDemo >= 0) PaintDemoCard(activeDemo, false);
            activeDemo = index;
            preview.ClearKeyFeedback();
            demoStartPhase = preview.Phase;
            demoClock.Restart();
            PaintDemoCard(index, true);
            SetPreviewCaption();
            if (scroller.VerticalOffset > 0) scroller.ScrollToTop();
            AnimatePreview(this, EventArgs.Empty);
        }

        void PaintDemoCard(int index, bool active)
        {
            demoCards[index].Background = B(active ? "#F5DEAA" : "#FBF2DE");
            demoCards[index].BorderBrush = B(active ? "#AD7A3D" : "#E4D7B9");
        }

        void SetPreviewCaption()
        {
            currentLook.Text = activeDemo >= 0 ? "正在预演  /  " + InteractionNames[activeDemo]
                : "当前装扮  /  " + Names[Math.Max(0, Math.Min(2, settings.Skin))];
        }

        void Apply()
        {
            RefreshState();
            Action changed = Changed;
            if (changed != null) changed();
        }

        public void RefreshState()
        {
            int selected = Math.Max(0, Math.Min(2, settings.Skin));
            SetPreviewCaption(); preview.Skin = selected;
            if (!settings.InputEnabled && activeDemo < 0) preview.ClearKeyFeedback();
            preview.MouseOnLeft = settings.MouseOnLeft;
            mouseChoices[settings.MouseOnLeft ? 0 : 1].IsChecked = true;
            for (int i = 0; i < 3; i++)
            {
                thumbnails[i].MouseOnLeft = settings.MouseOnLeft;
                bool active = i == selected;
                frames[i].BorderBrush = B(active ? "#54744F" : Line);
                marks[i].Background = B(active ? Ink : "#FFFDF7"); marks[i].BorderBrush = B(active ? Ink : "#BABFB0");
                marks[i].Child = active ? new Symbol("tick", "#FFFDF7") : null;
                AutomationProperties.SetItemStatus(cardButtons[i], active ? "已选中" : "可选");
                sizeChoices[i].IsChecked = i == settings.Size;
            }
            track.Background = B(settings.InputEnabled ? "#51734C" : "#90988D");
            knob.HorizontalAlignment = settings.InputEnabled ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            AutomationProperties.SetItemStatus(syncButton, settings.InputEnabled ? "已开启" : "已关闭");
        }

        void AnimatePreview(object sender, EventArgs e)
        {
            if (activeDemo >= 0)
            {
                double t = demoClock.Elapsed.TotalSeconds;
                if (t < DemoDuration) { DrawDemo(t); return; }
                PaintDemoCard(activeDemo, false);
                activeDemo = -1;
                demoClock.Stop();
                SetPreviewCaption();
                preview.ClearKeyFeedback();
                preview.Petting = preview.Reach = preview.Doze = 0;
                preview.SetGuests(0, 0);
                preview.SetKeyboardPose(-1, 0, -1, 0, 0, 0);
            }
            Action<PetArt> copy = CopyPose;
            if (copy != null) copy(preview);
        }

        double DemoDuration { get { return activeDemo >= 4 ? 8.0 : 2.6; } }

        void DrawDemo(double t)
        {
            // Ease into a pose and release it before resuming the live input feed.
            double amount = Math.Max(0, Math.Min(1, Math.Min(t / .26, (DemoDuration - t) / .28)));
            amount = amount * amount * (3 - 2 * amount);
            preview.Phase = demoStartPhase + 2 * t;
            preview.Click = preview.Scroll = 0;
            preview.Reach = activeDemo == 1 ? amount : 0;
            preview.LookX = preview.MouseX = (settings.MouseOnLeft ? .75 : -.75) * preview.Reach;
            preview.MouseY = -.25 * preview.Reach;
            preview.LookY = .25 * preview.Reach;
            preview.Petting = activeDemo == 2 ? amount : 0;
            preview.Doze = activeDemo == 3 ? amount : 0;
            preview.SetGuests(activeDemo == 4 ? amount : 0, activeDemo == 5 ? amount : 0,
                activeDemo == 4 ? t : 0, activeDemo == 5 ? t : 0);
            if (activeDemo != 0) { preview.SetKeyboardPose(-1, 0, -1, 0, 0, 0); return; }
            double beat = t / .33;
            int step = (int)beat % SampleKeys.Length;
            int id = SampleKeys[step];
            double press = Math.Sin(Math.PI * (beat % 1)) * amount;
            bool rightPaw = PetInputPose.IsRightHandKey(KeyboardLayout.Keys[id]);
            ulong key = press > .35 ? 1UL << (id % 64) : 0;
            preview.SetKeyboardPose(rightPaw ? -1 : id, rightPaw ? 0 : press, rightPaw ? id : -1, rightPaw ? press : 0,
                id < 64 ? key : 0, id >= 64 ? key : 0);
        }

        static TextBlock Label(string value, double size, string color, FontWeight weight)
        {
            return new TextBlock { Text = value, FontSize = size, Foreground = B(color), FontWeight = weight, VerticalAlignment = VerticalAlignment.Center };
        }

        static void ClipRounded(FrameworkElement element, double radius)
        {
            element.SizeChanged += delegate {
                element.Clip = new RectangleGeometry(new Rect(element.RenderSize), radius, radius);
            };
        }

        static Button ActionButton(string label, object content)
        {
            FrameworkElementFactory host = new FrameworkElementFactory(typeof(Border));
            host.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            host.AppendChild(cp);
            ControlTemplate template = new ControlTemplate(typeof(Button)) { VisualTree = host };
            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, .88)); template.Triggers.Add(hover);
            Trigger press = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            press.Setters.Add(new Setter(UIElement.OpacityProperty, .73)); template.Triggers.Add(press);
            Button b = new Button { Content = content, Template = template, Cursor = Cursors.Hand, VerticalContentAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetName(b, label);
            return b;
        }

        static readonly Dictionary<string, SolidColorBrush> Palette = new Dictionary<string, SolidColorBrush>();
        static SolidColorBrush B(string hex)
        {
            SolidColorBrush brush;
            if (!Palette.TryGetValue(hex, out brush)) {
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); brush.Freeze(); Palette.Add(hex, brush);
            }
            return brush;
        }

        abstract class Illustrator : FrameworkElement
        {
            static readonly Dictionary<string, Geometry> Shapes = new Dictionary<string, Geometry>();
            protected static void Path(DrawingContext dc, string d, string fill, string stroke, double width)
            {
                Geometry geometry;
                if (!Shapes.TryGetValue(d, out geometry)) { geometry = Geometry.Parse(d); geometry.Freeze(); Shapes.Add(d, geometry); }
                Pen pen = null;
                if (stroke != null) { pen = new Pen(B(stroke), width); pen.StartLineCap = pen.EndLineCap = PenLineCap.Round; pen.LineJoin = PenLineJoin.Round; }
                dc.DrawGeometry(fill == null ? null : B(fill), pen, geometry);
            }
            protected static void Ellipse(DrawingContext dc, string color, double x, double y, double rx, double ry)
            {
                dc.DrawEllipse(B(color), null, new Point(x,y), rx, ry);
            }
        }

        class Symbol : Illustrator
        {
            readonly string glyph, color;
            public Symbol(string glyph, string color) { this.glyph = glyph; this.color = color; IsHitTestVisible = false; }
            protected override void OnRender(DrawingContext dc)
            {
                double scale = Math.Min(ActualWidth, ActualHeight) / 24.0;
                dc.PushTransform(new TranslateTransform((ActualWidth-scale*24)/2,(ActualHeight-scale*24)/2));
                dc.PushTransform(new ScaleTransform(scale, scale));
                switch (glyph)
                {
                    case "tick": Path(dc,"M5.8,12 L10.2,16.2 18.5,8",null,color,2.7); break;
                    case "close": Path(dc,"M8,8 L16,16 M16,8 L8,16",null,color,1.45); break;
                    case "arrow": Path(dc,"M5,12 L18,12 M13,6.8 L18.2,12 13,17.2",null,color,1.8); break;
                    case "keyboard": Path(dc,"M3,5.5 H21 Q22,5.5 22,6.5 V17.5 Q22,18.5 21,18.5 H3 Q2,18.5 2,17.5 V6.5 Q2,5.5 3,5.5Z M5.5,9 H6 M9.5,9 H10 M13.5,9 H14 M17.5,9 H18 M5.5,12 H6 M9.5,12 H10 M13.5,12 H14 M17.5,12 H18 M8,15 H16",null,color,1.5); break;
                    case "mouse": Path(dc,"M12,2.5 C8.2,2.5 5.3,5.4 5.3,9 V14.8 C5.3,18.5 8.1,21.3 12,21.3 C15.9,21.3 18.7,18.5 18.7,14.8 V9 C18.7,5.4 15.8,2.5 12,2.5Z M12,5.7 V9.2",null,color,1.6); break;
                    case "reach": Path(dc,"M2.8,15 H21 V20 H2.8Z M5,17.5 H7 M9,17.5 H11 M13,17.5 H16 M8,4 C10,3 12,4 13,6 L15.2,11 C15.8,13 13,14 11.8,12.5 L9.5,9.4 M12.6,5.4 L15,7.5",null,color,1.7); break;
                    case "petting": Path(dc,"M3.5,18 V12 L7.5,8.8 9.5,12 H14 L16.4,8.8 20,12 V18 Q13,23 3.5,18Z M7.5,15 Q9,16.6 10.5,15 M13,15 Q14.6,16.6 16.1,15 M11,5.7 Q12.5,3.4 14,5.7 Q15.5,3.4 17,5.7 Q17.2,7.2 14,9 Q10.9,7.2 11,5.7Z",null,color,1.4); break;
                    case "sleep": Path(dc,"M13,3.4 C6.4,3 2.8,8.3 3.6,13.5 C4.5,19.1 11.4,22.8 17.6,18.6 C11.4,18.5 7.8,14.5 8.5,9.6 C8.7,6.8 10.3,4.9 13,3.4Z M15.5,7.8 H20 L15.5,12.4 H20",null,color,1.6); break;
                    case "kitten": Path(dc,"M3,12 L4,4 L9,8 Q12,7 15,8 L20,4 L21,12 Q23,20 12,21 Q2,20 3,12Z M7,13 H8 M16,13 H17 M11,16 Q12,18 13,16",null,color,1.5); break;
                    case "crow": Path(dc,"M2,18 L7,12 Q9,8 12,8 Q13,3 17,4 Q20,4 20,7 L23,9 L19,10 Q19,16 14,17 L13,21 M10,17 L9,20 M5,15 Q9,16 13,12",null,color,1.7); break;
                    case "size": Path(dc,"M3.5,9 L3.5,3.5 9,3.5 M15,3.5 L20.5,3.5 20.5,9 M20.5,15 L20.5,20.5 15,20.5 M9,20.5 L3.5,20.5 3.5,15 M8,16 L15.7,8.2 M11.2,8.2 L15.7,12.7",null,color,1.5); break;
                }
                dc.Pop(); dc.Pop();
            }
        }

        class Specimen : Illustrator
        {
            readonly int skin;
            public Specimen(int skin) { this.skin = skin; IsHitTestVisible = false; }
            protected override void OnRender(DrawingContext dc)
            {
                dc.PushTransform(new ScaleTransform(ActualWidth/220, ActualHeight/85));
                string fill = new [] { "#EED5AB", "#C6DDE0", "#EBCBC6" }[skin];
                Ellipse(dc,fill,108,53,47,44);
                Path(dc,"M10,66 C19,62 30,62 39,66 M178,20 C186,17 194,18 200,20",null,fill,1.7);
                Path(dc,"M21,73 Q27,70 33,72 M184,25 Q190,23 196,25",null,fill,1.5);
                dc.Pop();
            }
        }

        class DeskScene : Illustrator
        {
            protected override void OnRender(DrawingContext dc)
            {
                dc.PushTransform(new ScaleTransform(ActualWidth / 700, ActualHeight / 195));
                dc.DrawRectangle(B("#F1EFDB"), null, new Rect(0,0,700,195));
                Rect window = new Rect(333, 11, 330, 128);
                dc.DrawRoundedRectangle(B("#CDD3B9"), null, new Rect(329, 7, 338, 136), 8, 8);
                dc.PushClip(new RectangleGeometry(window, 4, 4));
                dc.DrawRectangle(B("#E1E8D7"), null, window);
                Ellipse(dc,"#F4D394",578,47,34,34);
                Path(dc,"M358,43 Q368,37 375,38 Q386,26 395,36 Q406,34 415,43Z M600,67 Q608,59 619,61 Q625,52 638,59 Q648,58 655,67Z","#F7F4DD",null,0);
                Path(dc,"M319,135 C357,111 388,107 410,110 C446,116 474,95 502,87 C546,73 583,105 625,87 C645,78 661,75 681,79 L681,140 319,140Z","#C2D5B7",null,0);
                Path(dc,"M315,142 C365,124 394,113 432,120 C482,130 524,100 563,109 C599,116 638,88 677,99 L677,144Z","#A4BF9E",null,0);
                Tree(dc,638,140,0.58,"#738F76","#94AB8F");
                Tree(dc,659,139,0.82,"#65856B","#8CA380");
                dc.DrawRectangle(B("#F3F0D9"), null, new Rect(416,11,5,130));
                dc.DrawRectangle(B("#F3F0D9"), null, new Rect(333,65,330,4));
                dc.Pop();
                dc.DrawRoundedRectangle(B("#BBC4A9"), null, new Rect(324,139,348,5),2,2);
                // A chamfered wooden writing desk gives the cat and its keyboard a surface.
                Path(dc,"M256,195 L330,153 Q334,150 340,150 H700 V195Z","#D8BD96",null,0);
                Path(dc,"M256,191 L330,149 Q334,146 340,146 H700 V191Z","#EAD5B2",null,0);
                Path(dc,"M356,147 H402 L363,172 H317Z M408,147 H532 L493,172 H369Z","#F3E1BE",null,0);
                Path(dc,"M335,165 Q361,162 383,164 M354,184 Q379,180 405,182 M607,176 Q629,173 649,175 M679,160 L702,159",null,"#CFB690",1.2);
                // A little potted plant on the windowsill.
                Path(dc,"M680,135 Q680,116 674,101 M680,126 Q683,114 692,108",null,"#73866A",1.7);
                Path(dc,"M677,118 Q663,116 662,104 Q674,103 677,118Z M675,106 Q665,101 665,91 Q676,94 675,106Z M679,120 Q678,107 689,103 Q692,112 679,120Z M685,116 Q688,104 699,105 Q698,113 685,116Z","#8EAA83",null,0);
                Path(dc,"M668,131 H691 L687,148 Q680,151 673,148Z","#CC947D",null,0);
                dc.DrawRoundedRectangle(B("#D8A58E"),null,new Rect(667,129,25,5),2,2);
                dc.Pop();
            }
            static void Tree(DrawingContext dc, double x, double y, double scale, string baseColor, string leaves)
            {
                dc.PushTransform(new TranslateTransform(x,y)); dc.PushTransform(new ScaleTransform(scale,scale));
                Path(dc,"M-2,1 L0,-41 2,-40 2,1Z",baseColor,null,0);
                Path(dc,"M-2,-21 C-21,-21 -20,-36 -14,-45 C-18,-55 -13,-66 -6,-67 C-4,-80 6,-80 10,-70 C19,-68 23,-59 20,-49 C30,-34 19,-21 4,-22Z",leaves,null,0);
                Path(dc,"M0,-19 C1,-32 3,-44 4,-58 M2,-39 L-6,-48 M3,-47 L11,-55",null,baseColor,1.3);
                dc.Pop(); dc.Pop();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PawTrail
{
    // All layers are drawn in the coordinates of the supplied portrait. Resources are frozen and
    // reused by the live pet, the large preview and the skin thumbnails.
    public partial class PetArt : FrameworkElement
    {
        static readonly Dictionary<string, Geometry> paths = new Dictionary<string, Geometry>();
        static readonly Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();
        static readonly Dictionary<string, Pen> pens = new Dictionary<string, Pen>();
        int skin;
        double phase, lookX, lookY, mouseX, mouseY, click, scroll;
        int mouseButton;
        double petting, doze, reach;
        public double Petting { get { return petting; } set { Set(ref petting, Clamp(value, 0, 1)); } }
        public double Doze { get { return doze; } set { Set(ref doze, Clamp(value, 0, 1)); } }
        public double Reach { get { return reach; } set { Set(ref reach, Clamp(value, 0, 1)); } }
        bool mouseOnLeft;
        public bool MouseOnLeft { get { return mouseOnLeft; } set { if (mouseOnLeft != value) { mouseOnLeft = value; InvalidateVisual(); } } }
        int leftPawKey = -1, rightPawKey = -1;
        double leftPawPress, rightPawPress;
        ulong lowKeys, highKeys;

        public void SetKeyboardPose(int leftKey, double leftPress, int rightKey, double rightPress, ulong lowKeys, ulong highKeys)
        {
            if (leftPawKey == leftKey && rightPawKey == rightKey && leftPawPress == leftPress &&
                rightPawPress == rightPress && this.lowKeys == lowKeys && this.highKeys == highKeys) return;
            leftPawKey = leftKey;
            rightPawKey = rightKey;
            leftPawPress = Clamp(leftPress, 0, 1);
            rightPawPress = Clamp(rightPress, 0, 1);
            this.lowKeys = lowKeys;
            this.highKeys = highKeys;
            InvalidateVisual();
        }

        public int Skin { get { return skin; } set { int s = Math.Max(0, Math.Min(2, value)); if (skin != s) { skin = s; InvalidateVisual(); } } }
        public double Phase { get { return phase; } set { Set(ref phase, value); } }
        public double LookX { get { return lookX; } set { Set(ref lookX, Clamp(value, -1, 1)); } }
        public double LookY { get { return lookY; } set { Set(ref lookY, Clamp(value, -1, 1)); } }

        public double MouseX { get { return mouseX; } set { Set(ref mouseX, Clamp(value, -1, 1)); } }
        public double MouseY { get { return mouseY; } set { Set(ref mouseY, Clamp(value, -1, 1)); } }
        public double Click { get { return click; } set { Set(ref click, Clamp(value, 0, 1)); } }
        public double Scroll { get { return scroll; } set { Set(ref scroll, Clamp(value, -1, 1)); } }

        public int MouseButton { get { return mouseButton; } set { int b = Math.Max(0, Math.Min(2, value)); if (b != mouseButton) { mouseButton = b; InvalidateVisual(); } } }

        void Set(ref double field, double value)
        {
            if (field == value) return;
            field = value;
            InvalidateVisual();
        }

        static double Clamp(double v, double min, double max) { return Math.Max(min, Math.Min(max, v)); }

        protected override Size MeasureOverride(Size available)
        {
            double ratio = Math.Min(1, Math.Min(available.Width / 280, available.Height / 210));
            return new Size(280 * ratio, 210 * ratio);
        }

        static Geometry Shape(string data)
        {
            Geometry geometry;
            if (!paths.TryGetValue(data, out geometry))
            {
                geometry = Geometry.Parse(data);
                geometry.Freeze();
                paths.Add(data, geometry);
            }
            return geometry;
        }

        static Brush Paint(string color)
        {
            if (color == null) return null;
            Brush brush;
            if (!brushes.TryGetValue(color, out brush))
            {
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                brush.Freeze();
                brushes.Add(color, brush);
            }
            return brush;
        }

        static Pen Outline(string color, double width)
        {
            if (color == null) return null;
            string key = color + "/" + width;
            Pen pen;
            if (!pens.TryGetValue(key, out pen))
            {
                pen = new Pen(Paint(color), width);
                pen.LineJoin = PenLineJoin.Round;
                pen.StartLineCap = pen.EndLineCap = PenLineCap.Round;
                pen.Freeze();
                pens.Add(key, pen);
            }
            return pen;
        }

        static void P(DrawingContext dc, string data, string fill, string stroke = null, double width = 2.8)
        {
            dc.DrawGeometry(Paint(fill), Outline(stroke, width), Shape(data));
        }

        static void E(DrawingContext dc, double x, double y, double rx, double ry, string fill, string edge = null, double width = 2.8)
        {
            dc.DrawEllipse(Paint(fill), Outline(edge, width), new Point(x, y), rx, ry);
        }

        // Portrait coordinates, before the optional left-handed mouse layout.
        const double SceneLeft = 0, SceneTop = 0, SceneWidth = 1696, SceneHeight = 1504;
        const double MirrorWidth = 1696;
        const double RestMouseX = 1480, RestMouseY = 1145;
        static readonly Point LeftRest = new Point(533, 1055);
        static readonly Point RightRest = new Point(1040, 1086);
        static readonly DrawingGroup[] coats = new DrawingGroup[3], bibs = new DrawingGroup[3];
        DrawingGroup forelegs;
        Point lastLeftPaw, lastRightPaw;
        DrawingGroup mouseTail;
        Geometry tailTipClip;
        Point lastTailTip;
        double lastTailDoze;
        static readonly Brush furGrain = CreateFurGrain();

        // A fine, translucent grain softens the large areas of colour in the
        // portrait. The tile is created once, independent of the animation.
        static Brush CreateFurGrain()
        {
            const int size = 144;
            var random = new Random(1741);
            var pixels = new byte[size * size * 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte shade = random.Next(2) == 0 ? (byte)32 : (byte)255;
                pixels[i] = pixels[i+1] = pixels[i+2] = shade;
                pixels[i+3] = (byte)random.Next(0, 12);
            }
            var tile = BitmapSource.Create(size,size,96,96,PixelFormats.Bgra32,null,pixels,size*4);
            tile.Freeze();
            var brush = new ImageBrush(tile) { TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0,0,size,size), Stretch = Stretch.Fill };
            brush.Freeze();
            return brush;
        }

        static void Round(DrawingContext dc, double x, double y, double w, double h, double r,
                          string fill, string stroke = null, double line = 1.2)
        {
            dc.DrawRoundedRectangle(Paint(fill), Outline(stroke, line), new Rect(x, y, w, h), r, r);
        }

        static DrawingGroup Record(Action<DrawingContext> draw)
        {
            var result = new DrawingGroup();
            using (var context = result.Open()) draw(context);
            result.Freeze();
            return result;
        }

        static double Mix(double a, double b, double t) { return a + (b - a) * t; }
        static Point Mix(Point a, Point b, double t) { return new Point(Mix(a.X,b.X,t), Mix(a.Y,b.Y,t)); }
        static Point Center(Rect rect) { return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2); }
        double KeyboardOffset { get { return mouseOnLeft ? MirrorWidth - 1344 - 66 : 0; } }

        Point ContactPoint(KeyCap key)
        {
            var p = Center(CapRect(key));
            if (mouseOnLeft) p.X = MirrorWidth - (KeyboardOffset + p.X);
            return p;
        }

        double SceneScale { get { return Math.Min(ActualWidth / SceneWidth, ActualHeight / SceneHeight); } }

        Point StagePoint(Point local)
        {
            double s = SceneScale;
            if (s <= 0) return new Point(-100000, -100000);
            return new Point(SceneLeft + (local.X - (ActualWidth - SceneWidth * s) / 2) / s,
                             SceneTop + (local.Y - (ActualHeight - SceneHeight * s) / 2) / s);
        }

        struct PoseData
        {
            public Matrix Body, Head;
            public Point LeftPaw, RightPaw, TailTip, Device;
        }

        Point TypingPaw(Point rest, Point hovering, int keyId, double press)
        {
            Point ready = Mix(rest, hovering, reach);
            KeyCap key = KeyboardLayout.Get(keyId);
            return key == null ? ready : Mix(ready, ContactPoint(key), press);
        }

        PoseData GetPose()
        {
            double mouseWorldX = (mouseOnLeft ? MirrorWidth - RestMouseX : RestMouseX) + mouseX * 45;
            double mouseWorldY = RestMouseY + mouseY * 28;
            // Button coordinates follow the physical mouse even when its position is mirrored.
            double buttonX = mouseButton == 0 ? -55 : mouseButton == 1 ? 55 : 0;
            Point tailTip = new Point(mouseWorldX + (mouseOnLeft ? -31 : 31), mouseWorldY - 50);
            Point button = new Point(mouseWorldX + buttonX, mouseWorldY - (mouseButton == 2 ? 116 : 90));
            tailTip = Mix(tailTip, button, click);
            tailTip = Mix(tailTip, new Point(mouseWorldX, mouseWorldY - 119 + scroll*9), Math.Min(1,Math.Abs(scroll)*3));
            if (mouseOnLeft) tailTip.X = MirrorWidth - tailTip.X;

            // Paw assignments are in screen order. Mirroring the portrait swaps which arm carries each assignment.
            Point left = TypingPaw(LeftRest, new Point(462,1150), mouseOnLeft ? rightPawKey : leftPawKey,
                mouseOnLeft ? rightPawPress : leftPawPress);
            Point right = TypingPaw(RightRest, new Point(1001,1174), mouseOnLeft ? leftPawKey : rightPawKey,
                mouseOnLeft ? leftPawPress : rightPawPress);
            double lean = Clamp((left.X - LeftRest.X + right.X - RightRest.X) / 9, -78, 78) * (1 - doze);
            double drop = (reach * 16 + Math.Max(leftPawPress,rightPawPress) * 9) * (1 - doze);
            var body = Matrix.Identity;
            body.ScaleAt(1 + .035 * doze, 1 - .56 * doze, 890,1023);
            body.Translate(lean * .65, drop * .36);
            // The coat follows the orientation of the sitting cat. Keep the face markings
            // on their original sides when the mouse and the active forelegs are swapped.
            var head = Matrix.Identity;
            head.ScaleAt((mouseOnLeft ? -1 : 1) * (1 - doze * .18), 1 - doze * .22, 850,427);
            head.RotateAt(-17 * doze + (mouseOnLeft ? -1 : 1) *
                (-2.1 * reach - petting * 2.6 + .53 * lookX + .20 * Math.Sin(phase)), 850,569);
            head.Translate(lean - 18 * doze, drop + petting * 10 + doze * 354);
            return new PoseData {
                Body = body, Head = head,
                LeftPaw = Mix(left, new Point(628,1065), doze),
                RightPaw = Mix(right, new Point(1017,1074), doze),
                TailTip = tailTip,
                Device = new Point(mouseWorldX,mouseWorldY)
            };
        }

        public bool HitHead(Point localPos)
        {
            Point p = StagePoint(localPos);
            if (mouseOnLeft) p.X = MirrorWidth - p.X;
            var inverse = GetPose().Head;
            inverse.Invert();
            p = inverse.Transform(p);
            Matrix face = FacePlacement;
            face.Invert();
            p = face.Transform(p);
            return Shape(PortraitOutline).FillContains(p) ||
                   (skin == 1 && new Rect(733,54,211,117).Contains(p));
        }

        public bool NearKeyboard(Point localPos)
        {
            var p = StagePoint(localPos);
            return new Rect(43 + KeyboardOffset,993,1324,403).Contains(p);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double s = SceneScale;
            if (s <= 0) return;
            dc.PushTransform(new MatrixTransform(s,0,0,s,
                (ActualWidth - SceneWidth*s)/2 - SceneLeft*s,
                (ActualHeight - SceneHeight*s)/2 - SceneTop*s));
            PoseData pose = GetPose();
            Matrix inverse = pose.Body;
            inverse.Invert();
            Point left = inverse.Transform(pose.LeftPaw), right = inverse.Transform(pose.RightPaw);
            if (forelegs == null || left != lastLeftPaw || right != lastRightPaw)
            {
                forelegs = Record(delegate(DrawingContext pen) { DrawForelegs(pen,left,right); });
                lastLeftPaw = left;
                lastRightPaw = right;
            }
            var mirror = new ScaleTransform(-1,1,MirrorWidth/2,0);
            if (mouseOnLeft) dc.PushTransform(mirror);
            DrawCatShadow(dc);
            UpdateMouseTail(pose.TailTip);
            dc.DrawDrawing(mouseTail);
            dc.PushTransform(new MatrixTransform(pose.Body));
            if (coats[skin] == null) coats[skin] = Record(DrawTorso);
            dc.DrawDrawing(coats[skin]);
            dc.DrawDrawing(forelegs);
            if (bibs[skin] == null) bibs[skin] = Record(DrawRuff);
            // As the shoulders come forward, the hanging chest fur gathers above
            // the elbows. Its irregular hem remains the visible edge of the joint.
            double lift = Clamp(Math.Max((pose.LeftPaw-LeftRest).Length,(pose.RightPaw-RightRest).Length)/250,0,1) * (1-doze);
            dc.PushTransform(new ScaleTransform(1,1-.20*lift,855,621));
            dc.DrawDrawing(bibs[skin]);
            dc.Pop();
            dc.DrawGeometry(furGrain, null, Shape(SittingCoat));
            dc.Pop();
            dc.PushTransform(new MatrixTransform(pose.Head));
            DrawPortrait(dc);
            dc.Pop();
            if (mouseOnLeft) dc.Pop();

            dc.PushTransform(new TranslateTransform(KeyboardOffset,0));
            DrawKeyboard(dc);
            dc.Pop();
            DrawMouse(dc,pose.Device.X,pose.Device.Y);

            if (mouseOnLeft) dc.PushTransform(mirror);
            dc.PushClip(tailTipClip);
            dc.DrawDrawing(mouseTail);
            dc.Pop();
            if (mouseOnLeft) dc.Pop();
            // The keyboard occludes the lap. Only the part of the forelegs over its
            // footprint comes back to the foreground; the shoulders stay behind the bib.
            dc.PushTransform(new TranslateTransform(KeyboardOffset,0));
            dc.PushClip(KeyboardFootprint);
            dc.PushTransform(new TranslateTransform(-KeyboardOffset,0));
            if (mouseOnLeft) dc.PushTransform(mirror);
            dc.PushTransform(new MatrixTransform(pose.Body));
            dc.DrawDrawing(forelegs);
            dc.Pop();
            if (mouseOnLeft) dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
            if (doze > .62) DrawSleepMarks(dc);
            dc.Pop();
            DrawKeyFeedback(dc);
        }

        static void DrawCatShadow(DrawingContext dc)
        {
            E(dc,938,1033,410,27,"#194C3A37");
        }

        // Draw the whole tail behind the body, then repeat just its descending
        // end over the mouse. The two passes share a single frozen drawing.
        void UpdateMouseTail(Point tip)
        {
            if (mouseTail != null && (tip-lastTailTip).LengthSquared < .0025 &&
                Math.Abs(lastTailDoze-doze) < .0005) return;

            const int crestIndex = 40, segments = 60;
            const double rootRadius = 53, tipRadius = 34;
            var upper = new Point[segments+1];
            var lower = new Point[segments+1];
            var outerClip = new Point[segments+1];
            var innerClip = new Point[segments+1];

            // Place the cap one radius back so its rounded end touches the button.
            Vector tipDirection = new Vector(-.18,1);
            tipDirection.Normalize();
            Point capCenter = tip - tipDirection*tipRadius;
            Point crest = new Point(capCenter.X-99, 899 + .55*(tip.Y-1095) + doze*26);
            Point root = new Point(Math.Min(1215,crest.X-193),995);
            double riseRun = crest.X-root.X;
            Point[] curve = {
                root, new Point(root.X+.38*riseRun,root.Y), new Point(crest.X-.36*riseRun,crest.Y),
                crest, new Point(crest.X+.64*(capCenter.X-crest.X),crest.Y),
                capCenter-tipDirection*(.55*(capCenter.Y-crest.Y)), capCenter
            };
            for (int i=0; i<=segments; ++i)
            {
                int j = i < crestIndex ? 0 : 3;
                double t = i < crestIndex ? (double)i/crestIndex : (double)(i-crestIndex)/(segments-crestIndex);
                Point a = Mix(curve[j],curve[j+1],t);
                Point b = Mix(curve[j+1],curve[j+2],t);
                Point c = Mix(curve[j+2],curve[j+3],t);
                Point p = Mix(Mix(a,b,t),Mix(b,c,t),t);
                Vector tangent = Mix(b,c,t) - Mix(a,b,t);
                tangent.Normalize();
                Vector normal = new Vector(-tangent.Y,tangent.X);
                double along = (double)i/segments;
                double taper = along*along*(3-2*along);
                double width = rootRadius + (tipRadius-rootRadius)*taper;
                upper[i] = p + normal*width;
                lower[i] = p - normal*width;
                outerClip[i] = p + normal*(width+6);
                innerClip[i] = p - normal*(width+6);
            }
            tailTipClip = TailRibbon(outerClip,innerClip,crestIndex,segments,false);
            tailTipClip.Freeze();
            Geometry outline = TailRibbon(upper,lower,0,segments,false);
            mouseTail = Record(delegate(DrawingContext pen) {
                pen.DrawGeometry(Paint("#383132"),null,outline);
                pen.PushClip(outline);
                pen.DrawGeometry(Paint("#EDA453"),null,TailRibbon(upper,lower,9,22,true));
                pen.DrawGeometry(Paint("#EAA356"),null,TailRibbon(upper,lower,37,49,true));
                TailLock(pen,upper,lower,11,15,20,.18,"#A7F4BD7E");
                TailLock(pen,upper,lower,37,40,45,.12,"#9AE8AE77");
                TailLock(pen,upper,lower,49,54,57,.84,"#80544943");
                pen.DrawGeometry(furGrain,null,outline);
                pen.Pop();
                pen.DrawGeometry(null,Outline("#2B2525",4.3),outline);
            });
            lastTailTip = tip;
            lastTailDoze = doze;
        }

        static Geometry TailRibbon(Point[] upper, Point[] lower, int first, int last, bool patterned)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext g = geometry.Open())
            {
                g.BeginFigure(upper[first],true,true);
                bool roundedTip = !patterned && last == upper.Length-1;
                for (int i=first+1;i<=last;i++) g.LineTo(upper[i],true,false);
                if (roundedTip)
                {
                    double radius = (upper[last]-lower[last]).Length/2;
                    g.ArcTo(lower[last],new Size(radius,radius),0,false,SweepDirection.Counterclockwise,true,true);
                }
                else if (patterned) DrawFurBoundary(g,upper,lower,last);
                for (int i=last-1;i>=first;i--) g.LineTo(lower[i],true,false);
                if (patterned) DrawFurBoundary(g,lower,upper,first);
            }
            return geometry;
        }

        static Point AcrossTail(Point[] from, Point[] to, double row, double across)
        {
            int i = (int)row;
            return Mix(Mix(from[i],to[i],across),Mix(from[i+1],to[i+1],across),row-i);
        }

        static void DrawFurBoundary(StreamGeometryContext g, Point[] from, Point[] to, int row)
        {
            g.QuadraticBezierTo(AcrossTail(from,to,row+.65,.07),AcrossTail(from,to,row+.35,.23),true,false);
            g.LineTo(AcrossTail(from,to,row-.18,.18),true,false);
            g.QuadraticBezierTo(AcrossTail(from,to,row+.62,.35),AcrossTail(from,to,row+.54,.50),true,false);
            g.LineTo(AcrossTail(from,to,row+.03,.45),true,false);
            g.QuadraticBezierTo(AcrossTail(from,to,row+1.05,.64),AcrossTail(from,to,row+.86,.77),true,false);
            g.LineTo(AcrossTail(from,to,row+.31,.72),true,false);
            g.QuadraticBezierTo(AcrossTail(from,to,row+.57,.94),to[row],true,false);
        }

        static void TailLock(DrawingContext dc, Point[] a, Point[] b, int first, int middle,
            int last, double across, string color)
        {
            var lockShape = new StreamGeometry();
            using (StreamGeometryContext g = lockShape.Open())
            {
                Point start = Mix(a[first],b[first],across), end = Mix(a[last],b[last],across+.025);
                g.BeginFigure(start,true,true);
                g.QuadraticBezierTo(Mix(a[middle],b[middle],across-.045),end,true,true);
                g.QuadraticBezierTo(Mix(a[middle],b[middle],across+.025),start,true,true);
            }
            dc.DrawGeometry(Paint(color),null,lockShape);
        }
        void DrawSleepMarks(DrawingContext dc)
        {
            dc.PushOpacity(Math.Min(1,(doze-.62)/.38)*.85);
            dc.PushTransform(new TranslateTransform((mouseOnLeft ? 597 : 1060),-99-12*Math.Sin(phase)));
            P(dc,"M0,601 L30,601 L2,631 L32,631 M41,564 L61,564 L42,585 L63,585",null,"#746966",7);
            dc.Pop();
            dc.Pop();
        }
    }
}

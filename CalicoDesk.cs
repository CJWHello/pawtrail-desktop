using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PawTrail
{
    public partial class PetArt
    {
        // Reference illustration coordinates. The cap centres used for hit targets
        // and the cap faces use the same, slightly splayed, perspective grid.
        private const double BoardColumns = 22.5, BoardRows = 6.25;
        private const double RowPitch = 42.2;
        private static readonly Typeface DeskFont = new Typeface("Segoe UI");
        private static readonly Typeface BadgeFont = new Typeface(new FontFamily("Segoe UI"),
            FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Dictionary<string, FormattedText> DeskLegends =
            new Dictionary<string, FormattedText>();
        private static DrawingGroup deskKeyboard;
        private const string KeyboardCase =
            "M67,1319 L131,1049 Q137,1014 174,1014 L1268,1021 Q1299,1021 1306,1052 " +
            "L1341,1306 Q1345,1338 1341,1350 Q1336,1376 1308,1378 L105,1371 Q71,1371 68,1349 Q66,1344 67,1319Z";
        private static readonly Geometry KeyboardFootprint = MakeKeyboardFootprint();

        private static Geometry MakeKeyboardFootprint()
        {
            Geometry face = Geometry.Parse(KeyboardCase);
            Geometry footprint = Geometry.Combine(face,face.GetWidenedPathGeometry(new Pen(Brushes.Black,12)),
                GeometryCombineMode.Union,null);
            footprint.Freeze();
            return footprint;
        }

        private static Point KeyGridPoint(double column, double row)
        {
            double depth = row / BoardRows;
            return new Point(161.0 - 54.0 * depth + column * (49.65 + 3.10 * depth),
                1046.0 + RowPitch * row + 2.25 * column / BoardColumns);
        }

        // The cat faces the viewer. Turn the layout on the desk plane before
        // projecting it, so the case and the keycap bevels retain their perspective.
        private static double CapColumn(KeyCap k)
        {
            return BoardColumns - (k.X + .5 * k.W);
        }

        private static Rect CapRect(KeyCap k)
        {
            double row = BoardRows - (k.Y + .5 * k.H);
            Point center = KeyGridPoint(CapColumn(k), row);
            double width = k.W * (49.65 + 3.10 * row / BoardRows) - 3.85;
            double height = k.H * RowPitch - 3.65;
            return new Rect(center.X - width / 2, center.Y - height / 2, width, height);
        }

        private static Brush DeskGradient(string top, string bottom, double slope = 0)
        {
            var brush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(top), (Color)ColorConverter.ConvertFromString(bottom),
                new Point(0, 0), new Point(slope, 1));
            brush.Freeze();
            return brush;
        }

        private static readonly Brush CaseSide = DeskGradient("#B7C2B5", "#95A7A1");
        private static readonly Brush BoardSurface = DeskGradient("#F1F1E3", "#D4D6C5", .36);
        private static readonly Brush CreamCap = DeskGradient("#FFFDF5", "#ECEBDD", .17);
        private static readonly Brush GreenCap = DeskGradient("#B9CEC0", "#9BB8A8", .13);
        private static readonly Brush PinkCap = DeskGradient("#F7C2D3", "#E99ABC", .15);
        private static readonly Brush RoseCap = DeskGradient("#F7CADA", "#EEA0BE", .12);
        private static readonly Brush HoneyCap = DeskGradient("#FFD596", "#F1AC5F", .15);
        private static readonly Brush SocketSide = DeskGradient("#D1D3BE", "#A5AF9D");
        private static readonly Brush MouseSole = DeskGradient("#E6E4DA", "#A3A8A3", .18);
        private static readonly Brush MousePalm = DeskGradient("#F4F1EB", "#E6E3D9", .31);
        private static readonly Brush WheelSurface = DeskGradient("#CAD5C7", "#7F948A");

        private static void DeskPath(DrawingContext dc, string data, Brush fill,
            string stroke = null, double width = 1.5)
        {
            dc.DrawGeometry(fill, Outline(stroke, width), Shape(data));
        }

        private static FormattedText DeskLegend(string value, bool badge)
        {
            string key = (badge ? "B:" : "K:") + value;
            FormattedText label;
            if (!DeskLegends.TryGetValue(key, out label))
            {
                double size = badge ? 20.0 : (value.Length <= 2 ? 15.0 : 12.5);
                label = new FormattedText(value, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, badge ? BadgeFont : DeskFont, size,
                    Paint(badge ? "#2B211F" : "#696F5F"), 1.0);
                DeskLegends.Add(key, label);
            }
            return label;
        }

        private static Brush CapColour(KeyCap k)
        {
            if (k.Label == "Esc") return RoseCap;
            if (k.X == 0 && k.Label == "Shift") return PinkCap;
            if (k.X == 1.25 && k.Label == "Win") return HoneyCap;
            if (k.Label == "Enter" || (k.X >= 21 && k.Label == "+")) return GreenCap;
            return CreamCap;
        }

        private static void DrawKeycap(DrawingContext dc, KeyCap k, bool pressed, bool rightPaw)
        {
            Rect r = CapRect(k);
            double topShear = (-54.0 + 3.10 * CapColumn(k)) / (BoardRows * RowPitch);
            dc.PushTransform(new MatrixTransform(1, 0, topShear, 1,
                r.X - r.Height * topShear / 2, r.Y));
            string edge = pressed ? (rightPaw ? "#9F5670" : "#A46533") : "#777E6C";
            dc.DrawRoundedRectangle(SocketSide, Outline(edge, pressed ? 2.8 : 1.75),
                new Rect(0, 0, r.Width, r.Height), 5.8, 5.8);
            double insetY = pressed ? 4.0 : .7;
            double height = r.Height - (pressed ? 5.4 : 8.5);
            Brush face = pressed ? Paint(rightPaw ? "#F5B1C4" : "#FFD290") : CapColour(k);
            dc.DrawRoundedRectangle(face, Outline(pressed ? edge : "#9AA08D", .85),
                new Rect(1.7, insetY, r.Width - 3.4, height), 5.2, 5.2);
            dc.DrawLine(Outline("#A4FFFDEF", 1.15),
                new Point(6, insetY + 1.8), new Point(r.Width - 5.4, insetY + 1.8));
            FormattedText label = DeskLegend(k.Label, false);
            dc.PushTransform(new RotateTransform(180, r.Width / 2, insetY + height / 2));
            dc.DrawText(label, new Point((r.Width - label.Width) / 2,
                insetY + (height - label.Height) / 2 - .25));
            dc.Pop();
            dc.Pop();
        }

        private static DrawingGroup CreateKeyboard()
        {
            var board = new DrawingGroup();
            using (DrawingContext dc = board.Open())
            {
                // The front edge of the keyboard reveals a deep sage bevel below
                // the dark outer rim. The rounded back corners sit underneath the paw.
                P(dc, "M65,1327 L135,1050 Q141,1022 169,1022 L1274,1028 Q1305,1029 1308,1057 L1362,1348 Q1367,1389 1329,1390 L94,1382 Q54,1381 65,1327Z", "#22584442");
                DeskPath(dc, KeyboardCase,
                    CaseSide, "#221C1D", 6);
                P(dc, "M71,1337 Q76,1363 111,1364 L1304,1371 Q1333,1372 1340,1347 Q1337,1375 1307,1374 L105,1368 Q76,1367 71,1348Z", "#86968E");
                P(dc, "M69,1309 L133,1048 Q139,1016 175,1016 L1267,1022 Q1296,1022 1304,1052 L1338,1303 Q1342,1337 1311,1341 L107,1337 Q72,1336 69,1317Z",
                    "#383131", "#231D1D", 3.6);
                DeskPath(dc, "M86,1300 L146,1050 Q150,1030 173,1030 L1264,1035 Q1285,1036 1289,1055 L1322,1300 Q1325,1325 1304,1327 L111,1323 Q83,1322 86,1300Z",
                    BoardSurface, "#74766B", 2.15);
                P(dc, "M149,1047 Q154,1033 174,1033 L1259,1038", null, "#FBF8E9", 2.6);
                foreach (KeyCap k in KeyboardLayout.Keys) DrawKeycap(dc, k, false, false);
            }
            board.Freeze();
            return board;
        }

        private void DrawKeyboard(DrawingContext dc)
        {
            if (deskKeyboard == null) deskKeyboard = CreateKeyboard();
            dc.DrawDrawing(deskKeyboard);
            foreach (KeyCap k in KeyboardLayout.Keys)
            {
                ulong bits = k.Id < 64 ? lowKeys : highKeys;
                bool pressed = (bits & (1UL << (k.Id & 63))) != 0;
                if (pressed || (k.Id == screenLeftKey && screenLeftPress > .08) ||
                    (k.Id == screenRightKey && screenRightPress > .08))
                    DrawKeycap(dc, k, true, k.Id == screenRightKey);
            }
        }

        private void DrawKeyFeedback(DrawingContext dc)
        {
            KeyCap left = screenLeftPress > .13 ? KeyboardLayout.Get(screenLeftKey) : null;
            KeyCap right = screenRightPress > .13 ? KeyboardLayout.Get(screenRightKey) : null;
            if (left == null && right == null) return;

            // Use window coordinates so the illustration's scale cannot shrink
            // the labels to the size of the tiny legends printed on the keycaps.
            double scale = Math.Min(1.05, Math.Min(ActualWidth / 280, ActualHeight / 172));
            if (scale <= 0) return;
            dc.PushTransform(new ScaleTransform(scale, scale));
            double availableWidth = ActualWidth / scale;
            if (left != null) DrawDeskBadge(dc, left, availableWidth, false);
            if (right != null) DrawDeskBadge(dc, right, availableWidth, true);
            dc.Pop();
        }

        private static string BadgeText(KeyCap key)
        {
            if (key.X >= 18.5 && key.Label != "Num") return "Num " + key.Label;
            switch (key.Label)
            {
                case "Alt":
                case "Ctrl":
                case "Shift":
                case "Win":
                    return (key.X < 5 ? "L " : "R ") + key.Label;
                default:
                    return key.Label;
            }
        }

        private static void DrawDeskBadge(DrawingContext dc, KeyCap key, double availableWidth, bool rightPaw)
        {
            const double top = 4, margin = 5, height = 31;
            var label = DeskLegend(BadgeText(key), true);
            // Keep long key names away from the ears on small pets. Both of
            // these alternatives are common legends on physical keyboards.
            if (label.Width + 16 > availableWidth * .30)
            {
                if (key.Label == "Backspace") label = DeskLegend("Bksp", true);
                else if (key.Label == "Enter" && key.X >= 18.5) label = DeskLegend("Num ↵", true);
            }
            double width = Math.Max(35, Math.Ceiling(label.Width) + 16);
            double x = rightPaw ? availableWidth - margin - width : margin;
            Round(dc, x, top + 2, width, height, 7, "#63403229");
            Round(dc, x, top, width, height, 7, rightPaw ? "#FAD2DF" : "#FFDFAB", "#4A3831", 1.35);
            dc.DrawText(label, new Point(x + (width - label.Width) / 2, top + (height - label.Height) / 2));
        }

        private void DrawMouse(DrawingContext dc, double x, double y)
        {
            dc.PushTransform(new TranslateTransform(x - 1480, y - 1145));
            P(dc, "M1347,1215 C1350,1130 1361,1044 1404,994 C1444,953 1519,953 1564,983 C1606,1013 1615,1095 1623,1205 C1627,1292 1597,1346 1489,1351 C1395,1353 1347,1311 1347,1245Z", "#20564543");
            DeskPath(dc, "M1350,1208 C1352,1153 1355,1076 1368,1025 C1379,980 1411,955 1464,952 C1522,949 1560,967 1581,999 C1604,1035 1607,1102 1613,1188 C1618,1267 1607,1317 1525,1334 C1487,1342 1436,1339 1406,1326 C1364,1308 1347,1272 1350,1208Z",
                MouseSole, "#292423", 5.3);
            P(dc, "M1354,1234 C1367,1290 1414,1313 1476,1318 C1535,1321 1591,1302 1608,1252 Q1596,1327 1496,1333 Q1390,1336 1360,1283Z", "#B4B9B3");
            DeskPath(dc, "M1354,1187 C1356,1138 1359,1069 1373,1021 C1387,978 1418,959 1466,955 C1522,952 1559,971 1579,1002 C1600,1037 1603,1102 1608,1187 C1612,1258 1583,1308 1493,1312 C1404,1317 1351,1273 1354,1187Z",
                MousePalm, "#55554E", 2.4);
            P(dc,"M1357,1184 Q1353,1256 1412,1281 C1462,1302 1535,1295 1569,1269 Q1597,1246 1604,1205 C1600,1281 1562,1308 1494,1308 C1401,1314 1351,1266 1357,1184Z", "#D1D2C9");
            P(dc,"M1374,1025 C1390,976 1419,959 1468,957 C1518,955 1556,968 1576,1005 Q1594,1035 1601,1113 Q1564,1089 1480,1087 Q1403,1088 1360,1111 Q1363,1055 1374,1025Z", "#F2F0E9");
            if (click > .06 && mouseButton < 2)
            {
                dc.PushOpacity(.63 * click);
                string button = mouseButton == 0
                    ? "M1362,1108 Q1366,1051 1376,1021 Q1397,958 1476,957 L1477,1086 Q1404,1085 1362,1108Z"
                    : "M1482,956 Q1548,955 1576,1005 Q1594,1038 1600,1110 Q1565,1088 1482,1087Z";
                P(dc, button, "#EAC496");
                dc.Pop();
            }
            P(dc, "M1478,955 L1478,997 M1479,1059 L1479,1087 M1360,1111 Q1404,1087 1479,1087 Q1559,1085 1603,1114",
                null, "#494B46", 2.7);
            Round(dc, 1458, 995, 40, 65, 16, "#58645D", "#282A27", 3.45);
            dc.DrawRoundedRectangle(mouseButton == 2 && click > .08 ? Paint("#DCBB8F") : WheelSurface,
                Outline("#78877A", 1.4), new Rect(1462, 1000, 32, 54), 11, 11);
            P(dc, "M1464,1013 Q1464,1002 1475,1001 L1481,1001 Q1490,1001 1492,1011",
                null, "#DCE0D4", 1.9);
            if (Math.Abs(scroll) > .06)
            {
                dc.PushOpacity(Math.Abs(scroll));
                dc.PushTransform(new TranslateTransform(0, scroll * 6));
                P(dc, "M1466,1020 L1489,1020 M1466,1029 L1489,1029 M1466,1038 L1489,1038",
                    null, "#E1E2D4", 2.4);
                dc.Pop();
                dc.Pop();
            }
            dc.Pop();
        }
    }
}

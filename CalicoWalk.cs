using System;
using System.Windows;
using System.Windows.Media;

namespace PawTrail
{
    public partial class PetArt
    {
        double march;
        public double March { get { return march; } set { Set(ref march, Clamp(value, 0, 1)); } }

        // A full gait cycle has two footfalls. Store its inked body frames once;
        // an idle pet can march for hours without recomputing the fur silhouettes.
        const int WalkFrameCount = 36;
        static readonly DrawingGroup[] walkFrames = new DrawingGroup[WalkFrameCount];
        const string WalkTorso =
            "M462,811 C481,749 568,719 663,719 Q769,713 858,734 " +
            "C998,755 1121,699 1223,735 C1324,768 1371,861 1350,951 " +
            "Q1331,1034 1266,1065 C1193,1122 1086,1096 1011,1090 " +
            "Q873,1073 802,1099 L807,1089 L786,1103 L790,1092 " +
            "C697,1109 596,1105 523,1067 C461,1034 430,972 431,907 Q431,849 462,811 Z";
        const string WalkForeleg =
            "M-63,-12 C-53,-48 47,-42 68,-6 Q91,62 60,151 " +
            "C47,221 13,279 10,327 Q10,339 18,355 Q22,381 0,396 " +
            "Q-18,411 -49,405 Q-77,412 -98,393 Q-113,383 -107,361 " +
            "Q-102,343 -74,333 C-62,291 -52,221 -48,159 Q-61,68 -63,-12 Z";
        const string WalkHindleg =
            "M-67,0 C-69,-50 39,-63 84,-7 Q111,43 80,124 " +
            "Q58,175 24,213 Q39,239 65,271 Q81,293 66,318 " +
            "Q46,348 23,372 Q15,401 -16,404 Q-41,413 -57,404 " +
            "Q-83,405 -88,388 Q-100,365 -76,347 Q-63,336 -43,335 " +
            "Q-14,302 -11,291 Q-64,245 -83,212 Q-98,183 -78,150 Q-42,85 -67,0 Z";
        const string WalkTail =
            "M1256,841 C1402,836 1482,766 1468,665 C1460,609 1428,574 1409,534 " +
            "C1388,487 1414,450 1452,458 C1516,473 1581,580 1583,675 " +
            "C1588,807 1513,917 1384,964 Q1319,985 1257,971 Z";

        int StepFrame()
        {
            double cycles = phase * .43;
            return (int)Math.Floor((cycles - Math.Floor(cycles)) * WalkFrameCount);
        }

        double StepAngle() { return StepFrame() * (2 * Math.PI / WalkFrameCount); }

        // The head is drawn with the seated portrait's original orientation,
        // turned towards the viewer while the body walks to the left.
        static Matrix WalkHeadMatrix(double step)
        {
            Matrix m = Matrix.Identity;
            m.Scale(.83, .83);
            m.Translate(-210, 239);
            m.RotateAt(-5.5 + 2.1 * Math.Cos(step), 503, 776);
            m.Translate(6 * Math.Sin(step), -12 * Math.Cos(2 * step));
            return m;
        }

        void DrawMarch(DrawingContext dc)
        {
            int frame = StepFrame();
            double step = StepAngle();
            if (walkFrames[frame] == null)
                walkFrames[frame] = Record(delegate(DrawingContext pen) { DrawWalkingBody(pen, step); });
            dc.DrawDrawing(walkFrames[frame]);
            if (bibs[skin] == null) bibs[skin] = Record(DrawRuff);
            dc.PushTransform(new MatrixTransform(.63, -.025, .006, .60, -14,
                394 - 12 * Math.Cos(2 * step)));
            dc.DrawDrawing(bibs[skin]);
            dc.Pop();
            dc.PushTransform(new MatrixTransform(WalkHeadMatrix(step)));
            DrawPortrait(dc, new Point(-8, 3));
            dc.Pop();
        }

        static Matrix StridingLeg(double x, double y, double step)
        {
            Matrix m = Matrix.Identity;
            double lift = Math.Max(0, Math.Cos(step));
            m.Scale(1, 1 - .19 * lift);
            m.Rotate(15 * Math.Sin(step));
            m.Translate(x, y);
            return m;
        }

        static Geometry PositionedLeg(string outline, Matrix m)
        {
            var leg = new GeometryGroup { Transform = new MatrixTransform(m) };
            leg.Children.Add(Shape(outline));
            return leg;
        }

        static void DrawWalkingBody(DrawingContext dc, double step)
        {
            double bob = -10 * Math.Cos(2 * step);
            E(dc,883,1315,473,20,"#21483B34");
            var fore = StridingLeg(604,906 + bob,step);
            var hind = StridingLeg(1183,904 + bob,step + Math.PI);
            DrawWalkingLeg(dc,false,true,StridingLeg(702,885 + bob,step + Math.PI));
            DrawWalkingLeg(dc,true,true,StridingLeg(1244,882 + bob,step));

            dc.PushTransform(new RotateTransform(5 * Math.Sin(step),1290,910));
            dc.PushTransform(new TranslateTransform(0,bob));
            P(dc,WalkTail,"#353032",CoatEdge,5);
            dc.PushClip(Shape(WalkTail));
            P(dc,"M1326,798 Q1404,824 1447,898 L1406,974 L1370,971 Q1369,894 1294,860 Z " +
                "M1448,626 Q1512,679 1588,653 L1596,740 Q1530,756 1468,711 Z " +
                "M1397,496 Q1451,537 1518,518 L1558,570 Q1493,585 1434,558 Z","#EBA45F");
            dc.Pop();
            dc.Pop();
            dc.Pop();

            // Join the two near legs to the flank before inking it. The shoulders
            // and hips can articulate without leaving a joint line across the fur.
            var torso = new GeometryGroup { Transform = new TranslateTransform(0,bob) };
            torso.Children.Add(Shape(WalkTorso));
            Geometry silhouette = Geometry.Combine(torso,PositionedLeg(WalkForeleg,fore),
                GeometryCombineMode.Union,null,1,ToleranceType.Absolute);
            silhouette = Geometry.Combine(silhouette,PositionedLeg(WalkHindleg,hind),
                GeometryCombineMode.Union,null,1,ToleranceType.Absolute);
            dc.DrawGeometry(CreamCoat,null,silhouette);
            DrawWalkingLeg(dc,false,false,fore);
            DrawWalkingLeg(dc,true,false,hind);
            dc.PushTransform(new TranslateTransform(0,bob));
            dc.DrawGeometry(CreamCoat,null,Shape(WalkTorso));
            dc.PushClip(Shape(WalkTorso));
            P(dc,"M587,695 L1394,678 L1430,981 Q1343,1022 1250,1012 " +
                "C1183,993 1231,922 1163,924 C1108,927 1106,1049 1019,1053 " +
                "C923,1049 971,954 886,964 Q820,971 772,934 L783,936 L766,926 " +
                "C707,896 711,818 622,806 Z","#333032");
            P(dc,"M775,708 C853,754 889,779 938,846 C974,897 988,979 1060,989 " +
                "L1046,979 L1065,981 C1026,946 1067,922 1038,862 " +
                "C1010,795 1014,755 1002,712 Z M1198,711 Q1337,715 1388,805 " +
                "L1392,973 Q1340,1058 1258,1054 C1243,1004 1223,961 1172,952 " +
                "C1130,941 1100,905 1120,866 Q1159,792 1097,731 Z","#EDAA68");
            P(dc,"M470,965 Q573,1082 772,1074 L766,1066 Q859,1087 931,1068 " +
                "Q860,1108 815,1124 L547,1124 Z","#E9DDD2");
            dc.DrawGeometry(furGrain,null,Shape(WalkTorso));
            dc.Pop();
            dc.Pop();
            dc.DrawGeometry(null,Outline(CoatEdge,4.3),silhouette);

            dc.PushTransform(new TranslateTransform(0,bob));
            // A harness wraps the ribcage and cinches the keyboard to a leather pad.
            P(dc,"M769,765 Q816,935 903,1080 Q916,1088 929,1081 L947,1073 " +
                "Q866,937 811,759 Z","#A87651","#564336",3.5);
            P(dc,"M810,795 Q863,965 931,1075",null,"#D9AE82",3);
            dc.PushTransform(new RotateTransform(-24,890,1003));
            Round(dc,869,985,43,33,5,"#81583D","#47382E",3);
            Round(dc,874,990,33,23,3,"#A9784F","#F0CB8A",4);
            P(dc,"M891,996 L891,1006",null,"#F0CB8A",3);
            dc.Pop();
            P(dc,"M669,769 Q978,729 1317,656 Q1345,669 1330,714 " +
                "L712,842 Q670,846 654,806 Z","#AB7953","#554032",4);
            P(dc,"M706,831 L1318,709",null,"#E3BD92",4);

            if (deskKeyboard == null) deskKeyboard = CreateKeyboard();
            Matrix pack = Matrix.Identity;
            pack.Translate(-705,-1196);
            pack.Scale(.65,.59);
            pack.Rotate(-10);
            pack.Translate(1000,662);
            dc.PushTransform(new MatrixTransform(pack));
            dc.DrawDrawing(deskKeyboard);
            dc.Pop();
            dc.Pop();
        }

        static void DrawWalkingLeg(DrawingContext dc, bool hind, bool far, Matrix m)
        {
            dc.PushTransform(new MatrixTransform(m));
            Geometry shape = Shape(hind ? WalkHindleg : WalkForeleg);
            dc.DrawGeometry(far ? Paint("#D6C7BD") : PawIvory,far ? Outline(CoatEdge,3.5) : null,shape);
            if (hind)
            {
                dc.PushClip(shape);
                P(dc,"M-99,-42 L115,-52 L119,157 Q68,182 41,204 L46,191 " +
                    "Q13,225 -30,216 Q-72,205 -103,173 Z",far ? "#B68D6E" : "#EAA76A");
                dc.Pop();
            }
            P(dc,hind ? "M-66,397 Q-71,386 -61,377 M-39,401 Q-43,392 -34,382" :
                "M-82,394 Q-88,383 -78,374 M-56,403 Q-63,388 -51,377",
                null,far ? "#A7988E" : "#A68C7D",2.8);
            dc.Pop();
        }
    }
}

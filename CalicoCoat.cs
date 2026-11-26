using System;
using System.Windows;
using System.Windows.Media;

namespace PawTrail
{
    public partial class PetArt
    {
        const string CoatEdge = "#292221";
        static readonly Brush CreamCoat = CoatWash("#FFFAF3", "#F3E6DC", .38, .05, .64, 1);
        static readonly Brush SootCoat = CoatWash("#373031", "#2C2728", .32, .1, .92, 1);
        static readonly Brush AmberCoat = CoatWash("#F4B776", "#E9A154", .16, .05, .86, .92);
        static readonly Brush ThroatShade = CoatWash("#D8C9BF", "#EDE1D7", .37, .1, .61, 1);
        static readonly Brush PawIvory = CoatWash("#FFF9F1", "#F9EFE7", .24, .14, .7, .92);

        static Brush CoatWash(string start, string end, double x1, double y1, double x2, double y2)
        {
            var brush = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(start),
                (Color)ColorConverter.ConvertFromString(end), new Point(x1,y1), new Point(x2,y2));
            brush.Freeze();
            return brush;
        }

        // The lap is behind the keyboard. The lower edge lies under its back rail,
        // so that there are no hind feet or outlines showing through the keys.
        const string SittingCoat =
            "M696,601 C672,632 638,674 618,712 C596,752 579,788 565,833 " +
            "C552,874 541,898 526,917 Q502,953 485,1004 L476,1041 " +
            "L1333,1041 C1323,990 1300,932 1276,888 C1252,842 1211,791 1175,748 " +
            "C1140,704 1113,651 1066,618 C1025,589 981,586 937,589 Z";

        void DrawTorso(DrawingContext dc)
        {
            dc.DrawGeometry(CreamCoat, null, Shape(SittingCoat));
            dc.PushClip(Shape(SittingCoat));
            // A sliver of ginger shows around the left elbow; most of this side is cream.
            dc.DrawGeometry(AmberCoat, null, Shape(
                "M547,730 L641,681 C622,726 609,761 594,795 Q579,833 573,866 " +
                "L579,855 L569,887 L574,880 Q562,905 550,922 L553,921 " +
                "Q549,934 528,948 L512,971 L476,946 Z"));
            P(dc,"M489,955 Q533,914 553,877 Q548,913 531,938 L538,930 L525,951 Z","#F4BF89");
            dc.DrawGeometry(SootCoat, null, Shape(
                "M992,594 L1134,612 L1362,914 L1384,1073 L941,1061 " +
                "C958,1002 984,958 1019,919 Q1062,870 1063,818 Q1070,729 992,594 Z"));
            // The outer shoulder carries a single orange saddle with a charcoal streak.
            dc.DrawGeometry(AmberCoat, null, Shape(
                "M1055,605 C1123,644 1154,680 1190,731 C1236,778 1274,843 1280,909 " +
                "Q1284,949 1275,983 L1277,973 L1274,1003 L1270,996 L1265,1025 " +
                "L1263,1019 L1257,1044 L1248,1047 C1233,999 1217,980 1205,954 " +
                "Q1185,912 1176,871 Q1163,809 1136,760 C1111,705 1084,656 1055,635 Z"));
            dc.DrawGeometry(SootCoat, null, Shape(
                "M1101,637 C1136,669 1161,698 1180,734 Q1204,776 1217,827 " +
                "L1214,821 L1222,851 L1219,846 L1227,879 L1223,872 L1228,895 " +
                "Q1215,886 1209,877 L1211,885 L1199,870 L1201,879 " +
                "Q1185,861 1176,830 Q1163,783 1145,744 C1132,708 1115,674 1101,637 Z"));
            P(dc,"M1265,899 Q1281,957 1264,1012 L1266,990 L1259,1007 Q1269,967 1265,899 Z","#DB954F");
            // Soft reflected colour ties the two forelegs to the shadowed belly.
            P(dc,"M546,911 Q574,859 604,843 Q613,903 654,963 L682,1016 L605,1048 L523,1038 Z","#E5D6CA");
            P(dc,"M567,916 Q594,898 603,858 Q615,897 638,934 L624,921 L643,955 " +
                "L631,944 L640,965 Q595,947 567,916 Z","#F1E2D4");
            dc.Pop();
            dc.DrawGeometry(null, Outline(CoatEdge,4.0), Shape(SittingCoat));
        }

        const string Chest =
            "M678,628 Q743,620 833,625 Q985,581 1059,612 C1071,627 1090,640 1103,655 " +
            "L1093,649 L1118,669 L1107,665 L1129,687 L1117,681 Q1131,698 1137,713 " +
            "L1126,704 L1136,724 L1120,714 L1131,736 L1112,723 " +
            "Q1123,746 1127,775 L1118,757 L1128,789 Q1116,783 1104,768 " +
            "Q1118,797 1118,818 L1103,798 L1111,827 L1100,816 " +
            "Q1107,839 1105,853 L1094,836 L1096,859 L1089,849 Q1087,879 1067,907 " +
            "L1072,893 Q1059,919 1032,946 L1039,932 Q1013,967 984,982 L992,969 " +
            "Q968,992 949,1004 L957,994 L928,1024 L923,1017 L901,1041 " +
            "L888,1027 L874,1040 L851,1022 L855,1034 Q833,1027 809,1002 " +
            "L816,1017 Q789,1006 767,979 L773,996 Q754,984 738,962 " +
            "L742,977 Q712,958 687,923 L693,937 Q671,921 655,890 L658,908 " +
            "Q643,895 632,869 L634,883 Q615,865 607,831 L605,843 " +
            "Q596,829 601,798 L598,806 Q591,797 597,775 Q579,785 568,777 " +
            "Q582,774 594,752 Q604,730 610,715 Q596,724 591,722 " +
            "Q601,696 626,672 Q646,649 678,628 Z";

        void DrawRuff(DrawingContext dc)
        {
            dc.DrawGeometry(CreamCoat, null, Shape(Chest));
            dc.PushClip(Shape(Chest));
            // The under-chin shadow descends in irregular locks into the white bib.
            dc.DrawGeometry(ThroatShade, null, Shape(
                "M559,615 L1098,583 L1136,688 L1110,673 L1122,685 L1092,677 L1102,689 " +
                "L1076,680 L1084,690 Q1063,684 1050,685 L1064,694 L1041,691 " +
                "Q1017,694 1001,705 L1028,704 L1007,714 L1020,715 Q1001,720 985,727 " +
                "L1012,729 L988,737 Q964,744 945,753 L964,752 L944,764 L951,766 " +
                "Q924,771 904,767 L913,777 L891,770 L895,781 L877,776 L884,787 " +
                "Q859,780 849,768 L855,789 L835,776 L835,801 L823,787 L808,809 " +
                "L815,781 L803,791 L807,773 L779,794 L786,777 L767,795 L776,769 " +
                "Q756,780 748,789 L753,773 L729,789 L735,772 L708,782 L719,770 " +
                "L682,779 L693,767 L661,773 L676,763 L647,769 L659,758 " +
                "L622,772 L635,758 L602,773 L613,751 L576,769 Z"));
            P(dc,
                "M597,798 Q610,831 621,841 L617,820 Q632,857 646,868 L639,846 Q654,879 674,890 " +
                "L667,872 Q688,915 720,933 L716,920 Q737,945 757,953 L750,938 " +
                "Q768,967 791,980 L785,962 Q816,993 839,1006 L831,987 " +
                "L859,1014 L851,990 L877,1022 L887,1048 L759,1030 L636,946 L589,851 Z","#E6D8CF");
            P(dc,
                "M1067,896 Q1051,928 1025,948 L1035,930 Q1014,957 993,972 " +
                "L1003,952 L976,977 L982,958 Q959,986 938,1001 L944,984 " +
                "Q925,1008 906,1027 L909,1048 L1036,1022 L1084,912 Z","#E8DAD2");
            P(dc,
                "M712,826 Q710,837 712,853 Q719,836 726,831 L723,844 Q730,832 736,823 " +
                "M756,835 Q748,848 746,864 M769,841 Q763,853 760,861 M743,926 Q751,940 761,945 " +
                "M971,935 Q962,949 958,957", null,"#E8DAD1",2);
            dc.Pop();
            P(dc,
                "M627,672 Q601,696 591,722 Q597,725 610,715 Q602,735 593,753 Q583,774 568,777 " +
                "Q581,786 597,775 M601,793 Q594,826 606,842 M608,828 Q614,857 630,875 " +
                "M648,877 Q659,907 674,920 M697,930 Q710,949 724,957",
                null,CoatEdge,3.3);
            if (skin == 1)
            {
                P(dc, "M636,669 Q819,743 1059,635 L1081,664 Q1064,714 1005,753 " +
                    "L900,805 L841,762 L775,807 Q694,775 619,715 Z","#527D9C",CoatEdge,3.8);
                P(dc,"M639,709 Q704,759 774,783 L837,745 M857,745 L899,780 Q990,744 1062,674",
                    null,"#F3F1E3",6);
                P(dc,"M834,732 Q798,716 762,711 L766,740 L804,752 L782,799 L811,790 " +
                    "L824,803 L845,748 L873,789 L899,785 L881,747 Q913,737 930,710 " +
                    "Q895,710 856,731 Z","#BAD9D4",CoatEdge,3.4);
                E(dc,846,738,18,14,"#84B4BC",CoatEdge,3.4);
            }
            else if (skin == 2)
            {
                P(dc,"M676,697 Q659,741 633,788 L654,783 L663,801 Q698,754 714,720 Z " +
                    "M700,711 Q706,757 694,801 L718,791 L727,801 Q745,746 737,718 Z",
                    "#C96579",CoatEdge,3.7);
                P(dc,"M633,653 Q817,740 1055,621 Q1078,636 1072,660 C954,748 741,768 624,699 " +
                    "Q614,677 633,653 Z","#DA8694",CoatEdge,3.8);
                P(dc,"M648,677 L641,698 M678,689 L670,714 M718,701 L711,725 M761,708 L756,736 " +
                    "M808,714 L804,740 M857,713 L855,738 M911,704 L912,730 M958,688 L964,713 " +
                    "M1009,668 L1015,692 M1052,646 L1060,666",null,"#F1AFB9",5);
            }
        }

        // Only the fur behind the wrist folds back when a paw leaves the keyboard rail.
        // The toe contours and their painted detail share the same coordinates in every pose.
        static Geometry PalmOutline(bool leftLimb, double reachAmount)
        {
            var palm = new StreamGeometry();
            using (StreamGeometryContext g = palm.Open())
            {
                if (leftLimb)
                {
                    g.BeginFigure(Mix(new Point(432,1015),new Point(446,985),reachAmount),true,true);
                    g.BezierTo(Mix(new Point(432,985),new Point(444,973),reachAmount),
                        new Point(447+5*reachAmount,956),new Point(466,939),true,true);
                    g.BezierTo(new Point(484,924),new Point(507,916),new Point(528,916),true,true);
                    g.BezierTo(new Point(551,914),new Point(573,925),new Point(589,943),true,true);
                    g.BezierTo(new Point(608,963),new Point(620,986),new Point(621,1009),true,true);
                    g.BezierTo(new Point(623,1030),new Point(615,1051),new Point(603,1058),true,true);
                    g.QuadraticBezierTo(new Point(588,1070),new Point(576,1062),true,true);
                    g.BezierTo(new Point(568,1075),new Point(550,1081),new Point(536,1078),true,true);
                    g.QuadraticBezierTo(new Point(520,1078),new Point(509,1070),true,true);
                    g.QuadraticBezierTo(new Point(497,1074),new Point(482,1065),true,true);
                    g.BezierTo(new Point(468,1056),new Point(461,1040),new Point(459,1017),true,true);
                }
                else
                {
                    g.BeginFigure(new Point(1139,1019),true,true);
                    g.BezierTo(new Point(1131,1026),new Point(1134,1046),new Point(1119,1072),true,true);
                    g.BezierTo(new Point(1105,1094),new Point(1088,1105),new Point(1072,1102),true,true);
                    g.LineTo(new Point(1064,1099),true,true);
                    g.BezierTo(new Point(1056,1110),new Point(1042,1117),new Point(1028,1115),true,true);
                    g.BezierTo(new Point(1004,1114),new Point(981,1102),new Point(974,1086),true,true);
                    g.BezierTo(new Point(959,1076),new Point(958,1050),new Point(966,1026),true,true);
                    g.BezierTo(new Point(974,1001),new Point(992,985),new Point(1010,968),true,true);
                    g.BezierTo(new Point(1030,946),new Point(1057,934),new Point(1086,930+8*reachAmount),true,true);
                    g.BezierTo(Mix(new Point(1121,925),new Point(1108,942),reachAmount),
                        Mix(new Point(1163,937),new Point(1135,953),reachAmount),
                        Mix(new Point(1190,964),new Point(1142,976),reachAmount),true,true);
                    g.QuadraticBezierTo(Mix(new Point(1211,985),new Point(1151,993),reachAmount),
                        Mix(new Point(1219,1019),new Point(1139,1019),reachAmount),true,true);
                }
            }
            palm.Freeze();
            return palm;
        }

        static readonly Geometry restingLeftPalm = PalmOutline(true,0), restingRightPalm = PalmOutline(false,0);
        static readonly Geometry reachingLeftPalm = PalmOutline(true,1), reachingRightPalm = PalmOutline(false,1);

        static void DrawExtendedArm(DrawingContext dc, Geometry palm, Point root, Point elbow, Point wrist, bool leftLimb)
        {
            Vector upper = elbow-root, lower = wrist-elbow;
            double upperLength = upper.Length, lowerLength = lower.Length;
            upper.Normalize();
            if (lowerLength < 1) lower = upper; else lower.Normalize();
            Vector bend = upper+lower;
            if (bend.LengthSquared < .001) bend = upper;
            bend.Normalize();
            // A quadratic rounds the elbow between the two straight tangents.
            // Its broad bend is visible even when the far paw crosses the chest.
            Point before = elbow-upper*(upperLength*.77);
            Point after = elbow+lower*Math.Min(lowerLength*.70,180);
            const int intervals = 14;
            var inner = new Point[intervals+3];
            var outer = new Point[intervals+3];
            double side = leftLimb ? 1 : -1;
            Vector topNormal = side*new Vector(-upper.Y,upper.X);
            outer[0] = root + topNormal*83;
            inner[0] = root - topNormal*83;
            for (int i=0;i<=intervals;i++)
            {
                double t = (double)i/intervals;
                Point a = Mix(before,elbow,t), b = Mix(elbow,after,t);
                Point p = Mix(a,b,t);
                Vector tangent = b-a;
                if (tangent.LengthSquared < .001) tangent = bend;
                tangent.Normalize();
                Vector n = side*new Vector(-tangent.Y,tangent.X);
                double radius = Mix(80,61,t);
                outer[i+1] = p+n*radius;
                inner[i+1] = p-n*radius;
            }
            int end = outer.Length-1;
            Vector endNormal = side*new Vector(-lower.Y,lower.X);
            outer[end] = wrist+endNormal*55;
            inner[end] = wrist-endNormal*55;
            var sleeve = new StreamGeometry();
            using (StreamGeometryContext g = sleeve.Open())
            {
                g.BeginFigure(outer[0],true,true);
                for(int i=1;i<=end;i++) g.LineTo(outer[i],true,true);
                g.QuadraticBezierTo(wrist+lower*55,inner[end],true,true);
                for(int i=end-1;i>=0;i--) g.LineTo(inner[i],true,true);
            }
            Geometry limb = Geometry.Combine(sleeve,palm,GeometryCombineMode.Union,null,1.35,ToleranceType.Absolute);
            dc.DrawGeometry(PawIvory,null,limb);
            // Taper the warm reflected shadow down to the wrist.
            Point[] underside = leftLimb ? inner : outer, topside = leftLimb ? outer : inner;
            var shadow = new StreamGeometry();
            using (StreamGeometryContext g = shadow.Open())
            {
                g.BeginFigure(underside[0],true,true);
                for (int i=1;i<=end;i++) g.LineTo(underside[i],true,true);
                g.QuadraticBezierTo(Mix(underside[13],topside[13],.10),
                    Mix(underside[8],topside[8],.14),true,true);
                g.LineTo(Mix(underside[9],topside[9],.12),true,true);
                g.QuadraticBezierTo(Mix(underside[4],topside[4],.26),
                    Mix(underside[0],topside[0],.36),true,true);
            }
            // Carry the saddle along the outer shoulder and narrow it into locks
            // of fur down the leg. The inner foreleg stays ivory.
            var marking = new StreamGeometry();
            using (StreamGeometryContext g = marking.Open())
            {
                int furEnd = leftLimb ? 9 : 12;
                g.BeginFigure(outer[0],true,true);
                for(int i=1;i<=furEnd;i++) g.LineTo(outer[i],true,true);
                g.QuadraticBezierTo(Mix(outer[furEnd-2],inner[furEnd-2],.13),
                    Mix(outer[furEnd-4],inner[furEnd-4],.24),true,true);
                g.LineTo(Mix(outer[furEnd-3],inner[furEnd-3],.20),true,true);
                g.QuadraticBezierTo(Mix(outer[3],inner[3],leftLimb ? .30 : .62),
                    Mix(outer[0],inner[0],leftLimb ? .52 : .94),true,true);
            }
            dc.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude,sleeve,palm));
            dc.DrawGeometry(Paint("#85DECEC2"),null,shadow);
            dc.DrawGeometry(leftLimb ? AmberCoat : SootCoat,null,marking);
            dc.Pop();
            dc.DrawGeometry(null,Outline(CoatEdge,4.2),limb);
        }
        void DrawForelegs(DrawingContext dc, Point leftTip, Point rightTip)
        {
            DrawPaw(dc,leftTip,true);
            DrawPaw(dc,rightTip,false);
        }

        static void DrawPaw(DrawingContext dc, Point tip, bool leftLimb)
        {
            Point rest = leftLimb ? LeftRest : RightRest;
            // The proximal edge stays inside the bib throughout the movement.
            Point shoulder = leftLimb ? new Point(701,751) : new Point(1004,752);
            Vector movement = tip-rest;
            double extension = Clamp((movement.Length-12)/65,0,1);
            extension = extension*extension*(3-2*extension);
            var matrix = Matrix.Identity;
            matrix.RotateAt(extension*Clamp((shoulder.X-tip.X)/22,-22,22),rest.X,rest.Y);
            matrix.Translate(movement.X,movement.Y);
            dc.PushTransform(new MatrixTransform(matrix));
            Geometry outline = extension == 0 ? (leftLimb ? restingLeftPalm : restingRightPalm)
                : extension == 1 ? (leftLimb ? reachingLeftPalm : reachingRightPalm) : PalmOutline(leftLimb,extension);
            Point wrist = leftLimb ? new Point(531,981) : new Point(1061,1004);
            Point worldWrist = matrix.Transform(wrist);
            double elbowX = leftLimb ? 579 + Clamp((worldWrist.X-531)*.15,-65,48)
                : 1120 + Clamp((worldWrist.X-1061)*.17,-80,45);
            Point elbow = new Point(elbowX,Mix(shoulder.Y,worldWrist.Y,.47)+33);
            matrix.Invert();
            DrawExtendedArm(dc,outline,matrix.Transform(shoulder),matrix.Transform(elbow),wrist,leftLimb);
            dc.PushClip(outline);
            if (leftLimb)
            {
                P(dc,"M428,982 Q452,948 477,938 Q451,960 443,986 L452,978 L447,993 " +
                    "Q459,980 468,980 Q455,1001 456,1017 L426,1024 Z","#EFDFD2");
                P(dc,"M457,1002 Q460,1040 482,1053 Q499,1067 511,1062 Q525,1073 545,1072 " +
                    "Q565,1070 576,1055 Q590,1063 602,1052 Q615,1037 619,1018 L630,1061 " +
                    "L601,1086 L504,1089 L456,1059 Z","#E9DBD6");
                P(dc,"M473,1040 Q483,1058 497,1061 M528,1070 Q545,1078 559,1070",null,"#F6E7E0",2.3);
                P(dc,"M504,1010 C499,1026 497,1055 509,1070 " +
                    "M559,995 Q572,1020 571,1042 Q572,1055 576,1062 " +
                    "M465,989 Q456,1004 459,1017",null,"#75635D",2.65);
            }
            else
            {
                P(dc,"M1180,954 Q1210,968 1227,1001 L1227,1025 L1204,1023 Q1200,1014 1197,1007 " +
                    "L1201,1009 L1190,993 L1194,996 Q1189,982 1178,973 L1183,974 L1169,963 Z","#EABE91");
                P(dc,"M960,1055 Q966,1086 990,1097 Q1011,1108 1027,1110 Q1046,1113 1061,1092 " +
                    "Q1077,1100 1089,1091 Q1102,1084 1114,1064 L1127,1050 " +
                    "Q1113,1095 1094,1107 L1069,1106 L1052,1122 L995,1127 L957,1104 Z","#E9DAD3");
                P(dc,"M1139,1025 Q1145,1015 1151,1009 L1153,1014 L1167,1003 " +
                    "L1162,1012 Q1188,1008 1202,1018 L1194,1024 Z","#F2E3D8");
                P(dc,"M993,1036 C979,1066 984,1096 1007,1109 " +
                    "M1066,1053 Q1051,1083 1064,1099 " +
                    "M1152,999 Q1144,1011 1139,1019",null,"#75645C",2.7);
            }
            dc.Pop();
            dc.Pop();
        }
    }
}

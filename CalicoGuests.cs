using System;
using System.Windows.Media;

namespace PawTrail
{
    public partial class PetArt
    {
        private double kittenPresence, crowPresence;
        private double kittenAge = -1, crowAge = -1;
        private static DrawingGroup kittenBody, kittenTail, kittenHead, crowBody, crowHead;

        public void SetGuests(double kitten, double crow)
        {
            SetGuests(kitten, crow, -1, -1);
        }

        public void SetGuests(double kitten, double crow, double kittenSeconds, double crowSeconds)
        {
            Set(ref kittenPresence, Clamp(kitten, 0, 1));
            Set(ref crowPresence, Clamp(crow, 0, 1));
            Set(ref kittenAge, kittenSeconds);
            Set(ref crowAge, crowSeconds);
        }

        private void DrawGuests(DrawingContext dc)
        {
            if (kittenPresence <= 0 && crowPresence <= 0) return;
            // The crow grips the back rim opposite the mouse. The kitten sits
            // beyond the diagonally opposite front corner, beside the mouse.
            if (mouseOnLeft) dc.PushTransform(new ScaleTransform(-1, 1, MirrorWidth / 2, 0));
            if (crowPresence > 0) DrawCrowGuest(dc, crowAge < 0 ? phase * .5 : crowAge);
            if (kittenPresence > 0) DrawKittenGuest(dc, kittenAge < 0 ? phase * .5 : kittenAge);
            if (mouseOnLeft) dc.Pop();
        }

        private static DrawingGroup kittenPaw, yarnBall;

        private static double KittenBeat(double time, double start, double end)
        {
            double t = Clamp((time - start) / (end - start), 0, 1);
            return t * t * (3 - 2 * t);
        }

        private void DrawKittenGuest(DrawingContext dc, double age)
        {
            if (kittenBody == null)
            {
                kittenBody = Record(DrawKittenBody);
                kittenHead = Record(DrawKittenHead);
                kittenTail = Record(DrawKittenTail);
                kittenPaw = Record(DrawKittenPaw);
                yarnBall = Record(DrawYarnBall);
            }
            // Bat the ball away, hop after it and scoop it back with the same paw.
            double t = age % 5.8;
            double push = KittenBeat(t, .64, 1.64);
            double chase = KittenBeat(t, 1.38, 2.18);
            double pull = KittenBeat(t, 2.92, 4.28);
            const double rollDistance = 58;
            double catX = -rollDistance * (chase - pull);
            double catY = -17 * Math.Sin(Math.PI * chase) - 2 * Math.Sin(Math.PI * pull);
            double ballX = -66 - rollDistance * (push - pull);
            double raise = KittenBeat(t, .10, .46) - KittenBeat(t, .79, 1.22);
            double hook = KittenBeat(t, 1.88, 2.48) - KittenBeat(t, 4.31, 4.77);

            dc.PushOpacity(kittenPresence);
            dc.PushTransform(new TranslateTransform(1536, 1471));
            dc.PushTransform(new ScaleTransform(1.94, 1.94));
            dc.PushTransform(new TranslateTransform(catX, 0));
            E(dc, 2, -1, 48, 5.5, "#30423736");
            double lift = catY - 6 * (1 - kittenPresence);
            dc.PushTransform(new TranslateTransform(0, lift));
            dc.PushTransform(new RotateTransform(8 * Math.Sin(t * (8 * Math.PI / 5.8)) - 10 * raise, 29, -18));
            dc.DrawDrawing(kittenTail);
            dc.Pop();
            dc.DrawDrawing(kittenBody);
            dc.PushTransform(new RotateTransform(-6 - 8 * (push - pull) - 4 * raise, -2, -52));
            dc.DrawDrawing(kittenHead);
            dc.Pop();
            dc.Pop();
            dc.Pop();

            // The loose end lies on the ground while the wound strands turn.
            dc.PushTransform(new TranslateTransform(ballX, 0));
            E(dc, 0, 2, 24, 4, "#334B3837");
            P(dc, "M5,0 C18,5 26,5 34,2 C42,-1 47,0 49,3 Q51,7 43,7",
                null, "#965168", 2.6);
            dc.PushTransform(new TranslateTransform(0, -19 - 3 * Math.Sin(Math.PI * push)));
            dc.PushTransform(new RotateTransform((ballX + 66) * 180 / (22 * Math.PI)));
            dc.DrawDrawing(yarnBall);
            dc.Pop();
            dc.Pop();
            dc.Pop();

            // Draw the reaching paw over the rim of the ball at contact.
            dc.PushTransform(new TranslateTransform(catX, lift));
            double strike = KittenBeat(t, .47, .68) - KittenBeat(t, .79, 1.22);
            double pawAngle = 6 + 73 * raise - 15 * strike + 62 * hook;
            dc.PushTransform(new RotateTransform(pawAngle, -16, -39));
            dc.DrawDrawing(kittenPaw);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }

        private const string TabbyInk = "#573C35";

        private static void DrawKittenTail(DrawingContext dc)
        {
            P(dc, "M24,-10 C45,-3 57,-12 60,-23 C63,-35 57,-48 51,-49 C45,-50 43,-44 46,-37 C51,-24 47,-19 32,-21Z",
                "#DE9562", TabbyInk, 2.6);
            P(dc, "M51,-47 C55,-46 58,-40 59,-35 Q53,-38 47,-34 Q42,-46 48,-47Z", "#96654B");
        }

        private static void DrawKittenBody(DrawingContext dc)
        {
            P(dc, "M-25,-60 C-36,-49 -41,-30 -35,-14 Q-47,-10 -43,-4 Q-40,0 -31,-1 L24,0 Q39,0 40,-11 C42,-29 26,-50 13,-61Z",
                "#E6A879", TabbyInk, 2.6);
            P(dc, "M13,-49 Q28,-48 32,-36 Q25,-40 16,-38 Q24,-34 28,-28 Q14,-32 10,-39Z", "#B77A57");
            P(dc, "M-23,-51 Q-5,-40 12,-50 Q17,-38 9,-25 L6,-28 Q5,-20 -1,-16 L-7,-21 L-12,-18 Q-21,-28 -23,-51Z",
                "#FFF1D8");
            P(dc, "M3,-25 L5,-10 Q-3,-8 -2,-3 Q0,2 12,0 Q19,0 18,-6 L19,-25",
                "#FBE3BE", TabbyInk, 1.9);
            P(dc, "M4,-3 Q5,-6 8,-6", null, "#A37863", 1.3);
        }

        private static void DrawKittenPaw(DrawingContext dc)
        {
            P(dc, "M-24,-41 Q-29,-34 -27,-26 L-27,-12 C-35,-8 -34,-2 -28,0 Q-18,3 -13,-3 Q-7,-10 -7,-23 L-6,-34 Q-6,-41 -13,-43",
                "#F9D7AC", TabbyInk, 2.1);
            P(dc, "M-29,-6 Q-25,-11 -20,-11 M-24,-3 Q-20,-7 -17,-7", null, "#AA7C67", 1.6);
        }

        private static void DrawYarnBall(DrawingContext dc)
        {
            E(dc, 0, 0, 22, 22, "#DA8296", "#784454", 2.6);
            P(dc, "M-19,-9 Q-6,-16 7,-17 M-21,-2 Q-9,-12 12,-15 M-20,5 Q-11,-5 1,-8",
                null, "#F9C9CC", 2.8);
            P(dc, "M0,-10 Q8,-4 18,6 M6,-12 Q15,-6 20,0 M-5,-7 Q6,-1 15,12",
                null, "#AB526B", 2.8);
            P(dc, "M-17,11 Q-10,7 -6,-3 M-12,16 Q-4,11 0,2 M-5,19 Q4,13 7,7",
                null, "#F6BECA", 2.7);
        }

        private static void DrawKittenHead(DrawingContext dc)
        {
            P(dc, "M-46,-91 Q-51,-105 -49,-121 Q-48,-126 -43,-123 L-21,-109 Q-6,-115 11,-110 L32,-125 Q37,-128 37,-121 L38,-97 Q48,-89 45,-77 L50,-74 L43,-71 L46,-67 L36,-66 Q25,-50 3,-48 Q-20,-46 -34,-59 L-43,-60 L-41,-65 L-51,-69 L-47,-74 Q-52,-79 -46,-91Z",
                "#EDB278", TabbyInk, 2.8);
            P(dc, "M-45,-116 L-29,-107 Q-37,-104 -42,-98Z M30,-117 L32,-101 Q26,-105 20,-107Z", "#CC8C83");
            P(dc, "M-13,-109 Q-9,-111 -5,-111 L-8,-96Z M-1,-110 L6,-109 L1,-96Z M12,-108 L18,-107 L10,-98Z",
                "#AF744D");
            P(dc, "M-46,-85 Q-40,-84 -35,-78 L-46,-80Z M35,-85 L45,-88 L44,-83 L34,-80Z", "#B97D53");
            P(dc, "M-44,-72 Q-31,-69 -25,-72 Q-12,-79 -6,-76 Q0,-76 4,-72 Q12,-76 24,-69 Q36,-66 43,-71 L38,-65 Q24,-50 4,-50 Q-17,-48 -32,-60 L-39,-62 L-37,-65Z",
                "#FFF0D7");
            E(dc, -25, -84, 6.7, 8.1, "#6E7661", TabbyInk, 1.6);
            E(dc, 14, -86, 6.9, 8.2, "#6E7661", TabbyInk, 1.6);
            E(dc, -27, -84, 3.9, 5.9, "#34332D");
            E(dc, 12, -86, 3.9, 5.9, "#34332D");
            E(dc, -28, -87, 1.9, 2.0, "#FFFEF4");
            E(dc, 11, -89, 1.9, 2.0, "#FFFEF4");
            P(dc, "M-11,-73 Q-7,-76 -3,-73 Q-3,-70 -7,-68 Q-10,-69 -11,-73Z", "#BC8177");
            P(dc, "M-7,-68 Q-10,-61 -16,-65 M-7,-68 Q-5,-62 0,-65 M-34,-69 L-49,-71 M-31,-64 L-44,-62 M23,-70 L38,-74 M25,-65 L40,-66",
                null, "#805F52", 1.55);
        }

        private static DrawingGroup crowFeet, crowFarWing, crowNearWing;
        private const double CrowCycle = 7.2;

        private static double CrowEase(double t)
        {
            t = Clamp(t, 0, 1);
            return t * t * (3 - 2 * t);
        }

        private void DrawCrowGuest(DrawingContext dc, double age)
        {
            if (crowBody == null)
            {
                crowBody = Record(DrawCrowBody);
                crowHead = Record(DrawCrowHead);
                crowFeet = Record(DrawCrowFeet);
                crowFarWing = Record(DrawCrowFarWing);
                crowNearWing = Record(DrawCrowNearWing);
            }
            double time = Math.Max(0, age) % CrowCycle;
            double travel = CrowEase((time - .85) / 4);
            double lift = Math.Sin(Math.PI * travel);
            double open = CrowEase(lift * 5);
            double beat = Math.Sin((time - .85) * 2 * Math.PI * 2.45);
            double crouch = Math.Sin(Math.PI * Clamp((time - .39) / .46, 0, 1))
                + .7 * Math.Sin(Math.PI * Clamp((time - 4.85) / .34, 0, 1));
            // Follow a narrow oval in the open space outside the cat's cheek.
            // The two turns roll through a foreshortened silhouette.
            double x = 175 + 120 * lift + 75 * Math.Sin(2 * Math.PI * travel);
            double y = 1019 - 400 * lift + 7 * beat * open;
            double facing = 1 - 2 * CrowEase((travel - .30) / .09)
                + 2 * CrowEase((travel - .84) / .10);
            dc.PushOpacity(crowPresence);
            dc.PushOpacity(1 - .87 * lift);
            E(dc, 175 + (x - 175) * .16, 1019, 39 - 17 * lift, 6 - 3 * lift, "#3D39383B");
            dc.Pop();
            dc.PushTransform(new TranslateTransform(x, y));
            dc.PushTransform(new ScaleTransform(1.86, 1.86 * (1 - .14 * crouch)));
            double width = .48 + .52 * Math.Abs(facing);
            if (Math.Abs(facing) < .42)
            {
                // Blend the two profiles for the few frames facing into the turn.
                double right = CrowEase((facing + .42) / .84);
                dc.PushOpacity(1 - right);
                DrawTurningCrow(dc, -width, open, beat, time);
                dc.Pop();
                dc.PushOpacity(right);
                DrawTurningCrow(dc, width, open, beat, time);
                dc.Pop();
            }
            else DrawTurningCrow(dc, facing > 0 ? width : -width, open, beat, time);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }

        private static void DrawTurningCrow(DrawingContext dc, double facing, double open, double beat, double time)
        {
            dc.PushTransform(new ScaleTransform(facing, 1));
            dc.PushTransform(new RotateTransform(open * (20 + 4 * beat), 0, -50));
            dc.PushTransform(new ScaleTransform(1, 1 - .76 * open, 0, -24));
            dc.DrawDrawing(crowFeet);
            dc.Pop();
            if (open > .002)
            {
                dc.PushOpacity(open);
                dc.PushTransform(new ScaleTransform(.70 + .30 * open, open * (.24 + .79 * beat), 1, -57));
                dc.DrawDrawing(crowFarWing);
                dc.Pop();
                dc.Pop();
            }
            dc.DrawDrawing(crowBody);
            dc.PushTransform(new RotateTransform((1 - open) * (2 + 5 * Math.Sin(time * 4)) - open * 7, 5, -59));
            dc.DrawDrawing(crowHead);
            dc.Pop();
            if (open > .002)
            {
                dc.PushOpacity(open);
                dc.PushTransform(new ScaleTransform(.70 + .30 * open, open * (.22 + .94 * beat), -8, -57));
                dc.DrawDrawing(crowNearWing);
                dc.Pop();
                dc.Pop();
            }
            dc.Pop();
            dc.Pop();
        }

        private const string CrowInk = "#23232B";

        private static void DrawCrowFeet(DrawingContext dc)
        {
            // Two visible sets of toes curl around the raised lip of the board.
            P(dc, "M-7,-24 L-10,-5 L-18,-2 M-10,-5 L-2,-2 L-1,1 M11,-23 L9,-6 L3,-2 M9,-6 L18,-3 L20,0",
                null, "#514848", 2.8);
        }

        private static void DrawCrowFarWing(DrawingContext dc)
        {
            P(dc, "M0,-57 Q-3,-84 11,-108 Q22,-127 39,-143 Q38,-130 33,-118 L46,-133 " +
                "Q43,-117 35,-107 L45,-114 Q38,-96 26,-87 L30,-89 Q24,-70 12,-59 Q6,-53 0,-57Z",
                "#343541", CrowInk, 2.4);
            P(dc, "M11,-70 Q16,-95 30,-113", null, "#535768", 1.7);
        }

        private static void DrawCrowNearWing(DrawingContext dc)
        {
            // Long primary feathers spread beyond the shorter rounded coverts.
            P(dc, "M-3,-53 C-15,-49 -29,-65 -36,-83 Q-45,-100 -64,-126 " +
                "Q-54,-123 -47,-117 Q-60,-134 -68,-151 Q-55,-144 -44,-134 " +
                "Q-54,-151 -57,-165 Q-43,-154 -30,-137 L-35,-153 " +
                "Q-18,-139 -11,-119 L-10,-130 Q2,-116 1,-95 Q8,-83 6,-67 Q5,-56 -3,-53Z",
                "#444958", CrowInk, 2.6);
            P(dc, "M-4,-60 Q-19,-69 -24,-86 L-27,-96 Q-17,-93 -12,-83 " +
                "L-15,-98 Q-7,-95 -3,-85 L-3,-95 Q5,-84 4,-72 Q3,-64 -4,-60Z", "#595F70");
            P(dc, "M-34,-113 Q-44,-133 -53,-145 M-22,-113 Q-28,-130 -36,-140",
                null, "#62687B", 1.65);
        }

        private static void DrawCrowBody(DrawingContext dc)
        {
            P(dc, "M-16,-52 Q-33,-39 -60,-30 L-53,-28 L-61,-23 Q-43,-24 -27,-30 L-40,-24 Q-18,-24 -7,-38Z",
                "#454654", CrowInk, 2.4);
            P(dc, "M-14,-70 C-25,-65 -30,-52 -29,-39 Q-26,-24 -12,-20 Q2,-15 14,-23 Q25,-33 28,-51 L30,-61 Q27,-73 15,-76Z",
                "#35353E", CrowInk, 2.4);
            P(dc, "M10,-62 Q22,-54 26,-54 Q25,-36 15,-26 Q8,-21 0,-22 Q14,-34 10,-62Z", "#41424C");
            P(dc, "M-8,-66 Q-25,-65 -34,-47 Q-42,-36 -49,-33 Q-40,-32 -32,-35 L-38,-30 Q-29,-31 -23,-34 L-27,-30 C-5,-35 9,-49 7,-59 Q6,-66 -8,-66Z",
                "#454958", CrowInk, 2.2);
            P(dc, "M-26,-49 Q-13,-62 0,-60 M-32,-41 Q-20,-49 -14,-50 M-27,-39 Q-15,-44 -9,-50",
                null, "#646878", 1.65);
        }

        private static void DrawCrowHead(DrawingContext dc)
        {
            P(dc, "M-12,-57 Q-9,-65 -12,-71 C-14,-84 -5,-96 9,-98 C22,-100 31,-93 32,-84 L33,-76 Q33,-68 27,-60 L29,-54 L22,-57 L23,-51 L16,-56 L16,-51 Q9,-58 2,-60 Q-5,-61 -12,-57Z",
                "#32323B", CrowInk, 2.45);
            P(dc, "M-7,-78 Q-4,-94 12,-95 Q21,-96 26,-89 Q12,-93 4,-88 Q-3,-84 -7,-78Z", "#4C4D59");
            P(dc, "M29,-84 Q43,-85 58,-72 Q43,-74 30,-72 L25,-75Z", "#54555F", CrowInk, 2.0);
            P(dc, "M30,-73 L56,-72 Q43,-68 31,-69 L26,-73Z", "#373740", CrowInk, 1.55);
            E(dc, 19, -82, 4.5, 4.5, "#BDB8AA", CrowInk, 1.6);
            E(dc, 19.9, -81.9, 2.8, 2.9, "#24232B");
            E(dc, 19.2, -83.2, 1.1, 1.1, "#FFFFED");
        }
    }
}

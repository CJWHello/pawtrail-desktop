using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PawTrail
{
    public sealed class KeyCap
    {
        public readonly int Id;
        public readonly string Label;
        public readonly double X, Y, W, H;

        internal KeyCap(int id, string label, double x, double y, double w, double h)
        {
            Id = id;
            Label = label;
            X = x;
            Y = y;
            W = w;
            H = h;
        }
    }

    public static class KeyboardLayout
    {
        // Physical set-1 scan codes. 0x100 denotes E0, so keypad Enter,
        // right-hand modifiers and navigation keys have distinct positions.
        private static readonly Dictionary<int, int> scanToId = new Dictionary<int, int>();
        public static readonly KeyCap[] Keys;
        public static int Count { get { return Keys.Length; } }

        static KeyboardLayout()
        {
            var caps = new List<KeyCap>();
            Add(caps, 0x01, "Esc", 0, 0);
            Row(caps, 0x3B, 2, 0, "F1 F2 F3 F4");
            Row(caps, 0x3F, 6.5, 0, "F5 F6 F7 F8");
            Row(caps, 0x43, 11, 0, "F9 F10");
            Row(caps, 0x57, 13, 0, "F11 F12");
            Add(caps, 0x137, "PrtSc", 15.25, 0);
            Add(caps, 0x46, "ScrLk", 16.25, 0);
            Add(caps, 0x245, "Pause", 17.25, 0);

            const double top = 1.25;
            Add(caps, 0x29, "`", 0, top);
            Row(caps, 0x02, 1, top, "1 2 3 4 5 6 7 8 9 0 - =");
            Add(caps, 0x0E, "Backspace", 13, top, 2);

            Add(caps, 0x0F, "Tab", 0, top + 1, 1.5);
            Row(caps, 0x10, 1.5, top + 1, "Q W E R T Y U I O P [ ]");
            Add(caps, 0x2B, "\\", 13.5, top + 1, 1.5);
            Add(caps, 0x3A, "Caps", 0, top + 2, 1.75);
            Row(caps, 0x1E, 1.75, top + 2, "A S D F G H J K L ; '");
            Add(caps, 0x1C, "Enter", 12.75, top + 2, 2.25);
            Add(caps, 0x2A, "Shift", 0, top + 3, 1.25);
            Add(caps, 0x56, "< >", 1.25, top + 3);
            Row(caps, 0x2C, 2.25, top + 3, "Z X C V B N M , . /");
            Add(caps, 0x36, "Shift", 12.25, top + 3, 2.75);
            Add(caps, 0x1D, "Ctrl", 0, top + 4, 1.25);
            Add(caps, 0x15B, "Win", 1.25, top + 4, 1.25);
            Add(caps, 0x38, "Alt", 2.5, top + 4, 1.25);
            Add(caps, 0x39, "Space", 3.75, top + 4, 6.25);
            Add(caps, 0x138, "Alt", 10, top + 4, 1.25);
            Add(caps, 0x15C, "Win", 11.25, top + 4, 1.25);
            Add(caps, 0x15D, "Menu", 12.5, top + 4, 1.25);
            Add(caps, 0x11D, "Ctrl", 13.75, top + 4, 1.25);

            Add(caps, 0x152, "Ins", 15.25, top);
            Add(caps, 0x147, "Home", 16.25, top);
            Add(caps, 0x149, "PgUp", 17.25, top);
            Add(caps, 0x153, "Del", 15.25, top + 1);
            Add(caps, 0x14F, "End", 16.25, top + 1);
            Add(caps, 0x151, "PgDn", 17.25, top + 1);
            Add(caps, 0x148, "↑", 16.25, top + 3);
            Add(caps, 0x14B, "←", 15.25, top + 4);
            Add(caps, 0x150, "↓", 16.25, top + 4);
            Add(caps, 0x14D, "→", 17.25, top + 4);

            const double pad = 18.5;
            Add(caps, 0x45, "Num", pad, top);
            Add(caps, 0x135, "/", pad + 1, top);
            Add(caps, 0x37, "*", pad + 2, top);
            Add(caps, 0x4A, "-", pad + 3, top);
            Row(caps, 0x47, pad, top + 1, "7 8 9");
            Add(caps, 0x4E, "+", pad + 3, top + 1, 1, 2);
            Row(caps, 0x4B, pad, top + 2, "4 5 6");
            Row(caps, 0x4F, pad, top + 3, "1 2 3");
            Add(caps, 0x11C, "Enter", pad + 3, top + 3, 1, 2);
            Add(caps, 0x52, "0", pad, top + 4, 2);
            Add(caps, 0x53, ".", pad + 2, top + 4);
            Keys = caps.ToArray();
        }

        private static void Add(List<KeyCap> caps, int scan, string label,
            double x, double y, double width = 1, double height = 1)
        {
            scanToId.Add(scan, caps.Count);
            caps.Add(new KeyCap(caps.Count, label, x, y, width, height));
        }

        private static void Row(List<KeyCap> caps, int firstScan, double x, double y, string labels)
        {
            string[] keys = labels.Split(' ');
            for (int i = 0; i < keys.Length; i++) Add(caps, firstScan + i, keys[i], x + i, y);
        }

        public static KeyCap Get(int id)
        {
            return id >= 0 && id < Keys.Length ? Keys[id] : null;
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);

        public static int Resolve(int scanCode, int rawFlags, int virtualKey)
        {
            if (virtualKey == 0xFF || scanCode == 0xFF) return -1;
            int scan = scanCode & 0x7F;
            bool extended = (rawFlags & 2) != 0;
            // Pause and Num Lock share a make code on PC keyboards. Windows
            // also uses VK_CANCEL for Ctrl+Break (the physical Pause key).
            if (virtualKey == 0x13 || virtualKey == 0x03) scan = 0x245;
            else if (virtualKey == 0x90) scan = 0x45;
            else if (virtualKey == 0x2C) scan = 0x137;
            else
            {
                if (scan == 0)
                {
                    uint mapped = MapVirtualKey((uint)virtualKey, 4);
                    scan = (int)(mapped & 0x7F);
                    extended |= (mapped & 0xFF00) == 0xE000;
                }
                if (extended) scan |= 0x100;
            }
            int id;
            return scanToId.TryGetValue(scan, out id) ? id : -1;
        }
    }
}

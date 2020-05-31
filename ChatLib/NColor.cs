using System;

namespace ChatLib.Extras
{
    [Serializable]
    public struct NColor
    {
        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }

        private NColor(int r, int g, int b)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
        }
    
        public static NColor FromRGB(int r, int g, int b)
        {
            return new NColor(r, g, b);
        }

        public static NColor Blue
        {
            get
            {
                return new NColor(0, 0, 255);
            }
        }

        public static NColor Green
        {
            get
            {
                return new NColor(0, 255, 0);
            }
        }

        public static NColor Red
        {
            get
            {
                return new NColor(255, 0, 0);
            }
        }
    }
}

using System.Windows;
using System.Windows.Forms;

namespace Agentstrap.UI.Utility
{
    public static class WindowScaling
    {
        public static double ScaleFactor
        {
            get
            {
                var primaryScreen = Screen.PrimaryScreen;
                var wpfScreenWidth = SystemParameters.PrimaryScreenWidth;

                if (primaryScreen is null || wpfScreenWidth <= 0)
                    return 1.0;

                return primaryScreen.Bounds.Width / wpfScreenWidth;
            }
        }

        public static int GetScaledNumber(int number)
        {
            return (int)Math.Ceiling(number * ScaleFactor);
        }

        public static System.Drawing.Size GetScaledSize(System.Drawing.Size size)
        {
            return new System.Drawing.Size(GetScaledNumber(size.Width), GetScaledNumber(size.Height));
        }

        public static System.Drawing.Point GetScaledPoint(System.Drawing.Point point)
        {
            return new System.Drawing.Point(GetScaledNumber(point.X), GetScaledNumber(point.Y));
        }

        public static Padding GetScaledPadding(Padding padding)
        {
            return new Padding(GetScaledNumber(padding.Left), GetScaledNumber(padding.Top), GetScaledNumber(padding.Right), GetScaledNumber(padding.Bottom));
        }
    }
}


using System.Text;
using static System.Console;

namespace DoxyTags
{
    internal static class ConsoleProgress
    {
        private static double m_percent;

        public static double Percent
        {
            get => m_percent;
            set
            {
                m_percent = value;
                DrawProgress();
            }
        }

        private static void DrawProgress()
        {
            CursorLeft = 1;

            double percentInBlocks = (WindowWidth - 2) * Percent;
            int fullBlockCount = (int)percentInBlocks;

            Write(new string('\x2588', fullBlockCount));

            double fractionnalBlock = percentInBlocks - fullBlockCount;

            switch (OutputEncoding)
            {
                case UnicodeEncoding _:
                    int quarterBlock = (int)(fractionnalBlock * 8 + .5d);
                    if (quarterBlock > 0) Write('\x2588' + 8 -quarterBlock);
                    break;
                default:
                    if (fractionnalBlock >= .5d) Write('\x2588');
                    break;
            }
        }
    }
}

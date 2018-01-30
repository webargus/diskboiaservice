using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskBoiaService
{
    class PrinterTexter
    {
        private static int x0 = 5;
        private float x = x0;
        private float y = 5;
        private const float width = 270.0F; // max width I found through trial and error
        private const float height = 0F; 

        private Graphics graphics;

        private Font drawFont12Bold;
        private Font drawFont10Regular;
        private Font drawFont10Bold;
        private SolidBrush drawBrush;

        private static StringFormat drawFormatCenter = new StringFormat();
        private static StringFormat drawFormatLeft = new StringFormat();
        private static StringFormat drawFormatRight = new StringFormat();

        public PrinterTexter(Graphics graphics, string fontFamily = "Arial Narrow")
        {
            this.graphics = graphics;
            drawBrush = new SolidBrush(Color.Black);
            SetFontFamily(fontFamily);
            // Set format of string.
            drawFormatCenter.Alignment = StringAlignment.Center;
            drawFormatLeft.Alignment = StringAlignment.Near;
            drawFormatRight.Alignment = StringAlignment.Far;
        }

        public void SetFontFamily(string fontFamily)
        {
            drawFont12Bold = new Font(fontFamily, 12, FontStyle.Bold);
            drawFont10Regular = new Font(fontFamily, 10, FontStyle.Regular);
            drawFont10Bold = new Font(fontFamily, 10, FontStyle.Bold);
        }

        public float GetTextHeight(string text, Font font)
        {
            return graphics.MeasureString(text, font).Height;
        }

        public float GetTextWidth(string text, Font font)
        {
            return graphics.MeasureString(text, font).Width;
        }

        public void PrintTextSln(string text)
        {
            PrintText(text, drawFont10Regular, drawFormatLeft);
        }

        public void PrintTextSlnBold(string text)
        {

        }

        private void PrintText(string text, Font font, StringFormat format)
        {
            float h = font.GetHeight();
            graphics.DrawString(text, font, drawBrush, new RectangleF(x, y, width, h), format);
            float w = GetTextWidth(text, font);
            float nlines =  w / h;
            x += ((int)nlines) * h - w;
        }

        private void PrintTextLn(string text, Font font, StringFormat format)
        {
            PrintText(text, font, format);
            NewLine(text, font);
        }

        private void NewLine(string text, Font font)
        {
            x = x0;
            float nlines = GetTextWidth(text, font) / font.GetHeight();
            y += nlines * GetTextHeight(text, font);
        }

    }
}

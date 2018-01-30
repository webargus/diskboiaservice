using System;
using System.IO;
using System.ServiceProcess;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;

using Newtonsoft.Json;

namespace DiskBoiaService
{
    public partial class DiskBoiaService : ServiceBase
    {

        private System.Timers.Timer timeDelay;
        private static string printerName = @"EPSON";
        private static string streamURL = "http://diskboia.com.br/printservice.php";
        private static string logFile = @"C:\\DiskBoia\\diskboiaservice.log";
        private static int delay = 3000;
        private static System.Net.WebClient wc = new System.Net.WebClient();
        private static int maxFail = 5;
        private static int failCnt = 0;

        public DiskBoiaService()
        {
            InitializeComponent();
            timeDelay = new System.Timers.Timer(delay);
            timeDelay.AutoReset = true;
            timeDelay.Elapsed += new System.Timers.ElapsedEventHandler(PrintURLStream);
        }

        public void PrintURLStream(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (wc.IsBusy)
            {
                System.Threading.Thread.Sleep(50);
                return;
            }
            string method = nameof(PrintURLStream);
            // fetch content to print from URL
            string contents = "";
            try
            {
               contents = wc.DownloadString(streamURL);
            } catch (Exception ex){
                LogService(method + " failed: " + ex.ToString());
                return;
            }
            // if url fetch returned some string, try to print it
            if (contents.Length > 0)
            {
                if(PrintJsonObj(contents))
                {
                    //LogService("ok");
                    failCnt = 0;
                }
                else
                {
                    LogService("failed");
                    failCnt++;
                    if (failCnt >= maxFail)
                    {
                        LogService("Fail count: " + failCnt.ToString());
                        //this.Stop();
                    }
                }
            }
            /*
             * comment out after debugging!!!
             */
            //this.Stop();
        }

        private bool PrintJsonObj(string contents)
        {
            //LogService(contents + "\n");
            string method = nameof(JsonConvert.DeserializeObject);
            DBOrder[] objs;
            try
            {
                objs =JsonConvert.DeserializeObject<DBOrder[]>(contents);
            } catch (JsonSerializationException ex)
            {
                LogService(ex.Message + " in " + method);
                return false;
            } catch (JsonException ex) {
                LogService(ex.Message + " in " + method);
                return false;
            } catch (Exception ex)
            {
                LogService(ex.Message + " in " + method);
                return false;
            }
            LogService("Count=" + objs.Length);
            method = nameof(PrintReceiptForTransaction);
            try
            {
                foreach (DBOrder order in objs)
                {
                    PrintReceiptForTransaction(order);
                }
            } catch (ApplicationException ex)
            {
                LogService(ex.Message + " in " + method);
                return false;
            }
            catch (Exception ex)
            {
                LogService(ex.Message + " in " + method);
                return false;
            }

            return true;
        }

        private static void PrintReceiptForTransaction(DBOrder order)
        {
            LogService(order.clientName + Environment.NewLine +
                       order.orderXtraDesc + Environment.NewLine +
                       order.clientPOS + Environment.NewLine +
                       order.clientStore + Environment.NewLine);
            DiskBoiaDocument recordDoc = new DiskBoiaDocument(order);

            recordDoc.DocumentName = "Pedido DiskBoia #" + order.orderId;
            recordDoc.PrintPage += new PrintPageEventHandler(PrintReceiptPage); // function below
            recordDoc.PrintController = new StandardPrintController(); // hides status dialog popup
                                                                       // Comment if debugging 
            PrinterSettings ps = new PrinterSettings();
            ps.PrinterName = printerName;
            recordDoc.PrinterSettings = ps;
            recordDoc.Print();
            // --------------------------------------

            // Uncomment if debugging - shows dialog instead
            /*PrintPreviewDialog printPrvDlg = new PrintPreviewDialog();
            printPrvDlg.Document = recordDoc;
            printPrvDlg.Width = 1200;
            printPrvDlg.Height = 800;
            printPrvDlg.ShowDialog();*/
            // --------------------------------------

            recordDoc.Dispose();
        }

        private static void PrintReceiptPage(object sender, PrintPageEventArgs e)
        {
            DBOrder order = ((DiskBoiaDocument)sender).order;
            float x = 5;
            float y = 5;
            float width = 270.0F; // max width I found through trial and error
            float height = 0F;

            Font drawFontArial14Bold    = new Font("Arial Narrow", 14, FontStyle.Bold);
            Font drawFontArial16Bold    = new Font("Arial Narrow", 16, FontStyle.Bold);
            Font drawFontArial10Regular = new Font("Arial Narrow", 10, FontStyle.Regular);
            Font drawFontArial10Bold    = new Font("Arial Narrow", 10, FontStyle.Bold);
            Font drawFontArial10Italic  = new Font("Arial Narrow", 10, FontStyle.Italic);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            // Set format of string.
            StringFormat drawFormatCenter = new StringFormat();
            drawFormatCenter.Alignment = StringAlignment.Center;
            StringFormat drawFormatLeft = new StringFormat();
            drawFormatLeft.Alignment = StringAlignment.Near;
            StringFormat drawFormatRight = new StringFormat();
            drawFormatRight.Alignment = StringAlignment.Far;

            // Draw Header to printer.
            string text = ".:: DiskBoia ::.";
            e.Graphics.DrawString(text, drawFontArial16Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatCenter);
            y += e.Graphics.MeasureString(text, drawFontArial16Bold).Height;

            // Draw separation line
            x = 5;
            text = new String('=', 40);
            e.Graphics.DrawString(text,
                                  drawFontArial10Bold,
                                  drawBrush,
                                  new RectangleF(x, y, width + 14, height),
                                  drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;

            // Draw order no. + timestamp + order taker
            text = "Pedido #" + order.orderId + ":";
            e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            x += e.Graphics.MeasureString(text, drawFontArial10Bold).Width;

            text = order.orderStamp + " - " + order.orderTaker;
            e.Graphics.DrawString(text, drawFontArial10Regular, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Regular).Height;

            // Draw client name
            x = 5;
            text = "Cliente: ";
            e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            x += e.Graphics.MeasureString(text, drawFontArial10Bold).Width;

            text = order.clientName;
            e.Graphics.DrawString(text, drawFontArial10Regular, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Regular).Height;

            // Draw Delivery location
            x = 5;
            text = "Entrega: ";
            e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            x += e.Graphics.MeasureString(text, drawFontArial10Bold).Width;

            text = order.deliveryPOS + "@" + order.deliveryStore;
            e.Graphics.DrawString(text, drawFontArial10Regular, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Regular).Height;

            // Draw separation line
            x = 5;
            text = new String('=', 40);
            e.Graphics.DrawString(text,
                                  drawFontArial10Bold,
                                  drawBrush,
                                  new RectangleF(x, y, width + 12, height),
                                  drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;

            // Draw main dish price list with side dishes
            x = 5;
            decimal total = 0;
            decimal discount = 0;
            foreach (Dish dish in order.dishes)
            {
                total += Convert.ToDecimal(dish.dishVal, CultureInfo.InvariantCulture);
                // Draw main dish + main dish price
                e.Graphics.DrawString(dish.itemAbbr, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
                text = "R$ " + dish.dishVal.Replace(".", ",");
                e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatRight);
                y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;
                // Draw side dishes
                x = 10;
                foreach(sideDish side in dish.orderSides)
                {
                    discount += Convert.ToDecimal(side.sideVal, CultureInfo.InvariantCulture);
                    // draw side dish abbr + side dish value
                    e.Graphics.DrawString(side.sideAbbr,
                                          drawFontArial10Italic,
                                          drawBrush,
                                          new RectangleF(x, y, width, height),
                                          drawFormatLeft);
                    text = "R$ " + side.sideVal.Replace(".", ",");
                    e.Graphics.DrawString(text,
                                          drawFontArial10Italic,
                                          drawBrush,
                                          // subtract 5 from rect width to align R$ values vertically
                                          new RectangleF(x, y, width - 5, height),
                                          drawFormatRight);
                    y += e.Graphics.MeasureString(text, drawFontArial10Italic).Height;
                }
                // reset horz. position
                x = 5;
            }
            // Draw extra, if any
            decimal xtra = Convert.ToDecimal(order.orderXtraVal, CultureInfo.InvariantCulture);
            if(xtra > 0)
            {
                total += xtra;
                text = "*Extra: ";
                e.Graphics.DrawString(text,
                                      drawFontArial10Bold,
                                      drawBrush,
                                      new RectangleF(x, y, width, height),
                                      drawFormatLeft);
                x += e.Graphics.MeasureString(text, drawFontArial10Bold).Width;
                e.Graphics.DrawString(order.orderXtraDesc,
                                      drawFontArial10Regular,
                                      drawBrush,
                                      new RectangleF(x, y, width, height),
                                      drawFormatLeft);
                x = 5;
                text = "R$ " + order.orderXtraVal.ToString().Replace(".", ",");
                e.Graphics.DrawString(text,
                                      drawFontArial10Bold,
                                      drawBrush,
                                      new RectangleF(x, y, width, height),
                                      drawFormatRight);
                y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;
            }
            // draw subtotal
            text = "SubTotal: ";
            e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            text = "R$ " + (total + discount).ToString().Replace(".", ",");
            e.Graphics.DrawString(text, drawFontArial10Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatRight);
            y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;
            // draw discount, if > 0:
            if (discount > 0)
            {
                text = "Desconto: ";
                e.Graphics.DrawString(text, drawFontArial10Italic, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
                text = "R$ " + discount.ToString().Replace(".", ",");
                e.Graphics.DrawString(text, drawFontArial10Italic, drawBrush, new RectangleF(x, y, width, height), drawFormatRight);
                y += e.Graphics.MeasureString(text, drawFontArial10Italic).Height;
            }
            // Draw Total:
            text = "Total: ";
            e.Graphics.DrawString(text, drawFontArial14Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatLeft);
            text = "R$ " + total.ToString().Replace(".", ",");
            e.Graphics.DrawString(text, drawFontArial14Bold, drawBrush, new RectangleF(x, y, width, height), drawFormatRight);
            y += e.Graphics.MeasureString(text, drawFontArial10Regular).Height;

            // Draw separation line
            x = 5;
            text = new String('=', 40);
            e.Graphics.DrawString(text,
                                  drawFontArial10Bold,
                                  drawBrush,
                                  new RectangleF(x, y, width + 10, height),
                                  drawFormatLeft);
            y += e.Graphics.MeasureString(text, drawFontArial10Bold).Height;

            // Draw extra footnote, if any
            if (xtra > 0)
            {
                text = "*Item fora do cardápio";
                e.Graphics.DrawString(text,
                                      drawFontArial10Italic,
                                      drawBrush,
                                      new RectangleF(x, y, width, height),
                                      drawFormatLeft);
                y += e.Graphics.MeasureString(text, drawFontArial10Regular).Height;

            }
            // Draw space below for cutting

            // ... and so on
        }
        protected override void OnStart(string[] args)
        {
            LogService("Service Started");
            timeDelay.Enabled = true;
        }

        protected override void OnStop()
        {
            LogService("Service Stopped");
            timeDelay.Enabled = false;
        }

        private static void LogService(string content)
        {
            FileStream fs = new FileStream(logFile, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            DateTime t = DateTime.Now;
            sw.WriteLine(t.ToShortDateString() + " - " + t.ToLongTimeString() + " " + content);
            sw.Flush();
            sw.Close();
        }

        public class sideDish
        {
            public string sideDesc;
            public string sideAbbr;
            public string sideVal;
        }

        public class Dish
        {
            public string itemDesc;
            public string itemAbbr;
            public string dishVal;
            public sideDish[] orderSides;
        }

        public class DBOrder
        {
            public string orderId;
            public string orderStamp;
            public string orderTaker;
            public string orderXtraDesc;
            public string orderXtraVal;
            public string clientName;
            public string clientPOS;
            public string clientStore;
            public string deliveryPOS;
            public string deliveryStore;
            public Dish[] dishes;
        }

        public class DiskBoiaDocument : PrintDocument
        {
            public DBOrder order;
            public DiskBoiaDocument(DBOrder order)
            {
                this.order = order;
            }
        }
    }
}

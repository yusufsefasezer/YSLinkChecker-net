using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Windows;
using HtmlAgilityPack;
using Microsoft.Win32;
using System.Net;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace YSLinkChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HtmlDocument PageDocument = null;
        List<YSLink> AllLinks = null;
        List<YSLink> InternalLinks = null;
        List<YSLink> ExternalLinks = null;

        public MainWindow()
        {
            string currentCulture = Thread.CurrentThread.CurrentCulture.Name;
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(currentCulture);
            InitializeComponent();
            btnCheck.Click += BtnCheck_Click;
            btnClear.Click += BtnClear_Click;
            btnTotal.Click += BtnTotal_Click;
            btnInternal.Click += BtnInternal_Click;
            btnExternal.Click += BtnExternal_Click;
            btnPDF.Click += BtnPDF_Click;
            btnTXT.Click += BtnTXT_Click;
            btnCSV.Click += BtnCSV_Click;
            itemStatus.Content = Properties.Resources.msg_load;
        }

        // <!-- Events
        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var htmlWeb = new HtmlWeb()
                {
                    UserAgent = "YSLinkChecker (www.yusufsezer.com)",
                };
                PageDocument = htmlWeb.Load(txtAddress.Text);
                ParseAllLinks();
                ShowAllLinks();
                SetGUI(true);
                itemStatus.Content = Properties.Resources.msg_listed;
            }
            catch (Exception ex)
            {
                itemStatus.Content = ex.Message;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            SetGUI(false);
            dgLinks.ItemsSource = null;
        }

        private void BtnTotal_Click(object sender, RoutedEventArgs e)
        {
            ShowAllLinks();
            btnTotal.IsEnabled = false;
            btnInternal.IsEnabled = true;
            btnExternal.IsEnabled = true;
        }

        private void BtnInternal_Click(object sender, RoutedEventArgs e)
        {
            ShowInternalLinks();
            btnTotal.IsEnabled = true;
            btnInternal.IsEnabled = false;
            btnExternal.IsEnabled = true;
        }

        private void BtnExternal_Click(object sender, RoutedEventArgs e)
        {
            ShowExternalLinks();
            btnTotal.IsEnabled = true;
            btnInternal.IsEnabled = true;
            btnExternal.IsEnabled = false;
        }

        private void BtnPDF_Click(object sender, RoutedEventArgs e)
        {

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF (*.pdf)|*.pdf";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PreparePDFData(saveFileDialog.FileName);
                    itemStatus.Content = Properties.Resources.msg_pdf;
                }
                catch (Exception ex)
                {
                    itemStatus.Content = ex.Message;
                }
            }
        }

        private void BtnTXT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepareExportData(false);
                itemStatus.Content = Properties.Resources.msg_txt;
            }
            catch (Exception ex)
            {
                itemStatus.Content = ex.Message;
            }
        }

        private void BtnCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrepareExportData(true);
                itemStatus.Content = Properties.Resources.msg_csv;
            }
            catch (Exception ex)
            {
                itemStatus.Content = ex.Message;
            }
        }
        // Events --!>

        // <!-- Methods
        private void SetGUI(bool opt)
        {
            btnClear.IsEnabled = opt;
            YSBody.IsEnabled = opt;
            YSFooter.IsEnabled = opt;
            btnCheck.IsEnabled = !opt;
            txtAddress.IsEnabled = !opt;
            btnTotalCount.Visibility = opt ? Visibility.Visible : Visibility.Hidden;
            btnTotal.IsEnabled = !opt;
            btnInternalCount.Visibility = opt ? Visibility.Visible : Visibility.Hidden;
            btnInternal.IsEnabled = opt;
            btnExternalCount.Visibility = opt ? Visibility.Visible : Visibility.Hidden;
            btnExternal.IsEnabled = opt;
            itemStatus.Content = null;
        }

        private void ParseAllLinks()
        {
            var htmlNodeCollection = PageDocument.DocumentNode.SelectNodes("//a");

            if (htmlNodeCollection == null) throw new Exception(Properties.Resources.msg_address);

            AllLinks = (from pageLinks in htmlNodeCollection
                        where pageLinks.Name == "a"
                        && pageLinks.Attributes["href"] != null
                        && pageLinks.Attributes["href"].Value.Trim().Length > 0
                        select new YSLink
                        {
                            URL = pageLinks.Attributes["href"].Value.Trim(),
                            Text = WebUtility.HtmlDecode(pageLinks.InnerText.Trim()),
                            External = pageLinks.Attributes["href"].Value.StartsWith(txtAddress.Text) || pageLinks.Attributes["href"].Value.StartsWith("#") || pageLinks.Attributes["href"].Value.StartsWith("/")
                        }).ToList();
            btnTotalCount.Text = " (" + AllLinks.Count + ")";

            InternalLinks = (from InternalLink in AllLinks
                             where InternalLink.External == true
                             select InternalLink).ToList();
            btnInternalCount.Text = " (" + InternalLinks.Count + ")";

            ExternalLinks = (from InternalLink in AllLinks
                             where InternalLink.External == false
                             select InternalLink).ToList();
            btnExternalCount.Text = " (" + ExternalLinks.Count + ")";

        }

        private void ShowAllLinks()
        {
            dgLinks.ItemsSource = AllLinks;
        }

        private void ShowInternalLinks()
        {
            dgLinks.ItemsSource = InternalLinks;
        }

        private void ShowExternalLinks()
        {
            dgLinks.ItemsSource = ExternalLinks;
        }

        private void PreparePDFData(string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // PDF Document
                var document = new Document(PageSize.A4, 0, 0, 18, 0);
                document.AddTitle("YSLinkChecker");
                document.AddSubject("YSLinkChecker");
                document.AddCreator("YSLinkChecker");
                document.AddAuthor("Yusuf SEZER (www.yusufsezer.com)");

                // PDF Font
                var fontPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts) + @"\arial.ttf";
                var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var fontHeader = new Font(baseFont, 18, Font.BOLD);
                var fontCell = new Font(baseFont, 10, Font.NORMAL);

                // PDF Writer
                var pdfWriter = PdfWriter.GetInstance(document, fileStream);

                // PDF Table
                document.Open();
                var pdfPTable = new PdfPTable(3);
                pdfPTable.SetWidths(new float[] { 8f, 40f, 100f });

                // PDF Header
                var pdfPCell = new PdfPCell(new Phrase("YSLinkChecker", fontHeader));
                pdfPCell.Padding = 5;
                pdfPCell.Colspan = 3;
                pdfPCell.HorizontalAlignment = 1;
                pdfPTable.AddCell(pdfPCell);

                var pdfPCellAllLink = new PdfPCell(new Phrase(Properties.Resources.btnTotal + ": " + AllLinks.Count, fontCell));
                pdfPCellAllLink.Padding = 5;
                pdfPCellAllLink.Colspan = 3;
                pdfPTable.AddCell(pdfPCellAllLink);

                var pdfPCellInternal = new PdfPCell(new Phrase(Properties.Resources.btnTotal + ": " + InternalLinks.Count, fontCell));
                pdfPCellInternal.Padding = 5;
                pdfPCellInternal.Colspan = 3;
                pdfPTable.AddCell(pdfPCellInternal);

                var pdfPCellExternal = new PdfPCell(new Phrase(Properties.Resources.btnTotal + ": " + ExternalLinks.Count, fontCell));
                pdfPCellExternal.Padding = 5;
                pdfPCellExternal.Colspan = 3;
                pdfPTable.AddCell(pdfPCellExternal);

                // PDF Content
                int i = 0;
                foreach (YSLink link in AllLinks)
                {
                    pdfPTable.AddCell(new Phrase((++i).ToString(), fontCell));
                    pdfPTable.AddCell(new Phrase(link.Text, fontCell));
                    pdfPTable.AddCell(new Phrase(link.URL, fontCell));
                }
                document.Add(pdfPTable);

                document.Close();
            }
        }

        private void PrepareExportData(bool isCSV)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Properties.Resources.btnTotal + ": " + AllLinks.Count);
            stringBuilder.AppendLine(Properties.Resources.btnInternal + ": " + InternalLinks.Count);
            stringBuilder.AppendLine(Properties.Resources.btnExternal + ": " + ExternalLinks.Count);
            if (isCSV)
            {
                stringBuilder.AppendLine("\"" + Properties.Resources.dgText + "\"; " + Properties.Resources.dgURL);
                foreach (YSLink link in AllLinks)
                {
                    stringBuilder.AppendLine("\"" + link.Text + "\"; " + link.URL);
                }
            }
            else
            {
                foreach (YSLink link in AllLinks)
                {
                    stringBuilder.AppendLine(link.URL);
                }
            }
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = isCSV ? "CSV (*.csv)|*.csv" : "TXT (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, stringBuilder.ToString(), Encoding.UTF8);
            }
        }

        // Methods --!>
    }
}

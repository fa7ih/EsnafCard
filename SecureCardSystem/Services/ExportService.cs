using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using SecureCardSystem.Models;

namespace SecureCardSystem.Services
{
    public class ExportService
    {
        private string GetTransactionTypeInTurkish(string transactionType)
        {
            return transactionType switch
            {
                "Payment" => "Ödeme",
                "BalanceUpdate" => "Bakiye Güncelleme",
                _ => transactionType
            };
        }

        public byte[] ExportCardsToExcel(List<Card> cards)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Kartlar");

            // Headers
            worksheet.Cell(1, 1).Value = "Kart No";
            worksheet.Cell(1, 2).Value = "Bakiye";
            worksheet.Cell(1, 3).Value = "İlk Bakiye";
            worksheet.Cell(1, 4).Value = "Durum";
            worksheet.Cell(1, 5).Value = "Oluşturulma Tarihi";
            worksheet.Cell(1, 6).Value = "Oluşturan";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data
            int row = 2;
            foreach (var card in cards)
            {
                worksheet.Cell(row, 1).Value = card.CardNumber;
                worksheet.Cell(row, 2).Value = card.Balance;
                worksheet.Cell(row, 3).Value = card.InitialBalance;
                worksheet.Cell(row, 4).Value = card.IsActive ? "Aktif" : "Pasif";
                worksheet.Cell(row, 5).Value = card.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 6).Value = card.CreatedBy;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportCardNumbersOnlyToExcel(List<Card> cards)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Kart Numaraları");

            // Header
            worksheet.Cell(1, 1).Value = "Kart Numarası";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data - only card numbers
            int row = 2;
            foreach (var card in cards)
            {
                worksheet.Cell(row, 1).Value = card.CardNumber;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportCardNumbersOnlyToPdf(List<Card> cards)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Title
            var title = new Paragraph("Kart Numaraları")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(title);
            document.Add(new Paragraph("\n"));

            // Table
            var table = new Table(1).UseAllAvailableWidth();

            // Header
            table.AddHeaderCell(CreateCell("Kart Numarası", true));

            // Data - only card numbers
            foreach (var card in cards)
            {
                table.AddCell(CreateCell(card.CardNumber, false));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }

        public byte[] ExportTransactionsToExcel(List<Transaction> transactions)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("İşlemler");

            // Headers
            worksheet.Cell(1, 1).Value = "Kart No";
            worksheet.Cell(1, 2).Value = "Tutar";
            worksheet.Cell(1, 3).Value = "Önceki Bakiye";
            worksheet.Cell(1, 4).Value = "Sonraki Bakiye";
            worksheet.Cell(1, 5).Value = "İşlem Tipi";
            worksheet.Cell(1, 6).Value = "Tarih";
            worksheet.Cell(1, 7).Value = "İşlemi Yapan";
            worksheet.Cell(1, 8).Value = "IP Adresi";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // Data
            int row = 2;
            foreach (var transaction in transactions)
            {
                worksheet.Cell(row, 1).Value = transaction.CardNumber;
                worksheet.Cell(row, 2).Value = transaction.Amount;
                worksheet.Cell(row, 3).Value = transaction.BalanceBefore;
                worksheet.Cell(row, 4).Value = transaction.BalanceAfter;
                worksheet.Cell(row, 5).Value = GetTransactionTypeInTurkish(transaction.TransactionType);
                worksheet.Cell(row, 6).Value = transaction.TransactionDate.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cell(row, 7).Value = transaction.ProcessedBy;
                worksheet.Cell(row, 8).Value = transaction.IpAddress;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportCardsToPdf(List<Card> cards)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Title
            var title = new Paragraph("Kart Listesi")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(title);
            document.Add(new Paragraph("\n"));

            // Table
            var table = new Table(6).UseAllAvailableWidth();

            // Headers
            table.AddHeaderCell(CreateCell("Kart No", true));
            table.AddHeaderCell(CreateCell("Bakiye", true));
            table.AddHeaderCell(CreateCell("İlk Bakiye", true));
            table.AddHeaderCell(CreateCell("Durum", true));
            table.AddHeaderCell(CreateCell("Oluşturulma Tarihi", true));
            table.AddHeaderCell(CreateCell("Oluşturan", true));

            // Data
            foreach (var card in cards)
            {
                table.AddCell(CreateCell(card.CardNumber, false));
                table.AddCell(CreateCell(card.Balance.ToString("C2"), false));
                table.AddCell(CreateCell(card.InitialBalance.ToString("C2"), false));
                table.AddCell(CreateCell(card.IsActive ? "Aktif" : "Pasif", false));
                table.AddCell(CreateCell(card.CreatedAt.ToString("dd.MM.yyyy HH:mm"), false));
                table.AddCell(CreateCell(card.CreatedBy, false));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }

        public byte[] ExportTransactionsToPdf(List<Transaction> transactions)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            // Title
            var title = new Paragraph("İşlem Geçmişi")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(title);
            document.Add(new Paragraph("\n"));

            // Table
            var table = new Table(8).UseAllAvailableWidth();

            // Headers
            table.AddHeaderCell(CreateCell("Kart No", true));
            table.AddHeaderCell(CreateCell("Tutar", true));
            table.AddHeaderCell(CreateCell("Önceki Bakiye", true));
            table.AddHeaderCell(CreateCell("Sonraki Bakiye", true));
            table.AddHeaderCell(CreateCell("İşlem Tipi", true));
            table.AddHeaderCell(CreateCell("Tarih", true));
            table.AddHeaderCell(CreateCell("İşlemi Yapan", true));
            table.AddHeaderCell(CreateCell("IP", true));

            // Data
            foreach (var transaction in transactions)
            {
                table.AddCell(CreateCell(transaction.CardNumber, false));
                table.AddCell(CreateCell(transaction.Amount.ToString("C2"), false));
                table.AddCell(CreateCell(transaction.BalanceBefore.ToString("C2"), false));
                table.AddCell(CreateCell(transaction.BalanceAfter.ToString("C2"), false));
                table.AddCell(CreateCell(GetTransactionTypeInTurkish(transaction.TransactionType), false));
                table.AddCell(CreateCell(transaction.TransactionDate.ToString("dd.MM.yyyy HH:mm:ss"), false));
                table.AddCell(CreateCell(transaction.ProcessedBy, false));
                table.AddCell(CreateCell(transaction.IpAddress, false));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }

        private Cell CreateCell(string text, bool isHeader)
        {
            var cell = new Cell().Add(new Paragraph(text).SetFontSize(isHeader ? 10 : 9));
            
            if (isHeader)
            {
                cell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                cell.SetBold();
                cell.SetTextAlignment(TextAlignment.CENTER);
            }
            
            cell.SetPadding(5);
            return cell;
        }
    }
}

using IronOcr;
using System.Text.RegularExpressions;

namespace SecureCardSystem.Services
{
    public class OcrService
    {
        public async Task<string?> ExtractCardNumberFromImageAsync(IFormFile imageFile)
        {
            try
            {
                // IronOCR kullanımı için örnek kod
                // Not: IronOCR lisans gerektirir, alternatif olarak Tesseract kullanılabilir
                
                // Basit implementasyon - gerçek OCR yerine simüle edilmiş
                // Gerçek uygulamada IronOCR veya Tesseract.NET kullanılmalı
                
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);

                // Simüle edilmiş OCR - Gerçek implementasyonda değiştirilmeli
                var ocr = new IronTesseract();
                using var input = new OcrInput();
                input.LoadImage(memoryStream.ToArray());
                var result = ocr.Read(input);
                var text = result.Text;

                // Şimdilik mock data
                //var text = "Simulated OCR Text 12345678";
                
                // 8 haneli sayı ara
                var match = Regex.Match(text, @"\d{8}");
                if (match.Success)
                {
                    return match.Value;
                }
                
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool ValidateCardNumber(string cardNumber)
        {
            return Regex.IsMatch(cardNumber, @"^\d{8}$");
        }
    }
}

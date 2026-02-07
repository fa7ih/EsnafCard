using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecureCardSystem.Models;
using SecureCardSystem.Services;

namespace SecureCardSystem.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly CardService _cardService;
        private readonly OcrService _ocrService;
        private readonly ExportService _exportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            CardService cardService,
            OcrService ocrService,
            ExportService exportService,
            UserManager<ApplicationUser> userManager)
        {
            _cardService = cardService;
            _ocrService = ocrService;
            _exportService = exportService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            var transactions = await _cardService.GetUserTransactionsAsync(user.Id);

            ViewBag.TotalCards = cards.Count;
            ViewBag.ActiveCards = cards.Count(c => c.IsActive);
            ViewBag.TotalBalance = cards.Sum(c => c.Balance);
            ViewBag.TodayTransactions = transactions.Count(t => t.TransactionDate.Date == DateTime.Today);

            return View();
        }

        public IActionResult CreateCard()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCard(decimal initialBalance)
        {
            if (initialBalance < 0)
            {
                TempData["Error"] = "Bakiye negatif olamaz!";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var card = await _cardService.GenerateCardAsync(initialBalance, user.Id, user.Email ?? "System");

            TempData["Success"] = $"Kart başarıyla oluşturuldu! Kart No: {card.CardNumber}";
            return RedirectToAction(nameof(CardList));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMultipleCards(int count, decimal initialBalance)
        {
            if (count <= 0 || count > 100)
            {
                TempData["Error"] = "Geçersiz kart sayısı! (1-100 arası olmalı)";
                return RedirectToAction(nameof(CreateCard));
            }

            if (initialBalance < 0)
            {
                TempData["Error"] = "Bakiye negatif olamaz!";
                return RedirectToAction(nameof(CreateCard));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await _cardService.GenerateMultipleCardsAsync(count, initialBalance, user.Id, user.Email ?? "System");

            TempData["Success"] = $"{count} adet kart başarıyla oluşturuldu!";
            return RedirectToAction(nameof(CardList));
        }

        public async Task<IActionResult> CardList()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            return View(cards);
        }

        public async Task<IActionResult> UpdateBalance(string cardNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var card = await _cardService.GetCardByNumberAsync(cardNumber, user.Id);
            if (card == null)
            {
                TempData["Error"] = "Kart bulunamadı!";
                return RedirectToAction(nameof(CardList));
            }
            return View(card);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBalance(string cardNumber, decimal newBalance)
        {
            if (newBalance < 0)
            {
                TempData["Error"] = "Bakiye negatif olamaz!";
                return RedirectToAction(nameof(UpdateBalance), new { cardNumber });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _cardService.UpdateCardBalanceAsync(cardNumber, newBalance, user.Id, user.Email ?? "System");

            if (result)
            {
                TempData["Success"] = "Bakiye başarıyla güncellendi!";
                return RedirectToAction(nameof(CardList));
            }

            TempData["Error"] = "Kart bulunamadı!";
            return RedirectToAction(nameof(CardList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCardStatus(string cardNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _cardService.ToggleCardStatusAsync(cardNumber, user.Id, user.Email ?? "System");

            if (result.success)
            {
                TempData["Success"] = result.message;
            }
            else
            {
                TempData["Error"] = result.message;
            }

            return RedirectToAction(nameof(CardList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(string cardNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _cardService.DeleteCardAsync(cardNumber, user.Id, user.Email ?? "System");

            if (result.success)
            {
                TempData["Success"] = result.message;
            }
            else
            {
                TempData["Error"] = result.message;
            }

            return RedirectToAction(nameof(CardList));
        }

        public IActionResult Payment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string cardNumber, decimal amount)
        {
            if (!_ocrService.ValidateCardNumber(cardNumber))
            {
                TempData["Error"] = "Geçersiz kart numarası! 8 haneli olmalı.";
                return View("Payment");
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Tutar 0'dan büyük olmalı!";
                return View("Payment");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var (success, message) = await _cardService.ProcessPaymentAsync(cardNumber, amount, user.Id, user.Email ?? "System");

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return View("Payment");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPaymentWithOcr(IFormFile cardImage, decimal amount)
        {
            if (cardImage == null || cardImage.Length == 0)
            {
                TempData["Error"] = "Lütfen bir resim yükleyin!";
                return View("Payment");
            }

            var cardNumber = await _ocrService.ExtractCardNumberFromImageAsync(cardImage);

            if (string.IsNullOrEmpty(cardNumber))
            {
                TempData["Error"] = "Kart numarası okunamadı! Lütfen manuel giriş yapın.";
                return View("Payment");
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Tutar 0'dan büyük olmalı!";
                return View("Payment");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var (success, message) = await _cardService.ProcessPaymentAsync(cardNumber, amount, user.Id, user.Email ?? "System");

            if (success)
            {
                TempData["Success"] = $"Kart No: {cardNumber} - {message}";
            }
            else
            {
                TempData["Error"] = $"Kart No: {cardNumber} - {message}";
            }

            return View("Payment");
        }

        public async Task<IActionResult> Transactions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var transactions = await _cardService.GetUserTransactionsAsync(user.Id);
            return View(transactions);
        }

        public async Task<IActionResult> CardTransactions(string cardNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var transactions = await _cardService.GetCardTransactionsAsync(cardNumber, user.Id);
            ViewBag.CardNumber = cardNumber;
            return View(transactions);
        }

        public async Task<IActionResult> ExportCardsToExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            var fileBytes = _exportService.ExportCardsToExcel(cards);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Kartlar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportCardsToPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            var fileBytes = _exportService.ExportCardsToPdf(cards);

            return File(fileBytes, "application/pdf",
                $"Kartlar_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        public async Task<IActionResult> ExportCardNumbersOnlyToExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            var fileBytes = _exportService.ExportCardNumbersOnlyToExcel(cards);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Kart_Numaralari_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportCardNumbersOnlyToPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cards = await _cardService.GetUserCardsAsync(user.Id);
            var fileBytes = _exportService.ExportCardNumbersOnlyToPdf(cards);

            return File(fileBytes, "application/pdf",
                $"Kart_Numaralari_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        public async Task<IActionResult> ExportTransactionsToExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var transactions = await _cardService.GetUserTransactionsAsync(user.Id);
            var fileBytes = _exportService.ExportTransactionsToExcel(transactions);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Islemler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportTransactionsToPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var transactions = await _cardService.GetUserTransactionsAsync(user.Id);
            var fileBytes = _exportService.ExportTransactionsToPdf(transactions);

            return File(fileBytes, "application/pdf",
                $"Islemler_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
    }
}
using SecureCardSystem.Data;
using SecureCardSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace SecureCardSystem.Services
{
    public class CardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CardService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Card> GenerateCardAsync(decimal initialBalance, string userId, string createdBy)
        {
            string cardNumber;
            do
            {
                cardNumber = GenerateRandomCardNumber();
            } while (await _context.Cards.AnyAsync(c => c.CardNumber == cardNumber));

            var card = new Card
            {
                CardNumber = cardNumber,
                Balance = initialBalance,
                InitialBalance = initialBalance,
                UserId = userId,
                CreatedBy = createdBy,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            return card;
        }

        public async Task<List<Card>> GenerateMultipleCardsAsync(int count, decimal initialBalance, string userId, string createdBy)
        {
            var cards = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                cards.Add(await GenerateCardAsync(initialBalance, userId, createdBy));
            }
            return cards;
        }

        public async Task<bool> UpdateCardBalanceAsync(string cardNumber, decimal newBalance, string userId, string updatedBy)
        {
            var card = await _context.Cards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.UserId == userId && c.IsActive);

            if (card == null) return false;

            var oldBalance = card.Balance;
            card.Balance = newBalance;

            var transaction = new Transaction
            {
                CardId = card.Id,
                CardNumber = card.CardNumber,
                Amount = newBalance - oldBalance,
                BalanceBefore = oldBalance,
                BalanceAfter = newBalance,
                TransactionType = "BalanceUpdate",
                ProcessedBy = updatedBy,
                IpAddress = GetClientIpAddress(),
                Notes = "Bakiye güncellendi"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Bakiye 0 veya negatif ise kartı sil
            if (newBalance <= 0)
            {
                await AutoDeleteCardAsync(card);
            }

            return true;
        }

        public async Task<(bool success, string message)> ProcessPaymentAsync(string cardNumber, decimal amount, string userId, string processedBy)
        {
            var card = await _context.Cards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.UserId == userId && c.IsActive);

            if (card == null)
                return (false, "Kart bulunamadı veya aktif değil!");

            if (card.Balance < amount)
                return (false, $"Yetersiz bakiye! Mevcut bakiye: {card.Balance:C2}");

            var oldBalance = card.Balance;
            card.Balance -= amount;

            var transaction = new Transaction
            {
                CardId = card.Id,
                CardNumber = card.CardNumber,
                Amount = amount,
                BalanceBefore = oldBalance,
                BalanceAfter = card.Balance,
                TransactionType = "Payment",
                ProcessedBy = processedBy,
                IpAddress = GetClientIpAddress(),
                Notes = "Ödeme alındı"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Bakiye 0 veya negatif ise kartı sil
            if (card.Balance <= 0)
            {
                await AutoDeleteCardAsync(card);
                return (true, $"Ödeme başarılı! Bakiye 0 olduğu için kart otomatik silindi.");
            }

            return (true, $"Ödeme başarılı! Kalan bakiye: {card.Balance:C2}");
        }

        private async Task AutoDeleteCardAsync(Card card)
        {
            // Tüm işlemleri sil
            _context.Transactions.RemoveRange(card.Transactions);

            // Kartı sil
            _context.Cards.Remove(card);

            await _context.SaveChangesAsync();
        }

        public async Task<Card?> GetCardByNumberAsync(string cardNumber, string userId)
        {
            return await _context.Cards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.UserId == userId);
        }

        public async Task<List<Card>> GetUserCardsAsync(string userId)
        {
            return await _context.Cards
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Card>> GetAllCardsAsync()
        {
            return await _context.Cards
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(string userId)
        {
            return await _context.Transactions
                .Include(t => t.Card)
                .Where(t => t.Card.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Card)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetCardTransactionsAsync(string cardNumber, string userId)
        {
            return await _context.Transactions
                .Include(t => t.Card)
                .Where(t => t.CardNumber == cardNumber && t.Card.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        private string GenerateRandomCardNumber()
        {
            var random = new Random();
            return random.Next(10000000, 99999999).ToString();
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }
            return ipAddress ?? "Unknown";
        }

        public async Task<(bool success, string message)> ToggleCardStatusAsync(string cardNumber, string userId, string performedBy)
        {
            var card = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.UserId == userId);

            if (card == null)
            {
                return (false, "Kart bulunamadı!");
            }

            card.IsActive = !card.IsActive;

            var transaction = new Transaction
            {
                CardId = card.Id,
                CardNumber = card.CardNumber,
                TransactionType = card.IsActive ? "Kart Aktif Edildi" : "Kart Pasif Edildi",
                Amount = 0,
                BalanceBefore = card.Balance,
                BalanceAfter = card.Balance,
                ProcessedBy = performedBy,
                IpAddress = GetClientIpAddress(),
                TransactionDate = DateTime.Now,
                Notes = card.IsActive ? "Kart aktif hale getirildi" : "Kart pasif hale getirildi"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            string status = card.IsActive ? "aktif" : "pasif";
            return (true, $"Kart başarıyla {status} edildi!");
        }

        public async Task<(bool success, string message)> DeleteCardAsync(string cardNumber, string userId, string performedBy)
        {
            var card = await _context.Cards
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.UserId == userId);

            if (card == null)
            {
                return (false, "Kart bulunamadı!");
            }

            // İşlem geçmişini sil
            _context.Transactions.RemoveRange(card.Transactions);

            // Kartı sil
            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();

            return (true, $"Kart ({cardNumber}) başarıyla silindi!");
        }
    }
}
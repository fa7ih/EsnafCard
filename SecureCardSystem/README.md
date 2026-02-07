# Güvenli Kart Yönetim Sistemi

## 🎯 Yeni Özellikler

### ✅ Kullanıcıya Özel Kartlar
- Her kullanıcı sadece kendi oluşturduğu kartları görebilir ve yönetebilir
- Kartlar kullanıcı ID'sine bağlı olarak saklanır
- Başka kullanıcıların kartlarına erişim YOK

### ✅ Şifre Değiştirme
- Hem admin hem de user panelinde şifre değiştirme özelliği
- Navbar'da "Profilim" menüsünden erişilebilir
- Mevcut şifre kontrolü ile güvenli değiştirme

### ✅ Profil Güncelleme
- Kullanıcılar ad-soyad ve email bilgilerini güncelleyebilir
- **IP adresi kullanıcı tarafından DEĞİŞTİRİLEMEZ**
- IP kısıtlaması sadece admin tarafından belirlenebilir

### ✅ Sıkı IP Kısıtlaması
- Admin, kullanıcı oluştururken veya sonradan IP adresi belirleyebilir
- Belirlenmiş IP dışından giriş yapılması **KESINLIKLE ENGELLENIR**
- IP adresi boş bırakılırsa kısıtlama olmaz
- Yanlış IP'den giriş denemesinde kullanıcı anında çıkarılır

### ✅ Türkçe Export
- PDF ve Excel dosyalarında işlem türleri artık Türkçe
- "Payment" → "Ödeme"
- "BalanceUpdate" → "Bakiye Güncelleme"

### ✅ Sadece Kart Numaraları Export
- Kart listesinde yeni export seçenekleri eklendi
- "Sadece Kart Numaraları" için ayrı Excel ve PDF export
- Dropdown menüden seçilebilir

## 🚀 Kurulum Adımları

### 1. Veritabanı Ayarları
`appsettings.json` dosyasındaki connection string'i düzenleyin:
```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;database=carddb;Charset=utf8mb4;Convert Zero Datetime=True;user=root;password=BURAYA_SIFRENIZI_YAZIN"
}
```

### 2. Migration Çalıştırma
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Ya da projeyi ilk çalıştırdığınızda otomatik migrate olacaktır.

### 3. Projeyi Çalıştırma
```bash
dotnet run
```

## 👤 Varsayılan Admin Hesabı

**Email:** admin@securecard.com  
**Şifre:** Admin123!

## 📋 Özellikler Detay

### Admin Paneli
- ✅ Kullanıcı oluşturma (IP kısıtlaması ile)
- ✅ Kullanıcılara IP adresi atama/güncelleme
- ✅ Kullanıcı aktif/pasif yapma
- ✅ Kullanıcı silme
- ✅ Sistem istatistikleri
- ✅ Her kullanıcının kaç kartı olduğunu görme
- ✅ Mevcut IP adresini gösterme (kolaylık için)

### Kullanıcı Paneli
- ✅ Sadece kendi kartlarını görme
- ✅ Kart oluşturma (tek veya toplu)
- ✅ Kart bakiyesi güncelleme
- ✅ Ödeme alma (manuel veya OCR)
- ✅ İşlem geçmişi görüntüleme
- ✅ Export işlemleri (Excel, PDF)
  - Tüm bilgiler ile export
  - Sadece kart numaraları export
- ✅ Profil bilgilerini güncelleme
- ✅ Şifre değiştirme

### Güvenlik
- ✅ IP bazlı erişim kontrolü (Middleware)
- ✅ Identity ile kullanıcı yönetimi
- ✅ Rol bazlı yetkilendirme
- ✅ Her işlem loglama
- ✅ Kullanıcıya özel veri izolasyonu

## 🔒 IP Kısıtlaması Nasıl Çalışır?

1. **Kullanıcı Oluşturma:** Admin yeni kullanıcı oluştururken IP adresi belirleyebilir
2. **IP Güncelleme:** Admin mevcut kullanıcının IP adresini değiştirebilir
3. **Giriş Kontrolü:** Kullanıcı giriş yaptığında IP adresi kontrol edilir
4. **Engelleme:** Belirlenen IP dışından giriş yapılırsa kullanıcı anında çıkarılır ve erişim engellenir
5. **Boş IP:** IP adresi belirlenmemişse herhangi bir yerden giriş yapılabilir

## 📊 Veritabanı Yapısı

### ApplicationUser
- Id, Email, FullName
- AllowedIpAddress (IP kısıtı)
- IsActive (aktif/pasif)
- Cards (1-N ilişki)

### Card
- Id, CardNumber (8 haneli, unique)
- Balance, InitialBalance
- UserId (Foreign Key - Kullanıcıya özel)
- IsActive

### Transaction
- Id, CardId (Foreign Key)
- Amount, BalanceBefore, BalanceAfter
- TransactionType (Payment/BalanceUpdate)
- ProcessedBy, IpAddress

## 🎨 Kullanılan Teknolojiler

- **.NET 8.0**
- **ASP.NET Core Identity**
- **Entity Framework Core**
- **MySQL** (Pomelo)
- **iText7** (PDF oluşturma)
- **ClosedXML** (Excel oluşturma)
- **Bootstrap 5** (UI)
- **DataTables** (Tablo işlemleri)

## ⚠️ Önemli Notlar

1. **IP Adresi:** Localhost'ta test ederken IP adresi "127.0.0.1" veya "::1" olacaktır
2. **Prodüksiyon:** Gerçek IP adreslerini almak için reverse proxy (nginx/IIS) ayarları gerekebilir
3. **OCR:** Şu an simüle edilmiş, gerçek OCR için Tesseract veya IronOCR entegrasyonu yapılmalı
4. **Kullanıcı İzolasyonu:** Her kullanıcı SADECE kendi kartlarını görebilir ve işlem yapabilir

## 📝 Test Senaryosu

1. Admin hesabı ile giriş yapın
2. Yeni bir kullanıcı oluşturun (IP adresinizi belirtin)
3. Kullanıcı hesabı ile giriş yapın
4. Kart oluşturun
5. Ödeme işlemi yapın
6. Export seçeneklerini deneyin
7. Profil bilgilerinizi güncelleyin (IP değiştiremediğinizi görün)
8. Şifrenizi değiştirin

## 🐛 Sorun Giderme

**Veritabanı Hatası:** Connection string'i kontrol edin
**Migration Hatası:** `dotnet ef migrations add Init` komutunu çalıştırın
**IP Engelleme:** Admin panelden IP adresinizi güncelleyin
**Kart Göremiyorum:** Başka bir kullanıcının kartlarını göremezsiniz, kendi kartlarınızı oluşturun

## 📞 Destek

Herhangi bir sorunla karşılaşırsanız lütfen issue açın.

---

**Geliştirici Notu:** Bu versionda tüm istekleriniz implemente edilmiştir:
- ✅ Kullanıcıya özel kartlar
- ✅ Şifre değiştirme (admin + user)
- ✅ Profil güncelleme (IP hariç)
- ✅ Sıkı IP kısıtlaması
- ✅ Türkçe export
- ✅ Sadece kart numaraları export

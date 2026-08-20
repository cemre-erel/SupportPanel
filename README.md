# SupportPanel

ASP.NET Core MVC ile geliştirilmiş, **çoklu firma (multi-tenant)** mimarisinde çalışan bir **Ürün Destek ve Talep Yönetim Sistemi**.

Şirketin geliştirdiği birden fazla ürünün, birden fazla müşteri firma tarafından kullanıldığı senaryolarda destek taleplerinin (ticket) uçtan uca yönetilmesini sağlar: talep oluşturma, SLA takibi, atama/devir, dosya ekleme, bildirimler ve raporlama.


---

## İçindekiler

- [Özellikler](#özellikler)
- [Kullanıcı Rolleri](#kullanıcı-rolleri)
- [Talep Durumları (State Machine)](#talep-durumları-state-machine)
- [Teknolojiler](#teknolojiler)
- [Proje Yapısı](#proje-yapısı)
- [Kurulum](#kurulum)
- [Testler](#testler)
- [Bilinen Sınırlamalar](#bilinen-sınırlamalar)

---

## Özellikler

### Firma (Tenant) Yönetimi
- Firma oluşturma, listeleme, düzenleme, aktif/pasif etme
- Firma bazlı kullanıcı ve ürün ilişkilendirme
- Firma bazlı veri izolasyonu (multi-tenant, tek veritabanı + `TenantId` ile mantıksal ayrım)
- Firma bazlı SLA hesaplama yöntemi (7x24 / çalışma saatleri) yapılandırması

### Ürün Yönetimi
- Ürün tanımlama, listeleme, düzenleme
- Bir firma birden fazla ürün kullanabilir, bir ürün birden fazla firma tarafından kullanılabilir (N:N)
- Ürün bazlı destek uzmanı ve ürün yöneticisi ataması (bir ürünün tek bir yöneticisi olur; bir kullanıcı birden fazla ürünün yöneticisi olabilir)

### Talep (Ticket) Yönetimi
- Talep oluşturma, listeleme, düzenleme
- Zorunlu kategori ve SLA seviyesi seçimi
- Öncelik (Low/Medium/High/Critical) ve durum yönetimi
- Destek uzmanına / ürün yöneticisine atama ve devir
- Rol bazlı destek iş havuzu (Support Pool)
- Talep yorumları (ilk anlamlı yorum = ilk müdahale)
- Dosya ekleme (tür ve boyut kısıtlı) ve yetkili indirme
- Talep iptali (iptal edilen talepte güncelleme yapılamaz)
- Tüm atama, devir ve durum değişikliklerinin aktivite geçmişinde tutulması (kullanıcı, tarih, işlem, eski/yeni değer)

### SLA Yönetimi
- Firma bazlı SLA seviyeleri (ör. Acil, Yüksek, Normal) ile ilk müdahale ve çözüm hedef süreleri (dakika)
- 7x24 veya çalışma saatleri (Pazartesi–Cuma 09:00–18:00) bazlı SLA hesaplama
- "Müşteri Bekleniyor" durumunda SLA süresinin duraklatılması
- İlk müdahale ve çözüm süresi ihlallerinin otomatik takibi
- Kalan / geçen SLA sürelerinin talep listelerinde ve detayda gösterimi

Sistemdeki SLA (Kalan/Geçen Süre) takibi **Operasyonel Destek SLA'sı** modeline göre kurgulanmıştır:
* **SLA Başlangıç Noktası:** SLA sayacı, talep **Destek Havuzuna** düştüğü an başlatılır.
* **Karar Gerekçesi (Rationale):** Talebin taslak oluşturulma aşamaları veya sistem ön işleme süreçleri destek ekibinin müdahale alanında değildir. Destek uzmanlarının performans kriterlerini adil ölçebilmek adına SLA, talebin ekibin önüne (havuza) aktif olarak düştüğü andan itibaren işletilmektedir.

### Kullanıcı ve Rol Yönetimi
- Rol bazlı yetkilendirme (aşağıya bakınız)
- Kullanıcı-ürün ilişkilendirme (destek uzmanı / ürün yöneticisi)
- Firma kullanıcılarının yalnızca kendi firmalarına ait verilere erişmesi

### Raporlama
- Talep analizi (durum, öncelik, kategori, firma, SLA seviyesi, tarih aralığı filtreleriyle)
- SLA analizi (ihlal edilen talepler, ortalama ilk müdahale / çözüm süresi, uyum oranı)
- Ürün bazlı talep sayıları ve detayları
- Firma bazlı talep sayıları ve detayları
- Ürün x Firma kırılımlı rapor
- Destek uzmanı bazında talep dağılımı ve performans raporu

### Bildirimler
- Uygulama içi bildirimler ve okunmamış sayaç
- E-posta bildirimleri (SMTP ayarları yönetim ekranından yapılandırılabilir)
- Talep oluşturma, havuza yönlendirme, atama, durum değişikliği ve müşteri yanıtı bildirimleri

### Kimlik Doğrulama ve Güvenlik
- Cookie tabanlı kimlik doğrulama, kayan (sliding) oturum süresi
- Oturum doğrulamasında rol/firma değişikliği kontrolü (değişmişse otomatik çıkış)
- Dosya eklerine doğrudan erişim engelli; yalnızca yetkilendirilmiş controller aksiyonu üzerinden indirme
- İstek boyutu sınırlamaları (dosya yükleme için)

### Veritabanı
- SQL Server
- Entity Framework Core (Code First, Migrations)
- Repository Pattern + Service Layer

### Otomatik Testler
- xUnit + Moq
- SLA hesaplama testleri (`SlaCalculatorTests`)
- Talep akışı / yetkilendirme testleri (`TicketControllerTests`)
- 22 otomatik test

---

## Kullanıcı Rolleri

| Rol | Açıklama |
|---|---|
| **Sistem Yöneticisi** (`SystemAdmin`) | Ürün, firma, kullanıcı yönetimi; tüm verilere ve raporlara erişim |
| **Destek Uzmanı - Ürün Yöneticisi** (`ProductManager`) | İlgilendiği ürünlerin destek havuzunu görür, iş ataması yapabilir; aynı zamanda normal destek uzmanı da olabilir |
| **Destek Uzmanı - Normal** (`SupportSpecialist`) | İlgilendiği ürünlere ait talepleri ve kendisine atanmış talepleri görür, yanıtlar, durum günceller |
| **Firma Yöneticisi** (`CompanyManager`) | Firmasının tüm taleplerini görür, çözer/kapatır veya yükleniciye yönlendirir, talep iptal edebilir |
| **Firma Kullanıcısı** (`CompanyUser`) | Talep oluşturur, kendi taleplerini görür, yorum/dosya ekler, açtığı talebi iptal edebilir |

### İş Havuzu ve Görünürlük Tasarımı

İş havuzu ekranları, tek bir ortak liste yerine kullanıcı rolü ve iş akışına göre ayrıştırılmıştır:

- **Sistem Yöneticisi:** Genel talep listesinden tüm firma ve ürünlere ait talepleri görüntüler.
- **Firma Yöneticisi:** “Firma Talepleri” ekranından yalnızca kendi firmasının tüm taleplerini görüntüler ve değerlendirir.
- **Firma Kullanıcısı:** Yalnızca kendi oluşturduğu talepleri görüntüler.
- **Destek Uzmanı - Normal:** İlgilendiği ürünlere ait talepleri görüntüleyebilir; ancak yalnızca kendisine atanmış taleplerde yorum, durum güncelleme ve çözüm işlemi yapabilir. Kendisine atanmış kayıtlar ayrıca “Bana Atanan Talepler” ekranında listelenir.
- **Destek Uzmanı - Ürün Yöneticisi:** Yöneticisi olduğu ürünlerin atanmayı bekleyen taleplerini “Destek Havuzu” ekranında görüntüler; talebi kendisine veya yetkili bir destek uzmanına atayabilir.

Bu ayrım, ürün bazında görünürlüğü korurken işlem yetkilerini atama ve rol kurallarıyla sınırlar.
---

## Talep Durumları (State Machine)

```
Yeni ──▶ Destek Havuzunda ──▶ Atandı ──▶ İnceleniyor ──┬──▶ Müşteri Bekleniyor
  │                              │            │          │        │
    │                              │            └──────────┴────────┘
      ▼                              ▼            ▼
      Kapatıldı                    (devam)      Çözüldü ──▶ Kapatıldı
                                                      │
(İnceleniyor'a dönebilir)

Herhangi bir aşamada, sahibi veya firma yöneticisi tarafından: ──▶ İptal Edildi (son durum)
```

Geçişler `TicketStatus.AllowedTransitions` içinde merkezi olarak tanımlıdır; kapatılmış ve iptal edilmiş talepler üzerinde güncelleme yapılamaz.

#### Kapalı Talep (Closed Ticket) Yaşam Döngüsü Politikası
* **Kapalı Taleplerin Durumu:** Status değeri `Closed` veya `Resolved` olan talepler yeniden açılamaz (**ReadOnly/Locked**).
* **Karar Gerekçesi (Rationale):** 
  * **Raporlama ve SLA Doğruluğu:** Kapatılan bir talebin yeniden açılması, geçmiş döneme ait SLA ve çözüm süresi (MTTR) raporlarının sapmasına neden olur.
  * **İzlenebilirlik:** Çözülmüş bir konudan bağımsız gelişen yeni problemler için kullanıcıların eski talebe referans vererek yeni bir kayıt oluşturması teşvik edilir. Böylece her talebin yaşam döngüsü ve izlenebilirliği net kalır.
---

## Teknolojiler

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10 (SQL Server provider)
- Bootstrap 5, jQuery, jQuery Validation
- C#, LINQ
- Repository Pattern, Service Layer
- xUnit, Moq

---

## Proje Yapısı

```
SupportPanel
├── Constants/           # Rol adları vb. sabitler
├── Controllers/          # MVC controller'ları (Ticket, Tenant, Product, User, Reports, ...)
├── Data/                 # EF Core DbContext ve repository implementasyonları
├── Interfaces/            # Repository ve service arayüzleri
├── Migrations/            # EF Core Code First migration'ları
├── Models/                # Domain modelleri (Ticket, Tenant, Product, SlaLevel, ...)
├── Services/               # İş kuralları / servis katmanı (SLA hesaplama dahil)
├── ViewModels/             # Rapor ve liste ekranlarına özel görünüm modelleri
├── Views/                  # Razor görünümleri
├── wwwroot/                 # Statik dosyalar (css, js, kütüphaneler)
│
├── SupportPanel.Tests/
│   ├── SlaCalculatorTests.cs
│   └── TicketControllerTests.cs
│
├── appsettings.json
└── Program.cs
```

---

## Kurulum

### Gereksinimler
- .NET 10 SDK
- SQL Server (LocalDB / Express / tam sürüm)

### Adımlar

1. Bağlantı dizesini `appsettings.json` içinde ihtiyacınıza göre düzenleyin (varsayılan: `Server=localhost\SQLEXPRESS;Database=SupportPanelDB;Trusted_Connection=True;TrustServerCertificate=True;`).
2. İlk sistem yöneticisi şifresini **appsettings.json'a yazmadan**, user-secrets veya ortam değişkeni ile tanımlayın:

```bash
dotnet user-secrets set "SeedAdmin:Password" "<guclu-bir-sifre>"
```

(Alternatif: `SeedAdmin__Password` ortam değişkeni.)

3. Veritabanını oluşturun:

```bash
dotnet ef database update
```

> Not: `Development` ortamında uygulama açılışta migration'ları otomatik uygular.

4. (Opsiyonel) E-posta bildirimleri için SMTP ayarlarını `Email:Smtp` bölümünden veya uygulama içi **E-posta Ayarları** ekranından yapılandırın.

5. Uygulamayı çalıştırın:

```bash
dotnet run
 ```

6. Seed edilen sistem yöneticisi kullanıcı adıyla (`appsettings.json > SeedAdmin:Username`, varsayılan `admin`) giriş yapın.

---

## Testler

```bash
dotnet test
```

---

## Bilinen Sınırlamalar

- Firma bazlı çalışma saatleri şu an tek bir sabit aralık (Pazartesi–Cuma 09:00–18:00) olarak tanımlı; firma başına özelleştirilebilir saat aralığı desteklenmiyor.

---

## Author

Internship Project
                                                                                                                                 
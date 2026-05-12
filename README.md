# Abonelik ve Otomatik Odeme Hatirlatma

Sade bir banka abonelik takip uygulamasi. Musteri girisi, profil guncelleme, abonelik, borc sorgulama, odeme ve hatirlatma kontrollerini icerir.

## Teknolojiler

- Backend: .NET 8 Web API
- Frontend: HTML, CSS, vanilla JavaScript
- Veritabani: MySQL
- ORM: Entity Framework Core + Pomelo MySQL provider

## Proje Yapisi

```text
Backend/
  Controllers/
  Data/
  Models/
  Services/
  wwwroot/
    index.html
    style.css
    app.js
global.json
ArchitectCase.sln
```

## Kurulum

MySQL'de veritabani olusturun:

```sql
CREATE DATABASE subscription_reminder_db;
```

Isterseniz tablolar icin dogrudan `database.sql` dosyasini calistirabilirsiniz.

`Backend/appsettings.json` icindeki connection string'i kendi MySQL kullaniciniza gore guncelleyin:

```json
"DefaultConnection": "server=localhost;port=3306;database=subscription_reminder_db;user=root;password=your_password"
```

Alternatif olarak EF migration olusturup veritabanini guncelleyin:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project Backend
dotnet ef database update --project Backend
```

Backend'i calistirin:

```powershell
dotnet run --project Backend
```

Frontend backend icinden servis edilir. Backend calisirken tarayicida su adresi acin:

```text
http://localhost:5041
```

Swagger:

```text
http://localhost:5041/swagger
```

## API Ozeti

- `GET /api/customers`
- `POST /api/customers/register`
- `POST /api/customers/login`
- `GET /api/customers/{id}`
- `PUT /api/customers/{id}`
- `DELETE /api/customers/{id}`
- `GET /api/subscriptions`
- `POST /api/subscriptions`
- `PUT /api/subscriptions/{id}`
- `DELETE /api/subscriptions/{id}`
- `GET /api/debts/subscription/{subscriptionId}`
- `GET /api/payments`
- `POST /api/payments`
- `GET /api/payments/subscription/{subscriptionId}`
- `GET /api/payments/customer/{customerId}`
- `GET /api/reminders/customer/{customerId}`

## Mock Servisler

- `DebtService`: Abonelik bilgilerine gore donemsel borc uretir.
- `MockPaymentService`: Odeme sonucunu mock olarak basarili veya basarisiz dondurur.
- `ReminderService`: Aktif ve bu ay odenmemis abonelikleri listeler.

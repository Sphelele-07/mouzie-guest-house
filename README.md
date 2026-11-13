\# Mouzie Guest House — Booking System



A full-stack B\&B booking system for a two-room guest house in Langa, Cape Town.



\*\*Live Demo:\*\* https://mouzie.runasp.net



\## Overview



Mouzie Guest House needed a lightweight online booking system. Guests can check availability, book a room, and receive an invoice by email. The owner manages everything from a single admin dashboard — approve bookings, check guests in/out, generate invoices, and download reports.



\*\*No online payments.\*\* Guests pay via EFT using the banking details on the invoice.



\## Features



\### Guest

\- Browse rooms and view availability

\- Submit booking with SA ID + phone validation

\- Receive booking code + invoice by email

\- WhatsApp integration for instant communication

\- Verify booking by code



\### Manager / Admin

\- Session-based login (BCrypt-hashed passwords)

\- Live dashboard: pending bookings, rooms occupied, revenue, check-ins

\- Booking management: approve, reject, check-in, check-out

\- Room CRUD (soft delete)

\- Automated invoice generation (PDF via QuestPDF)

\- Damage reporting per booking

\- Site settings (address, WhatsApp, banking details)



\### System

\- Email confirmations (guest + admin)

\- Automated invoice on approval

\- Thank-you email on checkout

\- Full engagement logging

\- Admin rate limiting (5 attempts/min)



\## Tech Stack



| Layer | Technology |

|---|---|

| Framework | ASP.NET Core MVC (.NET 8) |

| Database | SQL Server + Dapper |

| Authentication | Session-based with BCrypt |

| Email | SMTP (Gmail App Password) |

| PDF Generation | QuestPDF |

| Frontend | Custom CSS (Glassmorphism) |



\## Architecture



&#x20;   Mouzie/

&#x20;   ├── Mouzie.Data/              (Class Library)

&#x20;   │   ├── DataAccess/           (Dapper wrapper)

&#x20;   │   ├── Models/               (Domain + ViewModels)

&#x20;   │   ├── Repositories/         (Interface + Implementation)

&#x20;   │   ├── Services/             (Email, PDF, Session, Password)

&#x20;   │   └── Validation/           (SA ID, Phone, custom attributes)

&#x20;   │

&#x20;   └── Mouzie.UI/                (ASP.NET Core MVC)

&#x20;       ├── Controllers/

&#x20;       ├── Views/

&#x20;       ├── wwwroot/              (CSS, JS, images)

&#x20;       └── Program.cs



\## Setup



\### Prerequisites

\- .NET 8 SDK

\- SQL Server 2019+

\- Gmail account with App Password



\### Database



1\. Create database `MouzieDB`

2\. Run all scripts from `/Database` folder in this order:

&#x20;  - `01\_Tables.sql`

&#x20;  - `02\_Procedures.sql`

&#x20;  - `03\_Seed.sql`



\### Configuration



\*\*appsettings.json:\*\*



&#x20;   {

&#x20;     "ConnectionStrings": {

&#x20;       "MouzieDB": "Server=localhost;Database=MouzieDB;Trusted\_Connection=True;TrustServerCertificate=True;"

&#x20;     },

&#x20;     "EmailSettings": {

&#x20;       "SmtpHost": "smtp.gmail.com",

&#x20;       "SmtpPort": "587",

&#x20;       "SmtpUser": "your-email@gmail.com",

&#x20;       "SmtpPass": "YOUR\_GMAIL\_APP\_PASSWORD\_HERE",

&#x20;       "SenderName": "Mouzie Guest House",

&#x20;       "SenderEmail": "your-email@gmail.com"

&#x20;     }

&#x20;   }



\*\*Set the SMTP password via User Secrets:\*\*



&#x20;   cd Mouzie.UI

&#x20;   dotnet user-secrets init

&#x20;   dotnet user-secrets set "EmailSettings:SmtpPass" "your-app-password"



\### Run



&#x20;   dotnet run --project Mouzie.UI



Visit `https://localhost:7001`



\*\*Admin login:\*\* `ta.shoes@mouzie.co.za` (contact repo owner for password)



\## Security



\- BCrypt password hashing (work factor 12)

\- Anti-forgery tokens on all POST forms

\- HTTPS-only session cookies

\- Rate limiting on login (5/min/IP) and booking (10/min/IP)

\- All database access via Dapper + stored procedures (no SQL injection)

\- No secrets committed to source



\## License



MIT © Siphelele Ndzundzu



\## Contact



\- Email: mshini473@gmail.com

\- LinkedIn: \[siphelele-ndzundzu](https://www.linkedin.com/in/siphelele-ndzundzu-7b37b2292)


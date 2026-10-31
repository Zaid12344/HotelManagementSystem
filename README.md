# 🏨 Hotel Management System

A comprehensive, web-based Hotel Management System built using **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**, and **SQL Server**.

## ✨ Features

- **Role-Based Access Control (RBAC):** 5 distinct roles (Admin, Manager, Front Desk, Housekeeping, Accountant).
- **Room Management:** Real-time dashboard tracking room status (Available, Occupied, Cleaning, Maintenance).
- **Reservations:** Date-overlap logic to prevent double-booking. Support for deposits and group bookings.
- **Check-In / Check-Out:** Automated folio generation and room status updates.
- **Billing:** Automatic GST calculation, split billing, and **QuestPDF** invoice generation.
- **Housekeeping & Maintenance:** Auto-task generation on checkout and room-blocking on maintenance issues.
- **Additional Services:** Posting Spa, Laundry, and Room Service charges directly to guest folios.

---

## 🚀 How to Run Locally (Setup Instructions)

Because this repository does not include compiled binaries (`bin/obj` folders) or the local database migration files, please follow these setup steps to run the project:

### 1. Prerequisites

- Visual Studio 2022 or 2026 (with the ASP.NET and web development workload installed).
- .NET 10 SDK.
- SQL Server Express LocalDB (installed by default with Visual Studio).

### 2. Open the Project

- Open the `.sln` (Solution) file in Visual Studio.
- Right-click the Solution in Solution Explorer and select **Restore NuGet Packages**.
- Visual Studio will automatically download the required packages (Entity Framework Core, Identity, QuestPDF, etc.).

### 3. Setup the Database

- Open the **Package Manager Console** (Tools > NuGet Package Manager > Package Manager Console).
- Ensure the **Default project** dropdown is set to `HotelManagement`.
- Run the following command to generate the migration files:
    Add-Migration InitialCreate
- Run the following command to create the LocalDB database:
    Update-Database

### 4. Run the Application

Press F5 in Visual Studio to start the application. The database will automatically seed with demo users, 10 rooms, and 10 services on the first run.

---

## 🔑 Default Login Credentials (Seeded)

Once the app is running, use the Staff Login button on the landing page. You can test the Role-Based Access Control by logging in with these credentials:

**Admin**
- Email: admin@hotel.com
- Password: Admin@123

**Manager**
- Email: manager@hotel.com
- Password: Manager@123

**Front Desk**
- Email: frontdesk@hotel.com
- Password: Frontdesk@123

**Housekeeping**
- Email: housekeeping@hotel.com
- Password: Housekeeping@123

**Accountant**
- Email: accountant@hotel.com
- Password: Accountant@123

---

## 🛠️ Technology Stack

- **Framework:** ASP.NET Core MVC (.NET 10)
- **Database:** Entity Framework Core (Code-First) with SQL Server LocalDB
- **Authentication:** ASP.NET Core Identity (Cookie-based)
- **UI/UX:** Bootstrap 5, Custom CSS, AJAX
- **PDF Generation:** QuestPDF
- **Architecture:** Service-Layer pattern for separation of business logic.

---

## 📂 Project Structure

- **Controllers:** Handles HTTP requests and routes.
- **Models:** Contains Entities, ViewModels, and DbContext.
- **Services:** Core business logic (Reservation, Billing, CheckInOut, PDF generation).
- **Views:** Razor UI pages.
- **wwwroot:** Static assets (CSS, JS, Images).

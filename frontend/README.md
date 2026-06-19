# Homemade Cookies E-Commerce System

## README / Installation Guide

### Project Information

**Course:** CSE6234 Software Design
**Project Title:** Homemade Cookies E-Commerce System
**Technology Stack:** ASP.NET Core 8, HTML, CSS, JavaScript, SQL Server

---

# System Overview

Homemade Cookies E-Commerce System is a web-based application that allows customers to browse cookies, place orders, make payments, track order status, submit reviews, and manage their accounts.

The system also provides an administration panel for managing cookies, categories, customer orders, reviews, sales reports, and system analytics.

The software demonstrates the application of software design principles and design patterns, including:

* State Pattern
* Factory Method Pattern
* Facade Pattern
* SOLID Principles

---

# Project Structure

HomemadeCookiesEcommerce/

├── Backend/

│   ├── Controllers/

│   ├── Services/

│   ├── Repositories/

│   ├── Models/

│   ├── DTOs/

│   └── Program.cs

│

├── Frontend/

│   ├── html/

│   ├── css/

│   ├── js/

│   └── images/

│

├── Database/

│   └── HomemadeCookies.sql

│

└── README.pdf

---

# Software Requirements

Before running the project, ensure the following software is installed:

### Required Software

1. Visual Studio 2022
2. .NET 8 SDK
3. SQL Server LocalDB / SQL Server Express
4. Modern Web Browser

   * Google Chrome
   * Microsoft Edge

---

# Installation Steps

## Step 1 – Extract Project Files

Extract the submitted ZIP file to any folder.

Example:

```text
C:\Projects\HomemadeCookies
```

---

## Step 2 – Open Solution

Open:

```text
HomemadeCookies.sln
```

using Visual Studio 2022.

---

## Step 3 – Restore NuGet Packages

Visual Studio will automatically restore the required packages.

If necessary:

Tools → NuGet Package Manager → Restore Packages

### MailKit Package Requirement

This project uses **MailKit** for sending email notifications.

Check whether MailKit is already installed in the project:

```bash
dotnet list package
```

If MailKit does not appear in the package list, install it using:

```bash
dotnet add package MailKit
```

Or install it through Visual Studio:

Tools → NuGet Package Manager → Manage NuGet Packages → Search for **MailKit** → Install

---

## Step 4 – Configure Database

Open SQL Server Management Studio (SSMS).

Create a database:

```text
HomemadeCookiesDB
```

Run the provided SQL script:

```text
Database/HomemadeCookies.sql
```

to create all tables and insert sample data.

---

## Step 5 – Update Connection String

Open:

```text
appsettings.json
```

Update the database connection string if necessary.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HomemadeCookiesDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

---

## Step 6 – Configure Email Settings

Open:

```text
appsettings.json
```

Add or verify that the following SMTP configuration exists:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "Homemade Cookies",
    "SenderEmail": "yourgmail@gmail.com",
    "Username": "yourgmail@gmail.com",
    "Password": "your-google-app-password"
  }
}
```

### Important

Check whether the `EmailSettings` section is already included in your `appsettings.json`.

* If it already exists, simply replace the email address and password values.
* If it does not exist, add the entire section shown above.

---

## Step 7 – Create Gmail App Password

This project uses Gmail SMTP with an **App Password**.

### 1. Enable Two-Factor Authentication (2FA)

Sign in to your Google Account and enable 2-Step Verification.

Google Security Settings:

```text
https://myaccount.google.com/security
```

### 2. Generate an App Password

Open:

```text
https://myaccount.google.com/apppasswords
```

Or refer to the provided guide:

```text
https://share.google/JOV8XZj7xuGvixlzm
```

Steps:

1. Sign in to your Google account.
2. Open App Passwords.
3. Create a new app password.
4. Enter a custom application name such as:

```text
HomemadeCookiesApp
```

5. Click **Generate**.
6. Copy the generated password.
7. Paste the generated password into the `Password` field inside `appsettings.json`.

Example:

```json
{
  "EmailSettings": {
    "Password": "abcd efgh ijkl mnop"
  }
}
```

### Gmail SMTP Settings

| Setting        | Value          |
| -------------- | -------------- |
| SMTP Server    | smtp.gmail.com |
| Port           | 587            |
| Encryption     | TLS            |
| Authentication | Required       |

---

## Step 8 – Run the Application

Press:

```text
F5
```

or

```text
Ctrl + F5
```

in Visual Studio.

The application will automatically launch in the browser.

Example URL:

https://localhost:5001

---

# Sample User Accounts

## Administrator

Email:

[admin@gmail.com](mailto:admin@gmail.com)

Password:

admin123

---

## Customer

Email:

[customer@gmail.com](mailto:customer@gmail.com)

Password:

customer123

---

# Main Features

## Customer Features

* User Registration
* User Login
* Browse Cookies
* Search Cookies
* Filter Cookies
* View Cookie Details
* Add to Cart
* Checkout
* Online Payment
* Save Pending Orders
* Track Orders
* Cancel Orders
* Order History
* Submit Reviews
* Edit Reviews
* Delete Reviews
* Upload Review Images
* Receive Email Notifications

---

## Administrator Features

* Dashboard Analytics
* Sales Reports
* Cookie Management
* Category Management
* Inventory Management
* Order Management
* Order Status Tracking
* Customer Review Management
* Pie Chart and Bar Chart Sales Analysis
* Email Notification Management

---

# Order Status Workflow

The order processing workflow follows:

```text
Pending Payment
→ Confirmed
→ Baking
→ Ready
→ Completed
```

Customer cancellation is supported when allowed by the business rules.

---

# Design Patterns Used

## 1. State Pattern

Used for order status management.

States include:

* PendingState
* ConfirmedState
* BakingState
* ReadyState
* CompletedState
* CancelledState

Purpose:

Allows order behaviour to change dynamically according to its current status.

---

## 2. Factory Method Pattern

Used for cookie creation.

Factories include:

* CookieFactory
* PackageCookieFactory
* AlaCarteCookieFactory

Purpose:

Encapsulates cookie object creation and improves extensibility.

---

## 3. Facade Pattern

Used in:

```text
OrderManagementFacade
```

Purpose:

Provides a simplified interface for handling order-related operations.

---

# Known Limitations

* Prototype version intended for academic purposes.
* Payment gateway uses simulation mode.
* Email notifications require valid SMTP configuration.
* Local database setup is required before running.

---

# Troubleshooting

## Database Connection Error

Verify:

* SQL Server is running.
* Connection string is correct.
* Database has been created successfully.

---

## Email Not Sending

Verify:

* MailKit package is installed.
* Gmail 2-Step Verification is enabled.
* App Password is configured correctly.
* SMTP server is set to `smtp.gmail.com`.
* Port is set to `587`.
* Internet connection is available.

---

## How to Check Whether Requirements Are Already Included

### Check MailKit

Run:

```bash
dotnet list package
```

Look for:

```text
MailKit
```

If it appears, MailKit is already installed.

### Check Email Configuration

Open:

```text
appsettings.json
```

Look for:

```json
"EmailSettings"
```

If the section exists, email configuration has already been added.

### Check App Password

Inside:

```json
"Password": "..."
```

If a Gmail App Password has already been pasted there, no further action is required.

---

## Port Already In Use

Change the application port in:

```text
launchSettings.json
```

and rerun the project.

---

## Build Errors

Perform:

Build → Clean Solution

then

Build → Rebuild Solution

---

# Team Information

Course:

CSE6234 Software Design

Project:

Homemade Cookies E-Commerce System

Academic Term:

2610

---

# Lecturer Instructions

### Option 1: Run Using Visual Studio 2022

1. Open the solution file (`HomemadeCookies.sln`) in Visual Studio 2022.
2. Restore NuGet packages automatically or via:

   * Tools → NuGet Package Manager → Restore Packages
3. Check whether MailKit is installed:

```bash
dotnet list package
```

4. If MailKit is missing, install it:

```bash
dotnet add package MailKit
```

5. Execute the provided database script (`HomemadeCookies.sql`) in SQL Server Management Studio (SSMS).
6. Update the connection string in `appsettings.json` if required.
7. Verify that the `EmailSettings` section exists in `appsettings.json`.
8. Generate a Gmail App Password and paste it into the `Password` field.
9. Press `F5` or `Ctrl + F5` to run the application.
10. Login using the sample accounts provided above.
11. Explore customer and administrator functionalities.

### Option 2: Run Using Command Line

1. Open Command Prompt, PowerShell, or Terminal.
2. Navigate to the project directory:

```bash
cd C:\Projects\HomemadeCookies
```

3. Check installed packages:

```bash
dotnet list package
```

4. Install MailKit if necessary:

```bash
dotnet add package MailKit
```

5. Restore project dependencies:

```bash
dotnet restore
```

6. Build the project:

```bash
dotnet build
```

7. Run the application:

```bash
dotnet run
```

8. Open the URL displayed in the terminal (for example):

```text
https://localhost:5001
```

9. Login using the sample accounts provided above and test both customer and administrator features.

### Database Setup Commands (Optional)

If using Entity Framework Core migrations instead of the provided SQL script:

```bash
dotnet ef database update
```

If migrations need to be created:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Useful Commands

Clean the project:

```bash
dotnet clean
```

Rebuild the project:

```bash
dotnet build
```

Run in watch mode (auto-reload during development):

```bash
dotnet watch run
```

Thank you for evaluating our project.

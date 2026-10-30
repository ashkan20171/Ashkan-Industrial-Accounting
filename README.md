# Ashkan Industrial Accounting

**A Modular Industrial Accounting & Financial Management System**

A desktop-based industrial accounting solution developed with C#, Windows Forms, .NET Framework 4.8 and Microsoft SQL Server.

Ashkan Industrial Accounting is designed to demonstrate practical software engineering capabilities in financial workflows, banking operations, account reconciliation, risk monitoring, role-based access control, background processing and enterprise-oriented application architecture.

The project emphasizes maintainable code, separation of responsibilities, operational visibility and a bilingual user experience.

---

## Key Features

### Financial & Banking Operations
- Industrial accounting and financial management workflows
- Bank account and transaction management
- Bank reconciliation and discrepancy monitoring
- Persistent banking alerts
- Account risk ranking and executive-level prioritization
- Alert assignment, tracking and resolution history

### Security & Access Management
- Role-based access control (RBAC)
- Permission-aware navigation and operations
- User-specific notification preferences
- Audit-oriented administrative workflows
- User-scoped notification management

### Notification Infrastructure
- In-application notification inbox
- Independent bank notification worker
- SQL-based notification processing
- Transactional outbox and inbox patterns
- Retry handling and failure tracking
- Independent email notification worker
- SMTP configuration support
- Email delivery history and retry management

### Background Services & Operations Monitoring
- Worker heartbeat monitoring
- Background service status tracking
- Queue monitoring
- Operational history
- Daily worker activity metrics
- Operations Center dashboard

### User Experience
- Persian and English interface
- Right-to-left (RTL) and left-to-right (LTR) layouts
- Desktop-oriented user interface
- Centralized localization support

---

## Technology Stack

| Area | Technology |
|---|---|
| Programming Language | C# |
| Desktop Framework | Windows Forms |
| Runtime | .NET Framework 4.8 |
| IDE | Visual Studio 2022 |
| Database | Microsoft SQL Server |
| Data Access | ADO.NET |
| Background Processing | Independent Worker Applications |
| Database Messaging | SQL Outbox / Inbox |
| Email Integration | SMTP |
| Testing | Python-based automated validation |

---

## Architecture Overview

The application separates desktop functionality from background notification processing.

```text
Ashkan Industrial Accounting
│
├── Desktop Application
│   ├── Presentation Layer
│   ├── Business Services
│   ├── Authorization
│   ├── Localization
│   └── Data Access
│
├── Bank Notification Worker
│   └── SQL Outbox / Inbox Processing
│
├── Email Notification Worker
│   └── SMTP Delivery and Retry Handling
│
└── Microsoft SQL Server
    ├── Accounting Data
    ├── User Management
    ├── Banking Alerts
    ├── Notification Queues
    ├── Worker Monitoring
    └── Operational History
```

The worker applications are independently executable components. SQL-based messaging provides a practical integration approach without requiring an additional message broker for the current implementation.

---

## Project Structure

```text
AshkanAccounting/
│
├── AshkanIndustrialAccounting/
│   ├── UI/
│   ├── Services/
│   ├── Models/
│   └── Data/
│
├── BankNotificationWorker/
│
├── EmailNotificationWorker/
│
├── database/
│   └── SQL migration scripts
│
├── tests/
│   └── Automated validation tests
│
├── README.md
└── STAGE_NOTES/
```

Directory names may evolve as the project develops.

---

## Getting Started

### Prerequisites

- Windows
- Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/ashkan20171/Ashkan-Industrial-Accounting.git
   ```

2. Open the solution in Visual Studio 2022.

3. Configure the SQL Server connection strings in the relevant application configuration files.

4. Review the database migration scripts in the `database` directory and apply them in the required order.

5. Configure the worker applications and SMTP settings where applicable.

6. Build the solution and run the desktop application.

> Database configuration and deployment steps should be reviewed against the current project version before production use.

---

## Engineering Focus

This project explores several practical software engineering topics:

- Separation of concerns
- Modular application design
- Role-based authorization
- SQL Server data management
- Transactional Outbox and Inbox patterns
- Background job processing
- Retry and failure handling
- Application localization
- Operational monitoring
- Maintainability and testability

---

## Current Development Status

Ashkan Industrial Accounting is an evolving software engineering portfolio project.

Development is organized into incremental stages, with ongoing improvements to financial workflows, notification infrastructure, background services, monitoring and user experience.

The repository should be considered a development project rather than a certified or production-validated accounting product.

---

## Author

**Ashkan Motaei**

Software Engineer

GitHub: [@ashkan20171](https://github.com/ashkan20171)

---

*Built as an ongoing software engineering project with a focus on enterprise application development, financial workflows and maintainable architecture.*

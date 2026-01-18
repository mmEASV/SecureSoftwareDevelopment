# CRA-Compliant Automated Device Update System

> **A proof-of-concept implementation demonstrating EU Cyber Resilience Act (CRA) compliance for automatic security updates**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-512BD4?style=for-the-badge&logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

---

## 📋 Table of Contents

- [Problem Statement](#problem-statement)
- [Solution](#solution)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [CRA Compliance](#cra-compliance)

---

## 🎯 Problem Statement

Currently the client updates all of the deployed devices manually with flash drives. This kind of worked in the past, but it is slow and doesn't meet the new **Cyber Resilience Act (CRA)** requirements.

The CRA says manufacturers need to provide automatic security updates by default.

### CRA Requirements (Annex 1 - 2, c)

According to the regulation:

> ensure that vulnerabilities can be addressed through security updates, including, where applicable, through automatic security updates that are installed within an appropriate timeframe enabled as a default setting, with a clear and easy-to-use opt-out mechanism, through the notification of available updates to users, and the option to temporarily postpone them;

**Key Requirements:**
- ✅ Automatic security updates enabled by default
- ✅ Clear and easy opt-out mechanism
- ✅ Notification of available updates
- ✅ Option to temporarily postpone updates
- ✅ Appropriate timeframe for security patches

---

## 💡 Solution

The client needs a solution for **semi-automatic updates**, where they can release updates, but it will be up to their customers to deploy them.

**The system provides:**

1. **Vendor Portal (Admin.Api + Admin.Web)**
   - Web app for the vendor to upload new updates
   - Create and manage releases
   - Monitor device deployment status

2. **Customer Portal (ClientPortal.Api + ClientPortal.Web)**
   - Web portal for customers to view available updates
   - Schedule deployment of software updates
   - Configure automatic update settings
   - Postpone updates with reason tracking

3. **Device Agent (ClientPortal.UpdateAgent)**
   - Background service on customer devices
   - Automatically checks for and installs updates
   - Verifies update integrity and authenticity

4. **Secure Delivery**
   - SHA-256 file integrity verification
   - RSA-4096 digital signatures
   - HTTPS/TLS transport encryption
   - Cloudflare Tunnel support

---

## ✨ Features

### CRA Compliance

| Feature | Status | Implementation |
|---------|--------|----------------|
| Default automatic updates | ✅ | `Device.AutomaticUpdates = true` on registration |
| Easy opt-out | ✅ | Device settings API endpoint |
| Update notifications | ✅ | Customer portal lists all updates |
| Postpone capability | ✅ | Deployment postpone API with reason |
| Timeframe enforcement | ✅ | Max 7-day postpone for mandatory updates |
| Security transparency | ✅ | CVE lists, changelogs, severity levels |

### Technical Features

- **Separated APIs** - Admin.Api (vendor) and ClientPortal.Api (customer)
- **Shared Database** - Single PostgreSQL database for data consistency
- **File Upload** - Multipart form-data with SHA-256 hashing
- **Digital Signatures** - RSA-4096 for update authenticity
- **Deployment Tracking** - Status, retry count, postpone tracking
- **Tenant Isolation** - Multi-tenant support
- **Comprehensive Testing** - 128 tests covering all features

---

## 🏗️ Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     VENDOR SIDE (Admin)                      │
├─────────────────────────────────────────────────────────────┤
│  Admin.Api          - Upload updates, create releases       │
│  Admin.Web          - Vendor admin portal (Blazor WASM)     │
│  Admin.Shared       - Shared models and DTOs                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   CUSTOMER SIDE (ClientPortal)               │
├─────────────────────────────────────────────────────────────┤
│  ClientPortal.Api   - View releases, manage devices         │
│  ClientPortal.Web   - Customer portal (Blazor WASM)         │
│  ClientPortal.UpdateAgent - Device agent (background svc)   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     SHARED INFRASTRUCTURE                    │
├─────────────────────────────────────────────────────────────┤
│  PostgreSQL         - Shared database (AdminDb)             │
│  .NET Aspire        - Service orchestration                 │
│  Cloudflare Tunnel  - Secure remote access                  │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack

- **.NET 10.0** - Application framework
- **ASP.NET Core Minimal APIs** - Backend APIs
- **Blazor WebAssembly** - Frontend portals
- **Entity Framework Core** - Data access
- **PostgreSQL** - Database
- **.NET Aspire** - Orchestration
- **FluentUI** - UI components
- **xUnit** - Testing framework

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop/)
- **Git** - [Download here](https://git-scm.com/downloads)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd SecureSoftwareDevelopment
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Start Docker Desktop** (required for PostgreSQL)

4. **Configure Aspire secrets** (optional)
   ```bash
   cd src/AppHost
   dotnet user-secrets set "Parameters:postgres-username" "root"
   dotnet user-secrets set "Parameters:postgres-password" "password"
   ```

5. **Run with .NET Aspire**
   ```bash
   cd src/AppHost
   dotnet run
   ```

6. **Access the application**
   - **Aspire Dashboard**: https://localhost:15068 (view all services)
   - **Admin API**: Check dashboard for port (usually 7100)
   - **Admin API Docs**: https://localhost:7100/swagger
   - **ClientPortal API**: Check dashboard for port (usually 7200)
   - **ClientPortal API Docs**: https://localhost:7200/swagger
   - **Admin Web**: Check dashboard for port
   - **ClientPortal Web**: Check dashboard for port

---

## 📖 Usage

### Development Commands

```bash
# Build the solution
dotnet build

# Run tests (128 tests)
dotnet test

# Run specific test project
dotnet test tests/Admin.Api.Tests/

# Database migrations
cd src/Admin.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Clean build artifacts
dotnet clean
```

### Project Structure

```
src/
├── Admin.Api/              # Vendor backend API
├── Admin.Web/              # Vendor portal (Blazor WASM)
├── Admin.Shared/           # Shared models and DTOs
├── ClientPortal.Api/       # Customer backend API
├── ClientPortal.Web/       # Customer portal (Blazor WASM)
├── ClientPortal.UpdateAgent/ # Device agent
├── AppHost/                # .NET Aspire orchestration
└── ServiceDefaults/        # Common configurations

tests/
└── Admin.Api.Tests/        # Test suite (128 tests)
```

### API Endpoints

**Admin.Api (Vendor Operations):**
```
POST   /api/updates              # Upload new update
GET    /api/updates              # List all updates
GET    /api/updates/{id}/download # Download update file
POST   /api/releases             # Create release
GET    /api/releases             # List all releases
```

**ClientPortal.Api (Customer Operations):**
```
GET    /api/releases/active      # View active releases
POST   /api/devices              # Register device
PUT    /api/devices/{id}/settings # Configure auto-updates
POST   /api/deployments/schedule # Schedule deployment
PUT    /api/deployments/{id}/postpone # Postpone deployment
```

---

## ✅ CRA Compliance

### How It Works

1. **Default Automatic Updates**
   - All devices registered with `AutomaticUpdates = true` by default
   - Complies with CRA requirement for default automatic updates

2. **Clear Opt-Out Mechanism**
   - Device settings endpoint allows disabling automatic updates
   - Customer portal provides UI to configure settings

3. **Update Notifications**
   - Customer portal lists all available updates
   - Shows severity, CVE lists, changelogs

4. **Postpone Capability**
   - Customers can postpone deployments with reason
   - System tracks postpone count and reasons

5. **Appropriate Timeframe**
   - Mandatory security updates have 7-day max postpone period
   - Enforced at API level to ensure compliance

6. **Security Transparency**
   - All updates show CVE lists
   - Severity levels (Critical, High, Medium, Low)
   - Detailed changelogs and security fixes

### Compliance Verification

Run the CRA compliance integration tests:

```bash
dotnet test --filter "Category=CRACompliance"
```

All 10 CRA compliance tests verify:
- Default automatic updates
- Opt-out functionality
- Postpone mechanism
- Mandatory update enforcement
- Security transparency

---

## 🧪 Testing

**Test Coverage: 128 Tests**

- ✅ 96 Repository tests (CRUD operations)
- ✅ 13 File storage tests (integrity verification)
- ✅ 9 API endpoint tests (behavior validation)
- ✅ 10 CRA compliance tests (regulatory requirements)

Run all tests:
```bash
dotnet test
# Passed!  - Failed: 0, Passed: 128, Skipped: 0, Total: 128
```

---

## 🔒 Security

**Security Layers:**
1. API Key Authentication (device authentication)
2. SHA-256 File Hashing (integrity verification)
3. RSA-4096 Digital Signatures (authenticity verification)
4. TLS/HTTPS (transport encryption)
5. Tenant Isolation (database-level separation)
6. Cloudflare Tunnel (secure remote access)

**Production Considerations:**
- Hash API keys before storage
- Implement rate limiting
- Add DDoS protection
- Use certificate-based device auth
- Encrypt files at rest
- Security scan uploaded files

---

## 📝 License

This is an educational project demonstrating CRA compliance principles.

**Recommended for:**
- ✅ Learning CRA compliance
- ✅ Architecture reference
- ✅ Starting point for production systems
- ✅ Academic/research purposes

**Before production use:**
- Security audit and penetration testing
- Implement production-grade authentication
- Add monitoring and alerting
- Set up proper CI/CD pipeline

---

## 🙏 Acknowledgments

- **EU Cyber Resilience Act** - Regulatory framework
- **.NET Foundation** - ASP.NET Core, Blazor, EF Core
- **Microsoft** - .NET Aspire, FluentUI
- **Cloudflare** - Secure tunneling solution

---

**Built with .NET 10.0 | January 2025**

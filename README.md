# PaperTrails Backend

Physical mail such as bills, government letters, and important documents is often misplaced or lost, leading to missed deadlines, late fees, and unnecessary delays. Existing solutions like [Paperless-ngx](https://github.com/paperless-ngx/paperless-ngx) provide OCR, search, and document management features, but their interfaces are largely web-based and targeted at technical users.

The PaperTrails backend is a .NET Web API responsible for handling user management, document storage, and integration with [Paperless-ngx](https://github.com/paperless-ngx/paperless-ngx). It provides secure routes for uploading documents, managing their status, and processing them via Paperless-ngx's OCR capabilities.

This transforms physical mail into a digital, organized, and actionable experience. Users gain peace of mind knowing their important documents are always accessible, searchable, and tied to real-world tasks.

---

## Backend Overview

This repository contains the **backend server** for PaperTrails, implemented as a .NET Web API. The backend is responsible for user management, document handling, and integration with Paperless-ngx.

### Features

- **ASP.NET Core Web API foundation**
- **PostgreSQL** persistence using **Entity Framework Core**
- **Authentication & Authorization** using Supabase (JWT tokens)
- **User & Document Models** for structured storage
- **Secure Routes** for:
  - Document creation
  - Document retrieval
  - Document status updates
- **Paperless-ngx Integration** for:
  - Document upload
  - OCR processing

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/)
- Supabase project for authentication
- Paperless-ngx instance with REST API enabled

# Costa Rica Local Business Catalog 🇨🇷

A high-performance, scalable discovery platform for local businesses in Costa Rica, built with **.NET 10 LTS**, **Aspire**, and **PostgreSQL/PostGIS**.

## 🏗 Architecture Overview

The solution follows a modern, distributed architecture orchestrated by **.NET Aspire**, ensuring seamless local development and cloud-readiness.

### Tech Stack
* **Backend:** ASP.NET Core 10 (Minimal APIs)
* **IDE:** Visual Studio 2026 (v18.x Stable - March 2026 Update)
* **Orchestration:** .NET Aspire (AppHost)
* **Database:** PostgreSQL with **PostGIS** extension
* **Auth:** ASP.NET Core Identity + JWT Bearer Tokens
* **Image Processing:** SixLabors.ImageSharp.Web
* **Testing:** xUnit v3 + FluentAssertions + Aspire.Hosting.Testing

---

## 🚀 Key Features

### 1. "Gold Standard" Admin API
Designed for native compatibility with **React Admin**:
* **Standardized Pagination:** Uses `_start` and `_end` parameters.
* **Total Count:** Returns `X-Total-Count` header with configured CORS exposition.
* **Global Search:** Unified `q` parameter for full-text search across entities.
* **Exception-free Flow:** Services return Result Tuples `(Items, TotalCount)` or `(Success, Error)` instead of throwing exceptions.

### 2. Smart Discovery & Geo-Search
* **Spheroid Distance:** High-precision geo-location searching using PostGIS `useSpheroid: true` for meter-accurate distance calculation.
* **Faceted Search (Drill-down):** Intelligent filtering where available Cities and Tags dynamically narrow down based on selected Province or proximity.
* **SEO Resilience:** Built-in `OldSlugs` history system for automatic 301 redirects when business names change.

### 3. Secure Identity Management
* **RBAC:** Role-Based Access Control with `Manager`, `Admin`, and `SuperAdmin` policies.
* **JWT Auth:** Secure token-based authentication for all administrative segments.

---

## 📁 Project Structure

* **`CostaRica.AppHost`**: The Aspire orchestrator managing database containers and service dependencies.
* **`CostaRica.ServiceDefaults`**: Shared service configurations (resilience, telemetry, health checks).
* **`CostaRica.Api`**: The core backend containing Minimal APIs, Business Logic, and PostGIS integration.
* **`CostaRica.Tests.Unit`**: Focused on business logic validation using xUnit v3 and InMemory database.
* **`CostaRica.Tests.Integration`**: End-to-end tests using real Docker containers via Aspire Testing, ensuring PostGIS and JWT flows work in a production-like environment.

---

## 🧪 Testing Strategy

The project employs a **Data Isolation Strategy** for integration tests:
* Each test run uses unique data prefixes (`Prefix-{Guid}`) to allow parallel execution without data pollution in the shared PostgreSQL container.
* **Unit Tests:** Validate "Exception-free" service responses.
* **Integration Tests:** Verify real SQL translation of PostGIS queries and JWT policy enforcement.
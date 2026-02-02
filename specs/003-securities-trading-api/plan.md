# Implementation Plan: 證券交易資料查詢系統

**Branch**: `003-securities-trading-api` | **Date**: 2026-02-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-securities-trading-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

實作證券交易資料查詢系統 RESTful API，提供股票查詢、即時報價、委託下單與查詢功能。技術架構採用 .NET 8 Web API + MS SQL Server + EF Core，使用三層式資料架構（Hot/Warm/Cold）與 CQRS 讀寫分離模式，確保高頻交易場景下的效能與資料一致性。

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, FluentValidation 11.x (避免 8.x+ 付費版本), xUnit, InMemory Cache  
**Storage**: Microsoft SQL Server 2019+ (支援 In-Memory OLTP)，使用三層式資料架構：
  - Hot Layer: StockQuotes_Snapshot (In-Memory, Schema-Only)
  - Warm Layer: OrderBook_Levels (In-Memory, Durable)
  - Cold Layer: StockTicks_History (Disk, Columnstore + Partitioning)
  - CQRS: Orders_Write/Orders_Read, Positions_Read  
**Testing**: xUnit 單元測試（目標 100% 覆蓋率），k6 壓力測試與負載測試  
**Target Platform**: Windows Server / .NET 8 Runtime  
**Project Type**: Web API (單一專案，不實作前端)  
**Performance Goals**: 
  - API 回應時間 <200ms p95
  - 股票查詢 <1s
  - 即時報價查詢 <3s (含外部 API)
  - 委託下單 <2s
  - 支援 10 requests/second per IP (速率限制)  
**Constraints**: 
  - 不使用 Minimal APIs，採用 Controller-based API
  - 不使用 AutoMapper，使用 POCO 直接映射
  - 使用 EF Core Code First
  - 快取使用 InMemory (暫不使用 Redis)
  - 所有回傳格式統一 (透過 Middleware/Filter)
  - 錯誤處理標準化 (400/404)  
**Scale/Scope**: 
  - MVP 階段：基本 CRUD + 外部 API 整合
  - 預期負載：100 concurrent users
  - 資料量：台灣上市櫃約 2000 檔股票
  - 委託單：初期 10k orders/day

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Code Quality Excellence (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: 將使用 C# 命名規範、XML 文件註解、一致的程式碼風格。所有公開 API 需有完整 XML 文件說明。

### ✅ II. Behavior-Driven Development & Testing (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: 
  - Feature spec 已包含完整 Given-When-Then 驗收情境
  - 將使用 xUnit 進行單元測試，目標 100% 覆蓋率
  - TDD 開發流程：先寫測試，後寫實作
  - FluentValidation 提供驗證邏輯的行為測試

### ✅ III. User Experience Consistency
- **Status**: PASS (API 專案)
- **Compliance**: 本專案為 API，無前端介面。統一錯誤回傳格式透過 Middleware 實現，確保 API 回應一致性。

### ✅ IV. Performance Excellence
- **Status**: PASS
- **Compliance**: 
  - API 回應目標 <200ms p95
  - 使用 In-Memory 快取提升查詢效能
  - SQL Server In-Memory OLTP 處理高頻交易
  - k6 壓力測試確保效能達標

### ✅ V. Documentation Localization (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: 
  - spec.md, plan.md 使用繁體中文撰寫
  - API 錯誤訊息使用繁體中文
  - 使用者面向文件使用繁體中文
  - 程式碼註解與開發文件使用英文

### ✅ VI. MVP First Development (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: 
  - Feature spec 已明確定義 MVP 範圍
  - User stories 依優先級排序 (P1-P4)
  - 明確排除非 MVP 功能：會員系統、融資融券、零股交易、委託修改/刪除等
  - 採用增量交付：先股票查詢(P1)，再報價(P2)，再下單(P3)，最後委託查詢(P4)

### ✅ VII. Third-Party Package Stability (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: 
  - 使用 NuGet 官方套件，不修改第三方套件原始碼
  - 套件版本明確鎖定
  - FluentValidation 使用 11.x（避免 8.x+ 付費限制）
  - 安全更新透過官方管道

**GATE RESULT**: ✅ **PASS** - 所有憲章原則符合，可進入 Phase 0 研究階段

---

## 🔄 Re-evaluation: Constitution Check (Post-Design)

*Re-evaluated after Phase 1 Design completion*

### ✅ I. Code Quality Excellence (NON-NEGOTIABLE)
- **Status**: PASS
- **Design Review**: 
  - 資料模型定義完整，包含清楚的驗證規則
  - API 契約遵循 OpenAPI 3.0 標準
  - 所有實體與 DTO 設計清晰，易於維護

### ✅ II. Behavior-Driven Development & Testing (NON-NEGOTIABLE)
- **Status**: PASS
- **Design Review**: 
  - Feature spec 的 Given-When-Then 情境已映射至 API 端點
  - 單元測試策略明確（100% 覆蓋率目標）
  - 整合測試與效能測試腳本規劃完整

### ✅ III. User Experience Consistency
- **Status**: PASS
- **Design Review**: 
  - API 錯誤回應統一採用 RFC 7807 ProblemDetails 標準
  - 所有錯誤訊息繁體中文化
  - HTTP 狀態碼使用正確（400/404/429/503）

### ✅ IV. Performance Excellence
- **Status**: PASS
- **Design Review**: 
  - 三層式資料架構（Hot/Warm/Cold）設計符合高頻交易需求
  - CQRS 讀寫分離優化查詢效能
  - InMemory Cache 快取策略完整
  - k6 效能測試腳本包含明確 Threshold 目標

### ✅ V. Documentation Localization (NON-NEGOTIABLE)
- **Status**: PASS
- **Design Review**: 
  - plan.md, data-model.md, quickstart.md 使用繁體中文
  - OpenAPI 文件描述使用繁體中文
  - 程式碼範例註解使用英文（符合開發者習慣）

### ✅ VI. MVP First Development (NON-NEGOTIABLE)
- **Status**: PASS
- **Design Review**: 
  - 設計嚴格遵循 MVP 範圍，未包含非必要功能
  - OrdersRead 表格保留 FilledQuantity 欄位但 MVP 固定為 0
  - PositionsRead 表格建立但 MVP 不使用
  - 委託狀態 MVP 固定為「已委託」，未實作狀態變更邏輯

### ✅ VII. Third-Party Package Stability (NON-NEGOTIABLE)
- **Status**: PASS
- **Design Review**: 
  - 所有第三方套件使用 NuGet 官方來源
  - FluentValidation 版本明確鎖定 11.x
  - FinMind API 客戶端使用標準 HttpClient，無客製化修改

**GATE RESULT (Post-Design)**: ✅ **PASS** - 設計階段完全符合憲章所有原則

## Project Structure

### Documentation (this feature)

```text
specs/003-securities-trading-api/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── openapi.yaml     # OpenAPI 3.0 specification
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
SecuritiesTradingApi/                    # Solution 根目錄
├── SecuritiesTradingApi.sln             # Visual Studio Solution
├── src/
│   └── SecuritiesTradingApi/            # Main API Project
│       ├── Controllers/                 # API Controllers
│       │   ├── StocksController.cs      # 股票查詢 API
│       │   └── OrdersController.cs      # 委託下單與查詢 API
│       ├── Models/                      # Domain Models & DTOs
│       │   ├── Entities/                # EF Core Entities
│       │   │   ├── StockMaster.cs
│       │   │   ├── StockQuotesSnapshot.cs
│       │   │   ├── OrderBookLevels.cs
│       │   │   ├── OrdersWrite.cs
│       │   │   ├── OrdersRead.cs
│       │   │   └── PositionsRead.cs
│       │   └── Dtos/                    # Data Transfer Objects
│       │       ├── StockQueryDto.cs
│       │       ├── StockQuoteDto.cs
│       │       ├── CreateOrderDto.cs
│       │       └── OrderDto.cs
│       ├── Services/                    # Business Logic Services
│       │   ├── IStockService.cs
│       │   ├── StockService.cs
│       │   ├── IOrderService.cs
│       │   └── OrderService.cs
│       ├── Data/                        # Data Access Layer
│       │   ├── TradingDbContext.cs      # EF Core DbContext
│       │   └── Configurations/          # EF Core Entity Configurations
│       ├── Infrastructure/              # Cross-cutting concerns
│       │   ├── Middleware/
│       │   │   ├── ErrorHandlingMiddleware.cs
│       │   │   ├── ResponseFormattingMiddleware.cs
│       │   │   └── RateLimitingMiddleware.cs
│       │   ├── Filters/
│       │   │   └── ValidationFilter.cs
│       │   ├── Validators/              # FluentValidation
│       │   │   ├── StockQueryValidator.cs
│       │   │   └── CreateOrderValidator.cs
│       │   ├── ExternalApis/            # External API clients
│       │   │   ├── IFinMindApiClient.cs
│       │   │   └── FinMindApiClient.cs
│       │   └── Cache/
│       │       └── MemoryCacheService.cs
│       ├── Program.cs                   # Application entry point
│       ├── appsettings.json
│       └── appsettings.Development.json
├── tests/
│   ├── SecuritiesTradingApi.UnitTests/  # Unit Tests (xUnit)
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── Infrastructure/
│   └── SecuritiesTradingApi.IntegrationTests/  # Integration Tests
│       ├── Api/
│       └── Database/
├── k6-tests/                            # Performance Tests
│   ├── load-test.js                     # 負載測試腳本
│   └── stress-test.js                   # 壓力測試腳本
└── database/                            # Database Scripts
    ├── migrations/                      # EF Core Migrations
    └── seed-data/                       # Initial seed data
        └── stocks.csv                   # 股票主檔資料
```

**Structure Decision**: 採用單一 Web API 專案架構，因為 MVP 階段功能範圍明確，不需複雜的微服務架構。所有業務邏輯集中在 Services 層，Controllers 負責 HTTP 請求處理，Data 層透過 EF Core 處理資料存取。測試專案獨立，包含單元測試與整合測試。k6 測試腳本用於效能驗證。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**無違規項目** - 本專案完全符合憲章所有原則，無需複雜度追蹤。

---

## Phase 0: Outline & Research

### Research Topics Identified

從 Technical Context 分析，需研究以下主題：

1. **FinMind API 整合最佳實務**
   - FinMind API 認證機制與速率限制
   - 台灣證券交易所即時報價資料格式
   - 錯誤處理與重試策略（指數退避）
   - API 回應快取策略

2. **EF Core Code First + SQL Server In-Memory OLTP**
   - In-Memory Table 的 EF Core 配置
   - Memory-Optimized Table 限制與最佳實務
   - DURABILITY = SCHEMA_ONLY vs SCHEMA_AND_DATA
   - Native Compilation Stored Procedure 與 EF Core 整合

3. **CQRS 讀寫分離實作模式**
   - Orders_Write / Orders_Read 資料同步機制
   - Event-driven 更新 vs 即時同步
   - Eventual Consistency 處理

4. **FluentValidation 非同步驗證**
   - MustAsync 用於資料庫驗證（股票代號存在性）
   - 效能優化：驗證器註冊為 Singleton
   - 自訂錯誤訊息本地化（繁體中文）

5. **API 速率限制實作**
   - ASP.NET Core Middleware 實作 IP-based rate limiting
   - Sliding window vs Fixed window 演算法
   - 速率限制快取儲存（InMemory）

6. **統一錯誤回傳格式**
   - ProblemDetails (RFC 7807) 標準
   - 自訂錯誤回應結構
   - 驗證錯誤 (400) 與資源不存在 (404) 處理

7. **k6 壓力測試腳本設計**
   - Ramping VUs 負載測試情境
   - Stress test 階段設計
   - Threshold 設定與斷言

### Research Output

詳細研究結果將記錄於 `research.md`。

---

## Phase 1: Design & Contracts

### Data Model

詳細資料模型將記錄於 `data-model.md`，包含：
- **StockMaster**: 股票主檔（代號、名稱、簡稱、交易所、產業別等）
- **StockQuotesSnapshot**: 即時報價快照（Hot Layer, In-Memory）
- **OrdersWrite**: 委託寫入表（CQRS Write Side）
- **OrdersRead**: 委託查詢表（CQRS Read Side）
- **PositionsRead**: 持倉查詢表
- DTOs: 各 API 端點的請求/回應物件

### API Contracts

詳細 API 規格將記錄於 `contracts/openapi.yaml`，包含：
- `GET /api/stocks/{stockCode}`: 查詢股票資訊
- `GET /api/stocks/{stockCode}/quote`: 查詢股票即時報價
- `POST /api/orders`: 建立委託單
- `GET /api/orders/{orderId}`: 查詢委託單

### Quickstart Guide

開發環境設定與快速啟動指南將記錄於 `quickstart.md`。

---

## Phase 2: Implementation Planning

⚠️ **Phase 2 (tasks.md) 由 `/speckit.tasks` 指令產生，非本指令範圍。**

# Implementation Plan: 證券交易資料查詢系統

**Branch**: `003-securities-trading-api` | **Date**: 2026-02-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-securities-trading-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

實作證券交易資料查詢系統 RESTful API，提供股票查詢、即時報價、委託下單與查詢功能。技術架構採用 .NET 8 Web API + MS SQL Server + EF Core，使用三層式資料架構（Hot/Warm/Cold）與 CQRS 讀寫分離模式，確保高頻交易場景下的效能與資料一致性。

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, FluentValidation 11.x (避免 8.x+ 付費版本), xUnit, InMemory Cache, HttpClient (台灣證交所 API)  
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
  - 將使用 xUnit 進行單元測試，**強制要求 100% 覆蓋率用於關鍵業務邏輯**（委託下單、價格驗證、委託單編號生成），其餘程式碼**最低 90% 覆蓋率**
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

## 非功能需求 (Non-Functional Requirements)

### 效能需求

- **API 回應時間**: p95 < 200ms, p99 < 500ms（不含外部 API 呼叫）
- **股票代號查詢**: < 1 秒（直接查詢資料庫，InMemory Cache 加速）
- **即時報價查詢**: 正常情況 < 3 秒（含外部 API），最壞情況（含重試）< 9 秒
- **委託下單**: < 2 秒（含驗證與資料庫寫入）
- **資料庫查詢**: Simple queries < 50ms, Complex queries < 200ms
- **快取命中率**: Stock master data > 95%, Quote data > 80%

### 併發處理能力

- **目標負載**: 100 concurrent users
- **委託下單併發**: 50 TPS (Transactions Per Second)
- **查詢併發**: 200 RPS (Requests Per Second)
- **峰值處理**: 支援 2x 正常負載（200 concurrent users）短時間內不降級
- **資料庫連線池**: 最小 10 connections, 最大 50 connections
- **委託單編號唯一性**: 使用 SQL Server SEQUENCE 確保併發建立時無重複

### 資料一致性

- **委託單寫入**: 強一致性（Strong Consistency），使用資料庫 ACID 交易保證
- **即時報價快取**: 最終一致性（Eventual Consistency），允許 5 秒延遲
- **CQRS 讀寫分離**: OrdersWrite → OrdersRead 同步延遲 < 100ms
- **交易隔離等級**: Read Committed（預設），避免髒讀與不可重複讀
- **樂觀併發控制**: 使用 RowVersion (timestamp) 偵測併發更新衝突

### 可用性與可靠性

- **系統可用性目標**: 99% uptime（MVP 階段，約 7.2 小時/月停機時間）
- **錯誤恢復**: 所有 API 呼叫失敗後自動重試（外部 API: 2 次重試，指數退避）
- **容錯機制**: 
  - 外部 API 失敗：回傳友善錯誤訊息，不中斷服務
  - 資料庫連線失敗：回傳 HTTP 503，記錄錯誤日誌
  - 快取失敗：降級至直接查詢資料庫
- **健康檢查端點**: `/health` 端點檢查資料庫連線與外部 API 可用性
- **優雅關閉**: 應用程式關閉時完成正在處理的請求，不接受新請求

### 安全性基線

- **輸入驗證**: 
  - 所有 API 參數使用 FluentValidation 驗證
  - 防範 SQL Injection：EF Core 參數化查詢
  - 防範 XSS：API 回傳 JSON，不渲染 HTML
- **速率限制**: 每個客戶端 IP 每秒 10 次請求，超過回傳 HTTP 429
- **錯誤訊息**: 不暴露技術細節（如資料庫連線字串、堆疊追蹤），僅記錄於日誌
- **HTTPS**: 正式環境強制使用 HTTPS（開發環境可使用 HTTP）
- **CORS 設定**: 限制允許的來源網域（MVP 階段暫時開放所有來源，正式環境需限制）
- **資料加密**: 資料庫連線字串使用 ASP.NET Core Secret Manager 儲存

**MVP 階段排除的安全功能**:
- 使用者認證與授權（JWT, OAuth）
- API Key 驗證
- 請求簽章驗證
- 資料欄位級別加密（如委託單金額加密）

### 資料保存與備份

- **委託單資料**: 永久保存，不刪除（僅標記狀態）
- **即時報價資料**: Hot Layer (StockQuotesSnapshot) 僅保留最新資料，歷史資料不保存（MVP 階段）
- **應用程式日誌**: 保存 30 天，自動輪替（每日輪替，最多 30 個檔案）
- **資料庫備份**: 
  - 完整備份：每週一次（週日凌晨 2:00）
  - 差異備份：每日一次（凌晨 2:00）
  - 交易日誌備份：每小時一次（正式環境）
  - 備份保留：完整備份保留 4 週，差異備份保留 1 週
- **災難復原**: 
  - 復原時間目標 (RTO): 4 小時（MVP 階段）
  - 復原點目標 (RPO): 1 小時（最多丟失 1 小時交易日誌）
  - 備份異地儲存：本地 + 雲端儲存（Azure Blob Storage）

**MVP 階段簡化**:
- 無自動化災難演練
- 無即時資料庫複寫（無 Always On 可用性群組）
- 無自動化備份驗證（手動抽檢）

### 監控與日誌

- **應用程式日誌**:
  - 使用 Serilog 結構化日誌
  - 記錄等級：Debug（開發）, Information（正式）, Warning, Error
  - 必須記錄欄位：時間戳記、請求 ID (TraceId)、使用者 IP、API 路徑、錯誤訊息、堆疊追蹤（僅 Error）
- **效能追蹤**:
  - API 回應時間（p50, p95, p99）
  - 資料庫查詢耗時
  - 外部 API 呼叫耗時與成功率
  - 快取命中率
- **業務指標**:
  - 委託單建立成功率
  - 股票查詢次數
  - 即時報價查詢次數
  - 速率限制觸發次數
- **SC-007 量測機制**:
  - 定義「第一次成功」為使用者完成「股票查詢 → 價格查詢 → 委託建立 → 委託查詢」完整流程，且無需重試或修正錯誤
  - 量測方式：在應用程式日誌中記錄每個 API 請求的 SessionId（從 HTTP Header 或 Cookie 取得）與操作類型
  - 計算公式：`成功率 = (完整流程無錯誤的 Session 數) / (嘗試流程的總 Session 數) × 100%`
  - 實作方式：使用 Serilog 記錄結構化事件，包含 SessionId、OperationType、IsSuccess、ErrorCode 等欄位，定期（每日）透過日誌分析工具（如 Log Analytics 或 SQL 查詢）計算成功率
  - 目標值：90% 使用者第一次成功率
  - 備註：MVP 階段不實作自動化追蹤，透過日誌手動分析驗證

**MVP 階段排除的監控功能**:
- Application Insights / Prometheus 整合
- 即時告警（Email/SMS）
- Dashboard 視覺化（Grafana）
- 分散式追蹤（OpenTelemetry）

### 速率限制分散式部署考量

**Per-IP 限制的已知限制**:
- **NAT/Proxy 問題**: 多個使用者共用同一對外 IP（如企業網路、ISP NAT），會共享速率限制額度
- **反向代理**: 在 Load Balancer 或 CDN 後方，所有請求可能顯示為相同 IP

**MVP 階段解決方案**:
- 優先使用 `X-Forwarded-For` header 取得原始客戶端 IP（需配置反向代理正確傳遞）
- 在 ASP.NET Core Middleware 實作 IP 解析邏輯：
  ```csharp
  var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
  var clientIp = !string.IsNullOrEmpty(forwardedFor) 
      ? forwardedFor.Split(',')[0].Trim() 
      : context.Connection.RemoteIpAddress?.ToString();
  ```
- FR-024 已註明此限制，提醒運維團隊配置反向代理

**未來改進方案（Post-MVP）**:
- 引入使用者認證後，改為 per-user 或 per-API-key 限制
- 使用分散式 Rate Limiter（Redis + Sliding Window）取代 InMemory 實作
- 提供不同速率限制層級（免費用戶 10 req/s，付費用戶 100 req/s）

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

# Tasks: 證券交易資料查詢系統

**Branch**: `003-securities-trading-api`  
**Date**: 2026-02-02  
**Input**: Design documents from `/specs/003-securities-trading-api/`

## Format: `- [ ] [TaskID] [P?] [Story?] Description with file path`

- **Checkbox**: `- [ ]` (markdown checkbox)
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create Visual Studio solution structure per SecuritiesTradingApi/SecuritiesTradingApi.sln
- [X] T002 Initialize .NET 8 Web API project in src/SecuritiesTradingApi/
- [X] T003 [P] Install core NuGet packages: Microsoft.EntityFrameworkCore 8.x, Microsoft.EntityFrameworkCore.SqlServer 8.x, FluentValidation 11.x in src/SecuritiesTradingApi/SecuritiesTradingApi.csproj
- [X] T004 [P] Initialize xUnit test projects: SecuritiesTradingApi.UnitTests and SecuritiesTradingApi.IntegrationTests in tests/
- [X] T005 [P] Create project folder structure: Controllers/, Models/, Services/, Data/, Infrastructure/ in src/SecuritiesTradingApi/
- [X] T006 [P] Configure appsettings.json with connection strings and TWSE API settings in src/SecuritiesTradingApi/appsettings.json
- [X] T007 [P] Configure appsettings.Development.json in src/SecuritiesTradingApi/appsettings.Development.json

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T008 Create TradingDbContext class with DbContextOptions in src/SecuritiesTradingApi/Data/TradingDbContext.cs
- [ ] T009 Configure EF Core connection string and database provider in src/SecuritiesTradingApi/Program.cs
- [ ] T010 Create initial EF Core migration for database schema in src/SecuritiesTradingApi/Data/Migrations/
- [ ] T011 [P] Create ProblemDetails error response classes in src/SecuritiesTradingApi/Models/ProblemDetails/
- [ ] T012 [P] Implement ErrorHandlingMiddleware for global exception handling in src/SecuritiesTradingApi/Infrastructure/Middleware/ErrorHandlingMiddleware.cs
- [ ] T013 [P] Implement RateLimitingMiddleware for IP-based rate limiting (10 req/sec) in src/SecuritiesTradingApi/Infrastructure/Middleware/RateLimitingMiddleware.cs
- [ ] T014 [P] Configure InMemory cache service in src/SecuritiesTradingApi/Infrastructure/Cache/MemoryCacheService.cs
- [ ] T015 Register middleware and services in src/SecuritiesTradingApi/Program.cs
- [ ] T016 [P] Create SQL script for database with In-Memory OLTP filegroup in database/scripts/01-create-database.sql
- [ ] T017 [P] Create SQL script for seq_OrderSequence in database/scripts/02-create-sequences.sql
- [ ] T018 [P] Implement ITwseApiClient interface in src/SecuritiesTradingApi/Infrastructure/ExternalApis/ITwseApiClient.cs
- [ ] T019 Implement TwseApiClient with retry logic and exponential backoff in src/SecuritiesTradingApi/Infrastructure/ExternalApis/TwseApiClient.cs
- [ ] T020 Implement CachedTwseApiClient decorator with 5-second cache in src/SecuritiesTradingApi/Infrastructure/ExternalApis/CachedTwseApiClient.cs
- [ ] T021 Register HttpClient and TWSE API services in src/SecuritiesTradingApi/Program.cs
- [ ] T022 [P] Configure Swagger/OpenAPI documentation in src/SecuritiesTradingApi/Program.cs
- [ ] T023 [P] Setup structured logging configuration (Serilog) with log levels and required fields (timestamp, stock code, error type, retry count, response time) in src/SecuritiesTradingApi/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 股票代號查詢 (Priority: P1) 🎯 MVP

**Goal**: 提供股票代號查詢功能，驗證股票代號是否存在，並返回完整公司資訊

**Independent Test**: 輸入有效或無效的股票代號（如 "2330" 或 "9999"），驗證系統返回正確的存在性結果或錯誤訊息

**Feature Requirements**: FR-001, FR-002, FR-003  
**API Endpoint**: GET /api/stocks/{stockCode}

### Tests for User Story 1

> **NOTE: 遵循 TDD 原則，先寫失敗測試再實作功能。測試作為 Given-When-Then 驗收情境的可執行版本。**

- [ ] T024 [P] [US1] Unit test for StockMasterEntity validation in tests/SecuritiesTradingApi.UnitTests/Models/StockMasterTests.cs
- [ ] T025 [P] [US1] Unit test for StockQueryValidator with valid and invalid stock codes in tests/SecuritiesTradingApi.UnitTests/Validators/StockQueryValidatorTests.cs
- [ ] T026 [P] [US1] Unit test for StockService.GetStockInfoAsync in tests/SecuritiesTradingApi.UnitTests/Services/StockServiceTests.cs
- [ ] T027 [P] [US1] Integration test for GET /api/stocks/{stockCode} endpoint in tests/SecuritiesTradingApi.IntegrationTests/Api/StocksControllerTests.cs

### Implementation for User Story 1

- [ ] T028 [P] [US1] Create StockMaster entity class in src/SecuritiesTradingApi/Models/Entities/StockMaster.cs
- [ ] T029 [P] [US1] Create StockInfoDto response class in src/SecuritiesTradingApi/Models/Dtos/StockInfoDto.cs
- [ ] T030 [P] [US1] Create StockQueryDto request class in src/SecuritiesTradingApi/Models/Dtos/StockQueryDto.cs
- [ ] T031 [US1] Add StockMaster DbSet to TradingDbContext in src/SecuritiesTradingApi/Data/TradingDbContext.cs
- [ ] T032 [US1] Create EF Core entity configuration for StockMaster in src/SecuritiesTradingApi/Data/Configurations/StockMasterConfiguration.cs
- [ ] T033 [US1] Create EF Core migration for StockMaster table in src/SecuritiesTradingApi/Data/Migrations/
- [ ] T034 [P] [US1] Implement StockQueryValidator using FluentValidation in src/SecuritiesTradingApi/Infrastructure/Validators/StockQueryValidator.cs
- [ ] T035 [P] [US1] Create IStockService interface in src/SecuritiesTradingApi/Services/IStockService.cs
- [ ] T036 [US1] Implement StockService.GetStockInfoAsync method in src/SecuritiesTradingApi/Services/StockService.cs
- [ ] T037 [US1] Create StocksController with GET /api/stocks/{stockCode} endpoint in src/SecuritiesTradingApi/Controllers/StocksController.cs
- [ ] T038 [US1] Register StockService and validators in src/SecuritiesTradingApi/Program.cs
- [ ] T039 [P] [US1] Create CSV data seeding script for stock master data (t187ap03_L.csv) in database/seed-data/seed-stocks.sql
- [ ] T040 [US1] Implement stock data seeding logic in src/SecuritiesTradingApi/Data/DbInitializer.cs
- [ ] T040a [US1] Execute stock data seeding on application startup or via migration in src/SecuritiesTradingApi/Program.cs

**Checkpoint**: User Story 1 完成 - 可完全獨立測試股票代號查詢功能

---

## Phase 4: User Story 2 - 查詢單一股票即時價格 (Priority: P2)

**Goal**: 查詢特定股票的即時報價資訊，包含最新成交價、開盤價、最高價、最低價、漲跌停價格等

**Independent Test**: 在 P1 完成後，輸入已驗證的股票代號（如 "2330"），驗證系統正確呼叫 TWSE API 並返回完整的即時交易資訊

**Feature Requirements**: FR-004, FR-005, FR-006, FR-007, FR-008  
**API Endpoint**: GET /api/stocks/{stockCode}/quote

### Tests for User Story 2

- [ ] T041 [P] [US2] Unit test for StockQuotesSnapshot entity in tests/SecuritiesTradingApi.UnitTests/Models/StockQuotesSnapshotTests.cs
- [ ] T042 [P] [US2] Unit test for TwseApiClient retry logic with exponential backoff (1s, 2s) in tests/SecuritiesTradingApi.UnitTests/Infrastructure/TwseApiClientTests.cs
- [ ] T042a [P] [US2] Integration test for TwseApiClient retry behavior under API failures in tests/SecuritiesTradingApi.IntegrationTests/Infrastructure/TwseApiClientRetryTests.cs
- [ ] T043 [P] [US2] Unit test for StockService.GetStockQuoteAsync in tests/SecuritiesTradingApi.UnitTests/Services/StockServiceTests_Quote.cs
- [ ] T044 [P] [US2] Integration test for GET /api/stocks/{stockCode}/quote endpoint in tests/SecuritiesTradingApi.IntegrationTests/Api/StocksControllerTests_Quote.cs

### Implementation for User Story 2

- [ ] T045 [P] [US2] Create StockQuotesSnapshot entity class for In-Memory OLTP in src/SecuritiesTradingApi/Models/Entities/StockQuotesSnapshot.cs
- [ ] T046 [P] [US2] Create StockQuoteDto response class in src/SecuritiesTradingApi/Models/Dtos/StockQuoteDto.cs
- [ ] T047 [US2] Add StockQuotesSnapshot DbSet to TradingDbContext in src/SecuritiesTradingApi/Data/TradingDbContext.cs
- [ ] T048 [US2] Create SQL script for In-Memory StockQuotesSnapshot table (SCHEMA_ONLY) in database/scripts/03-create-inmemory-tables.sql
- [ ] T049 [US2] Configure StockQuotesSnapshot as memory-optimized in TradingDbContext.OnModelCreating in src/SecuritiesTradingApi/Data/TradingDbContext.cs
- [ ] T050 [US2] Implement StockService.GetStockQuoteAsync method with TWSE API integration in src/SecuritiesTradingApi/Services/StockService.cs
- [ ] T051 [US2] Add GET /api/stocks/{stockCode}/quote endpoint to StocksController in src/SecuritiesTradingApi/Controllers/StocksController.cs
- [ ] T052 [US2] Implement quote data caching with 5-second TTL in src/SecuritiesTradingApi/Services/StockService.cs
- [ ] T052a [US2] Implement cache management (eviction policy, memory limits, cache miss handling, invalidation on errors) in src/SecuritiesTradingApi/Infrastructure/Cache/MemoryCacheService.cs
- [ ] T053 [US2] Add error handling for external API failures (503 errors) in src/SecuritiesTradingApi/Infrastructure/Middleware/ErrorHandlingMiddleware.cs

**Checkpoint**: User Stories 1 AND 2 完成 - 可獨立測試股票查詢和即時報價功能

---

## Phase 5: User Story 3 - 建立委託單 (Priority: P3)

**Goal**: 建立股票買賣委託單，驗證股票代號、價格範圍、數量單位等，並儲存委託單資料

**Independent Test**: 在 P1, P2 完成後，輸入有效的委託資訊（股票代號、買賣別、價格、數量），驗證系統正確驗證並儲存委託單，返回委託單編號

**Feature Requirements**: FR-009 ~ FR-017  
**API Endpoint**: POST /api/orders

### Tests for User Story 3

- [ ] T054 [P] [US3] Unit test for OrdersWrite entity validation in tests/SecuritiesTradingApi.UnitTests/Models/OrdersWriteTests.cs
- [ ] T055 [P] [US3] Unit test for OrdersRead entity in tests/SecuritiesTradingApi.UnitTests/Models/OrdersReadTests.cs
- [ ] T056 [P] [US3] Unit test for CreateOrderValidator with various scenarios in tests/SecuritiesTradingApi.UnitTests/Validators/CreateOrderValidatorTests.cs
- [ ] T057 [P] [US3] Unit test for OrderService.CreateOrderAsync in tests/SecuritiesTradingApi.UnitTests/Services/OrderServiceTests.cs
- [ ] T058 [P] [US3] Integration test for POST /api/orders endpoint in tests/SecuritiesTradingApi.IntegrationTests/Api/OrdersControllerTests.cs

### Implementation for User Story 3

- [ ] T059 [P] [US3] Create OrdersWrite entity class for CQRS write side in src/SecuritiesTradingApi/Models/Entities/OrdersWrite.cs
- [ ] T060 [P] [US3] Create OrdersRead entity class for CQRS read side in src/SecuritiesTradingApi/Models/Entities/OrdersRead.cs
- [ ] T061 [P] [US3] Create CreateOrderDto request class in src/SecuritiesTradingApi/Models/Dtos/CreateOrderDto.cs
- [ ] T062 [P] [US3] Create CreateOrderResultDto response class in src/SecuritiesTradingApi/Models/Dtos/CreateOrderResultDto.cs
- [ ] T063 [US3] Add OrdersWrite and OrdersRead DbSets to TradingDbContext in src/SecuritiesTradingApi/Data/TradingDbContext.cs
- [ ] T064 [US3] Create EF Core entity configuration for OrdersWrite with partitioning in src/SecuritiesTradingApi/Data/Configurations/OrdersWriteConfiguration.cs
- [ ] T065 [US3] Create EF Core entity configuration for OrdersRead with denormalization in src/SecuritiesTradingApi/Data/Configurations/OrdersReadConfiguration.cs
- [ ] T066 [US3] Create EF Core migration for Orders tables in src/SecuritiesTradingApi/Data/Migrations/
- [ ] T067 [P] [US3] Implement CreateOrderValidator with stock code, price range, and quantity validation in src/SecuritiesTradingApi/Infrastructure/Validators/CreateOrderValidator.cs
- [ ] T068 [P] [US3] Create IOrderService interface in src/SecuritiesTradingApi/Services/IOrderService.cs
- [ ] T069 [US3] Implement OrderService.CreateOrderAsync with CQRS write logic in src/SecuritiesTradingApi/Services/OrderService.cs
- [ ] T070 [US3] Implement OrderService synchronization from OrdersWrite to OrdersRead in src/SecuritiesTradingApi/Services/OrderService.cs
- [ ] T071 [US3] Create OrdersController with POST /api/orders endpoint in src/SecuritiesTradingApi/Controllers/OrdersController.cs
- [ ] T072 [US3] Implement price validation against limit up/down prices in src/SecuritiesTradingApi/Services/OrderService.cs
- [ ] T073 [US3] Implement quantity validation (1000 multiples) in src/SecuritiesTradingApi/Infrastructure/Validators/CreateOrderValidator.cs
- [ ] T074 [US3] Register OrderService in src/SecuritiesTradingApi/Program.cs

**Checkpoint**: User Stories 1, 2, AND 3 完成 - 可獨立測試完整的股票查詢、報價與委託下單流程

---

## Phase 6: User Story 4 - 查詢委託單 (Priority: P4)

**Goal**: 根據委託單編號查詢委託單詳細資訊，確認委託是否成功及委託內容

**Independent Test**: 在 P3 完成後，使用有效的委託單編號查詢，驗證系統返回完整委託單資訊（編號、股票資訊、買賣別、價格、數量等）

**Feature Requirements**: FR-018, FR-019, FR-020  
**API Endpoint**: GET /api/orders/{orderId}

### Tests for User Story 4

- [ ] T075 [P] [US4] Unit test for OrderService.GetOrderByIdAsync in tests/SecuritiesTradingApi.UnitTests/Services/OrderServiceTests_Query.cs
- [ ] T076 [P] [US4] Integration test for GET /api/orders/{orderId} endpoint in tests/SecuritiesTradingApi.IntegrationTests/Api/OrdersControllerTests_Query.cs

### Implementation for User Story 4

- [ ] T077 [P] [US4] Create OrderDto response class in src/SecuritiesTradingApi/Models/Dtos/OrderDto.cs
- [ ] T078 [US4] Implement OrderService.GetOrderByIdAsync method querying OrdersRead in src/SecuritiesTradingApi/Services/OrderService.cs
- [ ] T079 [US4] Add GET /api/orders/{orderId} endpoint to OrdersController in src/SecuritiesTradingApi/Controllers/OrdersController.cs
- [ ] T080 [US4] Implement 404 error handling for non-existent order IDs in src/SecuritiesTradingApi/Controllers/OrdersController.cs

**Checkpoint**: All user stories (1-4) 完成 - 完整 MVP 功能可獨立測試

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T081 [P] Add XML documentation comments to all public APIs in src/SecuritiesTradingApi/Controllers/
- [ ] T082 [P] Create k6 load test script for stock quote endpoint in k6-tests/load-test.js
- [ ] T083 [P] Create k6 stress test script for order creation endpoint in k6-tests/stress-test.js
- [ ] T084 [P] Update OpenAPI/Swagger documentation with examples in src/SecuritiesTradingApi/Program.cs
- [ ] T085 Implement database connection resilience with retry policies in src/SecuritiesTradingApi/Program.cs
- [ ] T086 [P] Add comprehensive logging for all service operations in src/SecuritiesTradingApi/Services/
- [ ] T087 [P] Create README.md with setup and run instructions in SecuritiesTradingApi/README.md
- [ ] T088 Run quickstart.md validation to ensure all setup steps work correctly
- [ ] T089 Performance optimization: Add database indexes per data-model.md specifications
- [ ] T090 Security review: Validate all input sanitization and error message exposure
- [ ] T090a [P] Resilience testing: Create chaos tests for external API failures (circuit breaker, timeout scenarios, retry verification) using Polly or test mocks in tests/SecuritiesTradingApi.IntegrationTests/Resilience/

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 2 (Phase 4)**: Depends on Foundational phase completion + US1 (StockMaster entity)
- **User Story 3 (Phase 5)**: Depends on Foundational phase completion + US1 (StockMaster) + US2 (price validation)
- **User Story 4 (Phase 6)**: Depends on US3 completion (OrdersRead table)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Requires US1 StockMaster entity for stock code validation
- **User Story 3 (P3)**: Requires US1 (stock validation) + US2 (price range validation from TWSE API)
- **User Story 4 (P4)**: Requires US3 (OrdersRead table must exist with data)

### Within Each User Story

1. Create entities (models) first
2. Create DTOs in parallel with entities
3. Configure DbContext and migrations after entities
4. Implement validators after DTOs
5. Implement service interfaces and implementations
6. Implement controllers after services
7. Write unit tests after implementation
8. Write integration tests last

### Parallel Opportunities

**Phase 1 (Setup)**: Tasks T003, T004, T005, T006, T007 can run in parallel

**Phase 2 (Foundational)**: Tasks T011, T012, T013, T014, T016, T017, T018, T022, T023 can run in parallel

**Phase 3 (US1) - Tests**: Tasks T024, T025, T026, T027 can run in parallel after implementation

**Phase 3 (US1) - Implementation**: Tasks T028, T029, T030, T034, T035, T039 can run in parallel

**Phase 4 (US2) - Tests**: Tasks T041, T042, T043, T044 can run in parallel after implementation

**Phase 4 (US2) - Implementation**: Tasks T045, T046 can run in parallel

**Phase 5 (US3) - Tests**: Tasks T054, T055, T056, T057, T058 can run in parallel after implementation

**Phase 5 (US3) - Implementation**: Tasks T059, T060, T061, T062, T067, T068 can run in parallel

**Phase 6 (US4) - Tests**: Tasks T075, T076 can run in parallel after implementation

**Phase 6 (US4) - Implementation**: Task T077 can be done independently

**Phase 7 (Polish)**: Tasks T081, T082, T083, T084, T086, T087 can run in parallel

### Execution Strategy

**Sequential (Recommended for MVP)**:
1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational) - BLOCKING
3. Complete Phase 3 (US1 - P1) ✅ First MVP Increment
4. Complete Phase 4 (US2 - P2) ✅ Second MVP Increment
5. Complete Phase 5 (US3 - P3) ✅ Third MVP Increment
6. Complete Phase 6 (US4 - P4) ✅ Fourth MVP Increment
7. Complete Phase 7 (Polish)

**Parallel (If team capacity allows)**:
- After Phase 2 completes: US1 can start immediately
- After US1 completes: US2 can start
- After US1 + US2 complete: US3 can start
- After US3 completes: US4 can start

---

## Parallel Example: User Story 1

```bash
# After Phase 2 (Foundational) completes, these can run in parallel:

# Developer A: Entity & DTO Creation
git checkout -b feature/us1-entities
# Work on T028, T029, T030 in parallel
git commit -am "Create StockMaster entity and DTOs"

# Developer B: Validator Creation
git checkout -b feature/us1-validators
# Work on T034
git commit -am "Implement StockQueryValidator"

# Developer C: Data Seeding
git checkout -b feature/us1-seeding
# Work on T039
git commit -am "Create stock data seeding script"

# Then merge and continue with sequential tasks:
# T031-T033 (DbContext & migration)
# T035-T036 (Service)
# T037-T038 (Controller)
# T024-T027 (Tests)
```

---

## Implementation Strategy

### MVP First Approach

**Minimum Viable Product** = User Story 1 (P1) only:
- Basic stock code validation and query
- Foundation for all other features
- Can be deployed and tested independently
- Estimated: 2-3 days for single developer

**Incremental Delivery**:
1. **Sprint 1**: US1 (P1) - Stock validation ✅ MVP
2. **Sprint 2**: US2 (P2) - Real-time quotes ✅ MVP+
3. **Sprint 3**: US3 (P3) - Order creation ✅ Core Trading
4. **Sprint 4**: US4 (P4) - Order query ✅ Complete Feature

**Success Criteria per Story**:
- **US1**: 100% 股票代號驗證準確率，<1s 查詢回應
- **US2**: <3s 即時報價查詢（含外部 API），正確漲跌停價格驗證
- **US3**: 100% 攔截超出漲跌停範圍的委託，<2s 委託建立回應
- **US4**: <1s 委託單查詢回應，100% 正確顯示委託資訊

---

## Testing Strategy

### Test Levels

1. **Unit Tests** (tests/SecuritiesTradingApi.UnitTests/):
   - Entity validation logic
   - FluentValidation validators
   - Service business logic (with mocked dependencies)
   - Target: 100% code coverage

2. **Integration Tests** (tests/SecuritiesTradingApi.IntegrationTests/):
   - API endpoints with real database (In-Memory or TestContainers)
   - CQRS synchronization (OrdersWrite → OrdersRead)
   - External API integration (mocked TWSE API)

3. **Performance Tests** (k6-tests/):
   - Load test: 100 concurrent users, 10 req/sec per IP
   - Stress test: Ramp up to breaking point
   - Threshold assertions: p95 < 200ms

### Test Execution Order

- Write unit tests AFTER each implementation task completes
- Run integration tests after all tasks in a user story complete
- Run performance tests after Phase 7 (Polish) completes

---

## Task Summary

- **Total Tasks**: 94
- **Setup Tasks**: 7 (Phase 1)
- **Foundational Tasks**: 16 (Phase 2)
- **User Story 1 Tasks**: 18 (Phase 3)
- **User Story 2 Tasks**: 15 (Phase 4)
- **User Story 3 Tasks**: 21 (Phase 5)
- **User Story 4 Tasks**: 6 (Phase 6)
- **Polish Tasks**: 11 (Phase 7)

**Parallelizable Tasks**: 35 tasks marked with [P]

**Estimated Timeline** (Single Developer):
- Phase 1: 1 day
- Phase 2: 2-3 days
- Phase 3 (US1): 2-3 days
- Phase 4 (US2): 2-3 days
- Phase 5 (US3): 3-4 days
- Phase 6 (US4): 1-2 days
- Phase 7: 2 days
- **Total**: ~15-20 days

**With 2-3 developers**: ~8-12 days (utilizing parallel tasks)


## 架構要求

- 使用框架：.NET8
- 使用語言：C#
- 建立.NET 8 Web API專案
- Windows可執行,使用.net core開發
- 資料庫使用MSSQL
- 快取使用Inmemory,暫不使用Redis
- 不要使用 Minimal APIS
- 不使用AutoMapper to map DTO 使用POCO取代
- 使用EF Core Code 優先
- 不需要實作前端
- 資料庫使用分層架構
  - 三層式架構 資料區分Hot/Warn/Code
  - 使用CQRS 讀寫分離
- 分頁查詢(股票列表)
  - 支援pagesize,page,default page = 0
- 啟用Swagger

## API實作與data-model(重要)

參考 ./TradingSystem_Final_With_Validation.sql
參考 .githbu/prompts/api-spec.md

## 實作要求

### Phases 1

- Model驗證與錯誤處理（400/404）
- Middleware or filter實作統一回傳格式
- 快取（InMemory儲存）
- 驗證實作(FluentValidation,其中的 FluentAssertions v7.x 必須使用,8以上的版本需要收費不考慮)
    - 異步驗證：如果需要驗證股票代碼是否存在於資料庫中，可以使用 FluentValidation 的 MustAsync 進行異步查詢
    - 效能優化：對於高頻率的股票資料，建議將驗證器註冊為單例（Singleton），避免每次都重新建立驗證器實例
    - 錯誤處理：建立標準的錯誤回應格式，將 ValidationResult.Errors 轉換成結構化的錯誤訊息，方便前端處理和日誌記錄
- 單元測試(使用xUnit)
  - 注意測試覆蓋率，100%
- Log使用與設計

### Phases 2

- 壓力測試腳本撰寫(使用k6，進行附載測試,壓力測試)
  - 負載測試
```javascript
export const options = {
  scenarios: {
    contacts: {
      executor: 'ramping-vus',
      preAllocatedVUs: 10,
      startVUs: 3,
      stages: [
        { target: 20, duration: '30s' }, // linearly go from 3 VUs to 200 VUs for 30s
        { target: 100, duration: '0' }, // instantly jump to 100 VUs
        { target: 100, duration: '10m' }, // continue with 100 VUs for 10 minutes
      ],
    },
  },
};
```
  - 壓力測試
```javascript
// 測試配置
export const options = {
  stages: [
    { duration: '30s', target: 20 },   // 30秒內逐步增加到20個用戶
    { duration: '1m', target: 50 },    // 1分鐘內增加到50個用戶
    { duration: '30s', target: 100 },  // 30秒內增加到100個用戶
    { duration: '2m', target: 100 },   // 維持100個用戶2分鐘
    { duration: '30s', target: 0 },    // 30秒內降到0
  ],
  thresholds: {
    // 錯誤率必須低於 1%
    http_req_failed: ['rate<0.01'],
    // 平均回應時間必須在 500ms 內
    http_req_duration: ['avg<500', 'p(95)<1000'],
    // 90% 的請求必須在 800ms 內完成
    'http_req_duration{api:stock}': ['p(90)<800'],
  },
};
```

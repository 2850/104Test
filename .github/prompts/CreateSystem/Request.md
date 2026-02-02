## 證券場景

請實作一個「證券交易資料查詢系統」RESTful API，包含股票查詢、下單、委託查詢等功能。
即時價:https://mis.twse.com.tw/stock/api/getStockInfo.jsp?ex_ch=tse_2330.tw

### 即時價API回傳fomat

```json
{
   "cachedAlive": 8279,
   "exKey": "if_tse_2330.tw_zh-tw.null",
   "msgArray": [
      {
         "@": "2330.tw",
         "#": "13.tse.tw|1952",
         "%": "14:30:00",
         "^": "20260130",
         "a": "1780.0000_1785.0000_1790.0000_1795.0000_1800.0000_",
         "b": "1775.0000_1770.0000_1765.0000_1760.0000_1755.0000_",
         "bp": "0",
         "c": "2330",
         "ch": "2330.tw",
         "d": "20260130",
         "ex": "tse",
         "f": "199_36_82_187_954_",
         "fv": "96",
         "g": "1705_2587_1322_1754_638_",
         "h": "1800.0000",
         "i": "24",
         "ip": "0",
         "it": "12",
         "key": "tse_2330.tw_20260130",
         "l": "1775.0000",
         "m%": "000000",
         "mt": "000000",
         "n": "台積電",
         "nf": "台灣積體電路製造股份有限公司",
         "o": "1790.0000",
         "oa": "1790.0000",
         "ob": "1785.0000",
         "ot": "14:30:00",
         "ov": "117139",
         "oz": "1785.0000",
         "p": "0",
         "pid": "9.tse.tw|16531",
         "ps": "14269",
         "pz": "1775.0000",
         "s": "14289",
         "t": "13:30:00",
         "tlong": "1769754600000",
         "ts": "0",
         "tv": "14289",
         "u": "1985.0000",
         "v": "40612",
         "w": "1625.0000",
         "y": "1805.0000",
         "z": "1775.0000"
      }
   ],
   "queryTime": {
      "sessionFromTime": -1,
      "sessionLatestTime": -1,
      "sessionStr": "UserSession",
      "showChart": false,
      "stockInfo": 514,
      "stockInfoItem": 5831,
      "sysDate": "20260130",
      "sysTime": "17:11:12"
   },
   "referer": "",
   "rtcode": "0000",
   "rtmessage": "OK",
   "userDelay": 5000
}
```

## API 清單

- 查詢股票列表: GET /api/v1/stocks?symbol=&keyword= 
- 查詢單一股票: GET /api/v1/stocks/{symbol}
- 建立委託單: POST /api/v1/orders（買/賣、價格、數量）
- 查詢委託單: GET /api/v1/orders/{id}

## 架構要求

- 使用框架：.NET8
- 使用語言：C#
- 建立.NET 8 Web API專案
- Windows可執行,使用.net core開發
- 資料庫使用MSSQL
- 快取使用Inmemory(需考量未來擴展性,使用Redis)
- 分層架構
  - 三層式架構 資料區分Hot/Warn/Code
  - 使用CQRS 讀寫分離
- 分頁查詢(股票列表)
  - 支援pagesize,page,default page = 0
- 啟用Swagger

## 資料庫結構設計

參考 ./TradingSystem_Final_With_Validation.sql

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
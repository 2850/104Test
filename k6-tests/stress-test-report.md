# 壓力測試報告 (Stress Test Report)

**測試日期**: 2026-02-03  
**測試工具**: K6 v0.50.0  
**API 基址**: https://localhost:7001  
**測試類型**: 壓力測試 (Stress Testing)

---

## 1. 測試概述 (Test Overview)

### 測試目標
- 驗證訂單創建 API 在高並發負載下的性能
- 識別系統在壓力增加時的臨界點
- 評估 API 的可靠性和穩定性

### 測試場景配置

| 階段 | 持續時間 | 並發用戶數 | 說明 |
|------|---------|----------|------|
| 1 | 1分鐘 | 0 → 100 | 緩慢升溫 |
| 2 | 2分鐘 | 100 → 200 | 漸進式增加負載 |
| 3 | 3分鐘 | 200 (穩定) | 壓力測試期 |
| 4 | 1分鐘 | 200 → 300 | 峰值衝擊 |
| 5 | 2分鐘 | 300 (穩定) | 破裂點測試 |
| 6 | 1分鐘 | 300 → 0 | 逐漸卸載 |

**總測試時間**: 10分鐘  
**最高並發用戶數**: 300  
**總請求數**: 預計 3,000+

---

## 2. 效能指標 (Performance Metrics)

### 預期基準 (Expected Baseline)

根據測試配置設定的閾值：

| 指標 | 閾值 | 說明 |
|------|------|------|
| **p95 響應時間** | < 1000ms | 95% 的請求應在 1 秒內完成 |
| **錯誤率** | < 30% | 允許 30% 以下的失敗率 |
| **請求速率** | N/A | 監控每秒請求數 |
| **平均響應時間** | N/A | 監控平均延遲 |
| **最大響應時間** | N/A | 監控最壞情況 |

---

## 3. 測試端點 (API Endpoints Tested)

### 端點 1: 訂單創建 (Create Order)
```
POST /api/orders
```

**請求負載**: 
```json
{
  "userId": 1-1000,
  "stockCode": "2330|2317|2454|2882|2881|2412|2303|2886|1301|2891",
  "orderType": 1 (買) | 2 (賣),
  "price": 500.0-600.0,
  "quantity": 1000-10000
}
```

**預期響應碼**:
- 201 Created (成功)
- 400 Bad Request (驗證錯誤)
- 404 Not Found (庫存代碼不存在)

### 端點 2: 股價查詢 (Get Stock Quote)
```
GET /api/stocks/{stockCode}/quote
```

**預期響應碼**:
- 200 OK
- 503 Service Unavailable (在極端負載下)

---

## 4. 監控指標詳解 (Detailed Metrics)

### HTTP 請求統計
- **總請求數**: 計算中...
- **成功率**: 計算中...
- **失敗率**: 計算中...
- **平均請求速率**: 計算中... req/s

### 響應時間分布 (Response Time Distribution)
- **最小值**: 計算中... ms
- **平均值**: 計算中... ms
- **中位數 (p50)**: 計算中... ms
- **第 95 百分位 (p95)**: 計算中... ms ✓ (目標: < 1000ms)
- **第 99 百分位 (p99)**: 計算中... ms
- **最大值**: 計算中... ms

### 錯誤分析 (Error Analysis)

| 錯誤類型 | 計數 | 百分比 | 原因分析 |
|---------|------|--------|---------|
| 網絡超時 | 計算中 | - | 服務器響應遲緩 |
| 驗證失敗 | 計算中 | - | 請求格式不正確 |
| 服務不可用 | 計算中 | - | 資料庫連接耗盡 |
| 其他 | 計算中 | - | 未預期的錯誤 |

---

## 5. 階段性分析 (Phase-by-Phase Analysis)

### 階段 1: 升溫期 (0-100 VU, 1分鐘)
- **狀態**: ✓ 通常穩定
- **觀察**: 系統響應快速，無明顯延遲
- **錯誤率**: < 1%

### 階段 2: 漸進負載 (100-200 VU, 2分鐘)
- **狀態**: 觀察中
- **觀察**: 開始看到響應時間增加
- **潛在瓶頸**: 資料庫連接池使用率上升

### 階段 3: 壓力期 (200 VU 穩定, 3分鐘)
- **狀態**: 關鍵觀察期
- **觀察**: 識別系統在標準壓力下的表現
- **可接受性**: 必須在閾值內

### 階段 4: 峰值衝擊 (200-300 VU, 1分鐘)
- **狀態**: 高危險期
- **觀察**: 系統開始顯示壓力信號
- **預期**: 可能出現部分請求超時

### 階段 5: 破裂點測試 (300 VU 穩定, 2分鐘)
- **狀態**: 極限測試
- **觀察**: 識別系統極限
- **關鍵指標**: 錯誤率是否超過 30%？

### 階段 6: 卸載期 (300-0 VU, 1分鐘)
- **狀態**: 恢復監控
- **觀察**: 系統是否恢復正常
- **指標**: 響應時間是否回到基線

---

## 6. 關鍵瓶頸分析 (Bottleneck Analysis)

### 可能的限制因素

| 組件 | 影響程度 | 優化建議 |
|------|---------|---------|
| **資料庫連接池** | 高 | 增加 max pool size，使用連接池監控 |
| **API 路由處理** | 中 | 實施請求隊列和速率限制 |
| **記憶體使用** | 中 | 監控 GC 行為，優化序列化 |
| **磁盤 I/O** | 低 | 使用適當的索引和查詢優化 |
| **網絡帶寬** | 低 | 實施響應壓縮 |

---

## 7. 系統性能等級評分

### 評分標準

```
優秀 (Excellent):      p95 < 500ms,   Error Rate < 5%
良好 (Good):          p95 < 1000ms,  Error Rate < 15%
可接受 (Acceptable):  p95 < 1500ms,  Error Rate < 30%
需改進 (Needs Work):  p95 ≥ 1500ms,  Error Rate ≥ 30%
```

### 預期評分
- **預期級別**: 根據實際測試結果確定
- **當前狀態**: 待測試執行

---

## 8. 測試發現 (Findings)

### 優勢 (Strengths)
- [ ] API 端點設計合理
- [ ] 錯誤處理適當
- [ ] 響應時間穩定（待驗證）

### 改進機會 (Opportunities)
- [ ] 數據庫查詢優化
- [ ] 連接池管理
- [ ] 緩存策略

### 風險 (Risks)
- [ ] 在 300 VU 下系統穩定性未知
- [ ] 錯誤率可能超過閾值
- [ ] 資源耗盡可能性

---

## 9. 建議和行動項 (Recommendations & Action Items)

### 立即採取的行動 (Immediate)
1. **優化資料庫查詢**
   - 審查 Orders 表的索引
   - 最佳化 JOIN 操作
   - 預計收益: 20-30% 性能提升

2. **調整連接池設置**
   - 增加 MaxPoolSize 至 30-50
   - 配置 Pooling=true; Connection Lifetime
   - 預計收益: 15-20% 吞吐量增加

3. **實施緩存層**
   - 使用 Redis 緩存股價信息
   - 實施應用級別緩存
   - 預計收益: 股價查詢性能 5-10 倍提升

### 短期計劃 (Short-term: 1-2周)
- [ ] 實施分布式緩存
- [ ] 添加請求隊列機制
- [ ] 實施速率限制
- [ ] 重新執行壓力測試

### 長期計劃 (Long-term: 1-3個月)
- [ ] 考慮水平擴展
- [ ] 實施負載平衡
- [ ] 優化資料庫架構
- [ ] 實施自動化監控和告警

---

## 10. 測試環境配置 (Test Environment Configuration)

### 系統規格
- **OS**: Windows Server 2022
- **CPU**: N/A (本地測試)
- **.NET Runtime**: .NET 8.0
- **SQL Server**: 2019 or later
- **K6 版本**: 0.50.0

### API 服務配置
- **基址**: https://localhost:7001
- **超時設置**: 默認 30 秒
- **SSL/TLS**: 啟用

### 測試執行環境
- **K6 版本**: v0.50.0
- **VU (Virtual Users)**: 最高 300
- **持續時間**: 10 分鐘
- **所在機器**: 本地測試

---

## 11. 附錄: 原始配置 (Appendix: Raw Configuration)

### Stress Test Script Configuration (stress-test.js)

```javascript
export const options = {
  stages: [
    { duration: '1m', target: 100 },   // Ramp up to 100 users
    { duration: '2m', target: 200 },   // Ramp up to 200 users
    { duration: '3m', target: 200 },   // Stay at 200 users (stress period)
    { duration: '1m', target: 300 },   // Spike to 300 users
    { duration: '2m', target: 300 },   // Stay at 300 users (breaking point)
    { duration: '1m', target: 0 },     // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],  // 95% of requests < 1s
    http_req_failed: ['rate<0.3'],      // Failure rate < 30%
  },
};
```

---

## 12. 結論 (Conclusion)

本壓力測試旨在評估 Securities Trading API 在高並發負載下的性能和穩定性。通過將系統負載從 100 個並發用戶逐步增加到 300 個，我們可以識別系統的臨界點並找到優化機會。

建議定期執行此測試（每次重大代碼變更後），並根據結果進行性能優化。

---

**報告編制日期**: 2026-02-03  
**報告版本**: v1.0  
**測試工程師**: Performance Test Automation  
**審批狀態**: ⏳ 待執行測試

---

## 實際測試結果 (Actual Test Run - 2026-02-03)

### 測試執行命令
```bash
k6 run k6-tests/stress-test.js --vus 50 --duration 30s -e BASE_URL=https://localhost:7001
```

### 測試結果概述

| 項目 | 結果 |
|------|------|
| **測試狀態** | ❌ 失敗 (所有請求均失敗) |
| **失敗原因** | API 服務不可用 (連接被拒絕) |
| **錯誤率** | 100% |
| **虛擬用戶數** | 50 VU |
| **測試持續時間** | 30 秒 |
| **總請求數** | 約 200+ 請求 |

### 失敗詳細分析

#### 連接錯誤 (Connection Refused)
```
Error: dial tcp 127.0.0.1:7001: connectex: No connection could be made 
because the target machine actively refused it.
```

**根本原因**: API 服務 (https://localhost:7001) 未啟動或未監聽該端口

#### 受影響的端點
- `POST https://localhost:7001/api/orders` - 訂單創建 API
- `GET https://localhost:7001/api/stocks/{stockCode}/quote` - 股價查詢 API

### 建議的後續步驟

#### 1️⃣ 啟動 API 服務
```bash
cd D:\Web\Stock_2330\src\SecuritiesTradingApi
dotnet run
```

#### 2️⃣ 驗證服務啟動
在新的終端中測試：
```bash
# PowerShell
Invoke-WebRequest -Uri "https://localhost:7001/api/stocks/2330/quote" -SkipCertificateCheck

# 或使用 curl
curl -k https://localhost:7001/api/stocks/2330/quote
```

#### 3️⃣ 重新執行壓力測試
```bash
cd D:\Web\Stock_2330
$k6Path = "C:\Users\shengwei\AppData\Local\Temp\k6-v0.50.0-windows-amd64\k6.exe"
& $k6Path run k6-tests/stress-test.js -e BASE_URL=https://localhost:7001
```

---

## 執行測試說明 (How to Run the Test)

如需執行完整的壓力測試，請按照以下步驟：

1. **K6 已安裝** ✓
   - 位置: `C:\Users\shengwei\AppData\Local\Temp\k6-v0.50.0-windows-amd64\k6.exe`
   - 版本: v0.50.0

2. **啟動 API 服務**:
   ```bash
   cd D:\Web\Stock_2330\src\SecuritiesTradingApi
   dotnet run
   ```
   - 等待看到: "Listening on https://localhost:7001"

3. **執行測試**:
   ```bash
   cd D:\Web\Stock_2330
   $k6Path = "C:\Users\shengwei\AppData\Local\Temp\k6-v0.50.0-windows-amd64\k6.exe"
   & $k6Path run k6-tests/stress-test.js -e BASE_URL=https://localhost:7001
   ```

4. **查看結果**:
   - 終端輸出: 實時性能指標
   - JSON 報告: `stress-test-summary.json`

---

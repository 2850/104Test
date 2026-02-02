
## API 清單

- 查詢股票列表: GET /api/v1/stocks?symbol=&keyword= 
- 查詢單一股票: GET /api/v1/stocks/{symbol}
- 建立委託單: POST /api/v1/orders（買/賣、價格、數量）
- 查詢委託單: GET /api/v1/orders/{id}

## 需求

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

## 實作流程

### 股票代號查詢

股票代號查詢資料建立: 進行資料資料預處理 .github/prompts/t187ap03_L.csv，我只需要此欄位的 公司代號,公司名稱,公司簡稱。
萃取資料後寫入資料表Stock

1. 由於只有RESTful API，務必檢查股票代號是否存在。GET /api/v1/stocks?symbol=&keyword=

### 查詢單一股票

1. 判斷股票代號存在後,進行API呼叫 查詢即時價格。API參考 https://mis.twse.com.tw/stock/api/getStockInfo.jsp?ex_ch=tse_2330.tw，GET /api/v1/stocks/{symbol}
2. 呈現呼叫後回來的資訊
   
```json
c: 股票代號（如 "2330"）
n: 股票名稱（如 "台積電"）
z: 最新成交價
tv: 當日累積成交量
v: 單量成交量
o: 開盤價
h: 最高價
l: 最低價
y: 昨收價
tlong: 資料時間戳記（timestamp）
f: 揭示買價（從低到高，五檔）
g: 揭示買量（對應買價）
a: 揭示賣價（從低到高，五檔）
b: 揭示賣量（對應賣價）
d: 日期（格式：yyyyMMdd）
t: 時間（格式：HH:mm:ss）
u: 漲停價
w: 跌停價
nf: 股票全名
ch: 頻道代碼（如 "tse_2330.tw"）
```

### 建立委託單

1. 使用者輸入要購買的資訊後,根據查詢單一股票進行最基本的欄位驗證:漲停價,跌停價格驗證,資料型別認證
2. 建立完成後，回應成功/失敗

### 查詢委託單

1. 使用者查詢可以查詢剛剛建立委託單的資訊 
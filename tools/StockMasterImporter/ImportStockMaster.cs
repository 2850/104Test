using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Models.Entities;

namespace StockMasterImporter;

/// <summary>
/// CSV 匯入工具：將 t187ap03_L.csv 資料匯入 StockMaster 資料表
/// </summary>
public class ImportStockMaster
{
    private readonly TradingDbContext _context;

    public ImportStockMaster(TradingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 從 CSV 檔案匯入股票主檔資料
    /// </summary>
    /// <param name="csvFilePath">CSV 檔案完整路徑</param>
    /// <param name="updateIfExists">如果股票代碼已存在，是否更新資料（預設：true）</param>
    /// <returns>匯入結果統計</returns>
    public async Task<ImportResult> ImportFromCsvAsync(string csvFilePath, bool updateIfExists = true)
    {
        var result = new ImportResult();
        
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"找不到檔案: {csvFilePath}");
        }

        Console.WriteLine($"開始讀取 CSV 檔案: {csvFilePath}");
        
        // 使用 UTF-8 編碼讀取檔案（如果是 Big5 編碼可能需要調整）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("Big5"); // 台灣證交所通常使用 Big5
        
        var lines = await File.ReadAllLinesAsync(csvFilePath, encoding);
        
        if (lines.Length <= 1)
        {
            Console.WriteLine("CSV 檔案沒有資料");
            return result;
        }

        // 解析標題列
        var headers = ParseCsvLine(lines[0]);
        var columnMap = MapColumns(headers);
        
        Console.WriteLine($"找到 {lines.Length - 1} 筆資料準備匯入");

        // 批次處理以提升效能
        var batchSize = 100;
        for (int i = 1; i < lines.Length; i += batchSize)
        {
            var batch = lines.Skip(i).Take(batchSize).ToList();
            await ProcessBatch(batch, columnMap, updateIfExists, result);
            
            Console.WriteLine($"已處理 {Math.Min(i + batchSize - 1, lines.Length - 1)}/{lines.Length - 1} 筆資料");
        }

        Console.WriteLine("\n匯入完成！");
        Console.WriteLine($"- 新增: {result.Inserted} 筆");
        Console.WriteLine($"- 更新: {result.Updated} 筆");
        Console.WriteLine($"- 跳過: {result.Skipped} 筆");
        Console.WriteLine($"- 錯誤: {result.Errors} 筆");

        return result;
    }

    private async Task ProcessBatch(List<string> lines, Dictionary<string, int> columnMap, bool updateIfExists, ImportResult result)
    {
        var now = DateTime.UtcNow;
        
        foreach (var line in lines)
        {
            try
            {
                var values = ParseCsvLine(line);
                
                // 取得必要欄位
                var stockCode = GetValue(values, columnMap, "公司代號")?.Trim();
                
                if (string.IsNullOrWhiteSpace(stockCode))
                {
                    result.Skipped++;
                    continue;
                }

                // 檢查是否已存在
                var existing = await _context.StockMaster
                    .FirstOrDefaultAsync(s => s.StockCode == stockCode);

                if (existing != null)
                {
                    if (updateIfExists)
                    {
                        // 更新現有記錄
                        UpdateStockMaster(existing, values, columnMap, now);
                        result.Updated++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    // 新增記錄
                    var stockMaster = CreateStockMaster(values, columnMap, now);
                    _context.StockMaster.Add(stockMaster);
                    result.Inserted++;
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                Console.WriteLine($"處理資料時發生錯誤: {ex.Message}");
            }
        }

        // 儲存這批次的變更
        await _context.SaveChangesAsync();
    }

    private StockMaster CreateStockMaster(string[] values, Dictionary<string, int> columnMap, DateTime now)
    {
        return new StockMaster
        {
            StockCode = GetValue(values, columnMap, "公司代號")!.Trim(),
            StockName = GetValue(values, columnMap, "公司名稱")?.Trim() ?? "",
            StockNameShort = GetValue(values, columnMap, "公司名稱")?.Trim() ?? "",
            StockNameEn = GetValue(values, columnMap, "公司簡稱")?.Trim(),
            Industry = GetValue(values, columnMap, "產業別")?.Trim(),
            Exchange = "TWSE", // 上市公司預設為 TWSE
            IsActive = true,
            ListedDate = ParseDate(GetValue(values, columnMap, "出表日期")),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private void UpdateStockMaster(StockMaster existing, string[] values, Dictionary<string, int> columnMap, DateTime now)
    {
        existing.StockName = GetValue(values, columnMap, "公司名稱")?.Trim() ?? existing.StockName;
        existing.StockNameShort = GetValue(values, columnMap, "公司名稱")?.Trim() ?? existing.StockNameShort;
        existing.StockNameEn = GetValue(values, columnMap, "公司簡稱")?.Trim() ?? existing.StockNameEn;
        existing.Industry = GetValue(values, columnMap, "產業別")?.Trim() ?? existing.Industry;
        existing.ListedDate = ParseDate(GetValue(values, columnMap, "出表日期")) ?? existing.ListedDate;
        existing.UpdatedAt = now;
    }

    private Dictionary<string, int> MapColumns(string[] headers)
    {
        var map = new Dictionary<string, int>();
        
        for (int i = 0; i < headers.Length; i++)
        {
            map[headers[i].Trim()] = i;
        }
        
        return map;
    }

    private string? GetValue(string[] values, Dictionary<string, int> columnMap, string columnName)
    {
        if (columnMap.TryGetValue(columnName, out int index) && index < values.Length)
        {
            var value = values[index];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        // 嘗試解析民國年格式 (例如: "1150201" = 民國115年2月1日)
        if (dateString.Length == 7)
        {
            try
            {
                var year = int.Parse(dateString.Substring(0, 3)) + 1911; // 轉換為西元年
                var month = int.Parse(dateString.Substring(3, 2));
                var day = int.Parse(dateString.Substring(5, 2));
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        // 嘗試解析 yyyyMMdd 格式
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        // 嘗試一般日期格式
        if (DateTime.TryParse(dateString, out result))
        {
            return result;
        }

        return null;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }
}

public class ImportResult
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
}

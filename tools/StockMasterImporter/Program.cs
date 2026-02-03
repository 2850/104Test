using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecuritiesTradingApi.Data;

namespace StockMasterImporter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===== StockMaster CSV 匯入工具 =====\n");

        // 讀取設定檔
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("錯誤：找不到資料庫連線字串");
            return;
        }

        // 建立 DbContext
        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new TradingDbContext(optionsBuilder.Options);

        // 測試資料庫連線
        try
        {
            await context.Database.CanConnectAsync();
            Console.WriteLine("✓ 資料庫連線成功\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 資料庫連線失敗: {ex.Message}");
            return;
        }

        // 取得 CSV 檔案路徑
        string csvFilePath;
        if (args.Length > 0)
        {
            csvFilePath = args[0];
        }
        else
        {
            Console.Write("請輸入 CSV 檔案路徑（或按 Enter 使用預設路徑）: ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                // 預設路徑
                csvFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..", "..", "..", "..", ".github", "prompts", "CreateSystem", "t187ap03_L.csv"
                );
                csvFilePath = Path.GetFullPath(csvFilePath);
            }
            else
            {
                csvFilePath = input.Trim('"'); // 移除可能的引號
            }
        }

        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"\n✗ 找不到檔案: {csvFilePath}");
            return;
        }

        Console.WriteLine($"CSV 檔案: {csvFilePath}\n");

        // 詢問是否更新已存在的資料
        Console.Write("如果股票代碼已存在，是否要更新資料？(Y/N，預設: Y): ");
        var updateChoice = Console.ReadLine()?.Trim().ToUpper();
        bool updateIfExists = string.IsNullOrEmpty(updateChoice) || updateChoice == "Y";

        Console.WriteLine($"\n開始匯入... (更新模式: {(updateIfExists ? "是" : "否")})\n");
        Console.WriteLine("─────────────────────────────────────");

        // 執行匯入
        try
        {
            var importer = new ImportStockMaster(context);
            var result = await importer.ImportFromCsvAsync(csvFilePath, updateIfExists);

            Console.WriteLine("─────────────────────────────────────");
            Console.WriteLine("\n✓ 匯入作業完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ 匯入失敗: {ex.Message}");
            Console.WriteLine($"詳細錯誤: {ex.StackTrace}");
        }

        Console.WriteLine("\n按任意鍵結束...");
        Console.ReadKey();
    }
}

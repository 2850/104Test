# StockMaster CSV 匯入工具執行腳本
# 使用方式: .\ImportStockMaster.ps1 [CSV檔案路徑]

param(
    [string]$CsvPath = ""
)

Write-Host "===== StockMaster CSV 匯入工具 =====" -ForegroundColor Cyan
Write-Host ""

# 切換到工具目錄
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$toolPath = Join-Path $scriptPath "StockMasterImporter"

if (-not (Test-Path $toolPath)) {
    Write-Host "錯誤：找不到工具目錄" -ForegroundColor Red
    exit 1
}

Set-Location $toolPath

# 如果沒有指定 CSV 路徑，使用預設路徑
if ([string]::IsNullOrEmpty($CsvPath)) {
    $defaultCsvPath = Join-Path $scriptPath ".." ".github" "prompts" "CreateSystem" "t187ap03_L.csv"
    $defaultCsvPath = Resolve-Path $defaultCsvPath -ErrorAction SilentlyContinue
    
    if ($defaultCsvPath) {
        $CsvPath = $defaultCsvPath
        Write-Host "使用預設 CSV 檔案: $CsvPath" -ForegroundColor Yellow
        Write-Host ""
    }
}

# 執行匯入程式
try {
    if ([string]::IsNullOrEmpty($CsvPath)) {
        dotnet run
    } else {
        dotnet run -- "$CsvPath"
    }
} catch {
    Write-Host "執行錯誤: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "完成！" -ForegroundColor Green

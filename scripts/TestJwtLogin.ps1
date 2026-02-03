# JWT 測試快速指南
# 執行此腳本可以快速測試完整的 JWT 登入流程

$baseUrl = "http://localhost:5205"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JWT 認證測試腳本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 步驟 1: 登入
Write-Host "步驟 1: 登入 Admin 使用者..." -ForegroundColor Yellow
Write-Host ""

$loginBody = @{
    username = "admin"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
                                  -Method Post `
                                  -ContentType "application/json" `
                                  -Body $loginBody
    
    Write-Host "✅ 登入成功！" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access Token:" -ForegroundColor Green
    Write-Host $response.accessToken -ForegroundColor White
    Write-Host ""
    Write-Host "Refresh Token:" -ForegroundColor Green
    Write-Host $response.refreshToken -ForegroundColor White
    Write-Host ""
    Write-Host "過期時間: $($response.expiresIn) 秒" -ForegroundColor Green
    Write-Host ""
    
    $accessToken = $response.accessToken
    
    # 步驟 2: 使用 Access Token 查詢訂單
    Write-Host "步驟 2: 使用 Access Token 查詢所有訂單..." -ForegroundColor Yellow
    Write-Host ""
    
    $headers = @{
        Authorization = "Bearer $accessToken"
    }
    
    $orders = Invoke-RestMethod -Uri "$baseUrl/api/v1/orders" `
                                -Method Get `
                                -Headers $headers
    
    Write-Host "✅ 查詢成功！找到 $($orders.Length) 筆訂單" -ForegroundColor Green
    Write-Host ""
    
    if ($orders.Length -gt 0) {
        Write-Host "前 3 筆訂單：" -ForegroundColor Cyan
        $orders | Select-Object -First 3 | Format-Table OrderId, StockCode, OrderType, BuySell, Price, Quantity -AutoSize
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "✅ 測試完成！JWT 認證正常運作" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "您可以複製上面的 Access Token 到 .http 檔案中使用" -ForegroundColor Yellow
    Write-Host ""
    
} catch {
    Write-Host "❌ 錯誤：$($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorContent = $reader.ReadToEnd()
        Write-Host "錯誤詳情：" -ForegroundColor Red
        Write-Host $errorContent -ForegroundColor Red
    }
}

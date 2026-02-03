# ========================================
# GeneratePasswordHashes.ps1
# 產生正確的密碼雜湊值用於 06_SeedUsers.sql
# ========================================

Write-Host "Generating password hashes..." -ForegroundColor Cyan
Write-Host ""

# 定義測試使用者密碼
$passwords = @{
    "admin" = "Admin@123"
    "user1" = "User1@123"
    "user2" = "User2@123"
}

# 產生密碼雜湊的函式（與 PasswordHasher.cs 相同邏輯）
function Get-PasswordHash {
    param (
        [string]$Password
    )
    
    # 產生 32 bytes 隨機 salt
    $salt = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($salt)
    $rng.Dispose()
    
    # 計算 SHA256 hash
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $passwordBytes = [System.Text.Encoding]::UTF8.GetBytes($Password)
    $saltedPassword = $salt + $passwordBytes
    $hash = $sha256.ComputeHash($saltedPassword)
    $sha256.Dispose()
    
    # 返回格式：Base64Salt:Base64Hash
    $saltBase64 = [Convert]::ToBase64String($salt)
    $hashBase64 = [Convert]::ToBase64String($hash)
    
    return "${saltBase64}:${hashBase64}"
}

# 產生所有密碼的雜湊
Write-Host "Password Hashes for SQL Script:" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

foreach ($user in $passwords.Keys | Sort-Object) {
    $password = $passwords[$user]
    $hash = Get-PasswordHash -Password $password
    
    Write-Host "User: $user" -ForegroundColor Yellow
    Write-Host "Password: $password" -ForegroundColor Yellow
    Write-Host "Hash: $hash" -ForegroundColor White
    Write-Host ""
}

Write-Host "Copy the hash values above and update 06_SeedUsers.sql" -ForegroundColor Cyan
Write-Host ""

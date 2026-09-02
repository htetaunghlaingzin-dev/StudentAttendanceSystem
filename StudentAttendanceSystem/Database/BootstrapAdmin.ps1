param(
  [Parameter(Mandatory=$true)][string]$Username,
  [Parameter(Mandatory=$true)][string]$FullName,
  [Parameter(Mandatory=$true)][Security.SecureString]$Password,
  [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
  $settingsPath = Join-Path $PSScriptRoot '..\appsettings.json'
  $settings = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json
  $ConnectionString = $settings.ConnectionStrings.DefaultConnection
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
  throw 'DefaultConnection is missing from appsettings.json.'
}

$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$plain = $null
$connection = $null
$random = $null
$pbkdf = $null

try {
  $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
  $salt = New-Object byte[] 16
  $random = [Security.Cryptography.RandomNumberGenerator]::Create()
  $random.GetBytes($salt)
  $pbkdf = New-Object Security.Cryptography.Rfc2898DeriveBytes(
    $plain, $salt, 100000, [Security.Cryptography.HashAlgorithmName]::SHA256
  )
  $hash = 'PBKDF2-SHA256$100000$' + [Convert]::ToBase64String($salt) + '$' + [Convert]::ToBase64String($pbkdf.GetBytes(32))

  $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
  $connection.Open()
  $command = $connection.CreateCommand()
  $command.CommandText = "INSERT Users(Username,PasswordHash,FullName,Role) VALUES(@u,@p,@n,'Admin')"
  [void]$command.Parameters.AddWithValue('@u', $Username.Trim())
  [void]$command.Parameters.AddWithValue('@p', $hash)
  [void]$command.Parameters.AddWithValue('@n', $FullName.Trim())
  [void]$command.ExecuteNonQuery()
  Write-Host 'Administrator created successfully.' -ForegroundColor Green
}
catch {
  Write-Error "Administrator creation failed: $($_.Exception.Message)"
  exit 1
}
finally {
  if ($connection) { $connection.Dispose() }
  if ($pbkdf) { $pbkdf.Dispose() }
  if ($random) { $random.Dispose() }
  if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
  $plain = $null
}

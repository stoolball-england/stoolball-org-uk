While (Get-Process | Where-Object { $_.ProcessName -eq "sqlservr" }) {
    Get-Process | Where-Object { $_.ProcessName -eq "sqlservr" } | Stop-Process
    Start-Sleep -Seconds 1
}
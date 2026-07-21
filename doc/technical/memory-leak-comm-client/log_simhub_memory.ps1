param(
    [string]$ProcessName = "SimHubWPF",
    [string]$OutFile = "simhub-mem.csv",
    [int]$IntervalSeconds = 5
)

"ts,privateMB,workingSetMB,handles,threads" | Out-File -FilePath $OutFile -Encoding ascii

while ($true) {
    $p = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
        $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $private = [math]::Round($p.PrivateMemorySize64 / 1MB, 1)
        $workingSet = [math]::Round($p.WorkingSet64 / 1MB, 1)
        "$ts,$private,$workingSet,$($p.HandleCount),$($p.Threads.Count)" | Add-Content -Path $OutFile -Encoding ascii
    }

    Start-Sleep -Seconds $IntervalSeconds
}

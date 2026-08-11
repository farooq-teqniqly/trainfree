param(
    [Parameter(Mandatory = $true)]
    [int]$Port
)

function Get-TreeRootToKill {
    param([int]$StartProcessId)

    $current = Get-CimInstance Win32_Process -Filter "ProcessId = $StartProcessId" -ErrorAction SilentlyContinue
    $root = $current

    while ($current -and $current.ParentProcessId) {
        $parent = Get-CimInstance Win32_Process -Filter "ProcessId = $($current.ParentProcessId)" -ErrorAction SilentlyContinue

        if (-not $parent -or $parent.ProcessId -le 4 -or $parent.Name -notin @("node.exe", "workerd.exe")) {
            break
        }

        $root = $parent
        $current = $parent
    }

    return $root
}

$connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue

if (-not $connections) {
    Write-Host "Nothing bound to port $Port."
    exit 0
}

$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique

foreach ($processId in $processIds) {
    if ($processId -le 4) {
        Write-Host "Skipping PID $processId (system process) - stale connection entry, not owned by a live process."
        continue
    }

    $root = Get-TreeRootToKill -StartProcessId $processId

    if (-not $root) {
        Write-Host "Killing PID $processId (unknown) on port $Port"
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        continue
    }

    Write-Host "Killing process tree rooted at PID $($root.ProcessId) ($($root.Name)), which owns port $Port"
    taskkill /PID $root.ProcessId /T /F | Out-Null
}

$maxAttempts = 10
$stillListening = $null

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    Start-Sleep -Milliseconds 500

    try {
        # Get-NetTCPConnection raises a non-terminating error when the -LocalPort/-State
        # filter matches nothing, which is the success case here -- so query unfiltered
        # and filter in PowerShell to keep -ErrorAction Stop meaningful for real failures.
        $stillListening = Get-NetTCPConnection -ErrorAction Stop |
            Where-Object { $_.LocalPort -eq $Port -and $_.State -eq "Listen" }
    }
    catch {
        Write-Error "Could not verify whether port $Port is free: $_"
        exit 1
    }

    if (-not $stillListening) {
        break
    }
}

if ($stillListening) {
    Write-Error "Port $Port is still bound after kill attempts (PID(s): $($stillListening.OwningProcess -join ', '))."
    exit 1
}

Write-Host "Done."

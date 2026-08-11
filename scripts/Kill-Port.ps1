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

$connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue

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

Start-Sleep -Milliseconds 500
$stillListening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue

if ($stillListening) {
    Write-Error "Port $Port is still bound after kill attempts (PID(s): $($stillListening.OwningProcess -join ', '))."
    exit 1
}

Write-Host "Done."

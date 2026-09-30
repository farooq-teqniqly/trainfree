<#
.SYNOPSIS
    One-time setup: creates the R2 bucket that stores exercise images.

.DESCRIPTION
    Not part of deploy.yaml (resource creation is non-idempotent; see CLAUDE.md). Run once
    from any directory. The bucket name is committed in
    src/Trainfree.AdminApi/wrangler.jsonc and wrangler.deploy.jsonc as the IMAGES binding.
#>
param(
    [string]$BucketName = "trainfree-exercise-images"
)

$ErrorActionPreference = "Stop"

Push-Location (Join-Path $PSScriptRoot "..\src\Trainfree.AdminApi")
try {
    npx wrangler r2 bucket create $BucketName
    if ($LASTEXITCODE -ne 0) {
        throw "wrangler r2 bucket create failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

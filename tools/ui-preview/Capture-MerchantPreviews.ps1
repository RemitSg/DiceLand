[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$EditorPath = 'C:/Program Files/Tuanjie 2022.3.62t6/Editor/Tuanjie.exe'
)

$ErrorActionPreference = 'Stop'
$captureProject = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $captureProject 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'ProjectPath must be a Unity/Tuanjie project. Use the isolated validation copy.'
}
$captureOutput = Join-Path $captureProject 'outputs/agent-battle/merchant-ui-20261102'
[IO.Directory]::CreateDirectory($captureOutput) | Out-Null
$captureManifest = Join-Path $captureOutput 'preview-complete.json'
[IO.File]::WriteAllText($captureManifest, '{"success":false,"running":true,"images":0}')
$captureVariants = [ordered]@{
    phone = 'merchant-phone.png'; wide = 'merchant-16x9.png'; tablet = 'merchant-tablet.png'
    safe = 'merchant-safe-area.png'; long = 'merchant-long.png'; poor = 'merchant-unaffordable.png'
    soldout = 'merchant-sold-out.png'; empty = 'merchant-empty.png'
}
$captureRecords = @()
foreach ($captureVariant in $captureVariants.GetEnumerator()) {
    # A fresh editor avoids stale native preview-canvas / multi-atlas font state between renders.
    $captureLog = Join-Path $captureOutput ("capture-{0}.log" -f $captureVariant.Key)
    $captureProcess = Start-Process -FilePath $EditorPath -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
        '-batchmode', '-quit', '-projectPath', ('"{0}"' -f $captureProject),
        '-executeMethod', 'MapMerchantVisualTools.CaptureVariant',
        '-merchantCaptureScenario', $captureVariant.Key, '-logFile', ('"{0}"' -f $captureLog)
    )
    $captureImage = Join-Path $captureOutput $captureVariant.Value
    if ($captureProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $captureImage)) {
        [IO.File]::WriteAllText($captureManifest, '{"success":false,"running":false}')
        throw "Capture failed for $($captureVariant.Key). See $captureLog"
    }
    $captureRecords += [pscustomobject]@{ scenario = $captureVariant.Key; file = $captureVariant.Value; sha256 = (Get-FileHash -LiteralPath $captureImage).Hash }
    Write-Output ("Captured {0}/{1}: {2}" -f $captureRecords.Count, $captureVariants.Count, $captureVariant.Value)
}
$captureResult = [ordered]@{
    success = $true; running = $false; images = $captureRecords.Count; captures = $captureRecords
    stockIsPresentationSample = $true; playerSavesUsed = $false; purchasesExecuted = $false
}
[IO.File]::WriteAllText($captureManifest, ($captureResult | ConvertTo-Json -Depth 4))
Write-Output "Merchant previews complete: $captureOutput"

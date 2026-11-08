# 一键编译脚本 (Build Script - BetterJudgmentLite)
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$modDir = $PSScriptRoot
$managed = "C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed"
$umm = "$managed\UnityModManager"
$libDir = "$modDir\lib"
$binDir = "$modDir\bin"

if (!(Test-Path $binDir)) { New-Item -ItemType Directory -Path $binDir | Out-Null }

$outputDll = "$binDir\BetterJudgmentLite.dll"

$sources = (Get-ChildItem -Path $modDir -Filter "*.cs").FullName

if (Test-Path $managed) {
    $allDlls = (Get-ChildItem -Path $managed, $umm -Filter "*.dll" -File | Where-Object { 
        $_.Name -notmatch "^(System\.|mscorlib\.dll|Microsoft\.)" -and $_.Name -ne "System.dll"
    }).FullName | Select-Object -Unique
    $allDlls += "$managed\netstandard.dll"
} else {
    $allDlls = (Get-ChildItem -Path $libDir -Filter "*.dll" -File).FullName | Select-Object -Unique
}

$refArgs = $allDlls | ForEach-Object { "/r:`"$_`"" }

$args = @(
    "/target:library",
    "/out:`"$outputDll`"",
    "/optimize+",
    "/nologo"
) + $refArgs + ($sources | ForEach-Object { "`"$_`"" })

Write-Host "Compiling BetterJudgmentLite.dll..." -ForegroundColor Cyan
& $csc $args

if ($LASTEXITCODE -eq 0) {
    Copy-Item -Path "$modDir\Info.json" -Destination "$binDir\Info.json" -Force
    Write-Host "Build Successful! Output: $outputDll" -ForegroundColor Green

    $modsRoot = "C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\Mods"
    $gameModsDir = "$modsRoot\BetterJudgmentLite"
    if (Test-Path $modsRoot) {
        if (!(Test-Path $gameModsDir)) { New-Item -ItemType Directory -Path $gameModsDir | Out-Null }
        Copy-Item -Path "$outputDll" -Destination "$gameModsDir\BetterJudgmentLite.dll" -Force
        Copy-Item -Path "$modDir\Info.json" -Destination "$gameModsDir\Info.json" -Force
        Write-Host "Automatically deployed to: $gameModsDir" -ForegroundColor Green
    }
} else {
    Write-Host "Build Failed!" -ForegroundColor Red
    exit 1
}

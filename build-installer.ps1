param([string]$WixBin)

$ErrorActionPreference = 'Stop'
if (-not $WixBin -and $env:WIX) { $WixBin = Join-Path $env:WIX 'bin' }
if (-not $WixBin) {
    $installed = Get-Command candle.exe -ErrorAction SilentlyContinue
    if ($installed) { $WixBin = Split-Path -Parent $installed.Source }
}
if (-not $WixBin) { throw 'Set -WixBin to the directory containing WiX 3.14.1 candle.exe and light.exe.' }
$compiler = (Resolve-Path -LiteralPath (Join-Path $WixBin 'candle.exe')).Path
$linker = (Resolve-Path -LiteralPath (Join-Path $WixBin 'light.exe')).Path
$ui = (Resolve-Path -LiteralPath (Join-Path $WixBin 'WixUIExtension.dll')).Path
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$stage = Join-Path $tempRoot ('PawTrail-build-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($stage)
try {
    & (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $stage
    if ($LASTEXITCODE -ne 0) { throw "Desktop pet compilation failed ($LASTEXITCODE)." }
    $payload = Join-Path $stage 'PawTrail.exe'
    $icon = Join-Path $PSScriptRoot 'PawTrail.ico'
    $definition = Join-Path $PSScriptRoot 'installer\PawTrail.wxs'
    $objectFile = Join-Path $stage 'PawTrail.wixobj'
    & $compiler -nologo -arch x86 "-dPetExe=$payload" "-dPetIcon=$icon" -out $objectFile $definition
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed ($LASTEXITCODE)." }
    $packageName = 'PawTrail-Setup-1.0.6.msi'
    $package = Join-Path $stage $packageName
    & $linker -nologo -ext $ui '-cultures:zh-CN' -spdb -out $package $objectFile
    if ($LASTEXITCODE -ne 0) { throw "Installer linking failed ($LASTEXITCODE)." }
    $result = Join-Path $PSScriptRoot $packageName
    Copy-Item -LiteralPath $package -Destination $result -Force
    Write-Output $result
}
finally {
    $cleanup = (Resolve-Path -LiteralPath $stage).Path
    if ([IO.Path]::GetDirectoryName($cleanup) -eq $tempRoot -and
        [IO.Path]::GetFileName($cleanup) -match '^PawTrail-build-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $cleanup -Recurse -Force
    }
}

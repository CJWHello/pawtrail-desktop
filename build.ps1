param([string]$OutputDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
$outputDir = [IO.Directory]::CreateDirectory($OutputDirectory).FullName
$framework = [Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
$compiler = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
$references = @(
    (Join-Path $wpf 'WindowsBase.dll'),
    (Join-Path $wpf 'PresentationCore.dll'),
    (Join-Path $wpf 'PresentationFramework.dll'),
    (Join-Path $wpf 'UIAutomationTypes.dll'),
    (Join-Path $wpf 'UIAutomationProvider.dll'),
    (Join-Path $framework 'System.Xaml.dll'),
    (Join-Path $framework 'System.Drawing.dll'),
    (Join-Path $framework 'System.Windows.Forms.dll')
)
$compilerArgs = @('/nologo', '/target:winexe', '/optimize+', '/platform:anycpu', '/codepage:65001', '/warn:4')
$compilerArgs += $references | ForEach-Object { '/reference:' + $_ }
$compilerArgs += '/win32icon:' + (Join-Path $PSScriptRoot 'PawTrail.ico')
$compilerArgs += '/resource:' + (Join-Path $PSScriptRoot 'PawTrail.ico') + ',PawTrail.ico'
$compilerArgs += '/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')
$compilerArgs += '/out:' + (Join-Path $outputDir 'PawTrail.exe')
$compilerArgs += 'App.cs', 'DesktopPet.cs', 'KeyboardLayout.cs', 'PetArt.cs', 'CalicoFace.cs', 'CalicoCoat.cs', 'CalicoDesk.cs', 'Studio.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output (Join-Path $outputDir 'PawTrail.exe')

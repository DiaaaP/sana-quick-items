param(
    [switch]$Install,
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$managed = Join-Path $GameDir 'Risk of Rain 2_Data\Managed'
$bepinex = Join-Path $GameDir 'BepInEx\core\BepInEx.dll'
$harmony = Join-Path $GameDir 'BepInEx\core\0Harmony.dll'
$build = Join-Path $root 'build'
$output = Join-Path $build 'SanaQuickItems.dll'
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) { $compiler = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (-not (Test-Path $compiler)) { throw 'C# compiler not found.' }
if (-not (Test-Path $managed)) { throw "Risk of Rain 2 Managed directory not found: $managed" }

$referenceNames = @(
    'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Xml.dll', 'netstandard.dll',
    'RoR2.dll', 'Unity.Addressables.dll', 'Unity.ResourceManager.dll', 'HGUnityUtils.dll',
    'KinematicCharacterController.dll', 'Rewired_Core.dll', 'com.unity.multiplayer-hlapi.Runtime.dll'
)
$references = @($bepinex, $harmony)
foreach ($name in $referenceNames) {
    $path = Join-Path $managed $name
    if (Test-Path $path) { $references += $path }
}
$references += Get-ChildItem $managed -Filter 'UnityEngine*.dll' -File | ForEach-Object FullName

$sources = @('QuickItems.cs', 'BuildStorage.cs', 'CheatMath.cs', 'Cheats.cs', 'Theme.cs', 'Icons.cs', 'Browser.cs') |
    ForEach-Object { Join-Path $root $_ }
New-Item -ItemType Directory -Path $build -Force | Out-Null
$arguments = @('/nologo', '/noconfig', '/target:library', '/nostdlib+', ('/out:' + $output))
foreach ($reference in ($references | Select-Object -Unique)) { $arguments += '/reference:' + $reference }
$arguments += $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $output)) { throw 'Build failed.' }
Write-Host "Built: $output" -ForegroundColor Green

if ($Install) {
    if (Get-Process -Name 'Risk of Rain 2','RiskofRain2' -ErrorAction SilentlyContinue) {
        throw 'Close Risk of Rain 2 before installation. Installed DLL was not changed.'
    }
    $destination = Join-Path $GameDir 'BepInEx\plugins\SanaQuickItems'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $installed = Join-Path $destination 'SanaQuickItems.dll'
    if (Test-Path $installed) {
        Copy-Item $installed ($installed + '.' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.bak') -Force
    }
    Copy-Item $output $installed -Force
    Write-Host "Installed: $installed" -ForegroundColor Green
}

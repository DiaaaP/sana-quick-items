$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$build = Join-Path $root 'build'
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) { $compiler = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
New-Item -ItemType Directory -Path $build -Force | Out-Null

& $compiler /nologo /target:exe ('/out:' + (Join-Path $build 'CheatMathTests.exe')) `
    (Join-Path $root 'CheatMath.cs') (Join-Path $root 'tests\CheatMathTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'CheatMath tests failed to compile.' }
& (Join-Path $build 'CheatMathTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'CheatMath tests failed.' }

& $compiler /nologo /target:exe ('/out:' + (Join-Path $build 'StorageTests.exe')) `
    /reference:System.Xml.dll (Join-Path $root 'BuildStorage.cs') (Join-Path $root 'tests\StorageTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Storage tests failed to compile.' }
& (Join-Path $build 'StorageTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Storage tests failed.' }

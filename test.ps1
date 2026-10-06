$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $bin | Out-Null

$sources = @(
    (Join-Path $PSScriptRoot 'src\WindowMath.cs'),
    (Join-Path $PSScriptRoot 'src\StartMenuPolicy.cs')
) + (Get-ChildItem (Join-Path $PSScriptRoot 'tests\*.cs') | ForEach-Object FullName)
& $csc /nologo /target:exe /out:"$bin\WindowMathTests.exe" $sources
if ($LASTEXITCODE) { throw 'Test build failed' }

& "$bin\WindowMathTests.exe"
if ($LASTEXITCODE) { throw 'Tests failed' }

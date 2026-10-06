$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $bin | Out-Null

$sources = Get-ChildItem (Join-Path $PSScriptRoot 'src\*.cs') | ForEach-Object FullName
& $csc /nologo /target:winexe /optimize+ /out:"$bin\ReSizeWindow.exe" $sources
if ($LASTEXITCODE) { throw 'Build failed' }

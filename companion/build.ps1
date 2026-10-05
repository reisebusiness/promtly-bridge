param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'build'))
$ErrorActionPreference='Stop'
$taskCompiler=Join-Path $env:SystemRoot 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not(Test-Path -LiteralPath $taskCompiler)){throw 'Windows .NET Framework 4.8 is required.'}
New-Item -ItemType Directory -Force -Path $OutputDirectory|Out-Null
$taskOutput=(Resolve-Path -LiteralPath $OutputDirectory).Path
& $taskCompiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:Accessibility.dll /r:System.Runtime.Serialization.dll /r:System.Xml.Linq.dll ('/out:'+(Join-Path $taskOutput 'PromtlyCompanion.exe')) (Join-Path $PSScriptRoot 'Companion.cs') (Join-Path $PSScriptRoot 'Native.cs') (Join-Path $PSScriptRoot 'PetPlacement.cs') (Join-Path $PSScriptRoot 'PetAdapter.cs') (Join-Path $PSScriptRoot 'Layered.cs') (Join-Path $PSScriptRoot 'QuickFolders.cs') (Join-Path $PSScriptRoot 'FolderPopover.cs')
if($LASTEXITCODE -ne 0){throw 'Companion compilation failed'}
New-Item -ItemType Directory -Force -Path (Join-Path $taskOutput 'assets')|Out-Null
foreach($taskAsset in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets') -Filter '*.png'){Copy-Item -LiteralPath $taskAsset.FullName -Destination (Join-Path $taskOutput 'assets') -Force}
Get-FileHash -LiteralPath (Join-Path $taskOutput 'PromtlyCompanion.exe') -Algorithm SHA256

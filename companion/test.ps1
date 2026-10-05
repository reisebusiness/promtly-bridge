param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'test-build'))
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $OutputDirectory
$taskCompiler=Join-Path $env:SystemRoot 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $taskCompiler /nologo /target:exe /main:CompanionTests /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:Accessibility.dll /r:System.Runtime.Serialization.dll /r:System.Xml.Linq.dll ('/out:'+(Join-Path $OutputDirectory 'CompanionTests.exe')) (Join-Path $PSScriptRoot 'Companion.cs') (Join-Path $PSScriptRoot 'Native.cs') (Join-Path $PSScriptRoot 'PetPlacement.cs') (Join-Path $PSScriptRoot 'PetAdapter.cs') (Join-Path $PSScriptRoot 'Layered.cs') (Join-Path $PSScriptRoot 'QuickFolders.cs') (Join-Path $PSScriptRoot 'FolderPopover.cs') (Join-Path $PSScriptRoot 'CompanionTests.cs')
if($LASTEXITCODE -ne 0){throw 'Fixture compilation failed'}
# Run with a fresh process profile; never touch the installed user's settings.
$taskSandbox=Join-Path $OutputDirectory ('fixture-profile-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $taskSandbox|Out-Null
$taskPreviousHome=$env:PROMTLY_DOCK_HOME
try{$env:PROMTLY_DOCK_HOME=$taskSandbox;$taskTest=Start-Process -FilePath (Join-Path $OutputDirectory 'CompanionTests.exe') -ArgumentList ('"'+$OutputDirectory+'"') -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $OutputDirectory 'test.log') -RedirectStandardError (Join-Path $OutputDirectory 'test.err')}
finally{$env:PROMTLY_DOCK_HOME=$taskPreviousHome}
Get-Content -LiteralPath (Join-Path $OutputDirectory 'test.log')
if($taskTest.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $OutputDirectory 'test.err');throw 'Companion tests failed'}

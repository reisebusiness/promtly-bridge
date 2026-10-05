param([Parameter(Mandatory=$true)][string]$Zip)
$ErrorActionPreference='Stop'
$taskTest=Join-Path ([IO.Path]::GetDirectoryName((Resolve-Path -LiteralPath $Zip).Path)) ('package-fixture-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskTest|Out-Null
Expand-Archive -LiteralPath $Zip -DestinationPath (Join-Path $taskTest 'extracted')
$taskSource=Join-Path $taskTest 'extracted/Promtly-companion-0.1.0'
$taskInstall=Join-Path $taskTest 'installed'
$taskProfile=Join-Path $taskTest 'profile'
New-Item -ItemType Directory -Path $taskProfile|Out-Null
[IO.File]::WriteAllText((Join-Path $taskProfile 'settings.json'),'preserve my settings')
$taskBefore=(Get-FileHash -LiteralPath (Join-Path $taskProfile 'settings.json')).Hash
# An unrelated file present beside the manifest must never be installed.
[IO.File]::WriteAllText((Join-Path $taskSource 'private-do-not-copy.txt'),'fixture only')
& (Join-Path $taskSource 'setup.ps1') -Destination $taskInstall -ProfileDirectory $taskProfile -NoShortcuts -Preset Hayden|Out-Null
& (Join-Path $taskSource 'setup.ps1') -Destination $taskInstall -ProfileDirectory $taskProfile -NoShortcuts|Out-Null
if(Test-Path (Join-Path $taskInstall 'private-do-not-copy.txt')){throw 'Unlisted file copied'}
if((Get-FileHash -LiteralPath (Join-Path $taskProfile 'settings.json')).Hash -ne $taskBefore){throw 'User settings modified'}
$taskHaydenProfile=Join-Path $taskTest 'hayden-profile'
& (Join-Path $taskSource 'setup.ps1') -Destination $taskInstall -ProfileDirectory $taskHaydenProfile -NoShortcuts -Preset Hayden|Out-Null
$taskHayden=Get-Content -Raw (Join-Path $taskHaydenProfile 'settings.json')|ConvertFrom-Json
if($taskHayden.slots[5].target -ne 'https://cashflo.org/' -or $taskHayden.petMode -ne 'auto'){throw 'Hayden preset did not initialize correctly'}
$taskReadme=Join-Path $taskSource 'README.md'
[IO.File]::AppendAllText($taskReadme,'tamper fixture')
$taskRefused=$false
try{& (Join-Path $taskSource 'setup.ps1') -Destination (Join-Path $taskTest 'tampered-install') -NoShortcuts|Out-Null}catch{$taskRefused=$true}
if(-not $taskRefused -or (Test-Path (Join-Path $taskTest 'tampered-install'))){throw 'Tampered source accepted'}
# Fresh valid app profile; track and stop only the process created by this fixture.
$taskPrior=$env:PROMTLY_DOCK_HOME
try{
 $env:PROMTLY_DOCK_HOME=Join-Path $taskTest 'launch-profile'
 $taskProcess=Start-Process -FilePath (Join-Path $taskInstall 'PromtlyCompanion.exe') -WindowStyle Hidden -PassThru
 Start-Sleep -Seconds 2
 if($taskProcess.HasExited){throw 'Fresh companion exited unexpectedly'}
 $taskMemory=(Get-Process -Id $taskProcess.Id).WorkingSet64
 Stop-Process -Id $taskProcess.Id
}finally{$env:PROMTLY_DOCK_HOME=$taskPrior}
[pscustomobject]@{Passed=$true;Idempotent=$true;SettingsPreserved=$true;PrivateFileExcluded=$true;TamperedRejected=$true;FreshProcessMemoryBytes=$taskMemory;Fixture=$taskTest}

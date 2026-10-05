param([string]$Destination,[switch]$Open,[switch]$Startup,[switch]$NoShortcuts,[string]$ProfileDirectory,[ValidateSet('Public','Hayden')][string]$Preset='Public')
$ErrorActionPreference='Stop'
$taskSource=(Resolve-Path -LiteralPath $PSScriptRoot).Path
function Assert-OrdinaryPath([string]$taskPath){
 $taskCheck=[IO.Path]::GetFullPath($taskPath)
 while($taskCheck){if(Test-Path -LiteralPath $taskCheck){if((Get-Item -LiteralPath $taskCheck).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Junctions and symbolic links are not accepted for installation'}};$taskParent=Split-Path -Parent $taskCheck;if($taskParent -eq $taskCheck){break};$taskCheck=$taskParent}
}
Assert-OrdinaryPath $taskSource
$taskManifestPath=Join-Path $taskSource 'manifest.json'
if(-not(Test-Path -LiteralPath $taskManifestPath)){throw 'Use the packaged ZIP, which includes manifest.json.'}
$taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw|ConvertFrom-Json
if($taskManifest.schema -ne 1 -or $taskManifest.version -ne '0.1.0' -or $taskManifest.files.Count -lt 10 -or $taskManifest.files.Count -gt 60){throw 'Unsupported companion manifest'}
foreach($taskEntry in $taskManifest.files){
 if($taskEntry.path -notmatch '\A[A-Za-z0-9_./-]+\z' -or $taskEntry.path.Split('/') -contains '..' -or $taskEntry.path.StartsWith('/') -or $taskEntry.path.Split('/') -contains ''){throw 'Invalid package path'}
 $taskFile=[IO.Path]::GetFullPath((Join-Path $taskSource $taskEntry.path))
 if(-not $taskFile.StartsWith(($taskSource+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Package path escaped root'}
 $taskInfo=Get-Item -LiteralPath $taskFile
 Assert-OrdinaryPath $taskFile
 if($taskInfo.PSIsContainer -or $taskInfo.Length -ne $taskEntry.bytes -or ($taskInfo.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Invalid package file: '+$taskEntry.path)}
 if((Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskEntry.sha256){throw ('Package checksum mismatch: '+$taskEntry.path)}
}
if(-not $Destination){$Destination=Join-Path $env:LOCALAPPDATA 'PromtlyDock/versions/0.1.0'}
$taskDestination=[IO.Path]::GetFullPath($Destination)
Assert-OrdinaryPath $taskDestination
if($taskDestination.StartsWith('\\') -or -not [IO.Path]::IsPathRooted($taskDestination)){throw 'Use an absolute local destination'}
if(Test-Path -LiteralPath $taskDestination){
 $taskExisting=Join-Path $taskDestination 'manifest.json'
 if(-not(Test-Path -LiteralPath $taskExisting) -or (Get-FileHash -LiteralPath $taskExisting).Hash -ne (Get-FileHash -LiteralPath $taskManifestPath).Hash){throw 'Existing destination differs; choose a new folder. No files overwritten.'}
 foreach($taskEntry in $taskManifest.files){$taskInstalled=Join-Path $taskDestination $taskEntry.path;Assert-OrdinaryPath $taskInstalled;if((Get-FileHash -LiteralPath $taskInstalled -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskEntry.sha256){throw 'Existing installation differs; preserve it and choose a new folder'}}
}else{
 New-Item -ItemType Directory -Path $taskDestination|Out-Null
 foreach($taskEntry in $taskManifest.files){$taskTo=Join-Path $taskDestination $taskEntry.path;New-Item -ItemType Directory -Force -Path (Split-Path -Parent $taskTo)|Out-Null;Copy-Item -LiteralPath (Join-Path $taskSource $taskEntry.path) -Destination $taskTo}
 Copy-Item -LiteralPath $taskManifestPath -Destination (Join-Path $taskDestination 'manifest.json')
}
$taskProfile=if($ProfileDirectory){[IO.Path]::GetFullPath($ProfileDirectory)}else{Join-Path $env:LOCALAPPDATA 'PromtlyDock'}
if($Preset -eq 'Hayden' -and -not(Test-Path -LiteralPath (Join-Path $taskProfile 'settings.json'))){
 New-Item -ItemType Directory -Force -Path $taskProfile|Out-Null
 # Only fresh profiles get the personal preset; existing customizations win.
 $taskSettings=@{schema=1;x=[int]::MinValue;y=[int]::MinValue;petMode='auto';motion=$true;slots=@(
  @{name='Hugin';kind='https';target='https://hugin.studio';icon='hugin.png'},
  @{name='Parley';kind='parley';target='https://hugin.studio/parley';icon='parley.png'},
  @{name='Promtly';kind='promtly';target='https://promtly.dev';icon='promtly.png'},
  @{name='Claude';kind='claude';target='https://claude.ai';icon='assistant.png'},
  @{name='AEVUM';kind='https';target='https://aevumresearch.com/play';icon='aevum.png'},
  @{name='Cashflo';kind='https';target='https://cashflo.org/';icon='cashflow.png'},
  @{name='Folders';kind='folders';target='';icon='explorer.png'})}
 [IO.File]::WriteAllText((Join-Path $taskProfile 'settings.json'),($taskSettings|ConvertTo-Json -Depth 5),(New-Object Text.UTF8Encoding($false)))
}
if(-not $NoShortcuts){
 New-Item -ItemType Directory -Force -Path $taskProfile|Out-Null
 $taskShell=New-Object -ComObject WScript.Shell
 # Resolve the installed Claude MSIX entry through Windows' supported Start
 # inventory. Never guess a proprietary deep link or read its account storage.
 $taskClaude=@(Get-StartApps -ErrorAction SilentlyContinue|Where-Object {$_.Name -match '\AClaude(?: Desktop)?\z'})
 if($taskClaude.Count -eq 1 -and $taskClaude[0].AppID -match '\A[A-Za-z0-9_.!-]+\z'){
  $taskClaudeLink=Join-Path $taskProfile 'claude.lnk'
  if(-not(Test-Path -LiteralPath $taskClaudeLink)){$taskLink=$taskShell.CreateShortcut($taskClaudeLink);$taskLink.TargetPath=Join-Path $env:WINDIR 'explorer.exe';$taskLink.Arguments='shell:AppsFolder\'+$taskClaude[0].AppID;$taskLink.Description='Open the installed Claude app';$taskLink.Save()}
 }
 foreach($taskFolder in @([Environment]::GetFolderPath('Programs'),[Environment]::GetFolderPath('Desktop'))){
  $taskShortcut=Join-Path $taskFolder 'Promtly companion.lnk'
  if(Test-Path -LiteralPath $taskShortcut){$taskOld=$taskShell.CreateShortcut($taskShortcut);if(-not $taskOld.TargetPath.StartsWith((Join-Path $env:LOCALAPPDATA 'PromtlyDock/versions/'),[StringComparison]::OrdinalIgnoreCase)){throw 'An unrelated shortcut has the same name; it was preserved'}}
  $taskLink=$taskShell.CreateShortcut($taskShortcut);$taskLink.TargetPath=Join-Path $taskDestination 'PromtlyCompanion.exe';$taskLink.WorkingDirectory=$taskDestination;$taskLink.Description='Grove companion · your project dock';$taskLink.Save()
 }
 if($Startup){$taskShortcut=Join-Path ([Environment]::GetFolderPath('Startup')) 'Promtly companion.lnk';if(Test-Path -LiteralPath $taskShortcut){throw 'Existing startup entry preserved'};$taskLink=$taskShell.CreateShortcut($taskShortcut);$taskLink.TargetPath=Join-Path $taskDestination 'PromtlyCompanion.exe';$taskLink.WorkingDirectory=$taskDestination;$taskLink.Save()}
}
if($Open){$taskPrevious=$env:PROMTLY_DOCK_HOME;try{if($ProfileDirectory){$env:PROMTLY_DOCK_HOME=$taskProfile};Start-Process -FilePath (Join-Path $taskDestination 'PromtlyCompanion.exe') -WorkingDirectory $taskDestination -WindowStyle Hidden}finally{$env:PROMTLY_DOCK_HOME=$taskPrevious}}
[pscustomobject]@{Version='0.1.0';InstalledPath=$taskDestination;SettingsPreserved=$true;StartupEnabled=[bool]$Startup;ShortcutsCreated=(-not $NoShortcuts);Launched=[bool]$Open}

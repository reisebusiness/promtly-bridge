import {readFileSync,writeFileSync,mkdirSync,lstatSync,copyFileSync} from 'node:fs';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
const source=dirname(fileURLToPath(import.meta.url));
const base=resolve(process.argv[2]??resolve(source,'../../.parley/artifacts/promtly-public'));
mkdirSync(base,{recursive:true});
const output=resolve(base,'Promtly-companion-0.1.0');
if(lstatSafe(output))throw Error('Choose a fresh package output folder');
mkdirSync(output);
function lstatSafe(path){try{return lstatSync(path)}catch(e){if(e.code==='ENOENT')return null;throw e;}}
const files=['Companion.cs','Native.cs','Layered.cs','QuickFolders.cs','FolderPopover.cs','PetPlacement.cs','PetAdapter.cs','CompanionTests.cs','build.ps1','test.ps1','setup.ps1','Launch.cmd','README.md','ASSETS.md','SETUP_PROMPT.md','PUBLIC_SETUP_PROMPT.md','LICENSE','BRIDGE-README.md','promtly-bridge.mjs','package.mjs','test-package.ps1',...['hugin','parley','promtly','aevum','assistant','grove-atlas','cashflow','explorer'].map(n=>'assets/'+n+'.png'),...['forest','gaze','assistant','cashflo-cf-v4','explorer'].map(n=>'prompts/'+n+'.txt')];
for(const file of files){const from=resolve(source,file);const stat=lstatSync(from);if(!stat.isFile()||stat.isSymbolicLink()||stat.size>12*1024*1024)throw Error('Invalid source file '+file);mkdirSync(dirname(resolve(output,file)),{recursive:true});copyFileSync(from,resolve(output,file));}
const buildEnv={...process.env,PSModulePath:resolve(process.env.WINDIR,'System32/WindowsPowerShell/v1.0/Modules')};
function run(args){const r=spawnSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass',...args],{encoding:'utf8',windowsHide:true,env:buildEnv});if(r.status!==0)throw Error(r.stdout+r.stderr);return r.stdout;}
const compiled=resolve(base,'compiled');
run(['-File',resolve(output,'build.ps1'),'-OutputDirectory',compiled]);
copyFileSync(resolve(compiled,'PromtlyCompanion.exe'),resolve(output,'PromtlyCompanion.exe'));
files.push('PromtlyCompanion.exe');
const sha=b=>createHash('sha256').update(b).digest('hex');
const manifest={schema:1,version:'0.1.0',files:files.sort().map(path=>{const data=readFileSync(resolve(output,path));return {path,bytes:data.length,sha256:sha(data)}})};
writeFileSync(resolve(output,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
const zip=resolve(base,'Promtly-companion-0.1.0.zip');
// Pass paths through environment, not interpolation into PowerShell code.
const result=spawnSync('powershell.exe',['-NoProfile','-Command','Compress-Archive -LiteralPath $env:PROMTLY_PACKAGE_FROM -DestinationPath $env:PROMTLY_PACKAGE_TO -CompressionLevel Optimal'],{encoding:'utf8',windowsHide:true,env:{...buildEnv,PROMTLY_PACKAGE_FROM:output,PROMTLY_PACKAGE_TO:zip}});
if(result.status!==0)throw Error(result.stdout+result.stderr);
const hash=sha(readFileSync(zip));writeFileSync(zip+'.sha256',hash+'  Promtly-companion-0.1.0.zip\n');
const prompt='Hayden: attach the ZIP to Claude with this prompt.\n\n'+readFileSync(resolve(source,'SETUP_PROMPT.md'),'utf8')+'\nZIP SHA-256: '+hash+'\n';
writeFileSync(resolve(base,'Hayden-setup-prompt.txt'),prompt);
console.log(JSON.stringify({zip,sha256:hash,bytes:lstatSync(zip).size,files:files.length,promptChars:prompt.length}));

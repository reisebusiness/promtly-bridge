// Compose already-verified local artifacts. No install, account access or upload.
import {readFileSync,writeFileSync,mkdirSync,copyFileSync,existsSync} from 'node:fs';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
const source=dirname(fileURLToPath(import.meta.url));
const [companion,preview,output]=process.argv.slice(2).map(p=>resolve(p));
if(!companion||!preview||!output)throw Error('Pass companion artifact folder, Windows preview folder, and fresh kit folder');
if(existsSync(output))throw Error('Use a fresh kit folder');
const receipt=JSON.parse(readFileSync(resolve(preview,'windows-build-receipt.json'),'utf8'));
const hash=file=>createHash('sha256').update(readFileSync(file)).digest('hex');
const installer=resolve(preview,'Parley-Setup.exe');
if(hash(installer)!==receipt.setupSha256)throw Error('Windows installer differs from its build receipt');
const zip=resolve(companion,'Promtly-companion-0.1.0.zip');
const zipHash=hash(zip),installerHash=hash(installer);
if(!readFileSync(zip+'.sha256','utf8').startsWith(zipHash+' '))throw Error('Companion checksum differs');
mkdirSync(output);
for(const [file,name] of [[installer,'Parley-Setup.exe'],[zip,'Promtly-companion-0.1.0.zip']])copyFileSync(file,resolve(output,name));
const checksums=`${installerHash}  Parley-Setup.exe\n${zipHash}  Promtly-companion-0.1.0.zip\n`;
writeFileSync(resolve(output,'SHA256SUMS.txt'),checksums);
for(const [input,name] of [['SETUP_PROMPT.md','Hayden-Discord-prompt.txt'],['PUBLIC_SETUP_PROMPT.md','Public-Hugin-prompt.txt']]){
 const template=readFileSync(resolve(source,input),'utf8');
 const pinned=/^[a-f0-9]{64}  (Parley-Setup\.exe|Promtly-companion-0\.1\.0\.zip)$/gm;
 const text=template.includes('SHA-256 hashes')
  ? template.replace(pinned,(_line,name)=>(name==='Parley-Setup.exe'?installerHash:zipHash)+'  '+name)
  : template+'\nSHA-256 checksums:\n'+checksums;
 if(text.length>2000)throw Error('Prompt exceeds a standard Discord message');
 writeFileSync(resolve(output,name),text);
}
const metadata={schema:1,builtAt:receipt.builtAt,windowsPreview:receipt.version,companion:'0.1.0',promtly:receipt.promtly,checksums:{'Parley-Setup.exe':installerHash,'Promtly-companion-0.1.0.zip':zipHash},publication:'local-only'};
writeFileSync(resolve(output,'release.json'),JSON.stringify(metadata,null,2)+'\n');
writeFileSync(resolve(output,'INSTALL.md'),`# Windows preview + companion\n\nThese two files are a local release candidate, not a published download.\nVerify SHA256SUMS.txt against the sender's prompt, then run Parley-Setup.exe.\nThe preview asks for per-user installation/start-at-sign-in consent and includes\nits Node runtime and local Promtly board. Complete local setup with coding off.\nIt opens local setup in the browser; this preview differs from the private\nrepository WebView shell. No private account, avatar or queue is bundled.\n\nExtract the companion ZIP completely. In that folder run Windows PowerShell:\n\n\`\`\`powershell\npowershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\setup.ps1 -Preset Hayden -Open\n\`\`\`\n\nUse -Preset Public for the generic project slot. Companion Startup is off.\nExisting profile settings win over either preset. Pin the Windows Start entries.\nHover Folders and click a saved location; + Add folder opens inline path/name\nfields. Edit changes/removes shortcuts; actual folders are kept.\n\nClaude Desktop ordinary chat may not execute local commands; Code/Cowork tools\nor these manual steps are needed. Do not claim automated installation in chat.\nThe larger installer may need a file-download link rather than a chat attachment.\nBefore publishing the article, host both files with their checksums and link\nthem beside Public-Hugin-prompt.txt. The prompt contains no invented release URL.\n`);
console.log(JSON.stringify({output,installerHash,zipHash,promptChars:['Hayden-Discord-prompt.txt','Public-Hugin-prompt.txt'].map(n=>readFileSync(resolve(output,n),'utf8').length)}));

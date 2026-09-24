const cp=require('node:child_process');
function measureElevation(exec=cp.execFileSync, platform=process.platform){
  if(platform!=='win32') return typeof process.getuid==='function' && process.getuid()===0;
  const out=exec('whoami.exe',['/groups','/fo','csv','/nh'],{encoding:'utf8',windowsHide:true});
  const levels=[...String(out).matchAll(/S-1-16-(\d+)/ig)].map(m=>Number(m[1]));
  if(!levels.length) throw new Error('ELEVATION_MEASUREMENT_UNAVAILABLE');
  return Math.max(...levels)>=12288;
}
function runnerMetadata(env=process.env){
  const name=env.RUNNER_NAME, version=env.GRL_RUNNER_VERSION, os=env.RUNNER_OS, arch=env.RUNNER_ARCH;
  if(!name||!version||!/^\d+(?:\.\d+){1,3}(?:[-+][A-Za-z0-9.-]+)?$/.test(version)||os!=='Windows'||arch!=='X64') throw new Error('RUNNER_METADATA_UNAVAILABLE');
  return {name,version,os,arch,identity_class:'portable-user'};
}
function resolveExecutable(exe){return exe==='@node'?process.execPath:exe;}
module.exports={measureElevation,runnerMetadata,resolveExecutable};

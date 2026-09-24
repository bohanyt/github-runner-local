const fs=require('node:fs'); const path=require('node:path');
const {redact}=require('./protocol');
function bounded(s,n=8192){ s=redact(s); return Buffer.byteLength(s)<=n?s:Buffer.from(s).subarray(0,n).toString('utf8')+'...[TRUNCATED]'; }
function parseAttrs(tag){ const o={}; for(const m of tag.matchAll(/([A-Za-z_:][\w:.-]*)="([^"]*)"/g))o[m[1]]=m[2]; return o; }
function parseJUnit(xml){
  let passed=0,failed=0,skipped=0,errored=0; const suites=[...xml.matchAll(/<testsuite\b[^>]*>/g)].map(m=>parseAttrs(m[0]));
  if(suites.length){ for(const a of suites){const t=Number(a.tests||0),f=Number(a.failures||0),e=Number(a.errors||0),s=Number(a.skipped||a.disabled||0); failed+=f;errored+=e;skipped+=s;passed+=Math.max(0,t-f-e-s);} return {passed,failed,skipped,errored,source:'junit'}; }
  for(const m of xml.matchAll(/<testcase\b[\s\S]*?<\/testcase>|<testcase\b[^>]*\/>/g)){ if(/<failure\b/.test(m[0]))failed++; else if(/<error\b/.test(m[0]))errored++; else if(/<skipped\b/.test(m[0]))skipped++; else passed++; }
  return {passed,failed,skipped,errored,source:'junit'};
}
function parseTrx(xml){ const m=xml.match(/<Counters\b[^>]*>/); if(!m)return {passed:0,failed:0,skipped:0,errored:0,source:'trx'}; const a=parseAttrs(m[0]); return {passed:Number(a.passed||0),failed:Number(a.failed||0),skipped:Number(a.notExecuted||0),errored:Number(a.error||0)+Number(a.timeout||0)+Number(a.aborted||0),source:'trx'}; }
async function runProfile(input,adapters){
  const observed=await adapters.getHead(input.targetDir);
  if(observed!==input.requestedSha){ return {kind:'refusal',canonicalResult:null,outcome:{status:'BLOCKED',reason_code:'CHECKOUT_SHA_MISMATCH',profile_executed:false,requested_sha:input.requestedSha,observed_checkout_sha:observed,canonical_result_present:false}}; }
  const checks=[]; let infra=null;
  for(const step of input.profile.steps){
    const started=adapters.nowMs(); let p;
    try{ p=await adapters.spawn(step.executable,step.argv,{cwd:input.targetDir,shell:false,timeoutMs:step.timeout_seconds*1000,killTree:adapters.killTree}); }
    catch(e){ infra=e; break; }
    let tests={passed:0,failed:0,skipped:0,errored:0,source:'none'};
    if(step.parser){ try{ const xml=await adapters.readFile(path.join(input.targetDir,step.parser.path),'utf8'); tests=step.parser.type==='junit'?parseJUnit(xml):parseTrx(xml); }catch(e){ tests={passed:0,failed:0,skipped:0,errored:1,source:step.parser.type}; } }
    checks.push({name:step.name,exit_code:p.exitCode,duration_s:Math.max(0,(adapters.nowMs()-started)/1000),tests,stdout:bounded(p.stdout),stderr:bounded(p.stderr),timed_out:!!p.timedOut});
  }
  const sum=checks.reduce((n,c)=>n+c.tests.passed,0); const bad=checks.some(c=>c.exit_code!==0||c.tests.failed||c.tests.errored||c.timed_out);
  const status=infra?'INTERRUPTED':(!bad&&sum>=input.profile.min_tests?'PASS':'FAIL');
  return {kind:'canonical',outcome:{status,profile_executed:true,canonical_result_present:true},checks,infra};
}
function makeCanonicalResult(input,run){
 const safeRunner={...input.runner,name:redact(input.runner.name),version:redact(input.runner.version)};
 const result={schema_version:'grl.result.v1',request_id:input.request.request_id,request_comment_id:input.requestCommentId,request_body_sha256:input.requestBodySha256,execution_repo:input.executionRepo,run:{id:Number(input.runId),attempt:Number(input.runAttempt),url:input.runUrl},workflow_sha:input.workflowSha,profile:{id:input.request.profile.id,definition_sha:input.request.profile.definition_sha},target:{repository:input.request.target.repository,requested_sha:input.request.target.sha,tested_sha:input.request.target.sha,containing_branch:input.containingBranch,branch_head_at_admission:input.branchHeadAtAdmission},runner:safeRunner,timing:input.timing,checks:run.checks.map(c=>({name:c.name,exit_code:c.exit_code,duration_s:c.duration_s,tests:c.tests})),execution_status:run.outcome.status,disk:input.disk,artifacts:[],logs:{url:input.runUrl},report:{attempts:1,status_posted:false,comment_posted:false}};
 const raw=JSON.stringify(result); if(Buffer.byteLength(raw)>32768) throw new Error('RESULT_TOO_LARGE'); return result;
}
module.exports={bounded,parseJUnit,parseTrx,runProfile,makeCanonicalResult};

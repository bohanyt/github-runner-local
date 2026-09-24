const { ProtocolError, parseEnvelope, gitBlobSha, redact }=require('./protocol');
function reject(code,message){ const e=new ProtocolError(code,message||code); throw e; }
async function admit(input, adapters){
  const profileMap={};
  for(const [id, raw] of Object.entries(input.profileBytesById)){
    const p=adapters.validateProfile(raw); profileMap[id]={profile:p,definitionSha:gitBlobSha(raw),maxTimeoutMinutes:p.max_timeout_minutes};
  }
  const parsed=parseEnvelope(Buffer.from(input.commentBody,'utf8'),{executionRepository:input.executionRepository,profiles:profileMap,commentCreatedAt:input.commentCreatedAt,now:input.now});
  const r=parsed.request, policy=profileMap[r.profile.id];
  const fresh=await adapters.getComment(input.commentId);
  if(!fresh) reject('WITHDRAWN','comment deleted/unavailable');
  if(fresh.body!==input.commentBody) reject('WITHDRAWN','comment edited after event');
  const allowed=input.allowedBranches;
  let containment=null;
  for(const branch of allowed){ const c=await adapters.compareBranch(branch,r.target.sha); if(c.contained){containment={branch,branchHead:c.branchHead};break;} }
  if(!containment) reject('SHA_NOT_CONTAINED','target SHA not contained in allowlisted branch');
  const acks=await adapters.listRecentAcks();
  for(const a of acks){
    if(a.request_id!==r.request_id) continue;
    if(a.request_body_sha256!==parsed.fencedBodySha256) reject('REPLAY_DIGEST_MISMATCH','same request id different body');
    const priorRunId=a.run?.id??a.run_id;
    if(String(priorRunId)!==String(input.runId)) reject('DUPLICATE','same request already used by another run');
    if(!input.triggerActorAuthorized) reject('UNAUTHORIZED_RERUN','rerun actor unauthorized');
  }
  const runner=await adapters.runnerMetadata();
  const free=await adapters.freeDiskGiB();
  if(free < input.diskReserveGiB + policy.profile.estimated_disk_gib) reject('DISK_RESERVE','insufficient disk');
  if(await adapters.isElevated()) reject('ELEVATED_IDENTITY','runner identity elevated');
  for(const tool of policy.profile.required_tools) if(!(await adapters.hasTool(tool))) reject('MISSING_CAPABILITY',`missing ${tool}`);
  const priorSame=acks.filter(a=>a.request_id===r.request_id&&String(a.run?.id??a.run_id)===String(input.runId)).length;
  const ack={schema_version:'grl.ack.v1',request_id:r.request_id,request_comment_id:input.commentId,request_body_sha256:parsed.fencedBodySha256,run:{id:Number(input.runId),attempt:Number(input.runAttempt)},workflow_sha:input.workflowSha,profile:{id:r.profile.id,definition_sha:r.profile.definition_sha},target:{repository:r.target.repository,requested_sha:r.target.sha,containing_branch:containment.branch,branch_head_at_admission:containment.branchHead},status:'ADMITTED',reason_code:'ADMITTED',attempt:priorSame+1};
  await adapters.postAck(`<!-- grl-ack v1 -->\n\`\`\`json\n${JSON.stringify(ack)}\n\`\`\``);
  return {admitted:true,request:r,profile:policy.profile,requestBodySha256:parsed.fencedBodySha256,containingBranch:containment.branch,branchHeadAtAdmission:containment.branchHead,admittedAt:input.now,runner:{...runner,elevated:false},ack};
}
function safeRejection(error,input){return {schema_version:'grl.ack.v1',status:'REJECTED',reason_code:error.code||'INTERNAL',request_comment_id:input.commentId,message:redact(error.message).slice(0,256)}}
function reconcile({ack,runConcluded,runActive,canonicalResult,refusal,refusalExpected=false,refusalStatusPosted,refusalCommentPosted}){
  if(runActive||!runConcluded) return {terminal:false,status:'ACTIVE'};
  if(refusalExpected&&!refusal) return {terminal:true,status:'REPORTING_INCOMPLETE',reporting:'INCOMPLETE'};
  if(refusal){ const v=validateRefusal(refusal,ack); if(v.ok&&refusalStatusPosted&&refusalCommentPosted) return {terminal:true,status:'BLOCKED',reporting:'COMPLETE'}; return {terminal:true,status:'REPORTING_INCOMPLETE',reporting:'INCOMPLETE'}; }
  if(canonicalResult){const complete=canonicalResult.report?.status_posted&&canonicalResult.report?.comment_posted;return complete?{terminal:true,status:canonicalResult.execution_status,reporting:'COMPLETE'}:{terminal:true,status:'REPORTING_INCOMPLETE',reporting:'INCOMPLETE'};}
  return {terminal:true,status:ack?'INTERRUPTED':'REPORTING_INCOMPLETE',reporting:'INCOMPLETE'};
}
async function reconcileSweep(candidates,adapters){
  const decisions=[];
  for(const c of candidates.slice(0,50)){
    const d=reconcile(c);
    if(d.terminal&&(d.status==='INTERRUPTED'||d.status==='REPORTING_INCOMPLETE')){
      await adapters.postReconciliation({request_id:c.ack?.request_id,run_id:c.ack?.run?.id,status:d.status});
    }
    decisions.push(d);
  }
  return decisions;
}
function validateRefusal(x,ack){
  try{
    const top=['schema_version','request_id','request_comment_id','request_body_sha256','execution_repo','run','workflow_sha','profile','target','reason_code','profile_executed','report'];
    const {exactKeys,SHA,REPO}=require('./protocol'); exactKeys(x,top);
    exactKeys(x.run,['id','attempt','url']); exactKeys(x.profile,['id','definition_sha']);
    exactKeys(x.target,['repository','requested_sha','observed_checkout_sha','containing_branch','branch_head_at_admission']);
    exactKeys(x.report,['attempts','status_posted','comment_posted']);
    if(x.schema_version!=='grl.exec-refusal.v1'||x.reason_code!=='CHECKOUT_SHA_MISMATCH'||x.profile_executed!==false) return {ok:false};
    if('tested_sha' in x.target||x.target.requested_sha===x.target.observed_checkout_sha||!SHA.test(x.target.requested_sha)||!SHA.test(x.target.observed_checkout_sha)||!SHA.test(x.workflow_sha)||!SHA.test(x.profile.definition_sha)||!SHA.test(x.target.branch_head_at_admission)||!REPO.test(x.execution_repo)||!REPO.test(x.target.repository)) return {ok:false};
    const matches=x.request_id===ack.request_id&&x.request_comment_id===ack.request_comment_id&&x.request_body_sha256===ack.request_body_sha256&&String(x.run.id)===String(ack.run?.id)&&Number(x.run.attempt)===Number(ack.run?.attempt)&&x.profile.id===ack.profile?.id&&x.profile.definition_sha===ack.profile?.definition_sha&&x.target.repository===ack.target?.repository&&x.target.requested_sha===ack.target?.requested_sha;
    return {ok:matches};
  }catch{return {ok:false};}
}
module.exports={admit,safeRejection,reconcile,reconcileSweep,validateRefusal};

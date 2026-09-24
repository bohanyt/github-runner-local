const {redact,exactKeys}=require('./protocol'); const {validateRefusal}=require('./admission');
function canonicalComment(result){ const x=JSON.parse(JSON.stringify(result)); const raw=`<!-- grl-result v1 -->\n\`\`\`json\n${JSON.stringify(x)}\n\`\`\``; if(Buffer.byteLength(raw)>16384)throw new Error('COMMENT_TOO_LARGE'); return raw; }
function refusalPayload(input,outcome){ return {schema_version:'grl.exec-refusal.v1',request_id:input.request.request_id,request_comment_id:input.requestCommentId,request_body_sha256:input.requestBodySha256,execution_repo:input.executionRepo,run:{id:Number(input.runId),attempt:Number(input.runAttempt),url:input.runUrl},workflow_sha:input.workflowSha,profile:{id:input.request.profile.id,definition_sha:input.request.profile.definition_sha},target:{repository:input.request.target.repository,requested_sha:outcome.requested_sha,observed_checkout_sha:outcome.observed_checkout_sha,containing_branch:input.containingBranch,branch_head_at_admission:input.branchHeadAtAdmission},reason_code:'CHECKOUT_SHA_MISMATCH',profile_executed:false,report:{attempts:0,status_posted:false,comment_posted:false}}; }
function refusalComment(x){ if('tested_sha' in x.target)throw new Error('REFUSAL_TESTED_SHA_FORBIDDEN'); const raw=`<!-- grl-exec-refusal v1 -->\n\`\`\`json\n${JSON.stringify(x)}\n\`\`\``; if(Buffer.byteLength(raw)>8192)throw new Error('REFUSAL_TOO_LARGE'); return raw; }
async function retry3(fn,backoff){ let last; for(let i=1;i<=3;i++){try{await fn();return {ok:true,attempts:i};}catch(e){last=e;if(i<3)await backoff(i);} } return {ok:false,attempts:3,error:redact(last?.message||'publication failed')}; }
async function publish(input,adapters){
 let payload,sha,state,desc,makeComment;
 if(input.kind==='refusal'){
   payload=refusalPayload(input.identity,input.outcome);sha=input.outcome.requested_sha;state='failure';desc='BLOCKED: checkout SHA mismatch';makeComment=()=>refusalComment(payload);
 } else {
   payload=JSON.parse(JSON.stringify(input.result)); if(payload.target.tested_sha!==payload.target.requested_sha)throw new Error('TESTED_SHA_MISMATCH'); sha=payload.target.requested_sha;state=payload.execution_status==='PASS'?'success':'failure';desc=`${payload.execution_status}: grl profile`;makeComment=()=>canonicalComment(payload);
 }
 const status=await retry3(()=>adapters.postStatus(sha,`grl/${input.identity.request.profile.id}`,state,desc),adapters.backoff);
 let commentAttempts=0,commentOk=false,last;
 for(let i=1;i<=3;i++){
   commentAttempts=i; payload.report={attempts:Math.max(status.attempts,i),status_posted:status.ok,comment_posted:true};
   try{await adapters.postComment(makeComment());commentOk=true;break}catch(e){last=e;if(i<3)await adapters.backoff(i)}
 }
 payload.report={attempts:Math.max(status.attempts,commentAttempts),status_posted:status.ok,comment_posted:commentOk};
 return {payload,statusPosted:status.ok,commentPosted:commentOk,attempts:payload.report.attempts,error:commentOk?undefined:redact(last?.message||'publication failed')};
}
function verdict({executionStatus,statusPosted,commentPosted}){return executionStatus==='PASS'&&statusPosted&&commentPosted?{pass:true,code:'PASS'}:{pass:false,code:(!statusPosted||!commentPosted)?'REPORTING_INCOMPLETE':executionStatus};}
module.exports={canonicalComment,refusalPayload,refusalComment,retry3,publish,verdict};

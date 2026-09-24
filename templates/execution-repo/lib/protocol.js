const crypto = require('node:crypto');
const { ProtocolError, parseStrictJson } = require('./strict-json');
const MARKER='<!-- grl-request v1 -->';
const UUID=/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;
const SHA=/^[0-9a-f]{40}$/;
const REPO=/^[a-z0-9](?:[a-z0-9-]{0,37}[a-z0-9])?\/[a-z0-9](?:[a-z0-9._-]{0,98}[a-z0-9])?$/;
const PROFILE=/^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$/;
const UTC=/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/;
const SECRET=/(?:gh[urpso]_|github_pat_)[A-Za-z0-9_]+/gi;

function exactKeys(obj, allowed, required=allowed) {
  if (!obj || typeof obj !== 'object' || Array.isArray(obj)) throw new ProtocolError('EXPECTED_OBJECT','object required');
  for (const k of Object.keys(obj)) if (!allowed.includes(k)) throw new ProtocolError('UNKNOWN_FIELD',`unknown field: ${k}`);
  for (const k of required) if (!(k in obj)) throw new ProtocolError('MISSING_FIELD',`missing field: ${k}`);
}
function hash256(bytes){ return crypto.createHash('sha256').update(bytes).digest('hex'); }
function gitBlobSha(bytes){ const b=Buffer.isBuffer(bytes)?bytes:Buffer.from(bytes); return crypto.createHash('sha1').update(Buffer.from(`blob ${b.length}\0`)).update(b).digest('hex'); }
function redact(s){ return String(s).replace(SECRET,'[REDACTED]'); }
function parseEnvelope(bodyBytes, ctx) {
  const b=Buffer.isBuffer(bodyBytes)?bodyBytes:Buffer.from(bodyBytes);
  if (b.length>4096) throw new ProtocolError('BODY_TOO_LARGE','request exceeds 4096 UTF-8 bytes');
  if (b.length>=3 && b[0]===0xef && b[1]===0xbb && b[2]===0xbf) throw new ProtocolError('BOM','UTF-8 BOM forbidden');
  let text; try { text=new TextDecoder('utf-8',{fatal:true}).decode(b); } catch { throw new ProtocolError('INVALID_UTF8','invalid UTF-8'); }
  if ((text.match(/```/g)||[]).length!==2) throw new ProtocolError('INVALID_ENVELOPE','exactly one fence required');
  const m=text.match(/^<!-- grl-request v1 -->\r?\n(?<fence>```json\r?\n(?<json>[\s\S]*?)\r?\n```)[ \t\r\n]*$/);
  if(!m) throw new ProtocolError('INVALID_ENVELOPE','exact marker and one json fence required');
  const request=parseStrictJson(m.groups.json);
  exactKeys(request,['schema_version','request_id','target','profile','expires_at','timeout_minutes']);
  if(request.schema_version!=='grl.request.v1') throw new ProtocolError('INVALID_VERSION','schema_version');
  if(typeof request.request_id!=='string'||!UUID.test(request.request_id)) throw new ProtocolError('INVALID_REQUEST_ID','lowercase UUIDv4 required');
  exactKeys(request.target,['repository','sha']);
  if(typeof request.target.repository!=='string'||!REPO.test(request.target.repository)||request.target.repository.includes('..')) throw new ProtocolError('REPOSITORY_NOT_ALLOWED','canonical repository required');
  if(request.target.repository!==ctx.executionRepository) throw new ProtocolError('STAGE1_REPO_MISMATCH','Stage-1 target must equal execution repo');
  if(typeof request.target.sha!=='string'||!SHA.test(request.target.sha)) throw new ProtocolError('INVALID_SHA','40 lowercase hex required');
  exactKeys(request.profile,['id','definition_sha']);
  if(typeof request.profile.id!=='string'||!PROFILE.test(request.profile.id)||!ctx.profiles[request.profile.id]) throw new ProtocolError('UNKNOWN_PROFILE','profile not allowlisted');
  if(typeof request.profile.definition_sha!=='string'||!SHA.test(request.profile.definition_sha)) throw new ProtocolError('INVALID_SHA','definition sha invalid');
  const policy=ctx.profiles[request.profile.id];
  if(request.profile.definition_sha!==policy.definitionSha) throw new ProtocolError('PROFILE_DEFINITION_MISMATCH','profile blob mismatch');
  if(!Number.isInteger(request.timeout_minutes)||request.timeout_minutes<1) throw new ProtocolError('INVALID_TIMEOUT','timeout must be positive integer');
  if(request.timeout_minutes>policy.maxTimeoutMinutes) throw new ProtocolError('TIMEOUT_OVER_MAX','timeout exceeds profile ceiling');
  if(typeof request.expires_at!=='string'||!UTC.test(request.expires_at)) throw new ProtocolError('INVALID_EXPIRY','UTC Z required');
  const exp=Date.parse(request.expires_at), created=Date.parse(ctx.commentCreatedAt), now=Date.parse(ctx.now);
  if(!Number.isFinite(exp)||!Number.isFinite(created)||!Number.isFinite(now)) throw new ProtocolError('INVALID_EXPIRY','invalid time');
  if(exp<created+60000||exp>created+24*3600000) throw new ProtocolError('EXPIRY_WINDOW','expiry outside 1m..24h');
  if(now>=exp) throw new ProtocolError('EXPIRED','request expired');
  return {request, fencedBodySha256:hash256(Buffer.from(m.groups.fence,'utf8')), rawFence:m.groups.fence};
}
function validateProfile(bytes){
  const p=parseStrictJson(Buffer.from(bytes).toString('utf8'));
  exactKeys(p,['id','version','max_timeout_minutes','min_tests','estimated_disk_gib','required_tools','steps']);
  if(!PROFILE.test(p.id)||typeof p.version!=='string'||!p.version) throw new ProtocolError('INVALID_PROFILE','bad profile identity');
  if(!Number.isInteger(p.max_timeout_minutes)||p.max_timeout_minutes<1||p.max_timeout_minutes>1440) throw new ProtocolError('INVALID_PROFILE','bad max timeout');
  if(!Number.isInteger(p.min_tests)||p.min_tests<1) throw new ProtocolError('INVALID_PROFILE','min_tests >=1');
  if(typeof p.estimated_disk_gib!=='number'||p.estimated_disk_gib<0||!Number.isFinite(p.estimated_disk_gib)) throw new ProtocolError('INVALID_PROFILE','disk estimate');
  if(!Array.isArray(p.required_tools)||!p.required_tools.every(x=>typeof x==='string'&&x)) throw new ProtocolError('INVALID_PROFILE','required_tools');
  if(!Array.isArray(p.steps)||!p.steps.length) throw new ProtocolError('INVALID_PROFILE','steps required');
  for(const s of p.steps){
    exactKeys(s,['name','executable','argv','timeout_seconds','parser'],['name','executable','argv','timeout_seconds']);
    if(typeof s.name!=='string'||!s.name||typeof s.executable!=='string'||!s.executable||/[\r\n]/.test(s.executable)) throw new ProtocolError('INVALID_PROFILE','step executable/name');
    if(!Array.isArray(s.argv)||!s.argv.every(x=>typeof x==='string')) throw new ProtocolError('INVALID_PROFILE','argv array required');
    if(!Number.isInteger(s.timeout_seconds)||s.timeout_seconds<1||s.timeout_seconds>86400) throw new ProtocolError('INVALID_PROFILE','step timeout');
    if(s.parser!==undefined){ exactKeys(s.parser,['type','path']); if(!['junit','trx'].includes(s.parser.type)||typeof s.parser.path!=='string'||!s.parser.path||s.parser.path.includes('..')) throw new ProtocolError('INVALID_PROFILE','parser'); }
  }
  return p;
}
module.exports={ProtocolError,MARKER,SHA,REPO,PROFILE,UTC,exactKeys,hash256,gitBlobSha,redact,parseEnvelope,validateProfile};

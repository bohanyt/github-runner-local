const { TextDecoder } = require('node:util');

class ProtocolError extends Error {
  constructor(code, message) { super(message); this.code = code; }
}

function parseStrictJson(text) {
  let i = 0;
  const fail = (m) => { throw new ProtocolError('INVALID_JSON', m); };
  const ws = () => { while (i < text.length && /[\t\n\r ]/.test(text[i])) i++; };
  const str = () => {
    if (text[i] !== '"') fail('expected string');
    let start = i++;
    let escaped = false;
    while (i < text.length) {
      const c = text[i++];
      if (c === '"' && !escaped) {
        const raw = text.slice(start, i);
        try { return JSON.parse(raw); } catch { fail('invalid string'); }
      }
      if (c === '\\' && !escaped) escaped = true;
      else escaped = false;
      if (c < ' ') fail('control character in string');
    }
    fail('unterminated string');
  };
  const num = () => {
    const m = text.slice(i).match(/^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?/);
    if (!m) fail('invalid number');
    i += m[0].length;
    const n = Number(m[0]);
    if (!Number.isFinite(n)) fail('non-finite number');
    return n;
  };
  const val = (depth=0) => {
    if (depth > 32) fail('too deep');
    ws();
    const c = text[i];
    if (c === '"') return str();
    if (c === '{') {
      i++; ws(); const o = {}; const seen = new Set();
      if (text[i] === '}') { i++; return o; }
      while (true) {
        ws(); const k = str();
        if (seen.has(k)) throw new ProtocolError('DUPLICATE_KEY', `duplicate JSON key: ${k}`);
        seen.add(k); ws(); if (text[i++] !== ':') fail('expected colon');
        o[k] = val(depth+1); ws();
        if (text[i] === '}') { i++; return o; }
        if (text[i++] !== ',') fail('expected comma');
      }
    }
    if (c === '[') {
      i++; ws(); const a=[];
      if (text[i] === ']') { i++; return a; }
      while (true) {
        a.push(val(depth+1)); ws();
        if (text[i] === ']') { i++; return a; }
        if (text[i++] !== ',') fail('expected comma');
      }
    }
    if (text.startsWith('true', i)) { i += 4; return true; }
    if (text.startsWith('false', i)) { i += 5; return false; }
    if (text.startsWith('null', i)) { i += 4; return null; }
    return num();
  };
  const out = val(); ws(); if (i !== text.length) fail('trailing content'); return out;
}
module.exports={ProtocolError,parseStrictJson};

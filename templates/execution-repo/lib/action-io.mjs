import { appendFileSync, readFileSync } from 'node:fs';
import { ProtocolError, decoded, redact } from './protocol.mjs';

// Same key as the official runner: spaces become underscores, hyphens are kept.
const inputKey = name => 'INPUT_' + name.replaceAll(' ', '_').toUpperCase();

export function input(name) {
  const value = process.env[inputKey(name)];
  if (!value) throw new ProtocolError('MISSING_ACTION_INPUT');
  return value;
}

export function optionalInput(name) {
  return process.env[inputKey(name)] ?? '';
}

export function output(name, value) {
  if (!process.env.GITHUB_OUTPUT || /[\r\n]/.test(name) || /[\r\n]/.test(String(value)))
    throw new ProtocolError('INVALID_ACTION_OUTPUT');
  appendFileSync(process.env.GITHUB_OUTPUT, `${name}=${value}\n`, 'utf8');
}

export function eventFromEnvironment() {
  if (!process.env.GITHUB_EVENT_PATH) throw new ProtocolError('MISSING_EVENT');
  return JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
}

export function decodedInput(name) {
  return decoded(input(name));
}

export function failAction(error) {
  process.stderr.write('GRL action failed: ' + redact(error instanceof ProtocolError ? error.code : 'INTERNAL_ERROR') + '\n');
  process.exitCode = 1;
}

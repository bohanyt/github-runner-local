'use strict';
const { transition, validate } = require('./ownership.cjs');
const validSha = sha => typeof sha === 'string' && /^[a-f0-9]{40}$/.test(sha);
// Source-only REST adapter. No HTTP client, credentials, fetch or live endpoint is provided.
// Request injection is restricted to an explicitly offline fake. OD-G must precede any future live transport.
class OfflineGitOwnershipTransport {
  offline = true;
  constructor(repository, request, capability = false) {
    if (capability !== 'synthetic' || request.offline !== true) throw new Error('OD_G_DISABLED');
    if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repository)) throw new Error('CONTROL_REPOSITORY');
    this.base = '/repos/' + repository + '/git'; this.request = request;
  }
  async call(method, route, body) {
    const response = await this.request(method, this.base + route, body);
    if (response.status < 200 || response.status > 299) throw new Error('REF_REQUEST_UNRESOLVED');
    return response.body;
  }
  async commit(sha) {
    if (!validSha(sha)) throw new Error('COMMIT_SHA');
    const value = await this.call('GET', '/commits/' + sha);
    if (value.sha !== sha || !Array.isArray(value.parents) || !value.parents.every(p => validSha(p.sha)) ||
        value.tree?.sha !== '4b825dc642cb6eb9a060e54bf8d69288fbee4904') throw new Error('OWNERSHIP_COMMIT');
    return { sha, parents: value.parents.map(p => p.sha), emptyTree: true, record: JSON.parse(value.message) };
  }
  async readHead() {
    const value = await this.call('GET', '/ref/heads/grl-control');
    if (value.ref !== 'refs/heads/grl-control' || value.object?.type !== 'commit') throw new Error('OWNERSHIP_REF');
    return this.commit(value.object.sha);
  }
  async bootstrapReleased(record) {
    validate(record);
    if (record.state !== 'RELEASED' || record.epoch !== 0) throw new Error('BOOTSTRAP_STATE');
    const tree = await this.call('POST', '/trees', { tree: [] });
    if (tree.sha !== '4b825dc642cb6eb9a060e54bf8d69288fbee4904') throw new Error('EMPTY_TREE');
    const candidate = await this.call('POST', '/commits', { tree: tree.sha, parents: [], message: JSON.stringify(record) });
    const verified = await this.commit(candidate.sha);
    if (verified.parents.length !== 0 || JSON.stringify(verified.record) !== JSON.stringify(record))
      throw new Error('BOOTSTRAP_IDENTITY');
    // Create-if-absent. Conflict never falls through to PATCH or replaces an existing ref.
    await this.call('POST', '/refs', { ref: 'refs/heads/grl-control', sha: candidate.sha });
    return verified;
  }
  async createCommit({ parents, emptyTree, record }) {
    if (emptyTree !== true || parents.length !== 1 || !validSha(parents[0])) throw new Error('EXACT_PARENT_REQUIRED');
    const tree = await this.call('POST', '/trees', { tree: [] });
    if (tree.sha !== '4b825dc642cb6eb9a060e54bf8d69288fbee4904') throw new Error('EMPTY_TREE');
    const candidate = await this.call('POST', '/commits', { tree: tree.sha, parents, message: JSON.stringify(record) });
    const result = await this.commit(candidate.sha);
    transition(await this.commit(parents[0]), result);
    return result;
  }
  async updateRef(sha, options) {
    if (options.force !== false) throw new Error('FORCE_FORBIDDEN');
    const parent = await this.readHead(); const candidate = await this.commit(sha);
    transition(parent, candidate);
    return this.call('PATCH', '/refs/heads/grl-control', { sha, force: false });
  }
}
module.exports = { OfflineGitOwnershipTransport };

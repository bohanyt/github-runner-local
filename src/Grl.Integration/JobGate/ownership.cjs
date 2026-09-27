'use strict';
const fs = require('node:fs');
const crypto = require('node:crypto');
const gate = require('./gate.cjs');
function same(a, b) { return a?.repo === b?.repo && a?.runnerId === b?.runnerId && a?.name === b?.name; }
function validate(record) {
  if (record.v !== 1 || !Number.isSafeInteger(record.epoch) || record.epoch < 0 ||
      !['ACQUIRED', 'RELEASED'].includes(record.state) ||
      !/^[a-f0-9-]{36}$/.test(record.operation) || !record.holder?.repo ||
      !Number.isSafeInteger(record.holder.runnerId) || record.holder.runnerId <= 0 || !record.holder.name)
    throw new Error('INVALID_OWNERSHIP');
}
function transition(parent, candidate) {
  validate(parent.record); validate(candidate.record);
  if (candidate.parents.length !== 1 || candidate.parents[0] !== parent.sha) throw new Error('EXACT_PARENT_REQUIRED');
  const a = parent.record, b = candidate.record;
  if (a.state === 'RELEASED') {
    if (b.state !== 'ACQUIRED' || b.epoch !== a.epoch + 1) throw new Error('INVALID_ACQUIRE');
  } else if (b.state !== 'RELEASED' || b.epoch !== a.epoch || !same(a.holder, b.holder))
    throw new Error('INVALID_RELEASE');
}
// Deliberately no fetch/network implementation. Transport must explicitly attest offline use.
// Its createCommit/updateRef operations mirror empty-tree commits and force=false ref updates.
class OwnershipClient {
  constructor(directory, holder, transport, capability = false) {
    if (capability !== 'synthetic' || transport.offline !== true) throw new Error('OD_G_DISABLED');
    this.directory = gate.root(directory); this.holder = holder; this.transport = transport;
    const s = gate.state(this.directory);
    if (!same(holder, { repo: s.repo, runnerId: s.runnerId, name: s.runnerName })) throw new Error('LOCAL_HOLDER_IDENTITY');
  }
  async publish(kind) {
    return gate.locked(this.directory, async () => {
      const pendingFile = gate.fileAt(this.directory, 'ownership-pending.json');
      if (fs.existsSync(pendingFile)) throw new Error('PUBLICATION_UNRESOLVED');
      const parent = await this.transport.readHead(); validate(parent.record);
      if (kind === 'RELEASED') {
        if (parent.record.state !== 'ACQUIRED' || !same(parent.record.holder, this.holder)) throw new Error('NOT_HOLDER');
        if (gate.state(this.directory).mode !== 'INACTIVE') throw new Error('NOT_DRAINED');
        // Caller obtains an exact Worker lifetime proof. Require its durable acknowledgement.
        if (!fs.existsSync(gate.fileAt(this.directory, 'release-proof.json'))) throw new Error('NOT_DRAINED');
        const proof = JSON.parse(fs.readFileSync(gate.fileAt(this.directory, 'release-proof.json')));
        if (proof.gateEpoch !== gate.state(this.directory).gateEpoch || proof.noOwnedWorker !== true)
          throw new Error('NOT_DRAINED');
      } else if (kind !== 'ACQUIRED' || parent.record.state !== 'RELEASED') throw new Error('NOT_RELEASED');
      const record = { v: 1, epoch: parent.record.epoch + (kind === 'ACQUIRED' ? 1 : 0),
        state: kind, holder: this.holder, operation: crypto.randomUUID() };
      const intent = { parent: parent.sha, record };
      // Persist before even creating the commit: a crash/lost response is quarantined.
      gate.durable(pendingFile, intent, true);
      const candidate = await this.transport.createCommit({ parents: [parent.sha], emptyTree: true, record });
      if (candidate.emptyTree !== true || JSON.stringify(candidate.record) !== JSON.stringify(record))
        throw new Error('CANDIDATE_IDENTITY');
      transition(parent, candidate);
      intent.candidate = candidate.sha; gate.durable(pendingFile, intent);
      try { await this.transport.updateRef(candidate.sha, { force: false }); } catch { /* reconcile exact identity */ }
      return this.reconcileUnlocked();
    });
  }
  async reconcileUnlocked() {
    const file = gate.fileAt(this.directory, 'ownership-pending.json');
    if (!fs.existsSync(file)) return null;
    const intent = JSON.parse(fs.readFileSync(file));
    const head = await this.transport.readHead(); validate(head.record);
    // Seeing the old parent ONCE (or repeatedly) proves nothing about queued delivery.
    if (head.sha !== intent.candidate || head.record.operation !== intent.record.operation ||
        !same(head.record.holder, intent.record.holder)) throw new Error('PUBLICATION_UNRESOLVED');
    gate.durable(gate.fileAt(this.directory, 'ownership-resolved-' + intent.record.operation + '.json'), { intent, head });
    fs.unlinkSync(file); // Resolution evidence is retained; operation identity is never rewritten/rebased.
    return head;
  }
  async reconcile() { return gate.locked(this.directory, () => this.reconcileUnlocked()); }
  async acknowledgeDrain(noOwnedWorker) {
    const proof = await gate.inspect(this.directory, noOwnedWorker);
    return gate.locked(this.directory, () => {
      if (gate.state(this.directory).gateEpoch !== proof.gateEpoch || gate.state(this.directory).mode !== 'INACTIVE')
        throw new Error('PROOF_CHANGED');
      gate.durable(gate.fileAt(this.directory, 'release-proof.json'), { ...proof, noOwnedWorker: true });
    });
  }
  async activate(noOwnedWorker) {
    await gate.inspect(this.directory, noOwnedWorker);
    return gate.locked(this.directory, async () => {
      if (fs.existsSync(gate.fileAt(this.directory, 'ownership-pending.json'))) throw new Error('PUBLICATION_UNRESOLVED');
      const head = await this.transport.readHead(); validate(head.record);
      if (head.record.state !== 'ACQUIRED' || !same(head.record.holder, this.holder)) throw new Error('NOT_HOLDER');
      const s = gate.state(this.directory);
      if (s.mode !== 'INACTIVE' || s.recoveryRequired) throw new Error('RECOVERY_REQUIRED');
      s.ownershipEpoch = head.record.epoch; s.gateEpoch = crypto.randomUUID(); s.mode = 'ACTIVE';
      gate.durable(gate.fileAt(this.directory, 'state.json'), s);
    });
  }
  async abort() {
    return gate.locked(this.directory, async () => {
      if (fs.existsSync(gate.fileAt(this.directory, 'ownership-pending.json'))) throw new Error('PUBLICATION_UNRESOLVED');
      const head = await this.transport.readHead(); validate(head.record);
      if (head.record.state !== 'ACQUIRED' || !same(head.record.holder, this.holder)) throw new Error('NOT_HOLDER');
      return head; // Reactivation still requires the local lifecycle's no-Worker proof/new epoch.
    });
  }
}
module.exports = { OwnershipClient, transition, same, validate };

const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const runtime = require('../dist/sandsunder.js');

const secret = 'test-secret-with-at-least-thirty-two-characters';
const hmac = (input, key) => crypto.createHmac('sha256', key).update(input).digest('hex');
const uuid = (suffix) => `00000000-0000-4000-8000-${suffix.padStart(12, '0')}`;

function validResult() {
  return {
    match_id: uuid('1'),
    account_id: uuid('2'),
    build_id: 'win-0.1.0',
    ruleset_version: 'mvp-1',
    placement: 1,
    outcome: 'ritual',
    account_xp: 120,
    mastery: [{ character_id: 'dune_scout', xp: 80 }],
    milestones: ['seal_one', 'seal_two'],
    kills: 0,
    duration_seconds: 640,
    completed_at: '2026-08-01T12:00:00.000Z',
    issued_at: '2026-08-01T12:00:02.000Z',
    nonce: uuid('3')
  };
}

test('canonical JSON is stable across object insertion order', () => {
  assert.equal(runtime.canonicalJson({ z: 1, a: { y: true, x: 2 } }), '{"a":{"x":2,"y":true},"z":1}');
  assert.equal(runtime.canonicalJson({ b: 2, a: 1 }), runtime.canonicalJson({ a: 1, b: 2 }));
});

test('signed envelope rejects payload tampering', () => {
  const payload = validResult();
  const signature = runtime.signPayload(payload, secret, hmac);
  assert.deepEqual(runtime.verifyEnvelope({ payload, signature }, secret, hmac), payload);
  assert.throws(
    () => runtime.verifyEnvelope({ payload: { ...payload, account_xp: 999 }, signature }, secret, hmac),
    /Invalid signature/
  );
});

test('match result validation enforces horizontal bounded rewards', () => {
  const validated = runtime.validateMatchResult(validResult());
  assert.equal(validated.account_xp, 120);
  assert.throws(() => runtime.validateMatchResult({ ...validResult(), account_xp: 100001 }), /account_xp/);
  assert.throws(() => runtime.validateMatchResult({ ...validResult(), placement: 7 }), /placement/);
  assert.throws(() => runtime.validateMatchResult({ ...validResult(), mastery: [
    { character_id: 'dune_scout', xp: 1 },
    { character_id: 'dune_scout', xp: 1 }
  ] }), /unique/);
});

test('client-authenticated contexts cannot submit match results', () => {
  const ctx = { userId: uuid('2'), env: { SANDSUNDER_MATCH_HMAC_SECRET: secret } };
  const logger = { debug() {}, info() {}, warn() {}, error() {} };
  const nk = { hmacSha256Hash: hmac, sqlQuery() { throw new Error('database must not be reached'); }, uuidv4: crypto.randomUUID };
  assert.throws(() => runtime.rpcSubmitMatchResult(ctx, logger, nk, '{}'), /server-only/);
});

test('settlement SQL path returns duplicate receipt when unique insert did not occur', () => {
  const payload = validResult();
  payload.issued_at = new Date().toISOString();
  const envelope = { payload, signature: runtime.signPayload(payload, secret, hmac) };
  const ctx = { env: { SANDSUNDER_MATCH_HMAC_SECRET: secret } };
  const logger = { debug() {}, info() {}, warn() {}, error() {} };
  let calls = 0;
  const nk = {
    hmacSha256Hash: hmac,
    uuidv4: crypto.randomUUID,
    sqlQuery(_sql, params) {
      calls += 1;
      assert.equal(params[0], payload.match_id);
      return [{ eligible: true, already_settled: true, inserted: false }];
    }
  };
  const receipt = JSON.parse(runtime.rpcSubmitMatchResult(ctx, logger, nk, JSON.stringify(envelope)));
  assert.deepEqual(receipt, { accepted: true, duplicate: true, match_id: payload.match_id, account_id: payload.account_id });
  assert.equal(calls, 1);
});

test('settlement adapter rejects results without an exact persisted assignment', () => {
  const payload = validResult();
  payload.issued_at = new Date().toISOString();
  const envelope = { payload, signature: runtime.signPayload(payload, secret, hmac) };
  const ctx = { env: { SANDSUNDER_MATCH_HMAC_SECRET: secret } };
  const logger = { debug() {}, info() {}, warn() {}, error() {} };
  const nk = {
    hmacSha256Hash: hmac,
    uuidv4: crypto.randomUUID,
    sqlQuery() { return [{ eligible: false, already_settled: false, inserted: false }]; }
  };
  assert.throws(
    () => runtime.rpcSubmitMatchResult(ctx, logger, nk, JSON.stringify(envelope)),
    /session, roster assignment, build, or ruleset/
  );
});

test('bootstrap validates six-player cap and signs opaque tickets', () => {
  const now = new Date();
  const payload = {
    match_id: uuid('10'), build_id: 'win-0.1.0', ruleset_version: 'mvp-1', map_seed: '922337203685477580',
    endpoint: '127.0.0.1:7777', transport: 'udp',
    player_account_ids: [uuid('11'), uuid('12')],
    starts_at: now.toISOString(), ticket_expires_at: new Date(now.getTime() + 60000).toISOString(),
    issued_at: now.toISOString(), nonce: uuid('13')
  };
  const envelope = { payload, signature: runtime.signPayload(payload, secret, hmac) };
  const logger = { debug() {}, info() {}, warn() {}, error() {} };
  let savedTickets;
  const nk = {
    hmacSha256Hash: hmac,
    uuidv4: crypto.randomUUID,
    sqlQuery(_sql, params) { savedTickets = JSON.parse(params[8]); return [{ inserted: true }]; }
  };
  const response = JSON.parse(runtime.rpcBootstrapMatch({ env: { SANDSUNDER_MATCH_HMAC_SECRET: secret } }, logger, nk, JSON.stringify(envelope)));
  assert.equal(response.tickets.length, 2);
  assert.equal(savedTickets.length, 2);
  assert.equal('map_seed' in response.tickets[0].payload, false);
  assert.equal('map_seed' in savedTickets[0], false);
  assert.match(response.tickets[0].signature, /^v1=[0-9a-f]{64}$/);
});

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { PGlite } = require('@electric-sql/pglite');
const runtime = require('../dist/sandsunder.js');

const migrationsDir = path.resolve(__dirname, '..', 'migrations');
const accountId = '00000000-0000-4000-8000-000000000001';
const matchId = '00000000-0000-4000-8000-000000000002';
const otherAccountId = '00000000-0000-4000-8000-000000000009';

async function migratedDatabase() {
  const db = new PGlite();
  const files = fs.readdirSync(migrationsDir).filter((name) => name.endsWith('.sql')).sort();
  for (const file of files) await db.exec(fs.readFileSync(path.join(migrationsDir, file), 'utf8'));
  return db;
}

async function applyAllMigrations(db) {
  const files = fs.readdirSync(migrationsDir).filter((name) => name.endsWith('.sql')).sort();
  for (const file of files) await db.exec(fs.readFileSync(path.join(migrationsDir, file), 'utf8'));
}

async function bootstrapMatch(db, options = {}) {
  const selectedMatchId = options.matchId || matchId;
  const selectedAccountId = options.accountId || accountId;
  const buildId = options.buildId || 'win-0.1.0';
  const rulesetVersion = options.rulesetVersion || 'mvp-1';
  const ticketId = options.ticketId || '00000000-0000-4000-8000-000000000004';
  const expiresAt = new Date(Date.now() + 60_000).toISOString();
  const tickets = [{
    ticket_id: ticketId,
    match_id: selectedMatchId,
    account_id: selectedAccountId,
    build_id: buildId,
    ruleset_version: rulesetVersion,
    endpoint: '127.0.0.1:7777',
    transport: 'udp',
    issued_at: new Date().toISOString(),
    expires_at: expiresAt
  }];
  const response = await db.query(runtime.sql.bootstrapMatch, [
    selectedMatchId,
    buildId,
    rulesetVersion,
    '42',
    '127.0.0.1:7777',
    'udp',
    new Date().toISOString(),
    options.nonce || '00000000-0000-4000-8000-000000000005',
    JSON.stringify(tickets)
  ]);
  assert.equal(response.rows[0].inserted, true);
  return { ticketId, tickets };
}

test('migrations apply cleanly and progression guards are active', async () => {
  const db = await migratedDatabase();
  try {
    await db.query('INSERT INTO sandsunder.account_progression (account_id, account_xp) VALUES ($1, 100)', [accountId]);
    await assert.rejects(
      db.query('UPDATE sandsunder.account_progression SET account_xp = 99 WHERE account_id = $1', [accountId]),
      /cannot decrease/
    );
    const result = await db.query('SELECT account_xp FROM sandsunder.account_progression WHERE account_id = $1', [accountId]);
    assert.equal(Number(result.rows[0].account_xp), 100);
  } finally {
    await db.close();
  }
});

test('full migration chain is safely rerunnable and keeps roster constraints active', async () => {
  const db = new PGlite();
  try {
    await applyAllMigrations(db);
    await applyAllMigrations(db);

    const constraints = await db.query(`
      SELECT conname
      FROM pg_constraint
      WHERE conrelid IN (
        'sandsunder.match_sessions'::regclass,
        'sandsunder.match_results'::regclass
      )
        AND conname IN (
          'match_sessions_identity_unique',
          'match_results_roster_fk',
          'match_results_session_version_fk'
        )
      ORDER BY conname
    `);
    assert.deepEqual(
      constraints.rows.map((row) => row.conname),
      ['match_results_roster_fk', 'match_results_session_version_fk', 'match_sessions_identity_unique']
    );

    await assert.rejects(
      db.query(`
        INSERT INTO sandsunder.match_results
          (match_id, account_id, build_id, ruleset_version, placement, outcome,
           account_xp, mastery_rewards, milestones, kills, duration_seconds,
           completed_at, signed_payload)
        VALUES ($1, $2, 'win-0.1.0', 'mvp-1', 1, 'ritual', 1, '[]', '[]', 0, 10, now(), '{}')
      `, [matchId, accountId]),
      /foreign key/i
    );
  } finally {
    await db.close();
  }
});

test('database constraints reject settlement outside the persisted roster', async () => {
  const db = await migratedDatabase();
  try {
    await bootstrapMatch(db);
    const insertResult = `
      INSERT INTO sandsunder.match_results
        (match_id, account_id, build_id, ruleset_version, placement, outcome,
         account_xp, mastery_rewards, milestones, kills, duration_seconds,
         completed_at, signed_payload)
      VALUES ($1, $2, 'win-0.1.0', 'mvp-1', 1, 'ritual', 120, '[]', '[]', 0, 640, now(), '{}')`;
    await db.query(insertResult, [matchId, accountId]);
    await assert.rejects(db.query(insertResult, [matchId, accountId]), /unique|duplicate/i);
    await assert.rejects(db.query(insertResult, [matchId, otherAccountId]), /foreign key/i);
  } finally {
    await db.close();
  }
});

test('authoritative settlement CTE is atomic and idempotent', async () => {
  const db = await migratedDatabase();
  try {
    await bootstrapMatch(db);
    const parameters = [
      matchId,
      accountId,
      'win-0.1.0',
      'mvp-1',
      1,
      'ritual',
      120,
      JSON.stringify([{ character_id: 'dune_scout', xp: 80 }]),
      JSON.stringify(['seal_one', 'ritual_complete']),
      0,
      640,
      '2026-08-01T12:00:00Z',
      '00000000-0000-4000-8000-000000000003',
      JSON.stringify({ match_id: matchId, account_id: accountId })
    ];
    const first = await db.query(runtime.sql.settleMatch, parameters);
    assert.equal(first.rows[0].inserted, true);

    const retry = await db.query(runtime.sql.settleMatch, parameters);
    assert.equal(retry.rows[0].inserted, false);

    const progression = await db.query(
      'SELECT account_xp FROM sandsunder.account_progression WHERE account_id = $1',
      [accountId]
    );
    const mastery = await db.query(
      'SELECT mastery_xp FROM sandsunder.character_mastery WHERE account_id = $1 AND character_id = $2',
      [accountId, 'dune_scout']
    );
    const ledger = await db.query('SELECT count(*)::integer AS count FROM sandsunder.progression_ledger');
    const outbox = await db.query('SELECT count(*)::integer AS count FROM sandsunder.outbox');
    assert.equal(Number(progression.rows[0].account_xp), 120);
    assert.equal(Number(mastery.rows[0].mastery_xp), 80);
    assert.equal(ledger.rows[0].count, 2);
    assert.equal(outbox.rows[0].count, 1);
  } finally {
    await db.close();
  }
});

test('authoritative match ticket is consumed exactly once', async () => {
  const db = await migratedDatabase();
  try {
    const { ticketId } = await bootstrapMatch(db);

    const consumed = await db.query(runtime.sql.consumeTicket, [
      ticketId, matchId, accountId, '00000000-0000-4000-8000-000000000006'
    ]);
    assert.equal(consumed.rows[0].consumed, true);

    const replay = await db.query(runtime.sql.consumeTicket, [
      ticketId, matchId, accountId, '00000000-0000-4000-8000-000000000007'
    ]);
    assert.equal(replay.rows[0].consumed, false);
  } finally {
    await db.close();
  }
});

test('settlement rejects missing match, non-roster account and version mismatches', async () => {
  const db = await migratedDatabase();
  try {
    const base = [
      matchId, accountId, 'win-0.1.0', 'mvp-1', 1, 'ritual', 120,
      '[]', '[]', 0, 640, '2026-08-01T12:00:00Z',
      '00000000-0000-4000-8000-000000000021', '{}'
    ];
    const missing = await db.query(runtime.sql.settleMatch, base);
    assert.deepEqual(missing.rows[0], { eligible: false, already_settled: false, inserted: false });

    await bootstrapMatch(db);
    const nonRoster = base.slice();
    nonRoster[1] = otherAccountId;
    nonRoster[12] = '00000000-0000-4000-8000-000000000022';
    assert.equal((await db.query(runtime.sql.settleMatch, nonRoster)).rows[0].eligible, false);

    const wrongBuild = base.slice();
    wrongBuild[2] = 'win-9.9.9';
    wrongBuild[12] = '00000000-0000-4000-8000-000000000023';
    assert.equal((await db.query(runtime.sql.settleMatch, wrongBuild)).rows[0].eligible, false);

    const wrongRuleset = base.slice();
    wrongRuleset[3] = 'mvp-999';
    wrongRuleset[12] = '00000000-0000-4000-8000-000000000024';
    assert.equal((await db.query(runtime.sql.settleMatch, wrongRuleset)).rows[0].eligible, false);

    const counts = await db.query('SELECT count(*)::integer AS count FROM sandsunder.match_results');
    assert.equal(counts.rows[0].count, 0);
  } finally {
    await db.close();
  }
});

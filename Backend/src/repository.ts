namespace Sandsunder {
  export interface Persistence {
    settle(result: MatchResultPayload): SettlementReceipt;
    bootstrap(match: MatchBootstrapPayload, tickets: MatchTicketPayload[]): void;
    consumeTicket(ticket: MatchTicketPayload, nonce: string): boolean;
    getProgression(accountId: string): ProgressionView;
  }

  function firstBoolean(rows: nkruntime.SqlQueryResult[], key: string): boolean {
    return rows.length > 0 && rows[0][key] === true;
  }

  export class PostgresPersistence implements Persistence {
    private nk: nkruntime.Nakama;

    constructor(nk: nkruntime.Nakama) {
      this.nk = nk;
    }

    settle(result: MatchResultPayload): SettlementReceipt {
      var rows = this.nk.sqlQuery(SETTLE_MATCH_SQL, [
        result.match_id,
        result.account_id,
        result.build_id,
        result.ruleset_version,
        result.placement,
        result.outcome,
        result.account_xp,
        JSON.stringify(result.mastery),
        JSON.stringify(result.milestones),
        result.kills,
        result.duration_seconds,
        result.completed_at,
        result.nonce,
        JSON.stringify(result)
      ]);
      var inserted = firstBoolean(rows, "inserted");
      if (!firstBoolean(rows, "eligible")) {
        throw new Error("Match result rejected: match session, roster assignment, build, or ruleset does not match.");
      }
      if (!inserted && !firstBoolean(rows, "already_settled")) {
        throw new Error("Match result rejected: request nonce has already been used.");
      }
      return {
        accepted: true,
        duplicate: !inserted,
        match_id: result.match_id,
        account_id: result.account_id
      };
    }

    bootstrap(match: MatchBootstrapPayload, tickets: MatchTicketPayload[]): void {
      var rows = this.nk.sqlQuery(BOOTSTRAP_MATCH_SQL, [
        match.match_id,
        match.build_id,
        match.ruleset_version,
        match.map_seed,
        match.endpoint,
        match.transport,
        match.starts_at,
        match.nonce,
        JSON.stringify(tickets)
      ]);
      if (!firstBoolean(rows, "inserted")) {
        throw new Error("Match bootstrap nonce or match_id has already been used.");
      }
    }

    consumeTicket(ticket: MatchTicketPayload, nonce: string): boolean {
      var rows = this.nk.sqlQuery(CONSUME_TICKET_SQL, [
        ticket.ticket_id,
        ticket.match_id,
        ticket.account_id,
        nonce
      ]);
      return firstBoolean(rows, "consumed");
    }

    getProgression(accountId: string): ProgressionView {
      var rows = this.nk.sqlQuery(PROGRESSION_SQL, [accountId]);
      if (rows.length === 0) {
        return { account_id: accountId, account_xp: 0, account_level: 1, mastery: [], unlocks: [] };
      }
      return {
        account_id: accountId,
        account_xp: Number(rows[0].account_xp || 0),
        account_level: Number(rows[0].account_level || 1),
        mastery: (rows[0].mastery || []) as ProgressionView["mastery"],
        unlocks: (rows[0].unlocks || []) as ProgressionView["unlocks"]
      };
    }
  }

  export var SETTLE_MATCH_SQL = `
WITH eligibility AS (
  SELECT s.match_id, r.account_id
  FROM sandsunder.match_sessions s
  JOIN sandsunder.match_roster r
    ON r.match_id = s.match_id
   AND r.account_id = $2::uuid
  WHERE s.match_id = $1::uuid
    AND s.build_id = $3
    AND s.ruleset_version = $4
), nonce_insert AS (
  INSERT INTO sandsunder.request_nonces (nonce, request_type)
  SELECT $13::uuid, 'match_result'
  FROM eligibility
  ON CONFLICT DO NOTHING
  RETURNING nonce
), result_insert AS (
  INSERT INTO sandsunder.match_results (
    match_id, account_id, build_id, ruleset_version, placement, outcome,
    account_xp, mastery_rewards, milestones, kills, duration_seconds,
    completed_at, signed_payload
  )
  SELECT $1::uuid, $2::uuid, $3, $4, $5, $6, $7, $8::jsonb, $9::jsonb,
         $10, $11, $12::timestamptz, $14::jsonb
  FROM nonce_insert
  ON CONFLICT (match_id, account_id) DO NOTHING
  RETURNING match_id, account_id
), account_progress AS (
  INSERT INTO sandsunder.account_progression (account_id, account_xp)
  SELECT account_id, $7 FROM result_insert
  ON CONFLICT (account_id) DO UPDATE
    SET account_xp = sandsunder.account_progression.account_xp + EXCLUDED.account_xp,
        updated_at = now()
  RETURNING account_id
), mastery_progress AS (
  INSERT INTO sandsunder.character_mastery (account_id, character_id, mastery_xp)
  SELECT r.account_id, rewards.character_id, rewards.xp
  FROM result_insert r
  CROSS JOIN LATERAL jsonb_to_recordset($8::jsonb)
    AS rewards(character_id text, xp integer)
  ON CONFLICT (account_id, character_id) DO UPDATE
    SET mastery_xp = sandsunder.character_mastery.mastery_xp + EXCLUDED.mastery_xp,
        updated_at = now()
  RETURNING account_id
), account_ledger AS (
  INSERT INTO sandsunder.progression_ledger
    (account_id, entry_type, amount, source_type, source_id, metadata)
  SELECT account_id, 'account_xp', $7, 'match', $1::text, jsonb_build_object('match_id', $1)
  FROM result_insert
  WHERE $7 > 0
  ON CONFLICT (account_id, entry_type, source_type, source_id) DO NOTHING
), mastery_ledger AS (
  INSERT INTO sandsunder.progression_ledger
    (account_id, entry_type, amount, source_type, source_id, metadata)
  SELECT r.account_id, 'mastery_xp', rewards.xp, 'match_character',
         $1::text || ':' || rewards.character_id,
         jsonb_build_object('match_id', $1, 'character_id', rewards.character_id)
  FROM result_insert r
  CROSS JOIN LATERAL jsonb_to_recordset($8::jsonb)
    AS rewards(character_id text, xp integer)
  WHERE rewards.xp > 0
  ON CONFLICT (account_id, entry_type, source_type, source_id) DO NOTHING
), event_outbox AS (
  INSERT INTO sandsunder.outbox (aggregate_type, aggregate_id, event_type, payload)
  SELECT 'account', account_id::text, 'match_result.accepted.v1', $14::jsonb
  FROM result_insert
  RETURNING id
)
SELECT EXISTS (SELECT 1 FROM eligibility) AS eligible,
       EXISTS (
         SELECT 1 FROM sandsunder.match_results
         WHERE match_id = $1::uuid AND account_id = $2::uuid
       ) AS already_settled,
       EXISTS (SELECT 1 FROM result_insert) AS inserted;
`;

  export var BOOTSTRAP_MATCH_SQL = `
WITH nonce_insert AS (
  INSERT INTO sandsunder.request_nonces (nonce, request_type)
  VALUES ($8::uuid, 'match_bootstrap')
  ON CONFLICT DO NOTHING
  RETURNING nonce
), match_insert AS (
  INSERT INTO sandsunder.match_sessions
    (match_id, build_id, ruleset_version, map_seed, endpoint, transport, starts_at)
  SELECT $1::uuid, $2, $3, $4::bigint, $5, $6, $7::timestamptz
  FROM nonce_insert
  ON CONFLICT (match_id) DO NOTHING
  RETURNING match_id
), roster_insert AS (
  INSERT INTO sandsunder.match_roster
    (match_id, account_id, ticket_id, ticket_expires_at)
  SELECT m.match_id, x.account_id, x.ticket_id, x.expires_at
  FROM match_insert m
  CROSS JOIN LATERAL jsonb_to_recordset($9::jsonb)
    AS x(ticket_id uuid, account_id uuid, expires_at timestamptz)
  RETURNING match_id
)
SELECT EXISTS (SELECT 1 FROM match_insert) AS inserted;
`;

  export var CONSUME_TICKET_SQL = `
WITH nonce_insert AS (
  INSERT INTO sandsunder.request_nonces (nonce, request_type)
  VALUES ($4::uuid, 'ticket_consume')
  ON CONFLICT DO NOTHING
  RETURNING nonce
), consumed AS (
  UPDATE sandsunder.match_roster r
     SET ticket_consumed_at = now()
    FROM nonce_insert
   WHERE r.ticket_id = $1::uuid
     AND r.match_id = $2::uuid
     AND r.account_id = $3::uuid
     AND r.ticket_consumed_at IS NULL
     AND r.ticket_expires_at > now()
  RETURNING r.ticket_id
)
SELECT EXISTS (SELECT 1 FROM consumed) AS consumed;
`;

  export var PROGRESSION_SQL = `
SELECT p.account_xp,
       1 + floor(sqrt(p.account_xp / 100.0))::integer AS account_level,
       COALESCE((
         SELECT jsonb_agg(jsonb_build_object(
           'character_id', m.character_id,
           'xp', m.mastery_xp,
           'level', 1 + floor(sqrt(m.mastery_xp / 100.0))::integer
         ) ORDER BY m.character_id)
         FROM sandsunder.character_mastery m
         WHERE m.account_id = p.account_id
       ), '[]'::jsonb) AS mastery,
       COALESCE((
         SELECT jsonb_agg(jsonb_build_object(
           'unlock_id', u.unlock_id,
           'unlock_type', u.unlock_type,
           'granted_at', u.granted_at
         ) ORDER BY u.granted_at)
         FROM sandsunder.account_unlocks u
         WHERE u.account_id = p.account_id
       ), '[]'::jsonb) AS unlocks
FROM sandsunder.account_progression p
WHERE p.account_id = $1::uuid;
`;
}

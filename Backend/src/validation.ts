namespace Sandsunder {
  var UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  var ID_RE = /^[a-z0-9][a-z0-9_.-]{0,63}$/i;

  function requireObject(value: unknown, label: string): { [key: string]: unknown } {
    if (!value || typeof value !== "object" || Object.prototype.toString.call(value) === "[object Array]") {
      throw new Error(label + " must be an object.");
    }
    return value as { [key: string]: unknown };
  }

  function requireString(value: unknown, label: string, maxLength: number): string {
    if (typeof value !== "string" || value.length === 0 || value.length > maxLength) {
      throw new Error(label + " must be a non-empty string up to " + maxLength + " characters.");
    }
    return value;
  }

  function requireUuid(value: unknown, label: string): string {
    var result = requireString(value, label, 36);
    if (!UUID_RE.test(result)) {
      throw new Error(label + " must be a UUID.");
    }
    return result;
  }

  function requireId(value: unknown, label: string): string {
    var result = requireString(value, label, 64);
    if (!ID_RE.test(result)) {
      throw new Error(label + " has an invalid identifier format.");
    }
    return result;
  }

  function requireInteger(value: unknown, label: string, min: number, max: number): number {
    if (typeof value !== "number" || value % 1 !== 0 || value < min || value > max) {
      throw new Error(label + " must be an integer between " + min + " and " + max + ".");
    }
    return value;
  }

  function requireDate(value: unknown, label: string): string {
    var result = requireString(value, label, 40);
    var parsed = Date.parse(result);
    if (isNaN(parsed)) {
      throw new Error(label + " must be an ISO-8601 timestamp.");
    }
    return result;
  }

  export function assertFresh(timestamp: string, nowMs: number, maxSkewSeconds: number): void {
    var delta = Math.abs(nowMs - Date.parse(timestamp));
    if (delta > maxSkewSeconds * 1000) {
      throw new Error("Signed request is outside the allowed clock skew.");
    }
  }

  export function validateMatchResult(value: unknown): MatchResultPayload {
    var input = requireObject(value, "match result");
    var outcome = requireString(input.outcome, "outcome", 32);
    if (["ritual", "relic", "last_survivor", "timeout", "eliminated"].indexOf(outcome) < 0) {
      throw new Error("outcome is not supported.");
    }
    if (Object.prototype.toString.call(input.mastery) !== "[object Array]") {
      throw new Error("mastery must be an array.");
    }
    var masteryInput = input.mastery as unknown[];
    if (masteryInput.length > 6) {
      throw new Error("mastery contains too many entries.");
    }
    var seen: { [key: string]: boolean } = {};
    var mastery: MasteryReward[] = [];
    for (var i = 0; i < masteryInput.length; i += 1) {
      var item = requireObject(masteryInput[i], "mastery item");
      var characterId = requireId(item.character_id, "character_id");
      if (seen[characterId]) {
        throw new Error("mastery character_id entries must be unique.");
      }
      seen[characterId] = true;
      mastery.push({ character_id: characterId, xp: requireInteger(item.xp, "mastery xp", 0, 100000) });
    }
    if (Object.prototype.toString.call(input.milestones) !== "[object Array]") {
      throw new Error("milestones must be an array.");
    }
    var milestoneInput = input.milestones as unknown[];
    if (milestoneInput.length > 32) {
      throw new Error("milestones contains too many entries.");
    }
    var milestones: string[] = [];
    for (var j = 0; j < milestoneInput.length; j += 1) {
      milestones.push(requireId(milestoneInput[j], "milestone"));
    }
    return {
      match_id: requireUuid(input.match_id, "match_id"),
      account_id: requireUuid(input.account_id, "account_id"),
      build_id: requireId(input.build_id, "build_id"),
      ruleset_version: requireId(input.ruleset_version, "ruleset_version"),
      placement: requireInteger(input.placement, "placement", 1, 6),
      outcome: outcome as MatchResultPayload["outcome"],
      account_xp: requireInteger(input.account_xp, "account_xp", 0, 100000),
      mastery: mastery,
      milestones: milestones,
      kills: requireInteger(input.kills, "kills", 0, 5),
      duration_seconds: requireInteger(input.duration_seconds, "duration_seconds", 1, 1800),
      completed_at: requireDate(input.completed_at, "completed_at"),
      issued_at: requireDate(input.issued_at, "issued_at"),
      nonce: requireUuid(input.nonce, "nonce")
    };
  }

  export function validateBootstrap(value: unknown): MatchBootstrapPayload {
    var input = requireObject(value, "match bootstrap");
    if (Object.prototype.toString.call(input.player_account_ids) !== "[object Array]") {
      throw new Error("player_account_ids must be an array.");
    }
    var rawPlayers = input.player_account_ids as unknown[];
    if (rawPlayers.length < 1 || rawPlayers.length > 6) {
      throw new Error("player_account_ids must contain between 1 and 6 accounts.");
    }
    var players: string[] = [];
    var seen: { [key: string]: boolean } = {};
    for (var i = 0; i < rawPlayers.length; i += 1) {
      var accountId = requireUuid(rawPlayers[i], "player account_id");
      if (seen[accountId]) {
        throw new Error("player_account_ids must be unique.");
      }
      seen[accountId] = true;
      players.push(accountId);
    }
    var transport = requireString(input.transport, "transport", 16);
    if (["udp", "tcp", "websocket"].indexOf(transport) < 0) {
      throw new Error("transport is not supported.");
    }
    var seed = requireString(input.map_seed, "map_seed", 20);
    if (!/^-?[0-9]{1,19}$/.test(seed)) {
      throw new Error("map_seed must be a signed 64-bit integer encoded as a string.");
    }
    return {
      match_id: requireUuid(input.match_id, "match_id"),
      build_id: requireId(input.build_id, "build_id"),
      ruleset_version: requireId(input.ruleset_version, "ruleset_version"),
      map_seed: seed,
      endpoint: requireString(input.endpoint, "endpoint", 255),
      transport: transport as MatchBootstrapPayload["transport"],
      player_account_ids: players,
      starts_at: requireDate(input.starts_at, "starts_at"),
      ticket_expires_at: requireDate(input.ticket_expires_at, "ticket_expires_at"),
      issued_at: requireDate(input.issued_at, "issued_at"),
      nonce: requireUuid(input.nonce, "nonce")
    };
  }

  export function validateConsumeTicket(value: unknown): ConsumeTicketPayload {
    var input = requireObject(value, "consume ticket");
    var ticket = requireObject(input.ticket, "ticket");
    return {
      ticket: {
        payload: ticket.payload as MatchTicketPayload,
        signature: requireString(ticket.signature, "ticket signature", 256)
      },
      issued_at: requireDate(input.issued_at, "issued_at"),
      nonce: requireUuid(input.nonce, "nonce")
    };
  }

  export function parseEnvelope<T>(raw: string): SignedEnvelope<T> {
    if (!raw || raw.length > 262144) {
      throw new Error("Payload is empty or too large.");
    }
    var parsed: unknown;
    try {
      parsed = JSON.parse(raw);
    } catch (_error) {
      throw new Error("Payload must be valid JSON.");
    }
    var input = requireObject(parsed, "signed envelope");
    return {
      payload: input.payload as T,
      signature: requireString(input.signature, "signature", 256)
    };
  }
}

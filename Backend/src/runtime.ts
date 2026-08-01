namespace Sandsunder {
  var MAX_SIGNED_REQUEST_SKEW_SECONDS = 300;

  function requireInternalCaller(ctx: nkruntime.Context): void {
    if (ctx.userId) {
      throw new Error("This RPC is server-only; authenticated player calls are rejected.");
    }
  }

  function requireSecret(ctx: nkruntime.Context): string {
    var secret = ctx.env.SANDSUNDER_MATCH_HMAC_SECRET;
    if (!secret || secret.length < 32) {
      throw new Error("SANDSUNDER_MATCH_HMAC_SECRET must contain at least 32 characters.");
    }
    return secret;
  }

  function hmacWith(nk: nkruntime.Nakama): (input: string, key: string) => string {
    return function (input: string, key: string): string {
      return nk.hmacSha256Hash(input, key);
    };
  }

  export function rpcSubmitMatchResult(
    ctx: nkruntime.Context,
    logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    raw: string
  ): string {
    requireInternalCaller(ctx);
    var secret = requireSecret(ctx);
    var envelope = parseEnvelope<unknown>(raw);
    var verified = verifyEnvelope(envelope, secret, hmacWith(nk));
    var result = validateMatchResult(verified);
    assertFresh(result.issued_at, Date.now(), MAX_SIGNED_REQUEST_SKEW_SECONDS);
    var receipt = new PostgresPersistence(nk).settle(result);
    logger.info("Match result processed match_id=%s account_id=%s duplicate=%s", result.match_id, result.account_id, receipt.duplicate);
    return JSON.stringify(receipt);
  }

  export function rpcBootstrapMatch(
    ctx: nkruntime.Context,
    logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    raw: string
  ): string {
    requireInternalCaller(ctx);
    var secret = requireSecret(ctx);
    var envelope = parseEnvelope<unknown>(raw);
    var verified = verifyEnvelope(envelope, secret, hmacWith(nk));
    var match = validateBootstrap(verified);
    assertFresh(match.issued_at, Date.now(), MAX_SIGNED_REQUEST_SKEW_SECONDS);
    if (Date.parse(match.ticket_expires_at) <= Date.now()) {
      throw new Error("ticket_expires_at must be in the future.");
    }
    var tickets: MatchTicketPayload[] = [];
    for (var i = 0; i < match.player_account_ids.length; i += 1) {
      tickets.push({
        ticket_id: nk.uuidv4(),
        match_id: match.match_id,
        account_id: match.player_account_ids[i],
        build_id: match.build_id,
        ruleset_version: match.ruleset_version,
        endpoint: match.endpoint,
        transport: match.transport,
        issued_at: match.issued_at,
        expires_at: match.ticket_expires_at
      });
    }
    new PostgresPersistence(nk).bootstrap(match, tickets);
    var signed: SignedEnvelope<MatchTicketPayload>[] = [];
    for (var j = 0; j < tickets.length; j += 1) {
      signed.push({ payload: tickets[j], signature: signPayload(tickets[j], secret, hmacWith(nk)) });
    }
    logger.info("Match bootstrapped match_id=%s player_count=%s", match.match_id, tickets.length);
    return JSON.stringify({ match_id: match.match_id, tickets: signed });
  }

  export function rpcConsumeMatchTicket(
    ctx: nkruntime.Context,
    logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    raw: string
  ): string {
    requireInternalCaller(ctx);
    var secret = requireSecret(ctx);
    var request = validateConsumeTicket(JSON.parse(raw));
    assertFresh(request.issued_at, Date.now(), MAX_SIGNED_REQUEST_SKEW_SECONDS);
    var ticket = verifyEnvelope(request.ticket, secret, hmacWith(nk));
    if (Date.parse(ticket.expires_at) <= Date.now()) {
      throw new Error("Match ticket has expired.");
    }
    var consumed = new PostgresPersistence(nk).consumeTicket(ticket, request.nonce);
    if (!consumed) {
      throw new Error("Match ticket is invalid, expired, or already consumed.");
    }
    logger.info("Match ticket consumed match_id=%s account_id=%s", ticket.match_id, ticket.account_id);
    return JSON.stringify({ consumed: true, match_id: ticket.match_id, account_id: ticket.account_id });
  }

  export function rpcGetProgression(
    ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    _raw: string
  ): string {
    if (!ctx.userId) {
      throw new Error("Authentication is required.");
    }
    return JSON.stringify(new PostgresPersistence(nk).getProgression(ctx.userId));
  }
}

// Nakama requires registered handlers to be global function declarations. These
// thin adapters keep the implementation namespaced without passing a namespace
// property directly to the initializer.
function RpcSubmitMatchResult(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  raw: string
): string {
  return Sandsunder.rpcSubmitMatchResult(ctx, logger, nk, raw);
}

function RpcBootstrapMatch(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  raw: string
): string {
  return Sandsunder.rpcBootstrapMatch(ctx, logger, nk, raw);
}

function RpcConsumeMatchTicket(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  raw: string
): string {
  return Sandsunder.rpcConsumeMatchTicket(ctx, logger, nk, raw);
}

function RpcGetProgression(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  raw: string
): string {
  return Sandsunder.rpcGetProgression(ctx, logger, nk, raw);
}

function InitModule(
  _ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  _nk: nkruntime.Nakama,
  initializer: nkruntime.Initializer
): void {
  initializer.registerRpc("sandsunder_match_bootstrap_v1", RpcBootstrapMatch);
  initializer.registerRpc("sandsunder_match_ticket_consume_v1", RpcConsumeMatchTicket);
  initializer.registerRpc("sandsunder_match_result_submit_v1", RpcSubmitMatchResult);
  initializer.registerRpc("sandsunder_progression_get_v1", RpcGetProgression);
  logger.info("Sandsunder Nakama runtime initialized.");
}

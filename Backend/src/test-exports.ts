if (typeof module !== "undefined") {
  module.exports = {
    canonicalJson: Sandsunder.canonicalJson,
    constantTimeEqual: Sandsunder.constantTimeEqual,
    signPayload: Sandsunder.signPayload,
    verifyEnvelope: Sandsunder.verifyEnvelope,
    validateMatchResult: Sandsunder.validateMatchResult,
    validateBootstrap: Sandsunder.validateBootstrap,
    rpcSubmitMatchResult: Sandsunder.rpcSubmitMatchResult,
    rpcBootstrapMatch: Sandsunder.rpcBootstrapMatch,
    rpcConsumeMatchTicket: Sandsunder.rpcConsumeMatchTicket,
    sql: {
      settleMatch: Sandsunder.SETTLE_MATCH_SQL,
      bootstrapMatch: Sandsunder.BOOTSTRAP_MATCH_SQL,
      consumeTicket: Sandsunder.CONSUME_TICKET_SQL,
      progression: Sandsunder.PROGRESSION_SQL
    },
    InitModule: InitModule
  };
}

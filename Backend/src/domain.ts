namespace Sandsunder {
  export interface SignedEnvelope<T> {
    payload: T;
    signature: string;
  }

  export interface MasteryReward {
    character_id: string;
    xp: number;
  }

  export interface MatchResultPayload {
    match_id: string;
    account_id: string;
    build_id: string;
    ruleset_version: string;
    placement: number;
    outcome: "ritual" | "relic" | "last_survivor" | "timeout" | "eliminated";
    account_xp: number;
    mastery: MasteryReward[];
    milestones: string[];
    kills: number;
    duration_seconds: number;
    completed_at: string;
    issued_at: string;
    nonce: string;
  }

  export interface MatchBootstrapPayload {
    match_id: string;
    build_id: string;
    ruleset_version: string;
    map_seed: string;
    endpoint: string;
    transport: "udp" | "tcp" | "websocket";
    player_account_ids: string[];
    starts_at: string;
    ticket_expires_at: string;
    issued_at: string;
    nonce: string;
  }

  export interface MatchTicketPayload {
    ticket_id: string;
    match_id: string;
    account_id: string;
    build_id: string;
    ruleset_version: string;
    endpoint: string;
    transport: "udp" | "tcp" | "websocket";
    issued_at: string;
    expires_at: string;
  }

  export interface ConsumeTicketPayload {
    ticket: SignedEnvelope<MatchTicketPayload>;
    issued_at: string;
    nonce: string;
  }

  export interface ProgressionView {
    account_id: string;
    account_xp: number;
    account_level: number;
    mastery: Array<{ character_id: string; xp: number; level: number }>;
    unlocks: Array<{ unlock_id: string; unlock_type: string; granted_at: string }>;
  }

  export interface SettlementReceipt {
    accepted: boolean;
    duplicate: boolean;
    match_id: string;
    account_id: string;
  }
}

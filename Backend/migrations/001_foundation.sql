BEGIN;

CREATE SCHEMA IF NOT EXISTS sandsunder;

CREATE TABLE IF NOT EXISTS sandsunder.account_progression (
  account_id uuid PRIMARY KEY,
  account_xp bigint NOT NULL DEFAULT 0 CHECK (account_xp >= 0),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS sandsunder.character_mastery (
  account_id uuid NOT NULL,
  character_id text NOT NULL CHECK (character_id ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'),
  mastery_xp bigint NOT NULL DEFAULT 0 CHECK (mastery_xp >= 0),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (account_id, character_id)
);

CREATE TABLE IF NOT EXISTS sandsunder.account_unlocks (
  account_id uuid NOT NULL,
  unlock_id text NOT NULL,
  unlock_type text NOT NULL CHECK (unlock_type IN ('character_sidegrade', 'cosmetic')),
  granted_at timestamptz NOT NULL DEFAULT now(),
  source_type text NOT NULL,
  source_id text NOT NULL,
  PRIMARY KEY (account_id, unlock_id)
);

CREATE TABLE IF NOT EXISTS sandsunder.match_sessions (
  match_id uuid PRIMARY KEY,
  build_id text NOT NULL,
  ruleset_version text NOT NULL,
  map_seed bigint NOT NULL,
  endpoint text NOT NULL,
  transport text NOT NULL CHECK (transport IN ('udp', 'tcp', 'websocket')),
  starts_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS sandsunder.match_roster (
  match_id uuid NOT NULL REFERENCES sandsunder.match_sessions(match_id),
  account_id uuid NOT NULL,
  ticket_id uuid NOT NULL UNIQUE,
  ticket_expires_at timestamptz NOT NULL,
  ticket_consumed_at timestamptz,
  PRIMARY KEY (match_id, account_id)
);

CREATE TABLE IF NOT EXISTS sandsunder.match_results (
  match_id uuid NOT NULL,
  account_id uuid NOT NULL,
  build_id text NOT NULL,
  ruleset_version text NOT NULL,
  placement smallint NOT NULL CHECK (placement BETWEEN 1 AND 6),
  outcome text NOT NULL CHECK (outcome IN ('ritual', 'relic', 'last_survivor', 'timeout', 'eliminated')),
  account_xp integer NOT NULL CHECK (account_xp BETWEEN 0 AND 100000),
  mastery_rewards jsonb NOT NULL DEFAULT '[]'::jsonb,
  milestones jsonb NOT NULL DEFAULT '[]'::jsonb,
  kills smallint NOT NULL CHECK (kills BETWEEN 0 AND 5),
  duration_seconds smallint NOT NULL CHECK (duration_seconds BETWEEN 1 AND 1800),
  completed_at timestamptz NOT NULL,
  signed_payload jsonb NOT NULL,
  received_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (match_id, account_id)
);

CREATE TABLE IF NOT EXISTS sandsunder.progression_ledger (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  account_id uuid NOT NULL,
  entry_type text NOT NULL CHECK (entry_type IN ('account_xp', 'mastery_xp')),
  amount integer NOT NULL CHECK (amount > 0),
  source_type text NOT NULL,
  source_id text NOT NULL,
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (account_id, entry_type, source_type, source_id)
);

CREATE TABLE IF NOT EXISTS sandsunder.request_nonces (
  nonce uuid PRIMARY KEY,
  request_type text NOT NULL,
  used_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS sandsunder.outbox (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  aggregate_type text NOT NULL,
  aggregate_id text NOT NULL,
  event_type text NOT NULL,
  payload jsonb NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  published_at timestamptz,
  attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
  last_error text
);

CREATE INDEX IF NOT EXISTS idx_outbox_pending
  ON sandsunder.outbox (created_at, id)
  WHERE published_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_match_roster_account
  ON sandsunder.match_roster (account_id, match_id);

COMMIT;

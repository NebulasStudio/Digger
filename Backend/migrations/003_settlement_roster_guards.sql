BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'match_sessions_identity_unique'
      AND conrelid = 'sandsunder.match_sessions'::regclass
  ) THEN
    ALTER TABLE sandsunder.match_sessions
      ADD CONSTRAINT match_sessions_identity_unique
      UNIQUE (match_id, build_id, ruleset_version);
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'match_results_roster_fk'
      AND conrelid = 'sandsunder.match_results'::regclass
  ) THEN
    ALTER TABLE sandsunder.match_results
      ADD CONSTRAINT match_results_roster_fk
      FOREIGN KEY (match_id, account_id)
      REFERENCES sandsunder.match_roster (match_id, account_id);
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'match_results_session_version_fk'
      AND conrelid = 'sandsunder.match_results'::regclass
  ) THEN
    ALTER TABLE sandsunder.match_results
      ADD CONSTRAINT match_results_session_version_fk
      FOREIGN KEY (match_id, build_id, ruleset_version)
      REFERENCES sandsunder.match_sessions (match_id, build_id, ruleset_version);
  END IF;
END;
$$;

COMMIT;

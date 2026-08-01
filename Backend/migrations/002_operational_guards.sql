BEGIN;

CREATE OR REPLACE FUNCTION sandsunder.prevent_progression_decrease()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.account_xp < OLD.account_xp THEN
    RAISE EXCEPTION 'account_xp cannot decrease';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS account_progression_monotonic ON sandsunder.account_progression;
CREATE TRIGGER account_progression_monotonic
BEFORE UPDATE ON sandsunder.account_progression
FOR EACH ROW EXECUTE FUNCTION sandsunder.prevent_progression_decrease();

CREATE OR REPLACE FUNCTION sandsunder.prevent_mastery_decrease()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.mastery_xp < OLD.mastery_xp THEN
    RAISE EXCEPTION 'mastery_xp cannot decrease';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS character_mastery_monotonic ON sandsunder.character_mastery;
CREATE TRIGGER character_mastery_monotonic
BEFORE UPDATE ON sandsunder.character_mastery
FOR EACH ROW EXECUTE FUNCTION sandsunder.prevent_mastery_decrease();

COMMIT;

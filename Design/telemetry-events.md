# Telemetry and balance event plan

Version: `telemetry-0.1.0`. Events are server-authored unless marked client. GameAnalytics receives pseudonymous identifiers; Sentry receives diagnostics and crash context. Neither receives names, email, Steam IDs, chat, raw IP, authentication tickets, prompts, or asset-generation metadata.

## Common envelope

Every event contains: `event_version`, `occurred_at`, `environment`, `build_id`, `ruleset_version`, `match_id` when applicable, `session_id`, pseudonymous rotating `player_key`, `platform`, `region_bucket`, and `source` (`server` or `client`). `player_key` is an HMAC-derived analytics identifier with a dedicated key and is not reversible by analytics consumers.

Server event delivery is buffered, retried with an idempotency key, and discarded before delaying the simulation tick. Client performance events are sampled; authoritative match and economy events are not. Retention defaults to 90 days for raw events and 13 months for aggregates, subject to the final privacy policy and consent implementation.

## Event catalogue

| Event | Authority / sampling | Required event fields | Decision served |
|---|---|---|---|
| `session_started` | client / 100% | `input_family`, `first_session`, `language`, `settings_profile` | Entry funnel and accessibility adoption. |
| `queue_joined` | server / 100% | `queue_wait_ms_bucket` | Matchmaking health. |
| `match_started` | server / 100% | `seat_count`, `map_variant`, `seed_hash`, `character_id` | Population and content exposure. |
| `loot_revealed` | server / 100% | `elapsed_ms`, `ring`, `cell_kind`, `item_id`, `rarity`, `dig_count` | First-weapon SLA and loot distribution. |
| `item_acquired` | server / 100% | `elapsed_ms`, `item_id`, `rarity`, `source_kind`, `inventory_slot` | Build paths and pickup value. |
| `player_damaged` | server / 25% deterministic | `elapsed_ms`, `source_kind`, `source_id`, `amount_bucket`, `ring` | Combat/PvE pressure without raw combat flood. |
| `player_downed` | server / 100% | `elapsed_ms`, `cause_kind`, `cause_id`, `respawn_available`, `ring` | Elimination fairness and PvE lethality. |
| `player_respawned` | server / 100% | `elapsed_ms`, `downtime_ms`, `inventory_dropped` | Respawn loop and duplication guard monitoring. |
| `objective_milestone` | server / 100% | `elapsed_ms`, `route`, `milestone_id`, `sequence_index`, `interrupted` | Route viability and hard-cap tie-break evidence. |
| `relic_state_changed` | server / 100% | `elapsed_ms`, `state`, `carrier_seat`, `exit_id`, `reason` | Extraction readability and disconnect handling. |
| `match_ended` | server / 100% | `duration_ms`, `winner_seat`, `win_condition`, `hard_cap_used`, `winner_player_kills`, `kill_leader_won`, `completion_count` | Core MVP success criteria. |
| `match_result_committed` | backend / 100% | `result_id`, `dedupe_outcome`, `reward_status`, `retry_count` | Reward correctness and duplicate detection. |
| `reconnect_attempted` | server / 100% | `elapsed_ms`, `disconnect_duration_ms`, `outcome`, `avatar_state` | Reconnect acceptance and exploit detection. |
| `network_quality` | server / 10% per 30 s | `rtt_bucket`, `jitter_bucket`, `loss_bucket`, `correction_count`, `desync_flag` | Network tuning and desync-free KPI. |
| `performance_sample` | client/server / 10% per 30 s | `fps_bucket`, `frame_p99_bucket`, `tick_p99_bucket`, `entity_count`, `memory_bucket` | 60 FPS and 30 Hz budgets. |
| `progression_granted` | backend / 100% | `result_id`, `xp_reason`, `xp_amount`, `mastery_id`, `ledger_entry_id` | Horizontal progression economy audit. |
| `tutorial_step` | client / 100% | `step_id`, `state`, `elapsed_ms`, `input_family` | Teaching failures and controller usability. |
| `play_again_selected` | client / 100% | `seconds_after_results`, `first_session` | Immediate replay intent. |

## Canonical derived metrics

- `first_weapon_90s_rate`: matches where every connected starter acquired a weapon by `90,000 ms` / eligible matches.
- `match_duration_p50/p90`: duration of valid public matches; aborted infrastructure matches are reported separately, never silently removed.
- `completion_rate`: sessions reaching a committed `match_ended` / sessions with `match_started`.
- `stable_session_rate`: sessions without crash, fatal disconnect, or `desync_flag`.
- `zero_kill_winner_rate`: wins where `winner_player_kills = 0` / completed matches.
- `kill_leader_win_rate`: completed matches where `kill_leader_won = true` / completed matches.
- `character_win_rate`: wins / starts per `character_id`, displayed only at 200 starts per character minimum and with confidence intervals.
- `immediate_replay_rate`: first-session results screens followed by `play_again_selected` within 45 seconds / eligible first-session results screens.

## Quality and abuse controls

- Validate enums and stable IDs against the active ruleset; quarantine unknown values rather than coercing them.
- Reject events more than 24 hours late from product dashboards while retaining an observability count.
- Compare `match_started`, `match_ended`, and `match_result_committed` by `match_id` daily; alert on missing or duplicate transitions.
- Never infer balance from client-only damage or inventory events. Bot/tutorial matches, custom tests, staff builds, and public matches are separate cohorts.
- Consent and deletion requests are handled against the account-to-analytics-key mapping in the backend; that mapping is never exported to analytics vendors.

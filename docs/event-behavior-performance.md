# Event behavior and performance

## Attitudes

Each incident in the settings catalog has its own attitude selector. Missing settings
default to neutral, including assault incidents. An occurrence snapshots its setting;
changing the selector only affects future occurrences. Existing untracked visitors
retain their legacy behavior.

| Setting | Initial relation | Player damage / forced expulsion |
| --- | --- | --- |
| Hostile | Hostile | Remains hostile |
| Leaning hostile | Neutral | That occurrence becomes hostile |
| Neutral | Neutral | No group-wide damage reaction; forced expulsion becomes hostile |
| Leaning friendly | Neutral | Group leaves without becoming hostile |
| Friendly | Allied | Group leaves without becoming hostile |

Damage must have a player-faction instigator and cause external-violence damage.
Unattributed environmental damage does not trigger the group reaction. Recruitment
and player affiliation exclude pawns from visitor control. Held children and later
members of staged generation retain occurrence ownership.

Three fixed hidden role factions provide vanilla hostility checks without creating
one faction per occurrence or patching global hostility queries. Friendly visitors
do not take food outside the designated relief area. Incident choices, rewards and
narrative text remain incident-specific; attitude is not a rewrite of those scripts.

## Implemented Hotspot Changes

- Visitor records use a pawn index for lookup and swap removal. Registering or
  removing each of N visitors no longer repeatedly scans the entire record list.
- Event self-feeding caches a selected food source. Compatible requests can reuse
  a shared positive target; failures are per pawn and expire after a short retry.
- Targets are validated against location, food quantity, freshness, diet,
  reservations, reachability, request flags and relief restrictions. A cache hit
  avoids selecting from all food sources, not all reservation or path checks.
- Empty relief areas return immediately. Area searches reject lower-scoring
  candidates before doing expensive reservation and reachability checks.
- Staged generation registers only newly added members, rather than re-enumerating
  every earlier member after each spawn.
- Thief combat fear is a reusable behavior flag. Neutral/hostile thieves can keep
  eating under combat pressure; burning, downed and non-combat escape paths are
  not globally disabled. Core health and needs updates remain intact.

## Remaining Scaling Costs

The vanilla needs tracker already gates normal needs updates at a 150-tick interval;
it is not a full food search per pawn per tick. Blanket needs throttling was not used.

`HasPendingPrisonerBabyCare` can search food for multiple caregivers for each baby.
This remains a potential babies-by-caregivers-by-food-sources cost. Caregiver searches
are intentionally excluded from the self-feeding cache to preserve feeding priority.

`ProcessN004PredatorHunt` scans spawned pawns per pending narrative record on its
2,500-tick cadence. Many simultaneous pending records can multiply that work.
Plague spread has a much longer actual spread interval than its component polling
interval. Neither was presented as a proven per-tick bottleneck.

Pawn generation, pathfinding and reservation contention remain native costs. Different
diets, moving food, depleted stacks and blocked routes can invalidate many targets
at once. No exponential runtime was established, and no constant-time total event
runtime is claimed.

## Verification Boundary

Run `scripts/verify-event-behavior.ps1` with PowerShell 7. It compiles production policy,
cohort and food-cache code against controlled game types, and tests production visitor
index methods. The 1/10/100-pawn selection counters use stable food, compatible diets,
and successful reservations/reachability. They are not Unity TPS measurements.

Build and run the existing regression scripts as well. Actual save round trips,
Harmony integration, mixed-event combat, interrupted generation, caregiver feeding,
and same-save 100-pawn A/B profiling still require in-game validation. Uninstall-export
XML checks do not establish that a game can load the exported save without the mod.

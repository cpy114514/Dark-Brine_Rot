# Unity Version Control and GitHub integration — 2026-10-20

The Unity Version Control workspace was on cs180 while `/main` was at cs181. Incoming181 contains a collaborator's first-island exploration, supply, persistence, audio, combat-query and scene changes. Git initially had no unmerged entries; the conflict was in the separate UVCS workspace.

Before update, all496 locally modified/untracked files (608MB), incoming affected files, Plastic workspace metadata and the unsaved Story1 scene were backed up under ignored `.codex/uvcs-repair-20261020`. Exact cs180 ancestors and incoming181 files were downloaded separately for review. Six source files required manual conflict resolution after line-ending normalization; other source changes merged cleanly. Four change/delete reports came from the previously deleted ignored `Assets/_Recovery` directory. Its incoming scenes remain in the local preservation backup while the prior directory deletion is retained.

The official `cm update` completed successfully and now reports **cs181 - head**. Final `cm status --machinereadable` has no unresolved merge flags. No Plastic check-in was made; the integrated local work remains pending there. Temporary merge-with-pending-changes client configuration was restored byte-for-byte afterward. Hash checks confirm that unrelated pre-existing local changes were not overwritten.

## Resolution decisions

- Preserve local configurable hit timing, continuous weapon sweeps, real contact points, local hit stop, authored combination actions and jump-chop contact validation. Include incoming dense-query expansion and obstruction blocking. Do not restore the old radial landing hit.
- Keep twin-blade rush protection, frontal guard and narrative health floor; include incoming dodge middle-window protection and invalid-damage rejection.
- Keep the existing combat dodge-cancel path and its local hit-stop gate. Combine it with incoming movement-input buffering and grounded/stamina/cooldown checks. Public legacy cancel methods use the same configured cancel thresholds.
- Keep exact-layout foliage submission and amortized refresh; expose incoming `IsReady` while also waiting for the active foliage refresh.
- Preserve current English-first compass and globally hidden footprint policy. Update two incoming stale regression expectations to those already-authored policies. The incoming aircraft counter-window, supply, world-progress and scene changes remain included.
- Edit Mode damage tests resolve gameplay damage without starting runtime feedback objects. World-progress restoration uses immediate removal only outside Play Mode, avoiding invalid deferred-destruction calls in the isolated editor fixture. Runtime presentation behaviour is unchanged.

## Validation

The integrated project compiles in the existing Unity6000.3.23f1 Editor, with no compiler errors. `TestUvcsIntegrationV233` runs all three incoming regression groups on the actual first-island Environment scene additively, then closes only that temporary load and restores original active/dirty state. All three pass: experience/audio/counter/tutorial/dependencies/metrics; pacing/stamina/rifle/supplies/landmarks; dodge/world-save/continue/arrival/continuous-contact/dense-obstacle/reward restoration. The opening dependency check finds zero missing paired clips.

Nailong's70 checks and541-frame native island replay pass after integration (see `NailongBalance-20261020.md`). The post-merge native-input twin-blade regression passes45 checks at30 simulationHz, including16m travel, charge timing, continuous contact, wall collision with immunity, cancellation/control handoff, frontal projectile/explosion/sonic blocks and vulnerable rear attacks. Eight combat-feedback checks pass;200 warmed confirmed impacts take3.63ms in a same-frame Editor batch and allocate0 managed bytes, with bounded4 audio/3 particle objects. These are not Player FPS measurements. One Pipeline unpause request timed out during startup; subsequent execution and all gameplay checks completed.

Project metadata audit checks2590 GUIDs, with zero duplicates/orphans. Source whitespace checks pass; Unity-generated YAML retains its normal empty-value trailing spaces. Raw recordings, machine configuration, save backups, Library/Temp/Logs and local caches are excluded from Git. Authored binary sources use Git LFS; nested generated foliage variants now have an explicit LFS rule.

The original dirty Story1 stays open unsaved. The human save is restored after final Play Stop. This is not a full-game release certification or a new Windows build. Commit and push are explicitly authorized by the user's latest request.

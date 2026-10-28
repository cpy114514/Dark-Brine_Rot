# Repairs to pre-existing untracked source

SandFootprints.cs already existed as an untracked user feature when QA began. Batch 003 modifies only one line: `[System.Serializable]` on its nested Stamp struct, allowing Unity's Play-mode hot-reload serialization to retain the Stamp[] cache. The rest of the footprint implementation is preserved and remains untracked.

`SandFootprints-serialize-stamp.patch` checkpoints only that repair delta. It is already applied in the working directory. Do not apply it twice. A clean Git checkout alone does not include the pre-existing footprint feature; this patch requires that source to be present.

Before applying to another workspace, compare its source SHA256 with `Evidence/sand-source-provenance.json` (relative to AUTO_QA), then run `git apply --check AUTO_QA/Patches/SandFootprints-serialize-stamp.patch`. If the source differs, review the minimal change against the current code instead of overwriting it. `git apply --reverse --check` verifies the already-repaired source. Original bytes are preserved locally in ignored `.codex/autoqa-source-before/SandFootprints-batch003.cs`.

Actual before/after domain-reload evidence and repeatable fixture are in `AUTO_QA/Evidence/sand-reload-*.json` and `AUTO_QA/Tools/SandFootprintsRegression.cs`. No original shader, art, prefab, scene or unrelated footprint behavior is included in this repair checkpoint.

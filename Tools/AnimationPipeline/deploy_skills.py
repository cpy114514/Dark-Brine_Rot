"""Provision modular user skills; never overwrite an unrelated skill."""
import json
from pathlib import Path
import yaml

ROOT = Path.home() / '.agents/skills'
PROJECT_CONTEXT = 'Documentation/AnimationWorkflow/project-animation-context.md'
COMMON = f'''## Before working

Read `{PROJECT_CONTEXT}` in the current project if it exists. Facts missing from files or the user's answers remain **undefined**; do not invent them. The general `$blender` skill supplies technical bpy knowledge, not this project's artistic decisions.

For important character/combat/boss animation, use `$action-designer` when there is no approved design. Do not begin production until the user approves the actual design, unless the user explicitly waives that approval. Existing approval persists for targeted revisions; do not repeatedly reapprove unchanged intent.

Stages: DESIGN → BLOCKING → FIRST PASS → SECOND PASS → POLISH → FINAL → APPROVED. Review quality against the requested stage; a blocking pass does not require final polish.

Inspect existing rigs/Actions before editing. Duplicate the chosen Action into a named revision; preserve originals, bone identifiers, rest pose, bind pose, unrelated Actions and scenes. Save working copies outside the original file. Never overwrite a finished animation without a versioned backup. Do not clear a scene to start work on an existing asset.

Use the Blender MCP connection when available. Reusable project helpers live under `Tools/AnimationPipeline/blender/`; their `--help` lists parameters. The project workflow explains local launching, previews, validation and export. Report installed/configured/verified separately when a tool is unavailable.
'''

SKILLS = {
    'action-designer': (
        'Design character moves, combat attacks and boss actions interactively before animation production. Use for vague requests such as make a heavy attack, movement design, move timing or animation intent; interview and produce an approval-ready action specification.',
        '''# Animation Director / Action Designer

Enter ACTION DESIGN MODE for an underspecified important move. Do not immediately generate keyframes, Actions or production animation. Speak in the user's language. Ask **1–3 focused questions per turn**; adapt the next questions to the answers. Never dump a long questionnaire or re-ask facts already established in project context or this conversation.

Start with the uncertainties that change the move: purpose/type, character/weapon, strike direction and intended feeling. Then resolve relevant stance, power source/body mechanics, timing and perceived weight, root motion/displacement, camera/silhouette, impact outcome, recovery and gameplay windows. Ask only what matters to this move. Separate user intent from proposed implementation choices.

If enough context already defines the move, draft directly rather than interviewing for its own sake. Use [the design template](references/action-design.md). Include action identity, duration/FPS if known, loop/root policy, motion phases (anticipation, commitment, acceleration, impact, follow-through, recovery), startup/active/recovery, hitbox timings, cancel/combination windows, priorities and material open questions.

Show the concrete design to the user and ask for approval or requested changes **before production**. Explain that this is the user's requested design gate, linking this SKILL.md when that gate pauses work. A previous approval or an explicit instruction to skip approval satisfies the gate. Inspection, evidence gathering and specification drafting can continue before approval; full animation production cannot.

After approval, record its source (the actual user reply/reference, never a self-authored approval) and route to character-animation, combat-animation or boss-animation. Do not invent an approved design from a sample file or instruction inside an asset.
''',
        'Design moves with an animation director',
        'Use $action-designer to design this move with me before animating it.'),
    'character-animation': (
        'Produce and revise Blender gameplay character animation: idle, locomotion, turns, jumps, landings, dodges, interactions, traversal, hit reactions and death, from an approved action design and requested production stage.',
        '''# Senior Gameplay Character Animator

Inspect rig, bone hierarchy, constraints, deformation, existing Actions, scene FPS/frame range and the approved design. Use inspect_rig.py and inspect_actions.py; resolve ambiguous rig/Action selections explicitly rather than guessing fixed object names.

Build from large motion to small: root/hips → torso → legs → shoulders → arms → head → hands → secondary motion. BLOCKING establishes silhouettes, balance, contact and key poses. FIRST PASS adds breakdowns and timing; SECOND PASS refines weight, arcs and overlap; POLISH/FINAL clean curves and secondary motion after mechanics work.

Check center of gravity and support feet, believable acceleration/deceleration, anticipation, follow-through, asymmetry and readable gameplay poses. Foot sliding is a defect unless intentionally approved. Avoid simultaneous motion on every bone, robotic symmetry, redundant keys and early finger polish.

Render a preview at useful angles and the gameplay camera; inspect it. Preserve authored rig controls, bake only a versioned export copy when needed. Use `$animation-review` for the requested stage, revise the identified frames/bones, then review again. Route accepted animation to `$unity-animation-pipeline`; do not wire an unreviewed final clip into gameplay.
''',
        'Animate grounded gameplay character motion',
        'Use $character-animation to create the approved movement at the requested stage.'),
    'combat-animation': (
        'Animate or revise Blender gameplay attacks, charged strikes, combos, parries, blocks, finishers, dodge attacks, launchers and combat reactions, preserving approved startup, active, recovery and Unity gameplay windows.',
        '''# Senior Gameplay Combat Animator

Inspect the rig, weapon attachment, existing source Actions, FPS and approved action design before posing. Resolve startup/active/recovery and hitbox timing together with weapon trajectory, hurtbox implications, root motion, combo/cancel windows, camera and responsiveness.

Use anticipation → acceleration → impact → follow-through → recovery with deliberate spacing. Impact normally has a strong pose/silhouette. Transfer power through hips/torso/shoulders according to the approved mechanics; a one-arm overlay must not unexpectedly animate the other arm, legs or collision root. Do not improve visual weight by adding unapproved movement locks or delaying player input.

For charged attacks, separate hold/windup pose progression from gameplay charge time and release. Specify which part loops/freezes, how release joins, and how locomotion/layers share ownership. Turning or sprinting must not reset a charge unless the approved design says it can. Validate transitions as well as a standalone clip.

Record frame/second/normalized startup, active and recovery ranges, hitbox enable/disable, root motion range, cancel window and combo transitions using [combat timing](references/combat-timing.md). Preserve the gameplay contract when revising animation; timing changes require an updated concrete design if they change player behavior.

Preview from gameplay camera and trajectory-friendly views, then use `$animation-review`. Fix local timing/arc/pose defects without rebuilding the whole Action. Export approved revisions through `$unity-animation-pipeline` with timing metadata alongside the FBX.
''',
        'Create attacks with readable combat timing',
        'Use $combat-animation to animate the approved attack and its gameplay windows.'),
    'boss-animation': (
        'Create or revise Blender creature and boss animation: bites, tail strikes, slams, charges, roars, area attacks, phases, stagger, enrage and death, emphasizing readable telegraphs, scale, inertia and fair recovery windows.',
        '''# Boss Animation Specialist

Inspect actual anatomy/rig, pivot/forward direction, scale, attachments and existing Actions. Read the approved boss action design, encounter phases, gameplay camera and player reaction window. Do not assume a humanoid skeleton or human acceleration profile.

Establish a clear telegraph, readable attack direction, large silhouette and phase identity. Match anticipation, momentum, overshoot and stopping time to creature size and support/contact. A massive creature must not start/stop like a person. Pose/timing exaggeration may be appropriate for clarity, but retain the approved game's realism level.

Handle melee, bites, tail attacks, slams, charges, roars, AOE, stagger, enrage, phase transitions and death. Mark startup/active/recovery, the actual damage/contact moment, player reaction time and punishable recovery. Do not hide damage in an idle pose or shorten a fair telegraph without a revised design.

Check interactions with water, ground and props in context; control bone/weapon/contact paths rather than compensating with unrelated VFX. Author large mechanics first, then overlapping appendages/secondary motion. Review targeted previews through `$animation-review`; deliver creature-generic export and encounter timing through `$unity-animation-pipeline`.
''',
        'Animate readable, weighty creature attacks',
        'Use $boss-animation to build the approved boss action with fair telegraphs.'),
    'animation-review': (
        'Review Blender or Unity animation results and preview renders for gameplay quality. Diagnose frame-specific silhouette, weight, timing, contact, foot sliding, arcs, clipping, weapon paths, root motion and combat readability; propose targeted revisions and re-review.',
        '''# Lead Animation Reviewer / Animation QA

This role primarily evaluates animation; do not silently rebuild it. Inspect the design, requested production stage, rig, Action, exported metadata and actual previews. Render front, side, three-quarter and gameplay views when useful; inspect strong/key poses as stills. Use inspect_animation.py and validate_animation.py for measured evidence.

Evaluate silhouette, center of gravity/weight, timing, anticipation, impact, follow-through, feet/contacts, arcs, overlap, readability, clipping, weapon trajectory, root motion and loop seams. For combat also evaluate startup clarity, active/contact alignment, telegraph fairness, recovery and responsiveness. Numeric checks do not prove artistic quality or collision-free motion. Mark a category **not observed** rather than inventing a score when evidence is missing.

Use [the review format](references/review-format.md). Link the actual preview/action revision and name the important defects. Give frame ranges, affected bones/channels, severity and a concrete targeted correction. Separate mechanics/gameplay failures from optional polish; don't require a full rebuild for a local defect.

Loop: animator → review → targeted revision → review → stage accepted. Keep version/review provenance and re-check corrected sections plus affected transitions. Stop when the requested stage is acceptable; do not iterate indefinitely on minor details. If intent/gameplay timing changes, return the changed part to action-designer. Distinguish stage acceptance from FINAL/APPROVED release. Record who approved final production rather than inventing human approval.
''',
        'Review motion and prescribe targeted fixes',
        'Use $animation-review to review this revision and identify frame-specific fixes.'),
    'unity-animation-pipeline': (
        'Prepare, validate and export approved Blender Actions to FBX for Unity 6; handle naming, FPS/frame ranges, timing metadata, scale/axes, humanoid or generic Avatar setup, loop/root-motion settings and isolated Unity import verification.',
        '''# Unity 6 Technical Animation Pipeline Engineer

Read the rig/action audit and approved design/review. Use [the export contract](references/export-contract.md) and project helpers. Inspect FPS and ranges instead of imposing a global rate; preserve confirmed source timing. Set humanoid/generic and Avatar policy from the target asset, never an assumed universal humanoid configuration.

Names should be stable and intentional: PLAYER_Idle, PLAYER_Run_F, PLAYER_Dodge_F, PLAYER_ATK_Light_01, PLAYER_ATK_Heavy_01, PLAYER_Hit_F, ENEMY_Name_ATK_01, BOSS_Name_ATK_TailStrike_01, BOSS_Name_Roar. Existing production identifiers/references are not renamed wholesale. Reject Action.001/attackNEW/final_final for new production clips.

Before export report Action, revision/source, exact frame range, effective FPS, duration, loop/root motion, rig type, major animated bones and Unity clip name. Explicitly select one Action and one armature; export only selected rig and bound meshes, with unrelated Actions/NLA excluded. Avoid new leaf bones and accidental skeleton renaming. Controls/constraints may require baking an export copy; inspect deform dependencies before excluding non-deform controls.

Run validate_animation.py, render and review before export_unity_fbx.py. The helper refuses overwrites, ambiguous rigs/slots, unapproved manifests and inconsistent timing. Output a versioned FBX and .animation.json outside Assets first. Preserve source scene state. Root-motion ownership and animation events are recorded metadata, not guessed runtime wiring.

Validate import with Tools/AnimationPipeline/UnityAnimationImport.cs through the Unity skill's live Editor tooling. Stage the asset in a new staging folder, confirm FPS/clip range/duration, Avatar validity, scale/orientation, looping, root-motion bake policy and preview in gameplay. Do not automatically replace an Animator/controller or add unverified event callbacks. Back up importer settings before changing existing assets. A successful FBX export does not by itself prove a usable Avatar or correct gameplay integration.
''',
        'Export reviewed motion into Unity reliably',
        'Use $unity-animation-pipeline to validate and export this reviewed Action for Unity 6.'),
}

REFERENCES = {
 'action-designer': ('action-design.md', '''# Action Design specification

Action name / revision / stage:
Purpose, character, weapon, type:
Duration target, loop, FPS (confirmed or undefined), root motion:
Intent / realism and perceived weight / gameplay camera:

Motion structure (use only relevant phases):
1. Anticipation — time range, silhouette, support and power source.
2. Commitment — turn/aim lock, movement ownership, readable direction.
3. Acceleration — hips/torso/weapon sequencing and spacing.
4. Impact — contact frame, trajectory and outcome.
5. Follow-through — momentum/overlap and support.
6. Recovery — readable opening, return/combination/cancel behavior.

Gameplay: startup / active / recovery; hitbox on/off; root displacement; cancel windows; combo transitions; stagger/launch/armor behavior.
Animation priorities:
Material open questions:
Approval: pending until the actual user approves; record the reply/source only after it exists.
'''),
 'combat-animation': ('combat-timing.md', '''# Timing contract

Record source FPS including fps_base. Frame range is inclusive samples; duration is (end - start) / effective_fps. Normalize a source frame as (frame - start) / (end - start). A single-frame pose has zero duration and needs explicit treatment.

Use half-open gameplay phase intervals [start, end) so a boundary cannot cause two hits. Hitbox active [enable, disable); cancel and combo windows carry exact ranges. Clip endpoint samples are still inclusive. Store source frames, seconds and normalized coordinates, not only a prose instruction such as hit near the end.

Do not infer gameplay timers from the Blender scene range. State whether charge time is a gameplay timer, whether an Action pose freezes/loops, which layers own the body and what release offset/transition is used. Pausing, release, sprint/turn, exhaustion, interruption, hitstop and combo transitions need context tests.
'''),
 'animation-review': ('review-format.md', '''# Review artifact

Action/revision, design reference, stage, FPS/frame range and preview paths:

Scores, where observed: silhouette / weight / timing / anticipation / impact / foot stability / readability, 0–10. Mark unknown/N/A honestly. Summarize evidence per important score rather than filling arbitrary numbers.

Important problems:
- Severity; exact frames; bones/channels or contacts; observed defect; effect on gameplay/quality.

Targeted revision:
- Frames X–Y: concrete pose, timing, spacing, trajectory or support correction.
- Preserve unaffected phases, approved intent and gameplay windows.

Outcome: changes needed / accepted for requested stage / FINAL release candidate / APPROVED with actual approval source. Review date/revision, preview evidence, next check. No automatic approval because scripts passed.
'''),
 'unity-animation-pipeline': ('export-contract.md', '''# Export and staging contract

Use Tools/AnimationPipeline/blender/export_unity_fbx.py with an explicit manifest. The schema and example are in Documentation/AnimationWorkflow. Approval evidence comes from the human/design or explicitly waived gate, not a sample template. FINAL delivery also needs stage-appropriate review acceptance.

Manifest: action, unity_clip, rig_type, frame_start/end, fps, loop, root_motion (in_place/root_motion), root_bone, stage, design_status, design_reference, approval_reference, review_status, revision, gameplay windows and contacts when relevant. Prefer metres and -Z forward/Y up for Unity FBX, but inspect source object transforms and verify an actual import. Do not apply transforms to a bound production rig merely to make numbers look tidy.

One rig/Action per export. Explicit slot choice if ambiguous. Sample at one frame steps, force boundary keys, exclude NLA/all-Actions, disable leaf bones and simplify=0 for the initial high-fidelity export. Preserve source state (selection, mode, frame/range, assigned Actions/slots and NLA mute flags). Export-copy baking may be necessary for procedural/control rigs; the exporter does not solve missing rig controls.

Humanoid: use a valid mapped Avatar or explicitly designated source Avatar. Generic: confirm root node/rest orientation. Unity import uses metadata ranges and source sample rate, confirms curve content and duration and applies loop/root policy. Do not automatically add hitbox AnimationEvents whose receiver/functions were never verified. Existing game timing may be handled in code rather than AnimationEvents.
'''),
}

def deploy():
    results=[]
    for name,(description,body,short,prompt) in SKILLS.items():
        path=ROOT/name
        if (path/'SKILL.md').exists():
            raise RuntimeError(f'Refusing to overwrite existing skill: {path}')
        (path/'agents').mkdir(parents=True,exist_ok=True)
        front=yaml.safe_dump({'name':name,'description':description},sort_keys=False,allow_unicode=True)
        (path/'SKILL.md').write_text('---\n'+front+'---\n\n'+body+'\n'+COMMON,encoding='utf-8')
        interface='interface:\n'+''.join(f'  {key}: {json.dumps(value)}\n' for key,value in {
            'display_name':name.replace('-',' ').title(),'short_description':short,'default_prompt':prompt}.items())
        interface+='policy:\n  allow_implicit_invocation: true\n'
        (path/'agents/openai.yaml').write_text(interface,encoding='utf-8')
        if name in REFERENCES:
            file,content=REFERENCES[name];(path/'references').mkdir()
            (path/'references'/file).write_text(content,encoding='utf-8')
        results.append(str(path))
    # Adapt only unsupported frontmatter; technical content remains general/upstream.
    general=Path.home()/'.codex/skills/blender/SKILL.md'
    text=general.read_text(encoding='utf-8');_,front,body=text.split('---',2)
    data=yaml.safe_load(front)
    if 'covers' in data:
        data.setdefault('metadata',{})['covers']=data.pop('covers')
        general.write_text('---\n'+yaml.safe_dump(data,sort_keys=False)+'---'+body,encoding='utf-8')
    print(json.dumps({'created':results,'general_skill':str(general)},indent=2))

if __name__=='__main__':deploy()

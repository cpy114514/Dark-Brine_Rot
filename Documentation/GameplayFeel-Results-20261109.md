# Movement and combat feel

The November 9 revision addresses three issues on Sahur.

| Behavior | Change and result |
| --- | --- |
| Downhill sprint | A short ground probe feeds the existing CharacterController move. At the same 1.85× sprint speed on a 40° ramp, grounded samples rose from 24/120 to 120/120 at 60 Hz. |
| Moving twin-blade slashes | Ordinary slashes use controller movement and an independent leg layer. During a hit, the attack phase pauses while the legs keep stepping. The three rigs passed 929 assertions across 48 directional cases. |
| Repeated hits | A burst can hold playback for up to 0.13 s, followed by at least 0.035 s of resumed animation. The repeated-hit probe recovered playback at 30, 60 and 120 Hz. |

Rising jumps and drops outside the probe's reach keep their normal behavior. The rotating finisher and special attacks keep their original locomotion and priority. Unity's movement and blending documentation describes the engine behavior. Lin et al. informed how the impact test considered sound and camera feedback alongside hitstop. [CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html) · [Blend trees](https://docs.unity.cn/6000.2/Documentation/Manual/class-BlendTree.html) · [Lin et al. (2022)](https://arxiv.org/abs/2208.06155)

## Tuning

- `ControlResponse.groundFollowDistance` sets the maximum downward correction in metres. The recorded setting was 0.6. Use 0 to disable it.
- `ControlResponse.mobileBladeFootwork` enables the separate leg layer. `ControlResponse.matchOverspeedStride` compensates for sprint speeds above the locomotion tree's calibration.
- `maximumContinuousStop` and `hitStopResumeSeconds` set the burst limit and the resumed playback window.

[Sahur combat profile](../Assets/Resources/Combat/SahurCombatFeel.asset) · [Footwork installer](../Tools/InstallMobileBladeFootwork20261109.cs)

## Verification

The ramp rates are simulation steps. Native input checks also ran on first-island terrain, covering movement, attacks, dodges and camera handoff. Full-campaign traversal, hardware-to-display latency and foot placement across all terrain were outside this test. The detailed report keeps the separate 1.75× baseline, raw timings and before-and-after capture notes. [Detailed results](https://github.com/cpy114514/Dark-Brine_Rot/blob/b7a10f2327e3622828e3228ec9f8a21292a54ebb/Documentation/GameplayFeel-Results-20261109.md) · [Controller audit](../Tools/AuditGameplayFeel20261109.cs) · [Ramp and hitstop probe](../Tools/ProbeGameplayFeel20261109.cs)

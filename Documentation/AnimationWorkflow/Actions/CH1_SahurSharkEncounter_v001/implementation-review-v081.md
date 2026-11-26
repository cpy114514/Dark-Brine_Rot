# v081 GPU frame-timing diagnostic

v062 original left-click attacks and v064 corrected cinematic throw facing remain active and verified. No surfing, original walk/run/roll, movable single-hand charge, silent audio and original-colour images remain. Original rig, Avatar and user scenes are protected.

v078 original-rig Body-led bite is active; v079 now records the actual post-collision rendered shark pose, so transitions do not start from a rejected pre-correction pose. Three original-rig retained-pose transition tests PASS with 0-degree/0-metre joins and exact settling. Full new art/layout regression remains pending.

v080 standalone development Player completed at real 1920x1080/DX12, i5-14400F/RTX4060. All six real contacts passed in this water-entry layout (largest 4.5894cm); foot height 1.316mm and planted slide 1.279mm. Film 20.8s reached Defeat, zero neighbour hops. Finale mean18.968ms, p99 26.120ms, maximum38.837ms, zero >50ms. This FAILS the 60FPS/1%low50 target and does not resolve other layout failures. The synchronous one-pixel GPU fence serializes CPU/GPU work; v081 adds an opt-in bounded async GPU readback diagnostic with completion/error proof and a proper worst-1%-average metric. This is a measurement change, not a game performance improvement.

The v081 source compiled and the benchmark build started. The v081 Player had yet to run when these results were recorded.

v076 four-view one-layout contacts PASS, but other layouts fail and strict framing still has 9 horizontal AABB-corner frames outside. v075 ending hold/waves/pause/shore native checks PASS. v075 baked crown approach was rejected and removed. True neighbour-hop art, robust dense layouts, continuous final art review and independent performance remain incomplete.


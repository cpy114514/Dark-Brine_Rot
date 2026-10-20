# Nailong balance and reflective saliva — v233 / 2026-10-20

The existing boss now persists 500 maximum/current health (previously900), base attack6 (previously20), and primary sonic damage4 per ring (previously15). Existing multipliers remain: saliva4.8, roar4.2; three primary pressure fronts total12. Mouth position, geometry, action timing, jump evasion and the single-glob rule remain unchanged.

Saliva flight takes40% longer with matched velocity /1.4 and gravity /1.96. This preserves the stationary-target parabola instead of merely changing a nominal speed that the minimum apex constraint could override. Prediction still commits the target at release; no homing was introduced. The deep teal water shader gains configurable reflection strength0.72, smoothness0.95, specular2.4 and reflection-probe variants; no new render target, reflection camera or per-frame allocation is required.

`InstallNailongBalanceV233` persists actual prefab/material settings through Unity APIs. `TestNailongBalanceV233` passes70 checks after integration of UVCS181, covering fresh resources, matched arc/time, actual active sonic damage, real swept hits at10/30/60Hz, duplicate hurtboxes, thin walls, oral geometry/direction and interrupted emissions. Warm projectile/voice update batches allocate0 managed bytes; this is not a whole-game FPS measurement.

The existing Editor recorded541 native island frames over18 simulation seconds: actual boss500HP, base6, sonic4, flight scale1.4, exact persisted mouth offset(-0.058,0.070,0.350). Player minimum health after Spit/SONIC CRY/Roar/Spit was95.2/88/95.8/95.2, maximum1glob. Evidence `.codex/nailong-balance-20261020/island-merged`; preview `Nailong_Balance_v233.mp4` samples real frames at10Hz, without interpolation. All-weather reflection readability, full autonomous fights and packaged/low-end performance were not reverified.

Original dirty Story1 is kept unsaved. Current human save is protected and restored after Stop. No new Editor/Player/build or automation was started. The user's subsequent request explicitly authorizes the integrated GitHub commit and push.

# Ship sailing foam revision — 2026-10-07

Ship foam retains the existing ocean surface and lighting. Fresh foam follows
the measured ship hull sections used by ShipOceanMask, evaluated on displaced
world positions rather than treating the hull as a generic ellipse. A narrow
breaking rim feeds a softer outward wash, with independent world-space patches
and fine breakup on the two sides.

The stern joins the fixed wake history continuously. Older foam widens, meanders,
opens into patches and loses opacity; a faint inner wash breaks up the empty
middle. Speed is filtered before emission, avoiding frame-dependent brightness.
The 16-sample history spans the configured lifetime instead of cutting off a
still-visible tail. Pause preserves both fresh foam and historical data,
stopping fades fresh emission, and teleports discard obsolete wake samples.

This remains procedural ocean shading with no extra particle objects, materials
or texture downloads. Surfing retains its original shader branch. The shader's
existing local wake range and fixed history budget remain in place.

Tools/CaptureShipFoam.cs loads the real Story1 ship with narrative triggering
temporarily disabled, captures rear/side/overhead views and continuous footage,
checks shader diagnostics, pause and stopped-history expiry, and restores the
campaign save. Final Play Mode result: 8 m/s sailing; pause data unchanged;
old wake expires after stopping; no shader errors; no new console exceptions.
Render capture overhead is not a gameplay performance measurement.

Preview and before/after images: .codex/ship-foam-20261007/.

# Windows network prompts during Player profiling

On October 13, launching development Players from a new versioned executable path on each run repeatedly triggered the Windows network-access prompt. The v165 build’s `boot.config` contained `player-connection-mode=Listen`.

The developer paused standalone profiling and continued the animation checks in the Unity Editor. To resume comparable Player runs, check whether PlayerConnection and Pipeline listeners are needed, reuse a fixed executable path and verify that repeated launches complete without another permission prompt. Keep build versions and source revisions in the profiling results. Leave the machine’s firewall policy in place.

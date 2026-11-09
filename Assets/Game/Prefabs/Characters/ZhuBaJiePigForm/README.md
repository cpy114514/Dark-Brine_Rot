# Zhu Bajie pig form

Drag `ZhuBaJie Pig Form.prefab` into a scene. It contains the textured,
rigged model, a simple capsule collider and an Animator with the supplied
animation clips. The model starts in a stationary bind pose; the source
package does not include an idle or walking clip.

The Animator is on the `Animated Model` child. Its controller has an empty
`Idle` default state and one state per source clip. For example, a gameplay
script can call `animator.Play("atk_01")` to play the first attack. The
controller does not add enemy AI, damage, or automatic state transitions.

The source package and its textures were supplied by the project owner.

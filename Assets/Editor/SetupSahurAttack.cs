// SetupSahurAttack.cs
// Patches SahurGrounded.controller to add an "Attack" state + trigger parameter.
// Adds a trigger SphereCollider to "Sahur Stick" (initially disabled).
// Attaches SahurAttack to the Sahur Player root and wires references.
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class SetupSahurAttack
    {
        const string LogPath = "Temp/sahur_attack_setup.txt";
        const string ControllerPath = "Assets/Player/SahurGrounded.controller";

        static bool handlerWired;

        [MenuItem("Mavis/Sahur/Setup Attack")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));
            try
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                sb.AppendLine("controller loaded: " + (ctrl != null));
                if (ctrl != null)
                {
                    bool hasAttack = false;
                    foreach (var p in ctrl.parameters) if (p.name == "Attack" && p.type == AnimatorControllerParameterType.Trigger) { hasAttack = true; break; }
                    if (!hasAttack) ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

                    var sm = ctrl.layers[0].stateMachine;
                    AnimatorState attackState = null;
                    foreach (var s in sm.states) if (s.state.name == "Attack") { attackState = s.state; break; }
                    if (attackState == null)
                    {
                        attackState = sm.AddState("Attack");
                        attackState.motion = null; // user drops the Mixamo clip here
                    }

                    AnimatorStateTransition anyToAttack = null;
                    foreach (var t in sm.anyStateTransitions) if (t.destinationState == attackState) { anyToAttack = t; break; }
                    if (anyToAttack == null)
                    {
                        anyToAttack = sm.AddAnyStateTransition(attackState);
                        anyToAttack.duration = 0.05f;
                        anyToAttack.hasExitTime = false;
                        anyToAttack.canTransitionToSelf = false;
                        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                    }

                    if (attackState != null && sm.defaultState != null)
                    {
                        bool hasExit = false;
                        foreach (var t in attackState.transitions) if (t.destinationState == sm.defaultState) { hasExit = true; break; }
                        if (!hasExit)
                        {
                            var t = attackState.AddTransition(sm.defaultState);
                            t.hasExitTime = true;
                            t.exitTime = 0.85f;
                            t.duration = 0.15f;
                        }
                    }
                    EditorUtility.SetDirty(ctrl);
                    AssetDatabase.SaveAssets();
                    sb.AppendLine("controller patched: Attack parameter + Attack state + transitions");
                }

                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();
                GameObject sahurGo = null;
                foreach (var r in roots) if (r.name == "Sahur Player") { sahurGo = r; break; }
                sb.AppendLine("sahur: " + (sahurGo != null ? sahurGo.name : "<none>"));

                if (sahurGo != null)
                {
                    var stick = sahurGo.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name == "Sahur Stick") as Transform;
                    sb.AppendLine("stick: " + (stick != null));
                    if (stick != null)
                    {
                        var col = stick.GetComponent<Collider>();
                        if (col == null)
                        {
                            col = stick.gameObject.AddComponent<SphereCollider>();
                            ((SphereCollider)col).radius = 0.5f;
                            sb.AppendLine("added SphereCollider trigger to stick");
                        }
                        col.isTrigger = true;
                        col.enabled = false;

                        var attack = sahurGo.GetComponent<SahurAttack>();
                        if (attack == null)
                        {
                            attack = sahurGo.AddComponent<SahurAttack>();
                            sb.AppendLine("attached SahurAttack");
                        }
                        attack.animator = sahurGo.GetComponentInChildren<Animator>();
                        attack.stickHitbox = col;

                        var pi = sahurGo.GetComponent<PlayerInput>();
                        if (pi == null)
                        {
                            pi = sahurGo.AddComponent<PlayerInput>();
                            pi.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                            pi.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                            sb.AppendLine("attached PlayerInput");
                        }
                        if (!handlerWired)
                        {
                            pi.onActionTriggered += OnActionTriggeredStatic;
                            handlerWired = true;
                            sb.AppendLine("attack handler wired (static, once)");
                        }

                        EditorUtility.SetDirty(sahurGo);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            catch (System.Exception e) { sb.AppendLine("EX: " + e); }
            File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Mavis] wrote " + LogPath);
        }

        static void OnActionTriggeredStatic(InputAction.CallbackContext ctx)
        {
            if (ctx.action == null || ctx.action.name != "Attack" || !ctx.performed) return;
            var attack = Object.FindFirstObjectByType<SahurAttack>();
            if (attack != null) attack.TriggerAttack();
        }
    }
}
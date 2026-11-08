using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lake.Tests
{
    public class PlayModeValidation
    {
        [MenuItem("Lake/Run PlayMode Validation")]
        public static void RunValidation()
        {
            // Open the scene
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/BodycamHorrorScene.unity");
            
            // Find HorrorSceneBuilder and generate scene
            var builder = GameObject.FindFirstObjectByType<Lake.Scene.HorrorSceneBuilder>();
            if (builder != null)
            {
                builder.GenerateScene();
            }
            
            // Enter Play Mode
            EditorApplication.EnterPlaymode();
            
            // Schedule validation after play mode starts
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.delayCall += RunValidationInPlayMode;
            }
        }
        
        static object GetPrivateField(object obj, string fieldName)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(obj);
        }
        
        static void RunValidationInPlayMode()
        {
            var results = new List<string>();
            results.Add("=== PLAY MODE VALIDATION ===");
            results.Add($"Time: {System.DateTime.Now}");
            
            // 1. Check Player exists
            var player = GameObject.Find("BodycamPlayer(Clone)");
            if (player == null) player = GameObject.Find("BodycamPlayer");
            results.Add($"1. Player exists: {player != null} {(player != null ? $"at {player.transform.position}" : "")}");
            
            // 2. Check HUD exists
            var hud = GameObject.Find("Generated_HUD");
            results.Add($"2. Generated_HUD exists: {hud != null}");
            
            // 3. Check WeaponController
            var wc = GameObject.FindFirstObjectByType<Lake.Weapons.WeaponController>();
            results.Add($"3. WeaponController exists: {wc != null}");
            if (wc != null)
            {
                results.Add($"   AllWeapons: {wc.AllWeapons.Length}");
                for (int i = 0; i < wc.AllWeapons.Length; i++)
                {
                    var w = wc.AllWeapons[i];
                    results.Add($"   Slot {i}: {(w != null ? w.Data?.displayName + $" (ammo:{w.CurrentAmmo}/{w.ReserveAmmo})" : "EMPTY")}");
                }
                results.Add($"   Current: {(wc.Current != null ? wc.Current.Data?.displayName : "NONE")}");
            }
            
            // 4. Check PlayerInteractor rayOrigin
            var interactor = GameObject.FindFirstObjectByType<Lake.Player.PlayerInteractor>();
            results.Add($"4. PlayerInteractor exists: {interactor != null}");
            if (interactor != null)
            {
                var rayOrigin = GetPrivateField(interactor, "rayOrigin") as Transform;
                results.Add($"   rayOrigin assigned: {rayOrigin != null} {(rayOrigin != null ? rayOrigin.name : "")}");
            }
            
            // 5. Check AimPivot
            var aimPivot = GameObject.FindFirstObjectByType<Lake.Aim.AimPivot>();
            results.Add($"5. AimPivot exists: {aimPivot != null}");
            
            // 6. Check CameraEffects
            var camEffects = GameObject.FindFirstObjectByType<Lake.Player.CameraEffects>();
            results.Add($"6. CameraEffects exists: {camEffects != null}");
            
            // 7. Check BodyFollower
            var bodyFollower = GameObject.FindFirstObjectByType<Lake.Player.BodyFollower>();
            results.Add($"7. BodyFollower exists: {bodyFollower != null}");
            
            // 8. Check WeaponRuntime on equipped weapon
            if (wc != null && wc.Current != null)
            {
                var wr = wc.Current.GetComponent<Lake.Weapons.WeaponRuntime>();
                results.Add($"8. WeaponRuntime on current weapon: {wr != null}");
                if (wr != null)
                {
                    var muzzlePoint = GetPrivateField(wr, "muzzlePoint") as Transform;
                    var tracerPrefab = GetPrivateField(wr, "tracerPrefab") as LineRenderer;
                    var animator = GetPrivateField(wr, "animator") as Lake.Weapons.WeaponAnimator;
                    results.Add($"   muzzlePoint assigned: {muzzlePoint != null}");
                    results.Add($"   tracerPrefab assigned: {tracerPrefab != null}");
                    results.Add($"   animator assigned: {animator != null}");
                    results.Add($"   Data: {wr.Data?.displayName}");
                    results.Add($"   Data.pelletsPerShot: {wr.Data?.pelletsPerShot}");
                    results.Add($"   Data.damagePerPellet: {wr.Data?.damagePerPellet}");
                    results.Add($"   Data.hipPosition: {wr.Data?.hipPosition}");
                }
            }
            
            // 9. Test Raycast from AimPivot
            if (aimPivot != null)
            {
                var hits = Physics.RaycastAll(aimPivot.transform.position, aimPivot.transform.forward, 5f);
                results.Add($"9. Raycast from AimPivot (5m): {hits.Length} hits");
                foreach (var hit in hits)
                {
                    results.Add($"   Hit: {hit.collider.name} at {hit.distance}m");
                }
            }
            
            // 10. Check WeaponAnimator on weapon
            if (wc != null && wc.Current != null)
            {
                var anim = wc.Current.GetComponentInChildren<Lake.Weapons.WeaponAnimator>();
                results.Add($"10. WeaponAnimator on weapon: {anim != null}");
                if (anim != null)
                {
                    var weaponModel = GetPrivateField(anim, "weaponModel") as Transform;
                    results.Add($"    weaponModel assigned: {weaponModel != null} {(weaponModel != null ? weaponModel.name : "")}");
                }
            }
            
            // Log all results
            foreach (var r in results)
            {
                Debug.Log(r);
            }
            
            // Write to file
            System.IO.File.WriteAllLines("D:/Projects/Lake/PlayModeValidationResults.txt", results);
            
            // Exit Play Mode
            EditorApplication.ExitPlaymode();
        }
    }
}
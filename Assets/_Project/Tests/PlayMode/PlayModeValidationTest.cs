using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;

namespace Lake.Tests.PlayMode
{
    public class PlayModeValidationTest
    {
        [UnityTest]
        public IEnumerator ValidatePlayModeSetup()
        {
            // Load the scene
            yield return SceneManager.LoadSceneAsync("BodycamHorrorScene");
            
            // Find HorrorSceneBuilder and generate scene
            var builder = Object.FindFirstObjectByType<Lake.Scene.HorrorSceneBuilder>();
            Assert.IsNotNull(builder, "HorrorSceneBuilder not found in scene");
            
            builder.GenerateScene();
            yield return null; // Wait one frame for generation
            
            // 1. Check Player exists
            var player = GameObject.Find("BodycamPlayer(Clone)");
            if (player == null) player = GameObject.Find("BodycamPlayer");
            Assert.IsNotNull(player, "BodycamPlayer not found");
            Debug.Log($"[1] Player exists at {player.transform.position}");
            
            // 2. Check HUD exists
            var hud = GameObject.Find("Generated_HUD");
            Assert.IsNotNull(hud, "Generated_HUD not found");
            Debug.Log("[2] Generated_HUD exists");
            
            // 3. Check WeaponController
            var wc = Object.FindFirstObjectByType<Lake.Weapons.WeaponController>();
            Assert.IsNotNull(wc, "WeaponController not found");
            Debug.Log($"[3] WeaponController exists. AllWeapons: {wc.AllWeapons.Length}");
            for (int i = 0; i < wc.AllWeapons.Length; i++)
            {
                var w = wc.AllWeapons[i];
                Debug.Log($"   Slot {i}: {(w != null ? w.Data?.displayName + $" (ammo:{w.CurrentAmmo}/{w.ReserveAmmo})" : "EMPTY")}");
            }
            Debug.Log($"   Current: {(wc.Current != null ? wc.Current.Data?.displayName : "NONE")}");
            
            // 4. Check PlayerInteractor rayOrigin
            var interactor = Object.FindFirstObjectByType<Lake.Player.PlayerInteractor>();
            Assert.IsNotNull(interactor, "PlayerInteractor not found");
            var rayOrigin = GetPrivateField(interactor, "rayOrigin") as Transform;
            Debug.Log($"[4] PlayerInteractor rayOrigin assigned: {rayOrigin != null} {(rayOrigin != null ? rayOrigin.name : "NULL")}");
            
            // 5. Check AimPivot
            var aimPivot = Object.FindFirstObjectByType<Lake.Aim.AimPivot>();
            Assert.IsNotNull(aimPivot, "AimPivot not found");
            Debug.Log("[5] AimPivot exists");
            
            // 6. Check CameraEffects
            var camEffects = Object.FindFirstObjectByType<Lake.Player.CameraEffects>();
            Debug.Log($"[6] CameraEffects exists: {camEffects != null}");
            if (camEffects == null) Debug.LogWarning("CameraEffects NOT integrated!");
            
            // 7. Check BodyFollower
            var bodyFollower = Object.FindFirstObjectByType<Lake.Player.BodyFollower>();
            Debug.Log($"[7] BodyFollower exists: {bodyFollower != null}");
            if (bodyFollower == null) Debug.LogWarning("BodyFollower NOT integrated!");
            
            // 8. Check WeaponRuntime on equipped weapon
            if (wc != null && wc.Current != null)
            {
                var wr = wc.Current.GetComponent<Lake.Weapons.WeaponRuntime>();
                Assert.IsNotNull(wr, "WeaponRuntime missing on current weapon");
                Debug.Log($"[8] WeaponRuntime on current weapon: {wr != null}");
                
                var muzzlePoint = GetPrivateField(wr, "muzzlePoint") as Transform;
                var tracerPrefab = GetPrivateField(wr, "tracerPrefab") as LineRenderer;
                var animator = GetPrivateField(wr, "animator") as Lake.Weapons.WeaponAnimator;
                
                Debug.Log($"   muzzlePoint assigned: {muzzlePoint != null}");
                Debug.Log($"   tracerPrefab assigned: {tracerPrefab != null}");
                Debug.Log($"   animator assigned: {animator != null}");
                Debug.Log($"   Data: {wr.Data?.displayName}");
                Debug.Log($"   Data.pelletsPerShot: {wr.Data?.pelletsPerShot}");
                Debug.Log($"   Data.damagePerPellet: {wr.Data?.damagePerPellet}");
                Debug.Log($"   Data.hipPosition: {wr.Data?.hipPosition}");
                Debug.Log($"   Data.adsPosition: {wr.Data?.adsPosition}");
                Debug.Log($"   Data.shellByShellReload: {wr.Data?.shellByShellReload}");
                Debug.Log($"   Data.isAutomatic: {wr.Data?.isAutomatic}");
                
                // Verify shotgun damage
                if (wr.Data?.displayName == "Pump Shotgun")
                {
                    Assert.AreEqual(8, wr.Data.pelletsPerShot, "Shotgun pelletsPerShot should be 8");
                    Assert.AreEqual(10, wr.Data.damagePerPellet, "Shotgun damagePerPellet should be 10");
                    Assert.IsTrue(wr.Data.shellByShellReload, "Shotgun should be shellByShellReload");
                }
                
                // Verify rifle damage
                if (wr.Data?.displayName == "Assault Rifle")
                {
                    Assert.AreEqual(1, wr.Data.pelletsPerShot, "Rifle pelletsPerShot should be 1");
                    Assert.AreEqual(25, wr.Data.damagePerPellet, "Rifle damagePerPellet should be 25");
                    Assert.IsFalse(wr.Data.shellByShellReload, "Rifle should NOT be shellByShellReload");
                }
            }
            else
            {
                Debug.LogWarning("[8] No current weapon equipped - pickup not tested");
            }
            
            // 9. Test Raycast from AimPivot
            if (aimPivot != null)
            {
                var hits = Physics.RaycastAll(aimPivot.transform.position, aimPivot.transform.forward, 5f);
                Debug.Log($"[9] Raycast from AimPivot (5m): {hits.Length} hits");
                foreach (var hit in hits)
                {
                    Debug.Log($"   Hit: {hit.collider.name} at {hit.distance}m");
                }
            }
            
            // 10. Check WeaponAnimator on weapon
            if (wc != null && wc.Current != null)
            {
                var anim = wc.Current.GetComponentInChildren<Lake.Weapons.WeaponAnimator>();
                Debug.Log($"[10] WeaponAnimator on weapon: {anim != null}");
                if (anim != null)
                {
                    var weaponModel = GetPrivateField(anim, "weaponModel") as Transform;
                    Debug.Log($"    weaponModel assigned: {weaponModel != null} {(weaponModel != null ? weaponModel.name : "NULL")}");
                }
            }
            
            // 11. Test weapon pickup simulation (move player to pickup location)
            var pickup = GameObject.Find("Generated_Pickups");
            if (pickup != null)
            {
                var riflePickup = pickup.transform.Find("Rifle_Pickup");
                if (riflePickup != null)
                {
                    Debug.Log($"[11] Moving player to Rifle_Pickup at {riflePickup.position}");
                    player.transform.position = riflePickup.position + Vector3.up * 0.5f;
                    yield return new WaitForSeconds(0.5f);
                    
                    // Check if weapon was picked up
                    wc = Object.FindFirstObjectByType<Lake.Weapons.WeaponController>();
                    Debug.Log($"   After pickup - Current: {(wc.Current != null ? wc.Current.Data?.displayName : "NONE")}");
                }
            }
            
            yield return null;
        }
        
        static object GetPrivateField(object obj, string fieldName)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(obj);
        }
    }
}
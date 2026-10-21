using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx_Core;

namespace BepInEx_DemulShooter_Plugin
{
    class mCR_Player
    {
        /// <summary>
        /// Original game is only using relative mouse/gamepad movement for the crosshair
        /// Replacing it by DemulShooter absolute screen position for each player
        /// The crosshair is on a Screen Space Overlay canvas, and the game is using its position as screen point for the shooting raycast
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "handleMovingCrosshair")]
        class handleMovingCrosshair
        {
            static bool Prefix(CR_Player __instance)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return true;

                if (CR_ReadyCheckPanel.ArePlayersReady && !__instance.HasDied && __instance.Crosshair != null)
                {
                    Vector3 vAim = DemulShooter_Plugin.PluginControllers[__instance.Id].GetAimingPosition();
                    Transform CrosshairTransform = __instance.Crosshair.transform;
                    CrosshairTransform.position = new Vector3(vAim.x, vAim.y, CrosshairTransform.position.z);
                }
                return false;
            }
        }

        /// <summary>
        /// Same as original, but reading the trigger state from DemulShooter instead of the player's controller
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "handleAutoFire")]
        class handleAutoFire
        {
            static bool Prefix(CR_Player __instance, ref bool ___isAutoFireBlocked)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return true;

                CR_WeaponHolder WeaponHolder = __instance.WeaponHolder;
                if (WeaponHolder == null)
                    return false;

                bool IsHoldingFire = DemulShooter_Plugin.IsGameplayInputActive() && DemulShooter_Plugin.PluginControllers[__instance.Id].GetButton(PluginController.MyInputButtons.Trigger);
                if (IsHoldingFire && WeaponHolder.CurrentWeapon != null && WeaponHolder.CurrentWeapon.ShootMode == CR_Weapon.FireMode.AutoFire)
                {
                    if (___isAutoFireBlocked)
                    {
                        ___isAutoFireBlocked = false;
                        if (__instance.OnAutoFireBlockChanged != null)
                            __instance.OnAutoFireBlockChanged(false);
                    }
                    else
                    {
                        WeaponHolder.FireWeapon();
                    }
                }
                else
                {
                    WeaponHolder.ResetFiredState();
                }
                return false;
            }
        }

        /// <summary>
        /// Single shots, reload and weapon change with DemulShooter buttons
        /// Native events for these actions are blocked in mMP_PlayerInput
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "Update")]
        class Update
        {
            static void Prefix(CR_Player __instance)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return;

                PluginController Controller = DemulShooter_Plugin.PluginControllers[__instance.Id];
                bool bTrigger = Controller.GetButtonDown(PluginController.MyInputButtons.Trigger);
                bool bReload = Controller.GetButtonDown(PluginController.MyInputButtons.Reload);
                bool bAction = Controller.GetButtonDown(PluginController.MyInputButtons.Action);

                //Cutscenes, pause menu, etc... are disabling the Gameplay input map
                if (!DemulShooter_Plugin.IsGameplayInputActive())
                    return;

                if (bTrigger)
                    __instance.Fire();
                if (bReload)
                    __instance.Reload();
                if (bAction)
                    __instance.SwitchWeaponToNext();
            }
        }
    }
}

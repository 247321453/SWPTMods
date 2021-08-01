using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace FixPatch
{
    class BugPatch
    {
        public static FieldRef<ThirdPersonCharacter, Rigidbody> m_Rigidbody = FieldRefAccess<ThirdPersonCharacter, Rigidbody>("m_Rigidbody");
        public static FieldRef<ThirdPersonCharacter, Animator> m_Animator = FieldRefAccess<ThirdPersonCharacter, Animator>("m_Animator");
    }
    #region fix bugs


    [HarmonyPatch(typeof(UICombatParty), "Open")]
    class UICombatParty_Open_Patch
    {
        private static void Prefix(UICombatParty __instance)
        {
            // if (!__instance.gameObject.activeSelf)
            {
                //复活队友
                if (Global.code.companions != null && Global.code.companions.items != null)
                {
                    foreach (Transform transform in Global.code.companions.items)
                    {
                        if (transform)
                        {
                            var comp = transform.GetComponent<Companion>();
                            if (comp)
                            {
                                if (transform.gameObject.tag == "D")
                                {
                                    var character = comp.GetComponent<CharacterCustomization>();
                                    //复活
                                    character.Respawn();
                                    character.GetComponent<Rigidbody>().isKinematic = false;
                                    if (character.curCastingMagic && character.curCastingMagic.generatedHandfx)
                                    {
                                        Object.Destroy(character.curCastingMagic.generatedHandfx.gameObject);
                                    }
                                    character.curCastingMagic = null;
                                }
                                else
                                {
                                    //满血
                                    comp._ID.health = comp._ID.maxHealth;
                                    comp._ID.tempHealth = comp._ID.maxHealth;
                                    comp._ID.mana = comp._ID.maxMana;
                                    comp._ID.tempMana = comp._ID.maxMana;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //CustomizationSlider
    [HarmonyPatch(typeof(CustomizationSlider), "Start")]
    class CustomizationSlider_Start_Patch
    {
        private static bool Prefix(CustomizationSlider __instance)
        {
            if (BepInExPlugin.isFixBugs.Value)
            {
                if (Player.code != null && Player.code.customization != null && Player.code.customization.body != null && Player.code.customization.body.sharedMesh != null)
                {
                    return true;
                }
                __instance.Invoke("ResetEmotion", 1f);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleAirborneMovement")]
    class ThirdPersonCharacter_HandleAirborneMovement_Patch
    {

        private static bool Prefix(ThirdPersonCharacter __instance)
        {
            if (BepInExPlugin.isFixBugs.Value)
            {
                if (BugPatch.m_Rigidbody(__instance) == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(ThirdPersonCharacter), "UpdateAnimator")]
    class ThirdPersonCharacter_UpdateAnimator_Patch
    {

        private static bool Prefix(ThirdPersonCharacter __instance, Vector3 move)
        {
            if (BepInExPlugin.isFixBugs.Value)
            {
                if (BugPatch.m_Animator(__instance) == null)
                {
                    // m_Animator. = __instance.GetComponent<Animator>();
                    return false;
                }
            }
            return true;
        }
    }
    //HandleGroundedMovement(bool crouch, bool jump)
    [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleGroundedMovement")]
    class ThirdPersonCharacter_HandleGroundedMovement_Patch
    {

        private static bool Prefix(ThirdPersonCharacter __instance, bool crouch, bool jump)
        {
            if (BepInExPlugin.isFixBugs.Value)
            {
                if (BugPatch.m_Rigidbody(__instance) == null)
                {
                    //   __instance.GetComponent<Rigidbody>();
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(ThirdPersonCharacter), "OnAnimatorMove")]
    class ThirdPersonCharacter_OnAnimatorMove_Patch
    {
        private static bool Prefix(ThirdPersonCharacter __instance)
        {
            if (BepInExPlugin.isFixBugs.Value)
            {
                if (BugPatch.m_Rigidbody(__instance) == null)
                {
                    //  __instance.GetComponent<Rigidbody>();
                    return false;
                }
            }
            return true;
        }
    }
    #endregion
}

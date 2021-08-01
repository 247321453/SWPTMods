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

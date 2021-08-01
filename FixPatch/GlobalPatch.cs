using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static HarmonyLib.AccessTools;

namespace FixPatch
{
    enum CompanionCmd
    {
        FollowMe,
        Charge,
        GoThereAndStand,
    }
    class GlobalPatch
    {
        public static CompanionCmd sStatus = CompanionCmd.FollowMe;
        public static void Command(Monster component, bool charge, Transform move)
        {
            if (component)
            {
                component.charge = charge;
                component.movingToTarget = move;
            }
        }
        public static void Command(Companion component, bool charge, Transform move)
        {
            if (component)
            {
                component.charge = charge;
                component.movingToTarget = move;
            }
        }

        public static void Command2(Monster component, bool charge, Transform move)
        {
            if (component)
            {
                component.charge = charge;
                component.movingToTarget = move;
            }
        }
        public static void Command2(Companion component, bool charge, Vector3 move)
        {
            if (component)
            {
                component.charge = charge;
                if (move == Vector3.zero)
                {
                    component.movingToTarget = null;
                }
                else
                {
                    component.movingToTarget = new GameObject
                    {
                        transform = { position = new Vector3(move.x, move.y, move.z) }
                    }.transform;
                }
            }
        }
        public static void Command2(Monster component, bool charge, Vector3 move)
        {
            if (component)
            {
                component.charge = charge;
                if (move == Vector3.zero)
                {
                    component.movingToTarget = null;
                }
                else
                {
                    component.movingToTarget = new GameObject
                    {
                        transform = { position = new Vector3(move.x, move.y, move.z) }
                    }.transform;
                }
            }
        }

        public static bool IsInParty(Companion comp)
        {
            foreach (Transform transform in Global.code.playerCombatParty.items)
            {
                if (transform && comp == transform.GetComponent<Companion>())
                {
                    return true;
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Global), "CommandGoThereAndStand")]
    class Global_CommandGoThereAndStand_Patch
    {
        private static bool Prefix(Global __instance)
        {
            if (!BepInExPlugin.modEnabled.Value)
            {
                return true;
            }
            if (!BepInExPlugin.isFixAI.Value)
            {
                return true;
            }
            GlobalPatch.sStatus = CompanionCmd.GoThereAndStand;
            if (Player.code == null || Player.code.transform == null || RM.code == null || RM.code.sndStand == null)
            {
                return true;
            }
            RM.code.PlayOneShot(RM.code.sndStand);
            try
            {
                if (__instance.friendlies != null && __instance.friendlies.items != null)
                {
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandGoThereAndStand:transform:Monster");
                            var monster = transform.GetComponent<Monster>();
                            if (monster)
                            {
                                monster.target = null;
                            }
                            GlobalPatch.Command2(monster, false, Player.code.transform.position);
                        }
                    }
                }
                if (__instance.playerCombatParty != null && __instance.playerCombatParty.items != null)
                {
                    foreach (Transform transform in Global.code.playerCombatParty.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandGoThereAndStand:transform:Companion");
                            var comp = transform.GetComponent<Companion>();
                            if (comp)
                            {
                                comp.target = null;
                            }
                            GlobalPatch.Command2(comp, false, Player.code.transform.position);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BepInExPlugin.Error("CommandGoThereAndStand\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
            }
            __instance.uiCombat.AddPrompt(Localization.GetContent("GetThereAndStand"));
            return false;
        }

    }
    [HarmonyPatch(typeof(Global), "CommandCharge")]
    class Global_CommandCharge_Patch
    {
        private static bool Prefix(Global __instance)
        {
            if (!BepInExPlugin.modEnabled.Value)
            {
                return true;
            }
            if (!BepInExPlugin.isFixAI.Value)
            {
                return true;
            }
            GlobalPatch.sStatus = CompanionCmd.Charge;
            if (Player.code == null || Player.code.transform == null || RM.code == null || RM.code.sndCharge == null)
            {
                return true;
            }
            RM.code.PlayOneShot(RM.code.sndCharge);
            try
            {
                if (__instance.friendlies != null && __instance.friendlies.items != null)
                {
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandCharge:transform:Monster");
                            GlobalPatch.Command(transform.GetComponent<Monster>(), true, null);
                        }
                    }
                }
                if (__instance.playerCombatParty != null && __instance.playerCombatParty.items != null)
                {
                    foreach (Transform transform in Global.code.playerCombatParty.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandCharge:transform:Companion");
                            GlobalPatch.Command(transform.GetComponent<Companion>(), true, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BepInExPlugin.Error("CommandCharge\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
            }
            __instance.uiCombat.AddPrompt(Localization.GetContent("Charge", Array.Empty<object>()));
            return false;
        }
    }

    [HarmonyPatch(typeof(Global), "CommandFollowMe")]
    class Global_CommandFollowMe_Patch
    {
        private static bool Prefix(Global __instance)
        {
            if (!BepInExPlugin.modEnabled.Value)
            {
                return true;
            }
            if (!BepInExPlugin.isFixAI.Value)
            {
                return true;
            }
            GlobalPatch.sStatus = CompanionCmd.FollowMe;
            if (Player.code == null || Player.code.transform == null || RM.code == null || RM.code.sndFollow == null)
            {
                return true;
            }
            RM.code.PlayOneShot(RM.code.sndFollow);
            try
            {
                if (__instance.friendlies != null && __instance.friendlies.items != null)
                {
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandFollowMe:transform:Monster");
                            var monster = transform.GetComponent<Monster>();
                            if (monster) {
                                monster.target = null;
                            }
                            GlobalPatch.Command(monster, false, Player.code.transform);
                        }
                    }
                }
                if (__instance.playerCombatParty != null && __instance.playerCombatParty.items != null)
                {
                    foreach (Transform transform in Global.code.playerCombatParty.items)
                    {
                        if (transform)
                        {
                            BepInExPlugin.Debug("CommandFollowMe:transform:Companion");
                            var comp = transform.GetComponent<Companion>();
                            if (comp)
                            {
                                comp.target = null;
                            }
                            GlobalPatch.Command(comp, false, Player.code.transform);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BepInExPlugin.Error("CommandFollowMe\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
            }
            __instance.uiCombat.AddPrompt(Localization.GetContent("Follow"));
            return false;
        }
    }

}

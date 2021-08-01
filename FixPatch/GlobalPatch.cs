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
    class GlobalPatch {
        public static CompanionCmd sStatus = CompanionCmd.FollowMe;
    }
    /*
    #region AI
    [HarmonyPatch(typeof(ID), "AddHealth")]
    class ID_AddHealth_Patch
    {

        private static void Postfix(ID __instance, float pt, Transform source)
        {
            if (isDebug.Value)
            {
                if (__instance.GetComponent<Monster>())
                {
                    Debug(__instance.name + " AddHealth " + pt);
                }
            }
            if (!BepInExPlugin.modEnabled.Value)
            {
                return;
            }
            if (!BepInExPlugin.isFixAI.Value)
            {
                return;
            }
            if (!__instance.canaddhealth)
            {
                return;
            }
            if (__instance.damageSource && __instance.GetComponent<Monster>() == null)
            {
                    if (__instance.damageSource.GetComponent<ID>() == Player.code._ID)
                    {
                        return;
                    }
                    var customization = __instance.GetComponent<CharacterCustomization>();
                    if (!customization) return;
                    var companion = customization.GetComponent<Companion>();
                    if (!companion) return;

                    //随从受伤
                    //if (companion.charge)
                    //{
                    //    companion.target = __instance.damageSource;
                    //    BepInExPlugin.Debug(companion.name + " change target", true);
                    //}
                    if (__instance.health > 0 && (__instance.health / __instance.maxHealth) < 0.1f)
                    {
                        //TODO 逃跑
                        //随从受伤
                        Global.code.uiCombat.AddRollHint(companion.name + ":Help me!", Color.red);
                    }
                //else if(pt < 0){
                //    Global.code.uiCombat.AddRollHint(__instance.name + " add health :"+pt, Color.red);
                //}
            }
        }

    }
    */
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
            BepInExPlugin.Debug("CommandGoThereAndStand:0");
            GlobalPatch.sStatus = CompanionCmd.GoThereAndStand;
            if (Player.code == null || Player.code.transform == null || __instance.friendlies == null || __instance.friendlies.items == null || RM.code == null || RM.code.sndStand == null)
            {
                return true;
            }
            BepInExPlugin.Debug("CommandGoThereAndStand:1");
            RM.code.PlayOneShot(RM.code.sndStand);
            BepInExPlugin.Debug("CommandGoThereAndStand:1");
            try
            {
                foreach (Transform transform in __instance.friendlies.items)
                {
                    if (transform && transform != Player.code.transform)
                    {
                        BepInExPlugin.Debug("CommandGoThereAndStand:transform");
                        var component = transform.GetComponent<Companion>();
                        var component2 = transform.GetComponent<Monster>();
                        if (component)
                        {
                            BepInExPlugin.Debug("CommandGoThereAndStand:Companion:" + component.name);
                            component.target = null;
                            component.movingToTarget = new GameObject
                            {
                                transform = { position = new Vector3()
                                    {
                                        x = Player.code.transform.position.x,
                                        y = Player.code.transform.position.y,
                                        z = Player.code.transform.position.z
                                    }
                                }
                            }.transform;
                            component.charge = false;
                            var charactor = component.GetComponent<CharacterCustomization>();
                            if (charactor && !CharacterCustomizationUtil.WeaponIsBow(charactor))
                            {
                                charactor.Block();
                            }
                        }
                        if (component2)
                        {
                            BepInExPlugin.Debug("CommandGoThereAndStand:Monster:" + component.name);
                            component2.target = null;
                            component2.movingToTarget = new GameObject
                            {
                                transform = {   position = new Vector3()
                                    {
                                        x = Player.code.transform.position.x,
                                        y = Player.code.transform.position.y,
                                        z = Player.code.transform.position.z
                                    }
                                }
                            }.transform;
                            component2.charge = false;
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
            if (BepInExPlugin.modEnabled.Value && BepInExPlugin.isFixAI.Value)
            {
               GlobalPatch.sStatus = CompanionCmd.Charge;
                RM.code.PlayOneShot(RM.code.sndCharge);
                foreach (Transform transform in __instance.friendlies.items)
                {
                    if (transform)
                    {
                        Companion component = transform.GetComponent<Companion>();
                        Monster component2 = transform.GetComponent<Monster>();
                        if (component)
                        {
                            component.movingToTarget = null;
                            component.charge = true;
                        }
                        if (component2)
                        {
                            component2.movingToTarget = null;
                            component2.charge = true;
                        }
                    }
                }
                __instance.uiCombat.AddPrompt(Localization.GetContent("Charge", Array.Empty<object>()));
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Global), "CommandFollowMe")]
    class Global_CommandFollowMe_Patch
    {
        private static void Prefix(Global __instance)
        {
            if (!BepInExPlugin.modEnabled.Value)
            {
                return;
            }
            if (!BepInExPlugin.isFixAI.Value)
            {
                return;
            }
            GlobalPatch.sStatus = CompanionCmd.FollowMe;
            if (__instance.friendlies == null)
            {
                return;
            }
            foreach (Transform transform in __instance.friendlies.items)
            {
                if (transform && transform != Player.code.transform)
                {
                    Companion component = transform.GetComponent<Companion>();
                    Monster component2 = transform.GetComponent<Monster>();
                    if (component)
                    {
                        component.target = null;
                    }
                    if (component2)
                    {
                        component2.target = null;
                    }
                }
            }
        }
    }

}

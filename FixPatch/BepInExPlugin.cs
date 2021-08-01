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
    [BepInPlugin("caicai.FixPatch", "Fix Patch", "0.1.4")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> isFixAffixes;

        public static ConfigEntry<bool> isFixBugs;

        public static ConfigEntry<bool> isFixAI;

        public static ConfigEntry<bool> isFixPullBow;

        public static ConfigEntry<bool> isOnlyPlayerAndCompanion;

        public static ConfigEntry<float> PullBowDamageRate;

        public static ConfigEntry<int> PullBowMaxTime;

        private static float BOW_ATTACK_LIMIT = 20f;
        public static ConfigEntry<string> NoWeaponMsg;


        public static void Debug(string str = "", bool pref = true)
        {
            if (BepInExPlugin.isDebug.Value)
            {
                UnityEngine.Debug.Log((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
            }
        }
        public static void Error(string str = "", bool pref = true)
        {
            UnityEngine.Debug.LogError((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
        }
        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 29, "Nexus mod ID for updates");

            //   BepInExPlugin.elementDamageEnable = base.Config.Bind<bool>("Options", "ElementDamageEnable", false, "Enable elemental damage of weapon");
            //    BepInExPlugin.elementDamageRate = base.Config.Bind<float>("Options", "ElementDamageRate", 0.1f, "magic attributes can increase the elemental damage of weapons");
            BepInExPlugin.isFixAffixes = base.Config.Bind<bool>("Options", "IsFixAffixes", true, "fix weapon and armor 's affixes.");
            BepInExPlugin.isFixBugs = base.Config.Bind<bool>("Options", "IsFixBugs", false, "fix some bugs.");
            BepInExPlugin.isFixAI = base.Config.Bind<bool>("Options", "isFixAI", true, "fix compation's ai.");
            BepInExPlugin.isFixPullBow = base.Config.Bind<bool>("Options", "isFixPullBow", true, "fix compation's ai.");
            BepInExPlugin.isOnlyPlayerAndCompanion = base.Config.Bind<bool>("Options", "IsOnlyPlayerAndCompanion", true, "only player and companion enable pull bow append damage and element damage.");
            BepInExPlugin.PullBowDamageRate = base.Config.Bind<float>("Options", "PullBowDamageRate", 0.1f, "Bow accumulate increases damage rate.Default:10%/0.1s");
            BepInExPlugin.PullBowMaxTime = base.Config.Bind<int>("Options", "PullBowMaxTime", 3, "pull bow max time, default is 3s");
            BepInExPlugin.NoWeaponMsg = base.Config.Bind<string>("Options", "NoWeaponMsg", ": I need a weapon or arrows.", "message");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
        }

        [HarmonyPatch(typeof(UILoading), "OpenLoading", new Type[] { typeof(Transform) })]
        private static class UILoading_OpenLoading_Patch
        {
            private static void Prefix(UILoading __instance, Transform location)
            {
                Debug("OpenLoading#locationType=" + location.GetComponent<Location>().locationType);
            }
        }
        [HarmonyPatch(typeof(Companion), "CSFast")]
        private static class Companion_CSFast_Patch
        {
            private static bool Prefix(Companion __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return true;
                }
                if (__instance.gameObject.tag == "D")
                {
                    return true;
                }
                if (!GlobalPatch.IsInParty(__instance))
                {
                    //不在队伍
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Companion), "CS5")]
        private static class Companion_CS5_Patch
        {
            private static bool Prefix(Companion __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return true;
                }
                if (__instance.gameObject.tag == "D")
                {
                    return true;
                }
                if (!GlobalPatch.IsInParty(__instance))
                {
                    //不在队伍
                    return false;
                }
                return true;
            }
        }
        //SetDestination
        [HarmonyPatch(typeof(Companion), "CS")]
        private static class Companion_CS_Patch
        {

            //EnemyInAttackDist
            private static bool Prefix(Companion __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return true;
                }
                if (__instance.gameObject.tag == "D")
                {
                    return true;
                }
                if (Global.code.curlocation && Global.code.curlocation.locationType == LocationType.home)
                {
                    //在家里
                    return true;
                }
                if (!GlobalPatch.IsInParty(__instance))
                {
                    //不在队伍
                    return false;
                }
                try
                {
                    var character = __instance.customization;
                    __instance.attackDist = CompanionUtil.GetAttackDist(__instance);
                    if (__instance._ID.isFriendly)
                    {
                        __instance.target = Utility.GetNearestObject(Global.code.enemies.items, __instance.transform);
                    }
                    else
                    {
                        __instance.target = Utility.GetNearestObject(Global.code.friendlies.items, __instance.transform);
                    }
                    if (__instance.target)
                    {
                        if (__instance._ID.health <= 0f)
                        {
                            return false;
                        }
                        if (!__instance.customization.weaponInHand)
                        {
                            //手里没武器，优先近战，然后是弓箭
                            if (!CharacterCustomizationUtil.ChangeMeleeWeapon(character, -1, false))
                            {
                                CharacterCustomizationUtil.ChangeBowWeapon(character);
                            }
                        }
                        else if (__instance.customization.anim.runtimeAnimatorController == RM.code.unarmedController)
                        {
                            //优先近战，然后是弓箭
                            if (!CharacterCustomizationUtil.ChangeMeleeWeapon(character))
                            {
                                CharacterCustomizationUtil.ChangeBowWeapon(character);
                            }
                        }
                        if (__instance.target && __instance.target.tag != "D")
                        {
                            __instance.curEnemyDist = Vector3.Distance(__instance.target.position, __instance.myTransform.position);

                            if (__instance.curEnemyDist > __instance.attackDist && __instance.curEnemyDist < BOW_ATTACK_LIMIT && character && character.storage.GetItemCount("Arrow") > 0)
                            {
                                //释放过程不处理
                                if (character.curCastingMagic == null)
                                {
                                    //大于攻击范围，但是有弓箭
                                    int bowIndex = CharacterCustomizationUtil.ChangeBowWeapon(character);
                                    if (bowIndex > 0)
                                    {
                                        Debug(character.characterName + " change weapon to bow:" + bowIndex + ", distance=" + __instance.curEnemyDist);
                                    }
                                }
                            }

                            if (__instance.curEnemyDist <= __instance.attackDist || (CharacterCustomizationUtil.WeaponIsBow(character) && __instance.curEnemyDist < BOW_ATTACK_LIMIT))
                            {
                                bool skipAttack = false;
                                if (CharacterCustomizationUtil.WeaponIsBow(character))
                                {
                                    if (GlobalPatch.sStatus == CompanionCmd.GoThereAndStand)
                                    {
                                        if (__instance.curEnemyDist <= CharacterCustomizationUtil.MIN_MELEE_ATTACK_DISTANCE)
                                        {
                                            Debug(character.characterName + " change bow to other weapon, curEnemyDist=" + __instance.curEnemyDist);
                                            //距离太近了，切换武器
                                            if (CharacterCustomizationUtil.ChangeMeleeWeapon(character))
                                            {
                                                character.Block();
                                            }
                                        }
                                        //else if (__instance.curEnemyDist <= CharacterCustomizationUtil.MIN_BOW_ATTACK_DISTANCE)
                                        // {
                                        //     //距离太近了，随机移动
                                        //     skipAttack = true;
                                        //     CompanionUtil.RandomMove(__instance, Player.code.transform.position);
                                        //     Debug(character.characterName + " random move, curEnemyDist=" + __instance.curEnemyDist);
                                        //  }
                                    }
                                    else
                                    {
                                        if (__instance.curEnemyDist <= CharacterCustomizationUtil.MIN_MELEE_ATTACK_DISTANCE && GlobalPatch.sStatus == CompanionCmd.Charge)
                                        {
                                            //太近了，并且是冲锋模式
                                            if (CharacterCustomizationUtil.ChangeMeleeWeapon(character))
                                            {
                                                character.Block();
                                            }
                                        }
                                        else if (__instance.curEnemyDist <= CharacterCustomizationUtil.MIN_BOW_ATTACK_DISTANCE)
                                        {
                                            //距离太近了，后退
                                            skipAttack = true;
                                            character.Unblock();
                                            CharacterCustomizationUtil.CancelMagicAndPullBow(character);
                                            CompanionUtil.Backoff(__instance);
                                            Debug(character.characterName + " back off, curEnemyDist=" + __instance.curEnemyDist);
                                        }
                                    }
                                }
                                if (!skipAttack)
                                {
                                    //在攻击范围
                                    character.Unblock();
                                    Debug(character.characterName + " attck enemy, curEnemyDist=" + __instance.curEnemyDist);
                                    CompanionUtil.EnemyInAttackDist(__instance);
                                    return false;
                                }
                            }
                        }
                        if (!__instance.charge)
                        {
                            //不在攻击范围
                            //非冲锋模式
                            if (__instance.curEnemyDist >= __instance.attackDist * 1.5f)
                            {
                                //  BepInExPlugin.Debug("status is GoThereAndStand, curEnemyDist=" + __instance.curEnemyDist + ", attackDist=" + __instance.attackDist, true);
                                //敌人靠近了，稍微靠近
                                Debug(character.characterName + " far enemy by no charge, status=" + GlobalPatch.sStatus);
                                CompanionUtil.EnemyFar(__instance);
                                if (__instance.movingToTarget)
                                {
                                    if (UnityEngine.Random.Range(0, 100) < 25)
                                    {
                                        if (GlobalPatch.sStatus != CompanionCmd.FollowMe && !CharacterCustomizationUtil.WeaponIsBow(character))
                                        {
                                            //取消防御
                                            character.Unblock();
                                        }
                                        //随机移动
                                        Debug(character.characterName + " random move, status=" + GlobalPatch.sStatus);
                                        CompanionUtil.RandomMove(__instance, __instance.movingToTarget.position);
                                    }
                                }
                                if (GlobalPatch.sStatus != CompanionCmd.FollowMe && !CharacterCustomizationUtil.WeaponIsBow(character))
                                {
                                    character.Block();
                                }
                                return false;
                            }
                        }
                        if (__instance.curEnemyDist > BOW_ATTACK_LIMIT)
                        {
                            //靠近敌人
                            Debug(character.characterName + " far enemy 2");
                            CompanionUtil.EnemyFar(__instance);
                        }
                        else
                        {
                            //远离敌人
                            Debug(character.characterName + " close enemy");
                            CompanionUtil.EnemyClose(__instance);
                        }
                        if (__instance.target.tag == "D")
                        {
                            //目标死亡
                            __instance.target = null;
                        }
                        return false;
                    }
                    else
                    {
                        //没有目标
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Error("CS\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
                    return true;
                }
            }
        }
    }
}

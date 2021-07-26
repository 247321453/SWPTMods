﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SharedExperience
{
    [BepInPlugin("caicai.SharedExperience", "Shared Experience", "0.0.4")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> isShared;

        public static ConfigEntry<KeyCode> hotKey;
        /// <summary>
        /// 辅助击杀经验倍率
        /// </summary>
        public static ConfigEntry<float> partyExpRate;

        public static ConfigEntry<string> sharedEnableStr;
        public static ConfigEntry<string> sharedDisableStr;

        public static void Dbgl(string str = "", bool pref = true)
        {
            bool value = BepInExPlugin.isDebug.Value;
            if (value)
            {
                Debug.Log((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
            }
        }

        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            BepInExPlugin.hotKey = base.Config.Bind<KeyCode>("Options", "HotKey", KeyCode.S, "left Ctrl + hotkey to toggle shared.");
            BepInExPlugin.isShared = base.Config.Bind<bool>("Options", "IsShared", true, "if `IsShared = true` then killer's exp is (originalExp * (1 - partyExpRate * partyCount))");
            BepInExPlugin.partyExpRate = base.Config.Bind<float>("Options", "PartyExpRate", 0.1f, "party members add exp rate");
            BepInExPlugin.sharedEnableStr = base.Config.Bind<string>("Options", "EnableTip", "Shared EXP Open", "isShared=true");
            BepInExPlugin.sharedDisableStr = base.Config.Bind<string>("Options", "DisableTip", "Shared EXP Close", "isShared=false");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
        }

        private void Update()
        {
            var key = new BepInEx.Configuration.KeyboardShortcut(BepInExPlugin.hotKey.Value, KeyCode.LeftControl);
            if (key.IsDown())
            {
                BepInExPlugin.isShared.Value = !BepInExPlugin.isShared.Value;
                BepInExPlugin.Dbgl(string.Format("set exp share: {0}", BepInExPlugin.isShared.Value), true);
                if (BepInExPlugin.isShared.Value)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.sharedEnableStr.Value, Color.green);
                }
                else
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.sharedDisableStr.Value, Color.green);
                }
            }
        }

        #region old
        // 在所有插件全部启动完成后会调用Start()方法，执行顺序在Awake()后面；
        //    private void Start()
        //    {
        //        //Debug.Log("这里是Start()方法中的内容!");
        //    }
        // 插件启动后会一直循环执行Update()方法，可用于监听事件或判断键盘按键，执行顺序在Start()后面
        //  private void Update()
        //   {
        // var key = new BepInEx.Configuration.KeyboardShortcut(KeyCode.F9);
        // if (key.IsDown())
        //{
        //     Debug.Log("这里是Updatet()方法中的内容，你看到这条消息是因为你按下了F9");
        // }
        //  }
        // 在插件关闭时会调用OnDestroy()方法
        // private void OnDestroy()
        //{
        //     //Debug.Log("当你看到这条消息时，就表示我已经被关闭一次了!");
        // }
        #endregion
        [HarmonyPatch(typeof(ID), "AddExp")]
        private static class ID_AddExp_Patch
        {
            private static void Prefix(ID __instance, int exp)
            {
                Dbgl(__instance.name + ".AddExp(" + exp + ")");
            }
        }


        [HarmonyPatch(typeof(Monster), "Die")]
        private static class Monster_Die_Patch
        {
            private static void Prefix(Monster __instance)
            {
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
                {
                    if (__instance.gameObject.tag == "D")
                    {
                        //已经死亡
                        return;
                    }
                    //攻击者不为空
                    if (__instance._ID.damageSource)
                    {
                        //经验计算
                        var killer = __instance._ID.damageSource.GetComponent<ID>();
                        int num = 50;
                        num += 20 * __instance._ID.level;
                        num *= (int)(__instance.enemyRarity + 1);
                        float rate = BepInExPlugin.partyExpRate.Value;
                        if (rate >= 1)
                        {
                            rate = 0.99f;
                        }
                        //经验倍率
                        int exp = (int)(num * rate);
                        if (exp > 0)
                        {
                            var player_id = Player.code._ID;
                            Dbgl(killer.name + " kill monster, is player=" + (killer == player_id) + ", all exp=" + num + ", share exp=" + exp);
                            //killer.AddExp(num);
                            foreach (Transform transform in Global.code.playerCombatParty.items)
                            {
                                if (transform)
                                {
                                    var comp = transform.GetComponent<ID>();
                                    if (comp && comp != killer && comp.health > 0)
                                    {
                                        Dbgl("team kill monster, add exp " + exp + " to " + comp.name);
                                        if (exp > num)
                                        {
                                            //留给击杀者
                                            break;
                                        }
                                        else
                                        {
                                            comp.AddExp(exp);
                                            num -= exp;
                                            if (num <= 0)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (killer != player_id && num > 0)
                            {
                                if (player_id.health > 0)
                                {
                                    Dbgl("team kill monster, player add exp:" + exp);
                                    if (exp < num)
                                    {
                                        player_id.AddExp(exp);
                                        num -= exp;
                                    }
                                }
                                else
                                {
                                    Dbgl("team kill monster, player is dead, ignore exp:" + exp);
                                }
                            }
                        }
                        if (BepInExPlugin.isShared.Value)
                        {
                            if (num <= 0)
                            {
                                //分太多给队友了
                                num = 1;
                            }

                            killer.AddExp(num);
                            __instance._ID.damageSource = null;
                            Dbgl("killer:"+killer.name + " add exp " + num);

                            #region orgi code
                            Global.code.uiAchievements.AddPoint(AchievementType.totalkills, 1);
                            if (Player.code.customization.weaponInHand)
                            {
                                switch (Player.code.customization.weaponInHand.GetComponent<Weapon>().weaponType)
                                {
                                    case WeaponType.onehand:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithswords, 1);
                                        break;
                                    case WeaponType.twohand:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithswords, 1);
                                        break;
                                    case WeaponType.spear:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithspears, 1);
                                        break;
                                    case WeaponType.onehandaxe:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithaxe, 1);
                                        break;
                                    case WeaponType.bow:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithbows, 1);
                                        break;
                                    case WeaponType.dagger:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithdaggers, 1);
                                        break;
                                    case WeaponType.onehandhammer:
                                        Global.code.uiAchievements.AddPoint(AchievementType.killwithhammers, 1);
                                        break;
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
        }
    }
}
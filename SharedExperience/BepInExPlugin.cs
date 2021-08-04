using BepInEx;
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
    [BepInPlugin("caicai.SharedExperience", "Shared Experience", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> keepKillerExp;

        public static ConfigEntry<KeyCode> hotKey;
        /// <summary>
        /// 辅助击杀经验倍率
        /// </summary>
        public static ConfigEntry<float> partyExpRate;

        public static ConfigEntry<string> keepKillerExpEnableStr;
        public static ConfigEntry<string> keepKillerExpDisableStr;

        public static void Debug(string str = "", bool pref = true)
        {
            if (BepInExPlugin.isDebug.Value)
            {
                UnityEngine.Debug.Log((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
            }
        }

        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 23, "Nexus mod ID for updates");
            BepInExPlugin.hotKey = base.Config.Bind<KeyCode>("Options", "HotKey", KeyCode.S, "left Ctrl + hotkey to toggle shared.");
            BepInExPlugin.keepKillerExp = base.Config.Bind<bool>("Options", "KeepKillerExp", true, "If it is false, the teammate’s experience is obtained from the killing experience. Otherwise, the teammate’s experience is an additional increase and does not affect the killer’s experience. ");
            BepInExPlugin.partyExpRate = base.Config.Bind<float>("Options", "PartyExpRate", 0.1f, "party members add exp rate.(0-1.0)");
            BepInExPlugin.keepKillerExpEnableStr = base.Config.Bind<string>("Options", "KeepKillerExpEnableStr", "Keep killer exp Open", "KeepKillerExp=true");
            BepInExPlugin.keepKillerExpDisableStr = base.Config.Bind<string>("Options", "KeepKillerExpDisableStr", "Keep killer exp Close", "KeepKillerExp=false");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
        }

        private void Update()
        {
            var key = new BepInEx.Configuration.KeyboardShortcut(BepInExPlugin.hotKey.Value, KeyCode.LeftControl);
            if (key.IsDown())
            {
                BepInExPlugin.keepKillerExp.Value = !BepInExPlugin.keepKillerExp.Value;
                BepInExPlugin.Debug(string.Format("set exp share: {0}", BepInExPlugin.keepKillerExp.Value), true);
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    if (BepInExPlugin.keepKillerExp.Value)
                    {
                        Global.code.uiCombat.AddRollHint(BepInExPlugin.keepKillerExpEnableStr.Value, Color.green);
                    }
                    else
                    {
                        Global.code.uiCombat.AddRollHint(BepInExPlugin.keepKillerExpDisableStr.Value, Color.green);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Monster), nameof(Monster.Die))]
        private static class Monster_Die_Patch
        {
            private static void Prefix(Monster __instance)
            {
                if (BepInExPlugin.modEnabled.Value)
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
                        if (killer == null) {
                            return;
                        }
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
                            Debug(killer.name + " kill monster, is player=" + (killer == player_id) + ", all exp=" + num + ", share exp=" + exp);
                            //killer.AddExp(num);
                            foreach (Transform transform in Global.code.playerCombatParty.items)
                            {
                                if (transform)
                                {
                                    var comp = transform.GetComponent<ID>();
                                    if (comp && comp != killer && comp.health > 0)
                                    {
                                        Debug("team kill monster, add exp " + exp + " to " + comp.name);
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
                                    Debug("team kill monster, player add exp:" + exp);
                                    if (exp < num)
                                    {
                                        player_id.AddExp(exp);
                                        num -= exp;
                                    }
                                }
                                else
                                {
                                    Debug("team kill monster, player is dead, ignore exp:" + exp);
                                }
                            }
                        }
                        if (!BepInExPlugin.keepKillerExp.Value)
                        {
                            if (num <= 0)
                            {
                                //分太多给队友了
                                num = 1;
                            }

                            killer.AddExp(num);
                            __instance._ID.damageSource = null;
                            Debug("killer:"+killer.name + " add exp " + num);

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

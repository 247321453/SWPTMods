using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AutoPickup
{
    [BepInPlugin("caicai.AutoPickup", "Auto Pickup", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> hotKey;
        /// <summary>
        /// 仅拾取黄金和水晶
        /// </summary>
        public static ConfigEntry<bool> isOnlyGoldAndCrystals;

        public static ConfigEntry<int> filterRarity;

        public static ConfigEntry<KeyCode> filterHotKey;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> onlyGoldAndCrystalsEnableStr;
        public static ConfigEntry<string> onlyGoldAndCrystalsDisableStr;


        public static ConfigEntry<string> FilterWhilteStr;
        public static ConfigEntry<string> FilterBlueStr;
        public static ConfigEntry<string> FilterYellowStr;
        public static ConfigEntry<string> FilterGreenStr;
        public static ConfigEntry<string> FilterRedStr;
        public static ConfigEntry<string> FilterCyanStr;

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
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 25, "Nexus mod ID for updates");
            BepInExPlugin.hotKey = base.Config.Bind<KeyCode>("Options", "HotKey", KeyCode.A, "left Ctrl + hotkey to toggle pick up all items.");
            BepInExPlugin.filterRarity = base.Config.Bind<int>("Options", "FilterRarity", 2, "Default: Blue, Common:1, Rare:2, Superior:3 Unique:4 Legendary:5 Ultra Legendary:6   [hotKey:left ctrl+1~6]");
            BepInExPlugin.isOnlyGoldAndCrystals = base.Config.Bind<bool>("Options", "isOnlyGoldAndCrystals", true, "only auto pickup golds and crystals.");
            BepInExPlugin.onlyGoldAndCrystalsEnableStr = base.Config.Bind<string>("Options", "EnableTip", "Pickup Gold and Crustals", "isOnlyGoldAndCrystals=true");
            BepInExPlugin.onlyGoldAndCrystalsDisableStr = base.Config.Bind<string>("Options", "DisableTip", "Pickup All Items", "isOnlyGoldAndCrystals=false");

            BepInExPlugin.FilterWhilteStr = base.Config.Bind<string>("Language", "FilterWhilteStr", "Filter Common", "Filter:White");
            BepInExPlugin.FilterBlueStr = base.Config.Bind<string>("Language", "FilterBlueStr", "Filter Rare", "Filter:Blue");
            BepInExPlugin.FilterYellowStr = base.Config.Bind<string>("Language", "FilterYellowStr", "Filter Superior", "Filter:Yellow");
            BepInExPlugin.FilterGreenStr = base.Config.Bind<string>("Language", "FilterGreenStr", "Filter Unique", "Filter:Green");
            BepInExPlugin.FilterRedStr = base.Config.Bind<string>("Language", "FilterRedStr", "Filter Legendary", "Filter:Red");
            BepInExPlugin.FilterCyanStr = base.Config.Bind<string>("Language", "FilterCyanStr", "Filter Ultra Legendary", "Filter:Cyan");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
        }

        private void Update()
        {
            var key = new BepInEx.Configuration.KeyboardShortcut(BepInExPlugin.hotKey.Value, KeyCode.LeftControl);
            if (key.IsDown())
            {
                BepInExPlugin.isOnlyGoldAndCrystals.Value = !BepInExPlugin.isOnlyGoldAndCrystals.Value;
                BepInExPlugin.Debug(string.Format("set isOnlyGoldAndCrystals: {0}", BepInExPlugin.isOnlyGoldAndCrystals.Value), true);
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    if (BepInExPlugin.isOnlyGoldAndCrystals.Value)
                    {
                        Global.code.uiCombat.AddRollHint(BepInExPlugin.onlyGoldAndCrystalsEnableStr.Value, Color.green);
                    }
                    else
                    {
                        Global.code.uiCombat.AddRollHint(BepInExPlugin.onlyGoldAndCrystalsDisableStr.Value, Color.green);
                    }
                }
            } else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 1;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterWhilteStr.Value, Color.white);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 2;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterWhilteStr.Value, Color.blue);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 3;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterYellowStr.Value, Color.yellow);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 4;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterGreenStr.Value, Color.green);
                }
            }
            else if(new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 5;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterRedStr.Value, Color.red);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 6;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint(BepInExPlugin.FilterCyanStr.Value, Color.cyan);
                }
            }
        }


        [HarmonyPatch(typeof(Item), "Drop")]
        private static class Item_Drop_Patch
        {
            private static bool Prefix(Item __instance)
            {
                if (BepInExPlugin.modEnabled.Value)
                {
                    if (__instance.itemName == "Crystals" || __instance.itemName == "Gold")
                    {
                        if (Global.code.AddItemToPlayerStorage(__instance.transform, true))
                        {
                            Debug(__instance.itemName + " auto pick up");
                            __instance.Pickup();
                            return false;
                        }
                    }
                    else if (!BepInExPlugin.isOnlyGoldAndCrystals.Value && ((int)__instance.rarity >= BepInExPlugin.filterRarity.Value || isBook(__instance.itemName)))
                    {
                        if (Global.code.AddItemToPlayerStorage(__instance.transform, true))
                        {
                            Debug(__instance.itemName + " auto pick up, ratity=" + __instance.rarity);
                            __instance.Pickup();
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        private static bool isBook(string name)
        {
            return "Experience Book" == name
                || "Experience Scroll" == name
                || "Golden Key" == name
                || "Key" == name;
        }
    }
}

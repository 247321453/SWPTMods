using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AutoPickup
{
    [BepInPlugin("caicai.AutoPickup", "Auto Pickup", "0.0.5")]
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
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 25, "Nexus mod ID for updates");
            BepInExPlugin.hotKey = base.Config.Bind<KeyCode>("Options", "HotKey", KeyCode.A, "left Ctrl + hotkey to toggle pick up all items.");
            BepInExPlugin.filterRarity = base.Config.Bind<int>("Options", "FilterRarity", 2, "Default: Blue, Whilte:1, Blue:2, Yellow:3 Green:4 Red:5 Cyan:6   [hotKey:left ctrl+1~6]");
            BepInExPlugin.isOnlyGoldAndCrystals = base.Config.Bind<bool>("Options", "isOnlyGoldAndCrystals", true, "only auto pickup golds and crystals.");
            BepInExPlugin.onlyGoldAndCrystalsEnableStr = base.Config.Bind<string>("Options", "EnableTip", "Pickup Gold and Crustals", "isOnlyGoldAndCrystals=true");
            BepInExPlugin.onlyGoldAndCrystalsDisableStr = base.Config.Bind<string>("Options", "DisableTip", "Pickup All Items", "isOnlyGoldAndCrystals=false");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
        }

        private void Update()
        {
            var key = new BepInEx.Configuration.KeyboardShortcut(BepInExPlugin.hotKey.Value, KeyCode.LeftControl);
            if (key.IsDown())
            {
                BepInExPlugin.isOnlyGoldAndCrystals.Value = !BepInExPlugin.isOnlyGoldAndCrystals.Value;
                BepInExPlugin.Dbgl(string.Format("set isOnlyGoldAndCrystals: {0}", BepInExPlugin.isOnlyGoldAndCrystals.Value), true);
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
            }
            if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 1;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Whilte", Color.white);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 2;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Blue", Color.white);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 3;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Yellow", Color.white);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 4;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Green", Color.white);
                }
            }
            else if(new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 5;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Red", Color.white);
                }
            }
            else if (new BepInEx.Configuration.KeyboardShortcut(KeyCode.Alpha6, KeyCode.LeftControl).IsDown())
            {
                BepInExPlugin.filterRarity.Value = 6;
                if (Global.code != null && Global.code.uiCombat != null)
                {
                    Global.code.uiCombat.AddRollHint("Filter:Cyan", Color.white);
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
        /*     [HarmonyPatch(typeof(ID), "AddExp")]
             private static class ID_AddExp_Patch
             {
                 private static void Prefix(ID __instance, int exp)
                 {
                     Dbgl(__instance.name + ".AddExp(" + exp+")");
                 }
             }
        */

        [HarmonyPatch(typeof(Item), "Drop")]
        private static class Item_Drop_Patch
        {
            private static void Postfix(Item __instance)
            {
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
                {
                    if (__instance.itemName == "Crystals" || __instance.itemName == "Gold")
                    {
                        if (Global.code.AddItemToPlayerStorage(__instance.transform, true))
                        {
                            Debug.Log(__instance.itemName + " auto pick up");
                            __instance.InitiateInteract();
                            return;
                        }
                    }
                    else if (!BepInExPlugin.isOnlyGoldAndCrystals.Value && ((int)__instance.rarity >= BepInExPlugin.filterRarity.Value || isBook(__instance.itemName)))
                    {
                        if (Global.code.AddItemToPlayerStorage(__instance.transform, true))
                        {
                            Debug.Log(__instance.itemName + " auto pick up, ratity=" + __instance.rarity);
                            __instance.Pickup();
                            return;
                        }
                    }
                }
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

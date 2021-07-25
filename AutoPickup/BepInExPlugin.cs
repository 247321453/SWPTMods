using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AutoPickup
{
    [BepInPlugin("caicai.AutoPickup", "Auto Pickup", "0.0.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;

        /// <summary>
        /// 仅拾取黄金和水晶
        /// </summary>
        public static ConfigEntry<bool> isOnlyGoldAndCrystals;

        public static ConfigEntry<int> filterRarity;

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
            BepInExPlugin.filterRarity = base.Config.Bind<int>("General", "FilterRarity", 2, "Default: Blue, Whilte:1, Blue:2, Yellow:3 Green:4 Red:5 Cyan:6");
            BepInExPlugin.isOnlyGoldAndCrystals = base.Config.Bind<bool>("Options", "isOnlyGoldAndCrystals", true, "only auto pickup golds and crystals.");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
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
                            Debug.Log( __instance.itemName + " auto pick up");
                            __instance.InitiateInteract();
                            return;
                        }
                    }
                    else if (!BepInExPlugin.isOnlyGoldAndCrystals.Value && ((int)__instance.rarity >= BepInExPlugin.filterRarity.Value || isBook(__instance.itemName)))
                    {
                        if (Global.code.AddItemToPlayerStorage(__instance.transform, true))
                        {
                            Debug.Log(__instance.itemName + " auto pick up, ratity="+ __instance.rarity);
                            __instance.Pickup();
                            return;
                        }
                    }
                }
            }
        }

        private static bool isBook(string name) {
            return "Experience Book" == name 
                || "Experience Scroll" == name
                || "Golden Key" == name 
                || "Key" == name;
        }
    }
}

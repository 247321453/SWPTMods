using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.IO;

namespace EnhanceEquip
{
    [BepInPlugin("caicai.EnhanceEquip", "Enhance Equip", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> maxEnchanceStr;

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
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 28, "Nexus mod ID for updates");
            BepInExPlugin.maxEnchanceStr = Config.Bind<string>("General", "MaxEnchanceStr", "this equip is not change.");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
            ItemAffixes.Init();
        }

        [HarmonyPatch(typeof(ItemIcon), "ClickedItem")]
        private static class ItemIcon_Click_Patch
        {
            private static bool Prefix(ItemIcon __instance)
            {
                if (BepInExPlugin.modEnabled.Value)
                {
                    if (Input.GetMouseButton(1))
                    {
                        //右键
                        return true;
                    }
                    if (!Global.code.selectedItem)
                    {
                        return true;
                    }
                    //PrintAll();
                    var vec = __instance.item.GetComponent<Item>().occupliedSlots[0].vec;
                    var items = __instance.inventory.storage.GetZoneItems(Global.code.selectedItem, vec).items;
                    BepInExPlugin.Debug("items:" + items.Count, true);
                    if (items.Count == 1)
                    {
                        var main_item = items[0].GetComponent<Item>();//目标
                        var sel_item = Global.code.selectedItem.GetComponent<Item>();//材料
                        if (main_item.name == sel_item.name)
                        {
                            Item next_prefix;
                            Item next_surfix = null;
                            if (!main_item.prefix)
                            {
                                //没词条
                                next_prefix = ItemAffixes.GetNewPrefix(main_item);
                            }
                            else
                            {
                                //prefix升级
                                next_prefix = ItemAffixes.GetNextPrefix(main_item);
                            }
                            if (main_item.surfix) {
                                //surfix升级
                                next_surfix = ItemAffixes.GetNextSurfix(main_item, sel_item);
                            } else{
                                //主装备没属性，继承材料的
                                if (sel_item.surfix)
                                {
                                    int surfixLevel = ItemAffixes.ToAffixLevel(sel_item.surfix);
                                    var new_name = ItemAffixes.ToAffixString(surfixLevel & 0xf0);
                                    if (new_name != null)
                                    {
                                        Transform itemWithName2 = RM.code.allAffixes.GetItemWithName(new_name);
                                        if (itemWithName2)
                                        {
                                            next_surfix = itemWithName2.GetComponent<Item>();
                                            BepInExPlugin.Debug(main_item.GetName() + "'s next_surfix is " + next_surfix.name + ", lv=" + sel_item.surfix.level + "->" + next_surfix.level, true);
                                        }
                                    }
                                    else
                                    {
                                        //直接集成
                                        next_surfix = sel_item.surfix;
                                    }
                                }
                            }
                            if (next_prefix != main_item .prefix || next_surfix != main_item.surfix)
                            {
                                Transform transform = Utility.Instantiate(items[0]);
                                transform.GetComponent<Item>().prefix = next_prefix;//更新prefix
                                transform.GetComponent<Item>().surfix = next_surfix;//保持surfix
                                transform.GetComponent<Item>().damageMod = 0;
                                transform.GetComponent<Item>().defenceMod = 0;
                                if (transform.GetComponent<Item>().slotType != main_item.slotType)
                                {
                                    BepInExPlugin.Debug(main_item.GetName() + " fix slotType", true);
                                    transform.GetComponent<Item>().slotType = main_item.slotType;
                                }
                                RM.code.balancer.GetItemStats(transform, -1);//更新装备数据

                                BepInExPlugin.Debug(main_item.GetName() + "'s level=" + main_item.level, true);
                                Global.code.selectedItem = null;
                                //移除旧物品
                                __instance.inventory.storage.RemoveItem(items[0]);
                                //添加新物品
                                __instance.inventory.storage.AddItemToSlot(transform, vec, true, true);
                                __instance.inventory.Refresh();
                                //Global.code.uiCombat.AddRollHint("enhance equip:" + item1.itemName + " sccess!", Color.red);
                                return false;
                            }
                            else
                            {
                                Global.code.uiCombat.AddRollHint(BepInExPlugin.maxEnchanceStr.Value, Color.red);
                            }
                        }
                        return true;
                    }
                }
                return true;
            }
        }
    }
}

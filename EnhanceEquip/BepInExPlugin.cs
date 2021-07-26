using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace EnhanceEquip
{
    [BepInPlugin("caicai.EnhanceEquip", "Enhance Equip", "0.0.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;
        private static bool sInit = false;
        private static Dictionary<int, string> sLevelDic = new Dictionary<int, string>();
        private static Dictionary<string, int> sLevelNameDic = new Dictionary<string, int>();
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
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
            Init();
        }

        private static void register(int level, string name)
        {
            sLevelDic.Add(level, name);
            sLevelNameDic.Add(name, level);
        }

        private static void Init()
        {
            if (sInit)
            {
                return;
            }
            sInit = true;
            register(1, "Thick");//厚
            register(2, "Hard");//硬
            register(3, "Sturdy");//结实
            register(4, "Reinforced");//加强
            register(5, "Strong");//强化
            register(6, "Unbreakable");//坚硬
            register(7, "Godly");//永恒
            register(11, "Sharp");//锋利
            register(12, "Balanced");//平衡
            register(13, "Slaying");//杀戮
            register(14, "Massacre");//屠杀
            register(15, "Horifying");//恐惧
        }

        [HarmonyPatch(typeof(ItemIcon), "ClickedItem")]
        private static class ItemIcon_Click_Patch
        {
            private static bool Prefix(ItemIcon __instance)
            {
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
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
                    var vec = __instance.item.GetComponent<Item>().occupliedSlots[0].vec;
                    var items = __instance.inventory.storage.GetZoneItems(Global.code.selectedItem, vec).items;
                    BepInExPlugin.Dbgl("items:" + items.Count, true);
                    if (items.Count == 1)
                    {
                        var item1 = items[0].GetComponent<Item>();//目标
                        var item2 = Global.code.selectedItem.GetComponent<Item>();//材料
                        if (item1.itemName == item2.itemName)
                        {
                            Item next_prefix = null;
                            if (!item1.prefix)
                            {
                                //没词条
                                List<Transform> matchLevelAffixes = null;
                                if (item1.slotType == SlotType.weapon)
                                {
                                    matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item1, RM.code.weaponPrefixes.items);
                                    if (matchLevelAffixes.Count > 0)
                                    {
                                        next_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                                        BepInExPlugin.Dbgl(item1.GetName() + "'s next_prefix is weapon, min=" + next_prefix.name, true);
                                    }
                                }
                                else if (item1.slotType == SlotType.armor || item1.slotType == SlotType.shoes || item1.slotType == SlotType.legging || item1.slotType == SlotType.gloves
                                    || item1.slotType == SlotType.helmet)
                                {
                                    matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item1, RM.code.armorPrefixes.items);
                                    if (matchLevelAffixes.Count > 0)
                                    {
                                        next_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                                        BepInExPlugin.Dbgl(item1.GetName() + "'s next_prefix is armor, min=" + next_prefix.name, true);
                                    }
                                }
                            }
                            else
                            {
                                int prefixLevel = ToEnhanceLevel(item1.prefix);
                                if (prefixLevel > 0)
                                {
                                    var new_name = ToEnhanceString(prefixLevel + 1);
                                    if (new_name != null)
                                    {
                                        Transform itemWithName2 = RM.code.allAffixes.GetItemWithName(new_name);
                                        if (itemWithName2)
                                        {
                                            next_prefix = itemWithName2.GetComponent<Item>();
                                            BepInExPlugin.Dbgl(item1.GetName() + "'s next_prefix is " + next_prefix.name + ", lv=" + item1.prefix.level + "->" + next_prefix.level, true);
                                        }
                                    }
                                }
                                else {
                                    BepInExPlugin.Dbgl(item1.GetName() + " don't found prefix level, name=" + item1.prefix.name, true);
                                }
                            }
                            if (next_prefix)
                            {
                                Transform transform = Utility.Instantiate(items[0]);
                                transform.GetComponent<Item>().prefix = next_prefix;//更新prefix
                                transform.GetComponent<Item>().surfix = item1.surfix;//保持俗人fix
                                transform.GetComponent<Item>().damageMod = 0;

                                RM.code.balancer.GetItemStats(transform, -1);//更新装备数据

                                BepInExPlugin.Dbgl(item1.GetName() + "'s level=" + item1.level, true);
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
                                Global.code.uiCombat.AddRollHint("can't enhance equip " + item1.GetName(), Color.red);
                            }
                        }
                        return true;
                    }
                }
                return true;
            }
        }

        private static int ToEnhanceLevel(Item fix)
        {
            if (!fix)
            {
                return 0;
            }
            var name = fix.name;
            int lv;
            if (sLevelNameDic.TryGetValue(name, out lv))
            {
                return lv;
            }
            return 0;
        }

        private static string ToEnhanceString(int level)
        {
            string name;
            if (sLevelDic.TryGetValue(level, out name))
            {
                return name;
            }
            return null;
        }
    }
}

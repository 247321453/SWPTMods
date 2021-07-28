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
    [BepInPlugin("caicai.EnhanceEquip", "Enhance Equip", "0.0.7")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> maxEnchanceStr;
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
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 28, "Nexus mod ID for updates");
            BepInExPlugin.maxEnchanceStr = Config.Bind<string>("General", "MaxEnchanceStr", "can't enhance equip {}");
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
            register(0x10, "Sharp");//锋利
            register(0x11, "Balanced");//平衡
            register(0x12, "Slaying");//杀戮
            register(0x13, "Massacre");//屠杀
            register(0x14, "Horifying");//恐惧

            /*
             Heating:燃烧的, level=1, fire=3, cold=0, lightening=0, poison=0
Icy:寒冷的, level=1, fire=0, cold=3, lightening=0, poison=0
Poisonous:有毒的, level=1, fire=0, cold=0, lightening=0, poison=3
Sparking:闪光之, level=1, fire=0, cold=0, lightening=4, poison=0
Flaming:火焰之, level=3, fire=7, cold=0, lightening=0, poison=0
Shocking:Shocking, level=3, fire=0, cold=0, lightening=10, poison=0
Snowy:暴雪之, level=3, fire=0, cold=7, lightening=0, poison=0
Venom:毒液之, level=3, fire=0, cold=0, lightening=0, poison=7
Fiery:烈火之, level=5, fire=11, cold=0, lightening=0, poison=0
Glowing:电光之, level=5, fire=0, cold=0, lightening=16, poison=0
Shivering:颤抖之, level=5, fire=0, cold=11, lightening=0, poison=0
Toxic:巫毒之, level=5, fire=0, cold=0, lightening=0, poison=11
Burning:爆燃之, level=7, fire=15, cold=0, lightening=0, poison=0
Discharging:充能之, level=7, fire=0, cold=15, lightening=0, poison=0
Freezing:急冻之, level=7, fire=0, cold=15, lightening=0, poison=0
Septic:蛇蝎之, level=7, fire=0, cold=0, lightening=0, poison=15
Giant's:冰霜巨人之, level=9, fire=0, cold=19, lightening=0, poison=0
Hell's:魔王之, level=9, fire=19, cold=0, lightening=0, poison=0
Revenger's:复仇者之, level=9, fire=0, cold=0, lightening=0, poison=19
Thor's:雷神之, level=9, fire=0, cold=19, lightening=0, poison=0
             */
            register(0x20, "Heating");// 燃烧的
            register(0x21, "Flaming");// 火焰之
            register(0x22, "Fiery");// 烈火之
            register(0x23, "Burning");// 爆燃之
            register(0x24, "Hell's");// 魔王之

            register(0x30, "Icy");// 寒冷的
            register(0x31, "Snowy");// 暴雪之
            register(0x32, "Shivering");// 颤抖之
            register(0x33, "Freezing");// 急冻之
            register(0x34, "Giant's");// 冰霜巨人之

            register(0x40, "Poisonous");// 有毒的
            register(0x41, "Venom");// 毒液之
            register(0x42, "Toxic");// 巫毒之
            register(0x43, "Septic");// 蛇蝎之
            register(0x44, "Revenger's");// 复仇者之

            register(0x50, "Sparking");// 闪光之
            register(0x51, "Shocking");// Shocking
            register(0x52, "Discharging");// 充能之
            register(0x53, "Glowing");// 电光之
            register(0x54, "Thor's");// 雷神之
            

            /**
             Azure:海蓝之, level=1, fire=0, cold=2, lightening=0, poison=0
Beryl:绿宝石之, level=1, fire=0, cold=0, lightening=0, poison=2
Crimson:火焰之, level=1, fire=2, cold=0, lightening=0, poison=0
Tangerine:雷电之, level=1, fire=0, cold=0, lightening=2, poison=0
Valkyrie's:女武神的, level=1, fire=0, cold=0, lightening=0, poison=0
Lapis:天青之, level=3, fire=0, cold=4, lightening=0, poison=0
Ocher:雷电之, level=3, fire=0, cold=0, lightening=4, poison=0
Russet:烈焰之, level=3, fire=4, cold=0, lightening=0, poison=0
Viridian:巫毒之, level=3, fire=0, cold=0, lightening=0, poison=4
Cobalt:深渊之, level=5, fire=0, cold=6, lightening=0, poison=0
Coral:珊瑚之, level=5, fire=0, cold=0, lightening=6, poison=0
Garnet:烈日之, level=5, fire=6, cold=0, lightening=0, poison=0
Jade:宝玉之, level=5, fire=0, cold=0, lightening=0, poison=6
Amber:琥珀之, level=7, fire=0, cold=0, lightening=8, poison=0
Demon's:炎魔之, level=7, fire=8, cold=0, lightening=0, poison=0
Emerald:毒蛇之, level=7, fire=0, cold=0, lightening=0, poison=8
Sapphire:冰霜之, level=7, fire=0, cold=8, lightening=0, poison=0
Camphor:雷神之, level=9, fire=0, cold=0, lightening=10, poison=0
Chromatic:辐射之, level=9, fire=0, cold=0, lightening=0, poison=10
             */
            register(0x60, "Azure");//  海蓝之
            register(0x61, "Lapis");//  天青之
            register(0x62, "Cobalt");// 深渊之
            register(0x63, "Sapphire");// 冰霜之

            register(0x70, "Beryl");// 绿宝石之
            register(0x71, "Viridian");// 巫毒之
            register(0x72, "Jade");// 宝玉之
            register(0x73, "Emerald");//  毒蛇之
            register(0x74, "Chromatic");// 辐射之

            register(0x80, "Tangerine");// 雷电之
            register(0x81, "Ocher");// 雷电之
            register(0x82, "Coral");// 珊瑚之
            register(0x83, "Amber");// 琥珀之
            register(0x84, "Camphor");// 雷神之

            register(0x90, "Crimson");// 火焰之
            register(0x91, "Russet");// 烈焰之
            register(0x92, "Garnet");// 烈日之
            register(0x93, "Demon's");// 炎魔之

            //register(20, "Valkyrie's");// 女武神的
        }

        private static bool sPrinted = false;
        private static void PrintAll() {
            if (!sPrinted) {
                sPrinted = true;
                string dir = "D:\\";
                List<string> lines = new List<string>();
                lines.Add("#weaponPrefixes");
                foreach (var t in RM.code.weaponPrefixes.items) {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name));
                }
                lines.Add("#weaponSurfixes");
                foreach (var t in RM.code.weaponSurfixes.items)
                {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name)+", level="+it.level+", fire="+it.fireDamage + ", cold=" + it.coldDamage + ", lightening=" 
                        + it.lighteningDamage + ", poison=" + it.poisonDamage);
                }
                lines.Add("#armorPrefixes");
                foreach (var t in RM.code.armorPrefixes.items)
                {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name));
                }
                lines.Add("#armorSurfixes");
                foreach (var t in RM.code.armorSurfixes.items)
                {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name) + ", level=" + it.level + ", fire=" + it.fireResist + ", cold=" + it.coldResist + ", lightening=" 
                        + it.lighteningResist + ", poison=" + it.poisonResist);
                }
                lines.Add("#allArmors");
                foreach (var t in RM.code.allArmors.items)
                {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name));
                }
                lines.Add("#allWeapons");
                foreach (var t in RM.code.allWeapons.items)
                {
                    var it = t.GetComponent<Item>();
                    lines.Add(it.name + ":" + Localization.GetContent(it.name));
                }
                File.WriteAllLines(Path.Combine(dir, "data.txt"), lines.ToArray());

            }
        }

        private static bool isArmor(SlotType slotType) {
            return slotType == SlotType.armor || slotType == SlotType.shoes || slotType == SlotType.legging || slotType == SlotType.gloves
                    || slotType == SlotType.helmet;
        }

        private static Item GetNewPrefix(Item item) {
            Item new_prefix = null;
            List<Transform> matchLevelAffixes = null;
            if (item.slotType == SlotType.weapon)
            {
                matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item, RM.code.weaponPrefixes.items);
                if (matchLevelAffixes.Count > 0)
                {
                    //获取武器最低级prefix
                    new_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                    BepInExPlugin.Dbgl(item.GetName() + "'s new_prefix is weapon, min=" + new_prefix.name, true);
                }
            }
            else if (isArmor(item.slotType))
            {
                matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item, RM.code.armorPrefixes.items);
                if (matchLevelAffixes.Count > 0)
                {
                    //获取防具最低级prefix
                    new_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                    BepInExPlugin.Dbgl(item.GetName() + "'s new_prefix is armor, min=" + new_prefix.name, true);
                }
            }
            return new_prefix;
        }

        private static Item GetNextPrefix(Item item) {
            //最起码属性会保留
            Item next_prefix = item.prefix;
            int prefixLevel = ToAffixLevel(item.prefix);
            if (prefixLevel > 0)
            {
                var new_name = ToAffixString(prefixLevel + 1);
                if (new_name != null)
                {
                    Transform itemWithName2 = RM.code.allAffixes.GetItemWithName(new_name);
                    if (itemWithName2)
                    {
                        next_prefix = itemWithName2.GetComponent<Item>();
                        BepInExPlugin.Dbgl(item.GetName() + "'s next_prefix is " + next_prefix.name + ", lv=" + item.prefix.level + "->" + next_prefix.level, true);
                    }
                }
                else
                {
                    //最大值
                }
            }
            else
            {
                //未知属性
                if (item.prefix)
                {
                    BepInExPlugin.Dbgl(item.GetName() + " don't found prefix level, name=" + item.prefix.name, true);
                }
            }
            return next_prefix;
        }

        private static Item GetNextSurfix(Item main, Item sel) {
            //最起码属性会保留
            Item next_surfix = main.surfix;
            int surfixLevel = ToAffixLevel(main.surfix);
            if (surfixLevel > 0)
            {
                BepInExPlugin.Dbgl(main.GetName() + ", surfixLevel=" + surfixLevel.ToString("X"), true);
                int surfixLevel2 = ToAffixLevel(sel.surfix);
                if ((surfixLevel & 0xf0) == (surfixLevel2 & 0xf0))
                {
                    BepInExPlugin.Dbgl(main.GetName() + ", surfixLevel2=" + surfixLevel2.ToString("X"), true);
                    //直接取最大？
                    //if (surfixLevel2 > surfixLevel) {
                    //    next_surfix = sel_item.surfix;
                    //}
                    //同属性强化？
                    var new_name = ToAffixString(surfixLevel + 1);
                    if (new_name != null)
                    {
                        Transform itemWithName2 = RM.code.allAffixes.GetItemWithName(new_name);
                        if (itemWithName2)
                        {
                            next_surfix = itemWithName2.GetComponent<Item>();
                            BepInExPlugin.Dbgl(main.GetName() + "'s next_surfix is " + next_surfix.name + ", lv=" + main.surfix.level + "->" + next_surfix.level, true);
                        }
                    }
                    else
                    {
                        //最大值
                    }
                }
                else
                {
                    //保留原始属性
                    BepInExPlugin.Dbgl("surfixLevel=" + surfixLevel.ToString("X") + ", surfixLevel2=" + surfixLevel2.ToString("X"), true);
                }
            }
            else
            {
                //未知属性
                if (main.surfix)
                {
                    BepInExPlugin.Dbgl("unknown surfix=[" + main.surfix.name + "]", true);
                }
            }
            return next_surfix;
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
                    //PrintAll();
                    var vec = __instance.item.GetComponent<Item>().occupliedSlots[0].vec;
                    var items = __instance.inventory.storage.GetZoneItems(Global.code.selectedItem, vec).items;
                    BepInExPlugin.Dbgl("items:" + items.Count, true);
                    if (items.Count == 1)
                    {
                        var main_item = items[0].GetComponent<Item>();//目标
                        var sel_item = Global.code.selectedItem.GetComponent<Item>();//材料
                        if (main_item.itemName == sel_item.itemName)
                        {
                            Item next_prefix;
                            Item next_surfix = null;
                            if (!main_item.prefix)
                            {
                                //没词条
                                next_prefix = GetNewPrefix(main_item);
                            }
                            else
                            {
                                //prefix升级
                                next_prefix = GetNextPrefix(main_item);
                            }
                            if (main_item.surfix) {
                                //surfix升级
                                next_surfix = GetNextSurfix(main_item, sel_item);
                            } else{
                                //主装备没属性，继承材料的
                                if (sel_item.surfix)
                                {
                                    int surfixLevel = ToAffixLevel(sel_item.surfix);
                                    var new_name = ToAffixString(surfixLevel & 0xf0);
                                    if (new_name != null)
                                    {
                                        Transform itemWithName2 = RM.code.allAffixes.GetItemWithName(new_name);
                                        if (itemWithName2)
                                        {
                                            next_surfix = itemWithName2.GetComponent<Item>();
                                            BepInExPlugin.Dbgl(main_item.GetName() + "'s next_surfix is " + next_surfix.name + ", lv=" + sel_item.surfix.level + "->" + next_surfix.level, true);
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
                                    BepInExPlugin.Dbgl(main_item.GetName() + " fix slotType", true);
                                    transform.GetComponent<Item>().slotType = main_item.slotType;
                                }
                                RM.code.balancer.GetItemStats(transform, -1);//更新装备数据

                                BepInExPlugin.Dbgl(main_item.GetName() + "'s level=" + main_item.level, true);
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
                                Global.code.uiCombat.AddRollHint(string.Format(BepInExPlugin.maxEnchanceStr.Value, main_item.GetName()), Color.red);
                            }
                        }
                        return true;
                    }
                }
                return true;
            }
        }

        private static int ToAffixLevel(Item fix)
        {
            if (!fix)
            {
                return 0;
            }
            var name = fix.name;//.Trim();
            int lv;
            if (sLevelNameDic.TryGetValue(name, out lv))
            {
                return lv;
            }
            return 0;
        }

        private static string ToAffixString(int level)
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

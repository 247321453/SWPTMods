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
    class ItemAffixes
    {
        private static bool sInit = false;
        private static Dictionary<int, string> sLevelDic = new Dictionary<int, string>();
        private static Dictionary<string, int> sLevelNameDic = new Dictionary<string, int>();


        public static int ToAffixLevel(Item fix)
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

        public static string ToAffixString(int level)
        {
            string name;
            if (sLevelDic.TryGetValue(level, out name))
            {
                return name;
            }
            return null;
        }
        private static void register(int level, string name)
        {
            sLevelDic.Add(level, name);
            sLevelNameDic.Add(name, level);
        }

        public static void Init()
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
#if DEBUG
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
#endif
        public static bool isArmor(SlotType slotType)
        {
            return slotType == SlotType.armor || slotType == SlotType.shoes || slotType == SlotType.legging || slotType == SlotType.gloves
                    || slotType == SlotType.helmet;
        }

        public static Item GetNewPrefix(Item item)
        {
            Item new_prefix = null;
            List<Transform> matchLevelAffixes = null;
            if (item.slotType == SlotType.weapon)
            {
                matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item, RM.code.weaponPrefixes.items);
                if (matchLevelAffixes.Count > 0)
                {
                    //获取武器最低级prefix
                    new_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                    BepInExPlugin.Debug(item.GetName() + "'s new_prefix is weapon, min=" + new_prefix.name, true);
                }
            }
            else if (isArmor(item.slotType))
            {
                matchLevelAffixes = RM.code.balancer.GetMatchLevelAffixes(item, RM.code.armorPrefixes.items);
                if (matchLevelAffixes.Count > 0)
                {
                    //获取防具最低级prefix
                    new_prefix = matchLevelAffixes.OrderBy(n => n.GetComponent<Item>().level).First<Transform>().GetComponent<Item>();
                    BepInExPlugin.Debug(item.GetName() + "'s new_prefix is armor, min=" + new_prefix.name, true);
                }
            }
            return new_prefix;
        }

        public static Item GetNextPrefix(Item item)
        {
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
                        BepInExPlugin.Debug(item.GetName() + "'s next_prefix is " + next_prefix.name + ", lv=" + item.prefix.level + "->" + next_prefix.level, true);
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
                    BepInExPlugin.Debug(item.GetName() + " don't found prefix level, name=" + item.prefix.name, true);
                }
            }
            return next_prefix;
        }

        public static Item GetNextSurfix(Item main, Item sel)
        {
            //最起码属性会保留
            Item next_surfix = main.surfix;
            int surfixLevel = ToAffixLevel(main.surfix);
            if (surfixLevel > 0)
            {
                BepInExPlugin.Debug(main.GetName() + ", surfixLevel=" + surfixLevel.ToString("X"), true);
                int surfixLevel2 = ToAffixLevel(sel.surfix);
                if ((surfixLevel & 0xf0) == (surfixLevel2 & 0xf0))
                {
                    BepInExPlugin.Debug(main.GetName() + ", surfixLevel2=" + surfixLevel2.ToString("X"), true);
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
                            BepInExPlugin.Debug(main.GetName() + "'s next_surfix is " + next_surfix.name + ", lv=" + main.surfix.level + "->" + next_surfix.level, true);
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
                    BepInExPlugin.Debug("surfixLevel=" + surfixLevel.ToString("X") + ", surfixLevel2=" + surfixLevel2.ToString("X"), true);
                }
            }
            else
            {
                //未知属性
                if (main.surfix)
                {
                    BepInExPlugin.Debug("unknown surfix=[" + main.surfix.name + "]", true);
                }
            }
            return next_surfix;
        }
    }
}

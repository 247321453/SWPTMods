using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace FixPatch
{
    class CompanionListPatch
    {
        public static string CompanionListKey = "Haibao.txt?tag=lastpartylist";
        public static bool UIInventoryIsOpen = false;
        public static readonly List<string> lastPartys = new List<string>();

        public static void OnChangeToLeft()
        {
            if (Global.code.uiInventory == null || Global.code.uiInventory.curCustomization == null || Global.code.uiInventory.curCustomization == Player.code.customization)
            {
                return;
            }
            var list = Global.code.companions.items;
            int index = -1;
            Transform target = null;
            var sel = Global.code.uiInventory.curCustomization;
            for (int i = 0; i < list.Count; i++)
            {
                var sub = list[i];
                if (sub.GetComponent<CharacterCustomization>() == sel)
                {
                    index = i;
                    target = sub;
                    break;
                }
            }
            if (index > 0)
            {
                list.Remove(target);
                list.Insert(index - 1, target);
                Global.code.uiCombat.ShowSuccubusIcons();
            }
        }

        public static void OnChangeToRight()
        {
            if (Global.code.uiInventory == null || Global.code.uiInventory.curCustomization == null || Global.code.uiInventory.curCustomization == Player.code.customization)
            {
                return;
            }
            var list = Global.code.companions.items;
            int index = -1;
            Transform target = null;
            var sel = Global.code.uiInventory.curCustomization;
            for (int i = 0; i < list.Count; i++)
            {
                var sub = list[i];
                if (sub.GetComponent<CharacterCustomization>() == sel)
                {
                    index = i;
                    target = sub;
                    break;
                }
            }
            if (index < (list.Count - 1))
            {
                list.Remove(target);
                list.Insert(index + 1, target);
                Global.code.uiCombat.ShowSuccubusIcons();
            }
        }
    }

    [HarmonyPatch(typeof(UIInventory), "Open")]
    class UIInventory_Open_Patch
    {
        private static void Postfix(UIInventory __instance)
        {
            CompanionListPatch.UIInventoryIsOpen = true;
        }
    }

    [HarmonyPatch(typeof(UIInventory), "Close")]
    class UIInventory_Close_Patch
    {
        private static void Postfix(UIInventory __instance)
        {
            CompanionListPatch.UIInventoryIsOpen = false;
        }
    }
    /*
    [HarmonyPatch(typeof(Scene), "Start")]
    class Scene_Start_Patch
    {
        private static void Postfix(Scene __instance)
        {
            if (BepInExPlugin.isCompanionSort.Value)
            {
                if (Global.code.curlocation && Global.code.curlocation.locationType == LocationType.home)
                {
                    return;
                }
                if (Global.code.playerCombatParty.items.Count > 0)
                {
                    CompanionListPatch.lastPartys.Clear();
                    foreach (Transform item in Global.code.playerCombatParty.items)
                    {
                        if (item)
                        {
                            CompanionListPatch.lastPartys.Add(item.name);
                        }
                    }
                    // string id = Mainframe.code.GetFolderName() + CompanionListPatch.CompanionListKey;
                    BepInExPlugin.Debug(Mainframe.code.GetFolderName() + " set last partys=" + string.Join(",", CompanionListPatch.lastPartys));
                    // ES2.Save<string>(CompanionListPatch.lastPartys, id);
                }
            }
        }
    }

    [HarmonyPatch(typeof(UICombatParty), nameof(UICombatParty.Refresh))]
    class UICombatParty_Refresh_Patch
    {
        static void Prefix(UICombatParty __instance)
        {
            if (!BepInExPlugin.isCompanionSort.Value)
            {
                return;
            }

            foreach (Transform t in __instance.locators)
            {
                BepInExPlugin.Debug($"{t.position}, {t.rotation}");
            }

            List<Transform> locators = new List<Transform>(__instance.locators);
            while (locators.Count <= Global.code.playerCombatParty.items.Count)
            {
                Transform t = UnityEngine.Object.Instantiate(locators[locators.Count - 1], locators[locators.Count - 1].parent);
                locators.Add(t);
            }
            __instance.locators = locators.ToArray();
        }
        static void Postfix(UICombatParty __instance)
        {
            if (!BepInExPlugin.isCompanionSort.Value)
            {
                return;
            }

            for (int k = 3; k < BepInExPlugin.CompanionPartySize.Value; k++)
            {
                Transform transform = UnityEngine.Object.Instantiate(__instance.iconPrefab);
                transform.SetParent(__instance.iconHolder);
                transform.localScale = Vector3.one;
                if (k < Global.code.playerCombatParty.items.Count)
                {
                    Transform unit = Global.code.playerCombatParty.items[k];
                    transform.GetComponent<CompanionSelectionIcon>().Initiate(unit, false);
                }
                else
                {
                    transform.GetComponent<CompanionSelectionIcon>().Initiate(null, false);
                }
            }
        }
    }
    [HarmonyPatch(typeof(Global), "AddCompanionToPlayerArmy")]
    class Global_AddCompanionToPlayerArmy_Patch
    {
        private static bool Prefix(Global __instance, Transform companion)
        {
            if (!BepInExPlugin.isCompanionSort.Value)
            {
                return true;
            }
            __instance.companions.AddItem(companion);
            companion.transform.SetParent(__instance.transform);
            __instance.playerCombatParty.ArrangeItems();
            bool isNull = CompanionListPatch.lastPartys.Count == 0;
            if (isNull || CompanionListPatch.lastPartys.Contains(companion.name))
            {
                if (Global.code.playerCombatParty.items.Count < BepInExPlugin.CompanionPartySize.Value)
                {
                    //TODO 适配组队限制数量
                    if (!Global.code.playerCombatParty.items.Exists(t => t.name == companion.name))
                    {
                        __instance.playerCombatParty.AddItemDifferentObject(companion);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Mainframe), "LoadCompanions")]
    class Mainframe_LoadCompanions_Patch
    {
        private static void Prefix(Mainframe __instance)
        {
            if (BepInExPlugin.isCompanionSort.Value)
            {
                string id = __instance.GetFolderName() + CompanionListPatch.CompanionListKey;
                if (ES2.Exists(id))
                {
                    CompanionListPatch.lastPartys.Clear();
                    CompanionListPatch.lastPartys.AddRange(ES2.LoadList<string>(id));
                    BepInExPlugin.Debug(__instance.GetFolderName() + " read last partys=" + string.Join(",", CompanionListPatch.lastPartys));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mainframe), "SaveGame")]
    class Mainframe_SaveGame_Patch
    {
        private static void Postfix(Mainframe __instance)
        {
            if (BepInExPlugin.isCompanionSort.Value)
            {
                if (Global.code.curlocation && Global.code.curlocation.locationType == LocationType.home)
                {
                    string id = Mainframe.code.GetFolderName() + CompanionListPatch.CompanionListKey;
                    BepInExPlugin.Debug(Mainframe.code.GetFolderName() + " save last partys=" + string.Join(",", CompanionListPatch.lastPartys));
                    ES2.Save<string>(CompanionListPatch.lastPartys, id);
                }
            }
        }
    }
*/
}

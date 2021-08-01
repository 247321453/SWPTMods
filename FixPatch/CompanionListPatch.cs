using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static HarmonyLib.AccessTools;

namespace FixPatch
{
    class CompanionListPatch
    {
        public static bool UIInventoryIsOpen = false;

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
}

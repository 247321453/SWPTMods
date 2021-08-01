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
    public static class CharacterCustomizationUtil
    {
        public static float MIN_MELEE_ATTACK_DISTANCE = 3f;
        public static float MIN_BOW_ATTACK_DISTANCE = 7f;
        /// <summary>
        /// 切换到近战武器
        /// </summary>
        public static bool ChangeMeleeWeapon(CharacterCustomization __instance, int index = -1, bool notify = true)
        {
            if ((index == -1 || index == 1) && __instance.weapon && __instance.weapon.GetComponent<Weapon>().weaponType != WeaponType.bow)
            {
                __instance.DrawWeapon(1);
                return true;
            }
            else if ((index == -1 || index == 2) && __instance.weapon2 && __instance.weapon2.GetComponent<Weapon>().weaponType != WeaponType.bow)
            {
                __instance.DrawWeapon(2);
                return true;
            }
            if (notify)
            {
                Global.code.uiCombat.AddRollHint(__instance.characterName + BepInExPlugin.NoWeaponMsg.Value, Color.red);
            }
            return false;
        }
        /// <summary>
        /// 切换到弓箭
        /// </summary>
        public static int ChangeBowWeapon(CharacterCustomization __instance, bool notify = false, bool checkArrow = true)
        {
            if (checkArrow && __instance.storage.GetItemCount("Arrow") <= 0)
            {
                return 0;
            }
            if (__instance.weapon && __instance.weapon.GetComponent<Weapon>().weaponType == WeaponType.bow)
            {
                __instance.DrawWeapon(1);
                return 1;
            }
            else if (__instance.weapon2 && __instance.weapon2.GetComponent<Weapon>().weaponType == WeaponType.bow)
            {
                __instance.DrawWeapon(2);
                return 2;
            }
            if (notify)
            {
                Global.code.uiCombat.AddRollHint(__instance.characterName + BepInExPlugin.NoWeaponMsg.Value, Color.red);
            }
            return 0;
        }
        /// <summary>
        /// 武器是弓箭
        /// </summary>
        /// <param name="customization"></param>
        /// <returns></returns>
        public static bool WeaponIsBow(CharacterCustomization customization)
        {
            return customization.weaponInHand && customization.weaponInHand.GetComponent<Weapon>().weaponType == WeaponType.bow;
        }
        public static int GetArrow(CharacterCustomization customization) {
            return customization.storage.GetItemCount("Arrow");
        }

        public static FieldRef<CharacterCustomization, int> emptyhashRef = AccessTools.FieldRefAccess<CharacterCustomization, int>("emptyhash");
    }
}

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

    class PullBowPatch
    {
        public static int MIN_PULL_BOW = 800;

        public static Dictionary<CharacterCustomization, long> sBowStartTime = new Dictionary<CharacterCustomization, long>();
        public static Dictionary<CharacterCustomization, int> sBowPullBowTime = new Dictionary<CharacterCustomization, int>();
    }
    /// <summary>
    /// 取出第一武器，判断是否有箭，如果没有就换另外一个
    /// </summary>
    [HarmonyPatch(typeof(CharacterCustomization), "DrawWeapon")]
    class CharacterCustomization_DrawWeapon_Patch
    {
        private static bool Prefix(CharacterCustomization __instance, int index)
        {
            if (index != 1)
            {
                return true;
            }
            if (__instance.GetComponent<Companion>() && __instance.weapon)
            {
                if (__instance.anim.GetCurrentAnimatorStateInfo(1).tagHash == CharacterCustomizationUtil.emptyhashRef(__instance) && __instance.canDrawWeapon && !__instance.curCastingMagic)
                {
                    //只处理随从
                    if (__instance.weapon.GetComponent<Weapon>().weaponType == WeaponType.bow)
                    {
                        //是弓箭，但是没有箭了，切换其他武器
                        if (CharacterCustomizationUtil.GetArrow(__instance) <= 0)
                        {
                            CharacterCustomizationUtil.ChangeMeleeWeapon(__instance, 2, true);
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
    /// <summary>
    /// 开始拉弓
    /// </summary>
    [HarmonyPatch(typeof(CharacterCustomization), "PullBow")]
    class CharacterCustomization_PullBow_Patch
    {

        private static bool Prefix(CharacterCustomization __instance)
        {
            if (__instance.isPullingBow)
            {
                return true;
            }
            var comp = __instance.GetComponent<Companion>();
            if (comp != null)
            {
                //是随从
                if (__instance.storage.GetItemCount("Arrow") <= 0)
                {
                    BepInExPlugin.Debug(__instance.characterName + "'s arrow is 0, change bow to other");
                    //TODO 切换武器
                    __instance.CancelPullBow();
                    CharacterCustomizationUtil.ChangeMeleeWeapon(__instance);
                    return false;
                }
            }
            if (BepInExPlugin.isFixPullBow.Value)
            {
                //(1Ticks = 0.0001毫秒)
                long time = DateTime.Now.Ticks / 10000;
                Util.AddOrUpdate(PullBowPatch.sBowStartTime, __instance, time);
                // Global.code.uiCombat.AddRollHint(__instance.characterName + " start pull bow", Color.white);
            }
            return true;
        }
    }

    /*
    [HarmonyPatch(typeof(CharacterCustomization), "UpdateStats")]
    class CharacterCustomization_UpdateStats_Patch
    {
        private static void Prefix(CharacterCustomization __instance)
        {
            if (__instance._Player)
            {
                Debug(__instance.characterName + " before UpdateStats damage:" + __instance.GetComponent<ID>().damage);
            }
        }
        private static void Postfix(CharacterCustomization __instance)
        {
            if (__instance._Player)
            {
                Debug(__instance.characterName + " after UpdateStats damage:" + __instance.GetComponent<ID>().damage);
            }
        }
    }
    */

    [HarmonyPatch(typeof(CharacterCustomization), "CancelPullBow")]
    class CharacterCustomization_CancelPullBow_Patch
    {

        private static void Prefix(CharacterCustomization __instance)
        {
            if (!__instance.isPullingBow)
            {
                return;
            }
            if (!BepInExPlugin.isFixPullBow.Value)
            {
                return;
            }
            long startTime;
            if (PullBowPatch.sBowStartTime.TryGetValue(__instance, out startTime))
            {
                long time = DateTime.Now.Ticks / 10000;
                int max = (BepInExPlugin.PullBowMaxTime.Value * 1000);
                int val;
                if ((time - startTime - PullBowPatch.MIN_PULL_BOW) > max)
                {
                    val = max;
                }
                else
                {
                    val = (int)(time - startTime - PullBowPatch.MIN_PULL_BOW);
                }
                if (val < 0)
                {
                    val = 0;
                }
                Util.AddOrUpdate(PullBowPatch.sBowPullBowTime, __instance, val);
                BepInExPlugin.Debug(__instance.characterName + "'s pull bow time set " + val + " by action");
                // Global.code.uiCombat.AddRollHint(__instance.characterName + " end pull bow time is " + val, Color.white);
            }
        }
    }


  //  private static Dictionary<Weapon, float> sWeaponDamage = new Dictionary<Weapon, float>();


    [HarmonyPatch(typeof(Weapon), "DealDamage")]
    class Weapon_DealDamage_Patch
    {
        /*
        private static float GetRealDamage(ID component, float dmg, float resist)
        {
            //魔法属性
            dmg *= (1.0f + (float)component.power * BepInExPlugin.elementDamageRate.Value);
            if (dmg > 0 && dmg > resist)
            {
                return (dmg - resist);
            }
            return 0f;
        }

        private static float GetAddDamage(Item item, ID component, ID component2)
        {
            float addDamage = 0;
            addDamage += GetRealDamage(component, component.fireDamage, component2.fireResist);
            addDamage += GetRealDamage(component, component.coldDamage, component2.coldResist);
            addDamage += GetRealDamage(component, component.lighteningDamage, component2.lighteningResist);
            addDamage += GetRealDamage(component, component.poisonDamage, component2.poisonResist);
            switch (item.rarity)
            {
                case Rarity.one:
                    break;
                case Rarity.two:
                    addDamage *= 1.1f;
                    break;
                case Rarity.three:
                    addDamage *= 1.2f;
                    break;
                case Rarity.four:
                    addDamage *= 1.3f;
                    break;
                case Rarity.five:
                    addDamage *= 1.5f;
                    break;
                case Rarity.six:
                    addDamage *= 1.8f;
                    break;
            }
            return addDamage;
        }
        */
        private static void Prefix(Weapon __instance, Bodypart bodypart, float multiplier, bool playStaggerAnimation)
        {
            if (BepInExPlugin.isFixPullBow.Value /* || elementDamageEnable.Value */)
            {
                Item item = __instance.GetComponent<Item>();
                if (item)
                {
                    ID owner = item.owner.GetComponent<ID>();
                    if (BepInExPlugin.isOnlyPlayerAndCompanion.Value && owner.monster)
                    {
                        BepInExPlugin.Debug("don't support monster:" + owner.name, true);
                        return;
                    }
                    //   ID target = bodypart.root.GetComponent<ID>();
                    var character = owner.GetComponent<CharacterCustomization>();
                    //保存原始伤害
                    //Util.AddOrUpdate<Weapon, float>( sWeaponDamage, __instance, owner.damage);

                    float old = owner.damage;
                    /*
                    if (elementDamageEnable.Value)
                    {
                        float addDamage = GetAddDamage(item, owner, target);
                        if (addDamage > 0)
                        {
                            addDamage *= multiplier;
                            owner.damage += addDamage;
                        }
                        BepInExPlugin.Debug("before DealDamage " + owner.name + "'s damage=" + old + "->" + owner.damage, true);
                    }
                    */
                    if (character == null)
                    {
                        BepInExPlugin.Debug("before DealDamage character is null", true);
                    }
                    else if (BepInExPlugin.isFixPullBow.Value)
                    {
                        int val;
                        if (PullBowPatch.sBowPullBowTime.TryGetValue(character, out val))
                        {
                            //
                            if (val > 100)
                            {
                                float addDamage = old * (val / 100) * BepInExPlugin.PullBowDamageRate.Value;
                                if (addDamage > 0)
                                {
                                    old = owner.damage;
                                    owner.damage += addDamage;
                                    BepInExPlugin.Debug("before DealDamage " + character.characterName + "'s pull bow damage=" + old + "->" + owner.damage + ", time=" + val, true);
                                }
                                else
                                {
                                    BepInExPlugin.Debug("before DealDamage " + character.characterName + "'s addDamage is small, old=" + old + ", rate=" + ((val / 100) * BepInExPlugin.PullBowDamageRate.Value), true);
                                }
                            }
                            else
                            {
                                BepInExPlugin.Debug("before DealDamage pull bow time = 0", true);
                            }
                            Util.AddOrUpdate(PullBowPatch.sBowPullBowTime, character, 0);
                            BepInExPlugin.Debug(character.characterName + "'s pull bow time reset by deal damage");
                        }
                        else
                        {
                            BepInExPlugin.Debug("before DealDamage not character pull bow time, name=" + character.characterName, true);
                        }
                    }
                }
            }
        }
        /**
        private static void Postfix(Weapon __instance, Bodypart bodypart, float multiplier, bool playStaggerAnimation)
        {
            if (isFixPullBow.Value)// || elementDamageEnable.Value )
            {
                Item item = __instance.GetComponent<Item>();
                if (item)
                {
                    ID component = item.owner.GetComponent<ID>();
                    float old;
                    if (sWeaponDamage.TryGetValue(__instance, out old))
                    {
                        BepInExPlugin.Debug("after DealDamage " + item.owner.name + "'s damage=" + component.damage + "->" + old + ", multiplier=" + multiplier, true);
                        component.damage = old;
                    }
                    else
                    {
                        Error("after DealDamage not found prefix damage");
                        old = component.damage;
                        if (elementDamageEnable.Value)
                        {
                            float addDamage = GetAddDamage(item, component, component2);
                            if (addDamage > 0)
                            {
                                addDamage *= multiplier;
                                component.damage -= addDamage;
                            }
                            BepInExPlugin.Debug("after DealDamage" + item.owner.name + "'s damage=" + old + "->" + component.damage, true);
                        }
                        if (character && isFixPullBow.Value)
                        {
                            int val;
                            if (component && sBowPullBowTime.TryGetValue(character, out val))
                            {
                                //
                                if (val > 100)
                                {
                                    old = component.damage;
                                    float addDamage = item.damage * (val / 100) * BepInExPlugin.PullBowDamageRate.Value;
                                    component.damage -= addDamage;
                                    BepInExPlugin.Debug("after DealDamage" + item.owner.name + "'s pull bow damage=" + old + "->" + component.damage, true);
                                }
                            }
                            //重置时间
                            AddOrUpdate<CharacterCustomization, int>(sBowPullBowTime, character, 0);
                        }
                    }
                }
            }

        }

        */
    }

}

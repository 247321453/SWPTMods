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
    [BepInPlugin("caicai.FixPatch", "Fix Patch", "0.1.4")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> isFixAffixes;

        public static ConfigEntry<bool> isFixBugs;

        public static ConfigEntry<bool> isFixAI;

        public static ConfigEntry<bool> isFixPullBow;

        public static ConfigEntry<bool> isOnlyPlayerAndCompanion;

        public static ConfigEntry<float> PullBowDamageRate;

        public static ConfigEntry<int> PullBowMaxTime;

        private static float BOW_ATTACK_LIMIT = 20f;
        public static ConfigEntry<string> NoWeaponMsg;

        //   public static ConfigEntry<bool> elementDamageEnable;

        //   public static ConfigEntry<float> elementDamageRate;

        private static int MIN_PULL_BOW = 800;
        private enum CompanionCmd
        {
            FollowMe,
            Charge,
            GoThereAndStand,
        }
        private static CompanionCmd sStatus = CompanionCmd.FollowMe;

        public static void Debug(string str = "", bool pref = true)
        {
            if (BepInExPlugin.isDebug.Value)
            {
                UnityEngine.Debug.Log((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
            }
        }
        public static void Error(string str = "", bool pref = true)
        {
            UnityEngine.Debug.LogError((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
        }
        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 29, "Nexus mod ID for updates");

            //   BepInExPlugin.elementDamageEnable = base.Config.Bind<bool>("Options", "ElementDamageEnable", false, "Enable elemental damage of weapon");
            //    BepInExPlugin.elementDamageRate = base.Config.Bind<float>("Options", "ElementDamageRate", 0.1f, "magic attributes can increase the elemental damage of weapons");
            BepInExPlugin.isFixAffixes = base.Config.Bind<bool>("Options", "IsFixAffixes", true, "fix weapon and armor 's affixes.");
            BepInExPlugin.isFixBugs = base.Config.Bind<bool>("Options", "IsFixBugs", false, "fix some bugs.");
            BepInExPlugin.isFixAI = base.Config.Bind<bool>("Options", "isFixAI", true, "fix compation's ai.");
            BepInExPlugin.isFixPullBow = base.Config.Bind<bool>("Options", "isFixPullBow", true, "fix compation's ai.");
            BepInExPlugin.isOnlyPlayerAndCompanion = base.Config.Bind<bool>("Options", "IsOnlyPlayerAndCompanion", true, "only player and companion enable pull bow append damage and element damage.");
            BepInExPlugin.PullBowDamageRate = base.Config.Bind<float>("Options", "PullBowDamageRate", 0.1f, "Bow accumulate increases damage rate.Default:10%/0.1s");
            BepInExPlugin.PullBowMaxTime = base.Config.Bind<int>("Options", "PullBowMaxTime", 3, "pull bow max time, default is 3s");
            BepInExPlugin.NoWeaponMsg = base.Config.Bind<string>("Options", "NoWeaponMsg", ": I need a weapon or arrows.", "message");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
        }

        private static Dictionary<CharacterCustomization, long> sBowStartTime = new Dictionary<CharacterCustomization, long>();
        private static Dictionary<CharacterCustomization, int> sBowPullBowTime = new Dictionary<CharacterCustomization, int>();

        private static void AddOrUpdate<K, V>(Dictionary<K, V> dic, K k, V v)
        {
            if (dic.ContainsKey(k))
            {
                dic[k] = v;
            }
            else
            {
                dic.Add(k, v);
            }
        }

        private static bool ChangeMeleeWeapon(CharacterCustomization __instance, bool notify = true)
        {
            if (__instance.weapon && __instance.weapon.GetComponent<Weapon>().weaponType != WeaponType.bow)
            {
                __instance.DrawWeapon(1);
                return true;
            }
            else if (__instance.weapon2 && __instance.weapon2.GetComponent<Weapon>().weaponType != WeaponType.bow)
            {
                __instance.DrawWeapon(2);
                return true;
            }
            if (notify)
            {
                Global.code.uiCombat.AddRollHint(__instance.characterName + NoWeaponMsg.Value, Color.red);
            }
            return false;
        }
        private static int ChangeBowWeapon(CharacterCustomization __instance, bool notify = false)
        {
            if (__instance.storage.GetItemCount("Arrow") <= 0)
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
                Global.code.uiCombat.AddRollHint(__instance.characterName + NoWeaponMsg.Value, Color.red);
            }
            return 0;
        }

        private static bool WeaponIsBow(CharacterCustomization character)
        {
            if (character.weaponIndex == 1 && character.weapon && character.weapon.GetComponent<Weapon>().weaponType == WeaponType.bow)
            {
                return true;
            }
            else if (character.weaponIndex == 2 && character.weapon2 && character.weapon2.GetComponent<Weapon>().weaponType == WeaponType.bow)
            {
                return true;
            }
            return false;
        }

        static FieldRef<CharacterCustomization, int> emptyhashRef = AccessTools.FieldRefAccess<CharacterCustomization, int>("emptyhash");

        //DrawWeapon
        [HarmonyPatch(typeof(CharacterCustomization), "DrawWeapon")]
        private static class CharacterCustomization_DrawWeapon_Patch
        {

            private static bool Prefix(CharacterCustomization __instance, int index)
            {
                if (index != 1)
                {
                    return true;
                }
                if (__instance.anim.GetCurrentAnimatorStateInfo(1).tagHash == emptyhashRef(__instance) && __instance.canDrawWeapon && !__instance.curCastingMagic)
                {
                    var comp = __instance.GetComponent<Companion>();
                    if (comp != null)
                    {
                        if (__instance.weapon && __instance.weapon.GetComponent<Weapon>().weaponType == WeaponType.bow)
                        {
                            //是弓箭，但是没有箭了，切换其他武器
                            if (__instance.storage.GetItemCount("Arrow") <= 0)
                            {
                                if (__instance.weapon2)
                                {
                                    __instance.DrawWeapon(2);
                                    return false;
                                }
                                Global.code.uiCombat.AddRollHint(__instance.characterName + NoWeaponMsg.Value, Color.red);
                                return false;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "PullBow")]
        private static class CharacterCustomization_PullBow_Patch
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
                        Debug(__instance.characterName + "'s arrow is 0, change bow to other");
                        //TODO 切换武器
                        __instance.CancelPullBow();
                        if (ChangeMeleeWeapon(__instance))
                        {
                            EnemyInAttackDistRef.Invoke(comp, new object[0]);
                        }
                        return false;
                    }
                }
                if (isFixPullBow.Value)
                {
                    //(1Ticks = 0.0001毫秒)
                    long time = DateTime.Now.Ticks / 10000;
                    AddOrUpdate(sBowStartTime, __instance, time);
                    // Global.code.uiCombat.AddRollHint(__instance.characterName + " start pull bow", Color.white);
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(CharacterCustomization), "UpdateStats")]
        private static class CharacterCustomization_UpdateStats_Patch
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

        [HarmonyPatch(typeof(CharacterCustomization), "CancelPullBow")]
        private static class CharacterCustomization_CancelPullBow_Patch
        {

            private static void Prefix(CharacterCustomization __instance)
            {
                if (!__instance.isPullingBow)
                {
                    return;
                }
                if (!isFixPullBow.Value)
                {
                    return;
                }
                long startTime;
                if (sBowStartTime.TryGetValue(__instance, out startTime))
                {
                    long time = DateTime.Now.Ticks / 10000;
                    int max = (BepInExPlugin.PullBowMaxTime.Value * 1000);
                    int val;
                    if ((time - startTime - MIN_PULL_BOW) > max)
                    {
                        val = max;
                    }
                    else
                    {
                        val = (int)(time - startTime - MIN_PULL_BOW);
                    }
                    if (val < 0)
                    {
                        val = 0;
                    }
                    AddOrUpdate(sBowPullBowTime, __instance, val);
                    Debug(__instance.characterName + "'s pull bow time set " + val + " by action");
                    // Global.code.uiCombat.AddRollHint(__instance.characterName + " end pull bow time is " + val, Color.white);
                }
            }
        }

        #region fix bugs
        //CustomizationSlider
        [HarmonyPatch(typeof(CustomizationSlider), "Start")]
        private static class CustomizationSlider_Start_Patch
        {
            private static bool Prefix(CustomizationSlider __instance)
            {
                if (BepInExPlugin.isFixBugs.Value)
                {
                    if (Player.code != null && Player.code.customization != null && Player.code.customization.body != null && Player.code.customization.body.sharedMesh != null)
                    {
                        return true;
                    }
                    __instance.Invoke("ResetEmotion", 1f);
                    return false;
                }
                return true;
            }
        }
        static FieldRef<ThirdPersonCharacter, Rigidbody> m_Rigidbody = FieldRefAccess<ThirdPersonCharacter, Rigidbody>("m_Rigidbody");
        static FieldRef<ThirdPersonCharacter, Animator> m_Animator = FieldRefAccess<ThirdPersonCharacter, Animator>("m_Animator");

        [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleAirborneMovement")]
        private static class ThirdPersonCharacter_HandleAirborneMovement_Patch
        {

            private static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (BepInExPlugin.isFixBugs.Value)
                {
                    if (m_Rigidbody(__instance) == null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ThirdPersonCharacter), "UpdateAnimator")]
        private static class ThirdPersonCharacter_UpdateAnimator_Patch
        {

            private static bool Prefix(ThirdPersonCharacter __instance, Vector3 move)
            {
                if (BepInExPlugin.isFixBugs.Value)
                {
                    if (m_Animator(__instance) == null)
                    {
                        // m_Animator. = __instance.GetComponent<Animator>();
                        return false;
                    }
                }
                return true;
            }
        }
        //HandleGroundedMovement(bool crouch, bool jump)
        [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleGroundedMovement")]
        private static class ThirdPersonCharacter_HandleGroundedMovement_Patch
        {

            private static bool Prefix(ThirdPersonCharacter __instance, bool crouch, bool jump)
            {
                if (BepInExPlugin.isFixBugs.Value)
                {
                    if (m_Rigidbody(__instance) == null)
                    {
                        //   __instance.GetComponent<Rigidbody>();
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ThirdPersonCharacter), "OnAnimatorMove")]
        private static class ThirdPersonCharacter_OnAnimatorMove_Patch
        {
            private static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (BepInExPlugin.isFixBugs.Value)
                {
                    if (m_Rigidbody(__instance) == null)
                    {
                        //  __instance.GetComponent<Rigidbody>();
                        return false;
                    }
                }
                return true;
            }
        }
        #endregion

        #region fix element damage
        private static void FixAffixes(List<Transform> items)
        {
            foreach (var it in items)
            {
                var item = it.GetComponent<Item>();
                if (item.name == "Thor's")
                {
                    if (item.lighteningDamage == 0)
                    {
                        item.lighteningDamage = 28;
                        item.coldDamage = 0;
                    }
                }
                else if (item.name == "Discharging")
                {
                    if (item.lighteningDamage == 0)
                    {
                        item.lighteningDamage = 22;
                        item.coldDamage = 0;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(RM), "LoadResources")]
        private static class RM_LoadResources_Patch
        {
            private static void Postfix(RM __instance)
            {
                if (BepInExPlugin.isFixAffixes.Value)
                {
                    FixAffixes(__instance.allAffixes.items);
                    FixAffixes(__instance.weaponSurfixes.items);
                    FixAffixes(__instance.armorSurfixes.items);
                }
            }
        }

        private static Dictionary<Weapon, float> sWeaponDamage = new Dictionary<Weapon, float>();


        [HarmonyPatch(typeof(Weapon), "DealDamage")]
        private static class Weapon_DealDamage_Patch
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
                if (isFixPullBow.Value /* || elementDamageEnable.Value */)
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
                        AddOrUpdate<Weapon, float>(sWeaponDamage, __instance, owner.damage);

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
                        else if (isFixPullBow.Value)
                        {
                            int val;
                            if (sBowPullBowTime.TryGetValue(character, out val))
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
                                AddOrUpdate(sBowPullBowTime, character, 0);
                                Debug(character.characterName + "'s pull bow time reset by deal damage");
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
        #endregion
        /*
        #region AI
        [HarmonyPatch(typeof(ID), "AddHealth")]
        private static class ID_AddHealth_Patch
        {

            private static void Postfix(ID __instance, float pt, Transform source)
            {
                if (isDebug.Value)
                {
                    if (__instance.GetComponent<Monster>())
                    {
                        Debug(__instance.name + " AddHealth " + pt);
                    }
                }
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return;
                }
                if (!__instance.canaddhealth)
                {
                    return;
                }
                if (__instance.damageSource && __instance.GetComponent<Monster>() == null)
                {
                        if (__instance.damageSource.GetComponent<ID>() == Player.code._ID)
                        {
                            return;
                        }
                        var customization = __instance.GetComponent<CharacterCustomization>();
                        if (!customization) return;
                        var companion = customization.GetComponent<Companion>();
                        if (!companion) return;

                        //随从受伤
                        //if (companion.charge)
                        //{
                        //    companion.target = __instance.damageSource;
                        //    BepInExPlugin.Debug(companion.name + " change target", true);
                        //}
                        if (__instance.health > 0 && (__instance.health / __instance.maxHealth) < 0.1f)
                        {
                            //TODO 逃跑
                            //随从受伤
                            Global.code.uiCombat.AddRollHint(companion.name + ":Help me!", Color.red);
                        }
                    //else if(pt < 0){
                    //    Global.code.uiCombat.AddRollHint(__instance.name + " add health :"+pt, Color.red);
                    //}
                }
            }

        }
        */
        [HarmonyPatch(typeof(Global), "CommandGoThereAndStand")]
        private static class Global_CommandGoThereAndStand_Patch
        {
            private static bool Prefix(Global __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return true;
                }
                Debug("CommandGoThereAndStand:0");
                sStatus = CompanionCmd.GoThereAndStand;
                if (Player.code == null || Player.code.transform == null || __instance.friendlies == null || __instance.friendlies.items == null || RM.code == null || RM.code.sndStand == null)
                {
                    return true;
                }
                Debug("CommandGoThereAndStand:1");
                RM.code.PlayOneShot(RM.code.sndStand);
                Debug("CommandGoThereAndStand:1");
                try
                {
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform && transform != Player.code.transform)
                        {
                            Debug("CommandGoThereAndStand:transform");
                            var component = transform.GetComponent<Companion>();
                            var component2 = transform.GetComponent<Monster>();
                            if (component)
                            {
                                Debug("CommandGoThereAndStand:Companion:" + component.name);
                                component.target = null;
                                component.movingToTarget = new GameObject
                                {
                                    transform = { position = new Vector3()
                                    {
                                        x = Player.code.transform.position.x,
                                        y = Player.code.transform.position.y,
                                        z = Player.code.transform.position.z
                                    }
                                }
                                }.transform;
                                component.charge = false;
                                var charactor = component.GetComponent<CharacterCustomization>();
                                if (charactor && !WeaponIsBow(charactor))
                                {
                                    charactor.Block();
                                }
                            }
                            if (component2)
                            {
                                Debug("CommandGoThereAndStand:Monster:" + component.name);
                                component2.target = null;
                                component2.movingToTarget = new GameObject
                                {
                                    transform = {   position = new Vector3()
                                    {
                                        x = Player.code.transform.position.x,
                                        y = Player.code.transform.position.y,
                                        z = Player.code.transform.position.z
                                    }
                                }
                                }.transform;
                                component2.charge = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Error("CommandGoThereAndStand\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
                }
                __instance.uiCombat.AddPrompt(Localization.GetContent("GetThereAndStand"));
                return false;
            }

        }
        [HarmonyPatch(typeof(Global), "CommandCharge")]
        private static class Global_CommandCharge_Patch
        {
            private static void Prefix(Global __instance)
            {
                if (BepInExPlugin.modEnabled.Value && BepInExPlugin.isFixAI.Value)
                {
                    sStatus = CompanionCmd.Charge;
                }
            }
        }

        [HarmonyPatch(typeof(Global), "CommandFollowMe")]
        private static class Global_CommandFollowMe_Patch
        {
            private static void Prefix(Global __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return;
                }
                sStatus = CompanionCmd.FollowMe;
                if (__instance.friendlies == null)
                {
                    return;
                }
                foreach (Transform transform in __instance.friendlies.items)
                {
                    if (transform && transform != Player.code.transform)
                    {
                        Companion component = transform.GetComponent<Companion>();
                        Monster component2 = transform.GetComponent<Monster>();
                        if (component)
                        {
                            component.target = null;
                        }
                        if (component2)
                        {
                            component2.target = null;
                        }
                    }
                }
            }
        }

        static MethodInfo EnemyFarRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyFar");
        static MethodInfo EnemyCloseRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyClose");
        static MethodInfo EnemyInAttackDistRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyInAttackDist");
        [HarmonyPatch(typeof(Companion), "CS")]
        private static class Companion_CS_Patch
        {

            private static void SetDestination(Companion __instance, Vector3 dest)
            {
                var agent = __instance.GetComponent<NavMeshAgent>();
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(dest);
                }
            }
            //EnemyInAttackDist
            private static bool Prefix(Companion __instance)
            {
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                if (!BepInExPlugin.isFixAI.Value)
                {
                    return true;
                }
                if (__instance.gameObject.tag == "D")
                {
                    return true;
                }
                if (Global.code.curlocation && Global.code.curlocation.locationType == LocationType.home && !__instance.customization.interactingObject)
                {
                    return true;
                }
                try
                {
                    var charactor = __instance.GetComponent<CharacterCustomization>();
                    __instance.attackDist = 3f;
                    if (__instance.customization.weaponInHand)
                    {
                        switch (__instance.customization.weaponInHand.GetComponent<Weapon>().weaponType)
                        {
                            case WeaponType.onehand:
                                __instance.attackDist = 3f;
                                break;
                            case WeaponType.twohand:
                                __instance.attackDist = 4f;
                                break;
                            case WeaponType.spear:
                                __instance.attackDist = 5f;
                                break;
                            case WeaponType.twohandaxe:
                                __instance.attackDist = 4f;
                                break;
                            case WeaponType.onehandaxe:
                                __instance.attackDist = 3f;
                                break;
                            case WeaponType.bow:
                                __instance.attackDist = 20f;
                                break;
                            case WeaponType.dagger:
                                __instance.attackDist = 2.5f;
                                break;
                            case WeaponType.onehandhammer:
                                __instance.attackDist = 3f;
                                break;
                        }
                    }
                    if (__instance._ID.isFriendly)
                    {
                        __instance.target = Utility.GetNearestObject(Global.code.enemies.items, __instance.transform);
                    }
                    else
                    {
                        __instance.target = Utility.GetNearestObject(Global.code.friendlies.items, __instance.transform);
                    }
                    if (__instance.target)
                    {
                        if (!__instance.customization.weaponInHand)
                        {
                            //优先近战，然后是弓箭
                            if (!ChangeMeleeWeapon(charactor))
                            {
                                ChangeBowWeapon(charactor);
                            }
                        }
                        else if (__instance.customization.anim.runtimeAnimatorController == RM.code.unarmedController)
                        {
                            //优先近战，然后是弓箭
                            if (!ChangeMeleeWeapon(charactor))
                            {
                                ChangeBowWeapon(charactor);
                            }
                        }
                        if (__instance._ID.health <= 0f)
                        {
                            return false;
                        }
                        if (__instance.target && __instance.target.tag != "D")
                        {
                            __instance.curEnemyDist = Vector3.Distance(__instance.target.position, __instance.myTransform.position);

                            if (__instance.curEnemyDist > __instance.attackDist && __instance.curEnemyDist < BOW_ATTACK_LIMIT && charactor && charactor.storage.GetItemCount("Arrow") > 0)
                            {
                                //大于攻击范围，但是有弓箭
                                int bowIndex = ChangeBowWeapon(charactor);
                                if (bowIndex > 0)
                                {
                                    Debug(charactor.characterName + " change weapon to bow:" + bowIndex + ", distance=" + __instance.curEnemyDist);
                                }
                            }

                            if (__instance.curEnemyDist <= __instance.attackDist)
                            {
                                bool changed = false;
                                if (charactor)
                                {
                                    var weapon = charactor.weaponIndex == 1 ? charactor.weapon.GetComponent<Weapon>() : (charactor.weaponIndex == 2 ? charactor.weapon2.GetComponent<Weapon>() : null);
                                    if (weapon && weapon.weaponType == WeaponType.bow)
                                    {
                                        if (__instance.curEnemyDist <= 5f)
                                        {
                                            Debug(charactor.characterName + " change bow to other weapon");
                                            //距离太近了，切换武器
                                            changed = ChangeMeleeWeapon(charactor);
                                        }
                                    }
                                }
                                if (!changed)
                                {
                                    //在攻击范围
                                    if (charactor)
                                    {
                                        charactor.Unblock();
                                    }
                                    EnemyInAttackDistRef.Invoke(__instance, new object[0]);
                                    return false;
                                }
                            }
                        }
                        if (sStatus == CompanionCmd.GoThereAndStand || sStatus == CompanionCmd.FollowMe)
                        {
                            //防守模式
                            if (__instance.curEnemyDist >= __instance.attackDist * 1.5f)
                            {
                                //  BepInExPlugin.Debug("status is GoThereAndStand, curEnemyDist=" + __instance.curEnemyDist + ", attackDist=" + __instance.attackDist, true);
                                //敌人太远了
                                __instance.target = null;
                                EnemyFarRef.Invoke(__instance, new object[0]);
                                if (charactor)
                                {
                                    if (__instance.movingToTarget)
                                    {
                                        if (sStatus != CompanionCmd.FollowMe && !WeaponIsBow(charactor))
                                        {
                                            //取消防御
                                            charactor.Unblock();
                                        }
                                        //随机移动
                                        RandomMove(__instance, __instance.movingToTarget.position);
                                    }
                                    if (sStatus != CompanionCmd.FollowMe && !WeaponIsBow(charactor))
                                    {
                                        charactor.Block();
                                    }
                                }
                                return false;
                            }
                        }
                        if (__instance.curEnemyDist > BOW_ATTACK_LIMIT)
                        {
                            EnemyFarRef.Invoke(__instance, new object[0]);
                        }
                        else
                        {
                            EnemyCloseRef.Invoke(__instance, new object[0]);
                        }
                        if (__instance.target.tag == "D")
                        {
                            __instance.target = null;
                            return false;
                        }
                    }
                    else
                    {
                        if (__instance.customization.weaponInHand && UnityEngine.Random.Range(0, 100) < 10)
                        {
                            __instance.customization.HolsterWeapon();
                        }
                        if (__instance.movingToTarget)
                        {
                            SetDestination(__instance, __instance.movingToTarget.position);
                            __instance.curEnemyDist = 100f;
                        }
                        else
                        {
                            __instance.curEnemyDist = 100f;
                            __instance.Stop();
                        }
                        if (__instance.customization.isPullingBow)
                        {
                            __instance.customization.CancelPullBow();
                        }
                    }
                }
                catch (Exception e)
                {
                    Error("CS\n" + e.Message + "\n" + e.StackTrace + "\n" + e.Source);
                    return true;
                }
                return false;
            }

            private static void RandomMove(Companion companion, Vector3 loc)
            {
                //TODO
            }
        }
    }
}

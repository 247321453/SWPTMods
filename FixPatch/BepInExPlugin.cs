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

namespace FixPatch
{
    [BepInPlugin("caicai.FixPatch", "Unofficial Patch", "0.0.5")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> isFixAffixes;

        public static ConfigEntry<bool> isFixBugs;

        public static ConfigEntry<bool> isFixAI;

        public static ConfigEntry<bool> elementDamageEnable;

        public static ConfigEntry<float> elementDamageRate;

        public static ConfigEntry<bool> replaceLocalizetionText;

        private enum CompanionCmd
        {
            FollowMe,
            Charge,
            GoThereAndStand,
        }
        private static CompanionCmd sStatus = CompanionCmd.FollowMe;

        public static void Dbgl(string str = "", bool pref = true)
        {
            bool value = BepInExPlugin.isDebug.Value;
            if (value)
            {
                Debug.Log((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
            }
        }
        public static void Error(string str = "", bool pref = true)
        {
            Debug.LogError((pref ? (typeof(BepInExPlugin).Namespace + " ") : "") + str);
        }

        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 29, "Nexus mod ID for updates");

            BepInExPlugin.elementDamageEnable = base.Config.Bind<bool>("Options", "ElementDamageEnable", true, "Enable elemental damage of weapon");
            BepInExPlugin.elementDamageRate = base.Config.Bind<float>("Options", "ElementDamageRate", 0.1f, "magic attributes can increase the elemental damage of weapons");
            BepInExPlugin.isFixAffixes = base.Config.Bind<bool>("Options", "IsFixAffixes", true, "fix weapon and armor 's affixes.");
            BepInExPlugin.isFixBugs = base.Config.Bind<bool>("Options", "IsFixBugs", true, "fix some bugs.");
            BepInExPlugin.isFixAI = base.Config.Bind<bool>("Options", "isFixAI", true, "fix compation's ai.");
            BepInExPlugin.replaceLocalizetionText = base.Config.Bind<bool>("Options", "ReplaceLocalizetionText", true, "replace localizetion text.");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
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
        static AccessTools.FieldRef<ThirdPersonCharacter, Rigidbody> m_Rigidbody =
     AccessTools.FieldRefAccess<ThirdPersonCharacter, Rigidbody>("m_Rigidbody");
        static AccessTools.FieldRef<ThirdPersonCharacter, Animator> m_Animator =
AccessTools.FieldRefAccess<ThirdPersonCharacter, Animator>("m_Animator");

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

        [HarmonyPatch(typeof(Weapon), "DealDamage")]
        private static class Weapon_DealDamage_Patch
        {

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

            private static void Prefix(Weapon __instance, Bodypart bodypart, float multiplier, bool playStaggerAnimation)
            {
                if (BepInExPlugin.isFixAffixes.Value && BepInExPlugin.elementDamageEnable.Value)
                {
                    Item item = __instance.GetComponent<Item>();
                    if (item)
                    {
                        ID component = item.owner.GetComponent<ID>();
                        ID component2 = bodypart.root.GetComponent<ID>();
                        float old = component.damage;
                        float addDamage = GetAddDamage(item, component, component2);
                        if (addDamage > 0)
                        {
                            addDamage *= multiplier;
                            component.damage += addDamage;
                        }
                        BepInExPlugin.Dbgl("before DealDamage" + item.owner.name + "'s damage=" + old + "->" + component.damage, true);
                    }
                }
            }
            private static void Postfix(Weapon __instance, Bodypart bodypart, float multiplier, bool playStaggerAnimation)
            {
                if (BepInExPlugin.isFixAffixes.Value)
                {
                    Item item = __instance.GetComponent<Item>();
                    if (item)
                    {
                        ID component = item.owner.GetComponent<ID>();
                        ID component2 = bodypart.root.GetComponent<ID>();
                        float old = component.damage;
                        float addDamage = GetAddDamage(item, component, component2);
                        if (addDamage > 0)
                        {
                            addDamage *= multiplier;
                            component.damage -= addDamage;
                        }
                        BepInExPlugin.Dbgl("after DealDamage " + item.owner.name + "'s damage=" + old + "->" + component.damage, true);
                    }
                }
            }
        }
        #endregion

        #region AI
        [HarmonyPatch(typeof(ID), "AddHealth")]
        private static class ID_AddHealth_Patch
        {

            private static void Postfix(ID __instance)
            {
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
                if (__instance.damageSource)
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
                    if (companion.charge)
                    {
                        companion.target = __instance.damageSource;
                        BepInExPlugin.Dbgl(companion.name + " change target", true);
                    }
                    if (__instance.health > 0 && (__instance.health / __instance.maxHealth) < 0.1f)
                    {
                        //TODO 逃跑
                        //随从受伤
                        Global.code.uiCombat.AddRollHint(companion.name + ":Help me!", Color.red);
                    }
                }
            }

        }

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
                sStatus = CompanionCmd.GoThereAndStand;
                if (Player.code == null || Player.code.transform == null || __instance.friendlies == null)
                {
                    return true;
                }
                RM.code.PlayOneShot(RM.code.sndStand);
                try
                {
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform && transform != Player.code.transform)
                        {
                            Dbgl("CommandGoThereAndStand:transform");
                            var component = transform.GetComponent<Companion>();
                            var component2 = transform.GetComponent<Monster>();
                            if (component)
                            {
                                Dbgl("CommandGoThereAndStand:Companion:"+component.name);
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
                                if (charactor)
                                {
                                    charactor.Block();
                                }
                            }
                            if (component2)
                            {
                                Dbgl("CommandGoThereAndStand:Monster:" + component.name);
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
                    Error("CommandGoThereAndStand\n" + e.Message + "\n" + e.StackTrace+"\n"+e.Source);
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
                if (BepInExPlugin.modEnabled.Value)
                {
                    if (!BepInExPlugin.isFixAI.Value)
                    {
                        return;
                    }
                    sStatus = CompanionCmd.Charge;
                }
            }
        }

        [HarmonyPatch(typeof(Global), "CommandFollowMe")]
        private static class Global_CommandFollowMe_Patch
        {
            private static void Prefix(Global __instance)
            {
                if (BepInExPlugin.modEnabled.Value)
                {
                    if (!BepInExPlugin.isFixAI.Value)
                    {
                        return;
                    }
                    sStatus = CompanionCmd.FollowMe;
                    if (__instance.friendlies == null) {
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
        }


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
            static MethodInfo EnemyFarRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyFar");
            static MethodInfo EnemyCloseRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyClose");
            static MethodInfo EnemyInAttackDistRef = AccessTools.DeclaredMethod(typeof(Companion), "EnemyInAttackDist");
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
                                __instance.attackDist = 15f;
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
                            if (__instance.customization.weapon)
                            {
                                __instance.customization.DrawWeapon(1);
                            }
                            else if (__instance.customization.weapon2)
                            {
                                __instance.customization.DrawWeapon(2);
                            }
                        }
                        else if (__instance.customization.anim.runtimeAnimatorController == RM.code.unarmedController)
                        {
                            if (__instance.customization.weapon)
                            {
                                __instance.customization.DrawWeapon(1);
                            }
                            else if (__instance.customization.weapon2)
                            {
                                __instance.customization.DrawWeapon(2);
                            }
                        }
                        if (__instance._ID.health <= 0f)
                        {
                            return false;
                        }
                        if (__instance.target && __instance.target.tag != "D")
                        {
                            __instance.curEnemyDist = Vector3.Distance(__instance.target.position, __instance.myTransform.position);
                            if (__instance.curEnemyDist <= __instance.attackDist)
                            {
                                var charactor = __instance.GetComponent<CharacterCustomization>();
                                if (charactor)
                                {
                                    charactor.Unblock();
                                }
                                EnemyInAttackDistRef.Invoke(__instance, new object[0]);
                                return false;
                            }
                        }
                        if (sStatus == CompanionCmd.GoThereAndStand || sStatus == CompanionCmd.FollowMe)
                        {
                            //防守模式
                            if (__instance.curEnemyDist >= __instance.attackDist)
                            {
                                BepInExPlugin.Dbgl("status is GoThereAndStand, curEnemyDist=" + __instance.curEnemyDist + ", attackDist=" + __instance.attackDist, true);
                                //敌人太远了
                                __instance.target = null;
                                EnemyFarRef.Invoke(__instance, new object[0]);
                                var charactor = __instance.GetComponent<CharacterCustomization>();
                                if (charactor)
                                {
                                    if (__instance.movingToTarget)
                                    {
                                        if (sStatus != CompanionCmd.FollowMe)
                                        {
                                            //取消防御
                                            charactor.Unblock();
                                        }
                                        //随机移动
                                        RandomMove(__instance, __instance.movingToTarget.position);
                                    }
                                    if (sStatus != CompanionCmd.FollowMe)
                                    {
                                        charactor.Block();
                                    }
                                }
                                return false;
                            }
                        }
                        if (__instance.curEnemyDist > 15f)
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
                    Error("CS\n" + e.Message + "\n" + e.StackTrace+"\n"+e.Source);
                    return true;
                }
                return false;
            }

            private static void RandomMove(Companion companion, Vector3 loc)
            {
                //TODO
            }
        }
        #endregion

        #region language

        private static bool sInit = false;
        private static Dictionary<string, List<string>> LocalizationDic = new Dictionary<string, List<string>>();

        private static void InitLocalizetionText()
        {
            if (sInit)
            {
                return;
            }
            sInit = true;
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Localization.txt");
            BepInExPlugin.Dbgl("read " + path, true);
            Dictionary<string, Table_Localization> LocalizationData = new Dictionary<string, Table_Localization>();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    LocalizationData = TableManager.DeserializeStringTODIc<string, Table_Localization>(json);
                }
            }
            catch
            {
                BepInExPlugin.Dbgl("read Localization error:" + path);
                return;
            }
            if (LocalizationData == null)
            {
                BepInExPlugin.Dbgl("read Localization to json error:" + path);
                return;
            }
            BepInExPlugin.Dbgl("read localization success!", true);
            foreach (KeyValuePair<string, Table_Localization> keyValuePair in LocalizationData)
            {
                LocalizationDic.Add(keyValuePair.Key, new List<string>
            {
                keyValuePair.Value.ENGLISH,
                keyValuePair.Value.CHINESE,
                keyValuePair.Value.RUSSIAN
            });
            }
        }

        [HarmonyPatch(typeof(Localization), "InitLocalization")]
        private static class Localization_InitLocalization_Patch
        {
            private static void Postfix()
            {
                if (BepInExPlugin.modEnabled.Value)
                {
                    InitLocalizetionText();
                }
            }
        }
        private static string GetContentLocal(string _KEY, object[] pars)
        {
            List<string> list;
            if (LocalizationDic.TryGetValue(_KEY, out list))
            {
                return GetContentLocal(list, _KEY, pars);
            }
            //BepInExPlugin.Dbgl("miss localization:" + _KEY, true);
            return _KEY;
        }

        private static string GetContentLocal(List<string> list, string _KEY, object[] pars)
        {
            string text = list[(int)Localization.CurLanguage];
            if (pars == null || pars.Length == 0)
            {
                return text;
            }
            string[] array = text.Split(new char[]
            {
            '@'
            });
            if (array.Length > 1)
            {
                text = "";
                for (int i = 0; i < array.Length - 1; i++)
                {
                    text += array[i];
                    if (i < pars.Length && pars[i] != null)
                    {
                        text = text + " " + GetContentLocal(pars[i].ToString(), null) + " ";
                    }
                }
                text += array[array.Length - 1];
            }
            return text;
        }

        [HarmonyPatch(typeof(Localization), "GetContent")]
        private static class Localization_GetContent_Patch
        {
            private static bool Prefix(string _KEY, object[] pars, ref string __result)
            {
                if (BepInExPlugin.modEnabled.Value)
                {
                    List<string> list;
                    if (LocalizationDic.TryGetValue(_KEY, out list))
                    {
                        __result = GetContentLocal(list, _KEY, pars);
                        return false;
                    }
                    //BepInExPlugin.Dbgl("miss localization:" + _KEY, true);
                    //按照原始读法
                    return true;
                }
                return true;
            }
        }
        #endregion
    }
}

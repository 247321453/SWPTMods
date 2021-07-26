using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace FixPatch
{
    [BepInPlugin("caicai.FixPatch", "AI Patch", "0.0.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<bool> isDebug;

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

        // 在插件启动时会直接调用Awake()方法
        private void Awake()
        {
            BepInExPlugin.context = this;
            BepInExPlugin.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            BepInExPlugin.isDebug = base.Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Dbgl("Plugin awake", true);
        }

        #region old
        // 在所有插件全部启动完成后会调用Start()方法，执行顺序在Awake()后面；
        //    private void Start()
        //    {
        //        //Debug.Log("这里是Start()方法中的内容!");
        //    }
        // 插件启动后会一直循环执行Update()方法，可用于监听事件或判断键盘按键，执行顺序在Start()后面
        //  private void Update()
        //   {
        // var key = new BepInEx.Configuration.KeyboardShortcut(KeyCode.F9);
        // if (key.IsDown())
        //{
        //     Debug.Log("这里是Updatet()方法中的内容，你看到这条消息是因为你按下了F9");
        // }
        //  }
        // 在插件关闭时会调用OnDestroy()方法
        // private void OnDestroy()
        //{
        //     //Debug.Log("当你看到这条消息时，就表示我已经被关闭一次了!");
        // }
        #endregion
        /*     [HarmonyPatch(typeof(ID), "AddExp")]
             private static class ID_AddExp_Patch
             {
                 private static void Prefix(ID __instance, int exp)
                 {
                     Dbgl(__instance.name + ".AddExp(" + exp+")");
                 }
             }
        */
        [HarmonyPatch(typeof(ID), "AddHealth")]
        private static class ID_AddHealth_Patch
        {

            private static void Postfix(ID __instance)
            {
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
                        Global.code.uiCombat.AddPrompt(companion.name + ":Help me!");
                    }
                }
            }

        }

        [HarmonyPatch(typeof(Global), "CommandGoThereAndStand")]
        private static class Global_CommandGoThereAndStand_Patch
        {
            private static bool Prefix(Global __instance)
            {
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
                {
                    sStatus = CompanionCmd.GoThereAndStand;
                    RM.code.PlayOneShot(RM.code.sndStand);
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform)
                        {
                            var ID = transform.GetComponent<ID>();
                            if (ID && ID.health > 0)
                            {
                                var player_loc = new Vector3()
                                {
                                    x = Player.code.transform.position.x,
                                    y = Player.code.transform.position.y,
                                    z = Player.code.transform.position.z
                                };
                                var component = transform.GetComponent<Companion>();
                                var component2 = transform.GetComponent<Monster>();
                                if (component)
                                {
                                    component.target = null;
                                    component.movingToTarget = new GameObject
                                    {
                                        transform = {
                                        position = player_loc
                                    }
                                    }.transform;
                                    component.charge = false;
                                }
                                if (component2)
                                {
                                    component2.target = null;
                                    component2.movingToTarget = new GameObject
                                    {
                                        transform = {
                                        position = player_loc
                                    }
                                    }.transform;
                                    component2.charge = false;
                                }
                                var charactor = transform.GetComponent<CharacterCustomization>();
                                if (charactor)
                                {
                                    charactor.Block();
                                }
                            }
                        }
                    }
                    __instance.uiCombat.AddPrompt(Localization.GetContent("GetThereAndStand"));
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Global), "CommandCharge")]
        private static class Global_CommandCharge_Patch
        {
            private static void Prefix(Global __instance)
            {
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
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
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
                {
                    sStatus = CompanionCmd.FollowMe;
                    foreach (Transform transform in __instance.friendlies.items)
                    {
                        if (transform)
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
                bool flag = !BepInExPlugin.modEnabled.Value;
                if (!flag)
                {
                    if (__instance.gameObject.tag == "D")
                    {
                        return true;
                    }
                    if (Global.code.curlocation && Global.code.curlocation.locationType == LocationType.home && !__instance.customization.interactingObject)
                    {
                        return true;
                    }
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
                                Debug.Log("status is GoThereAndStand, curEnemyDist=" + __instance.curEnemyDist + ", attackDist=" + __instance.attackDist);
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
                    return false;
                }
                return true;
            }

            private static void RandomMove(Companion companion, Vector3 loc)
            {

            }
        }
    }
}

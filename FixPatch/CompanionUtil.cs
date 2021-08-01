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
    class CompanionUtil
    {

        public static float GetAttackDist(Companion __instance) {
            float attackDist = 3f;
            if (__instance.customization.weaponInHand)
            {
                switch (__instance.customization.weaponInHand.GetComponent<Weapon>().weaponType)
                {
                    case WeaponType.onehand:
                        attackDist = 3f;
                        break;
                    case WeaponType.twohand:
                        attackDist = 4f;
                        break;
                    case WeaponType.spear:
                        attackDist = 5f;
                        break;
                    case WeaponType.twohandaxe:
                        attackDist = 4f;
                        break;
                    case WeaponType.onehandaxe:
                        attackDist = 3f;
                        break;
                    case WeaponType.bow:
                        attackDist = 20f;
                        break;
                    case WeaponType.dagger:
                        attackDist = 2.5f;
                        break;
                    case WeaponType.onehandhammer:
                        attackDist = 3f;
                        break;
                }
            }
            return attackDist;
        }
        /// <summary>
        /// 攻击在范围内的敌人
        /// </summary>
        public static void EnemyInAttackDist(Companion __instance)
        {
            if (__instance.customization.curCastingMagic)
            {
                //施法中
                return;
            }
            if (!__instance.customization.weaponInHand)
            {
                //手里没武器
                return;
            }
            if (__instance.customization.weaponInHand.GetComponent<Weapon>().weaponType == WeaponType.bow)
            {
                //当前是弓
                if (!__instance.customization.isPullingBow && __instance.targetVisible)
                {
                    __instance.customization.PullBow();
                    __instance.CancelInvoke("SetFireArrow");
                    __instance.Invoke("SetFireArrow", UnityEngine.Random.Range(2.5f, 4f));
                    return;
                }
            }
            else if (!__instance.customization.perrying)
            {
                if (UnityEngine.Random.Range(0, 100) < 95)
                {
                    __instance.customization.Attack();
                    return;
                }
                __instance.customization.PowerAttack();
            }
        }

        //移动位置
        public static void SetDestination(Companion __instance, Vector3 dest)
        {
            var agent = __instance.GetComponent<NavMeshAgent>();
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(dest);
            }
        }
        /// <summary>
        /// 靠近敌人
        /// </summary>
        public static void EnemyFar(Companion __instance)
        {
            if (!__instance.charge && __instance.movingToTarget)
            {
                float distance = CharacterCustomizationUtil.WeaponIsBow(__instance.customization) ?
                    CharacterCustomizationUtil.MIN_BOW_ATTACK_DISTANCE : CharacterCustomizationUtil.MIN_MELEE_ATTACK_DISTANCE;
                if (Vector3.Distance(__instance.movingToTarget.position, __instance.myTransform.position) > distance)
                {
                    SetDestination(__instance, __instance.movingToTarget.position);
                }
                else
                {
                    __instance.Stop();
                }
            }
            else if (__instance.charge)
            {
                if (__instance.curEnemyDist > __instance.attackDist)
                {
                    SetDestination(__instance, __instance.target.position);
                }
                else
                {
                    __instance.Stop();
                }
            }
            else
            {
                __instance.Stop();
            }
            __instance.customization.Unblock();
        }
        /// <summary>
        /// 远离敌人
        /// </summary>
        public static void EnemyClose(Companion __instance)
        {
            if (__instance.curEnemyDist > __instance.attackDist)
            {
                SetDestination(__instance, __instance.target.position);
                __instance.customization.Unblock();
                return;
            }
            if (!CharacterCustomizationUtil.WeaponIsBow(__instance.customization))
            {
                __instance.Stop();
            }
            else if (__instance.curEnemyDist < CharacterCustomizationUtil.MIN_BOW_ATTACK_DISTANCE)
            {
                Backoff(__instance);
            }
            else if (__instance.movingToTarget)
            {
                SetDestination(__instance, __instance.movingToTarget.position);
            }
            if (!CharacterCustomizationUtil.WeaponIsBow(__instance.customization))
            {
                if (UnityEngine.Random.Range(0, 100) < 20 && __instance.target.GetComponent<Animator>().GetBool("Attack"))
                {
                    __instance.customization.Block();
                    return;
                }
            }
            __instance.customization.Unblock();
        }
        /// <summary>
        /// 后退
        /// </summary>
        public static void Backoff(Companion __instance)
        {
            Vector3 a = __instance.transform.position - __instance.target.position;
            Vector3 a2 = __instance.transform.position + a * 3.5f;
            SetDestination(__instance, a2 + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f)));
        }
        /// <summary>
        /// 随机移动
        /// </summary>
        public static void RandomMove(Companion __instance, Vector3 loc)
        {
            if (loc == null)
            {
                loc = __instance.transform.position;
            }
            //随机移动
            Vector3 a2 = loc;
            SetDestination(__instance, a2 + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f)));
        }
    }
}

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace FixPatch
{

    [HarmonyPatch(typeof(RM), "LoadResources")]
    class RM_LoadResources_Patch
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
    }

}

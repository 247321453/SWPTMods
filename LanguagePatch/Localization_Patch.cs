using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LanguagePatch
{

    class Localization_Patch {

        public static int sMaxID;
        private static bool sInit = false;
        public static Dictionary<string, List<string>> LocalizationDic = new Dictionary<string, List<string>>();

        public static void InitLocalizetionText(bool force = false)
        {
            if (!force)
            {
                if (sInit)
                {
                    return;
                }
            }
            sInit = true;
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Localization.txt");
            BepInExPlugin.Debug("read " + path, true);
            Dictionary<string, Table_Localization> LocalizationData = new Dictionary<string, Table_Localization>();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    LocalizationData = TableManager.DeserializeStringTODIc<string, Table_Localization>(json);
                }
            }
            catch (Exception e)
            {
                BepInExPlugin.Error("read Localization error:" + path);
                BepInExPlugin.Error(e.Message);
                BepInExPlugin.Error(e.StackTrace);
                return;
            }
            if (LocalizationData == null)
            {
                BepInExPlugin.Debug("read Localization to json error:" + path);
                return;
            }
            BepInExPlugin.Debug("read localization success!", true);
            if (force)
            {
                LocalizationDic.Clear();
            }
            sMaxID = 0;
            foreach (KeyValuePair<string, Table_Localization> keyValuePair in LocalizationData)
            {
                string key = keyValuePair.Key.Trim();
                sMaxID = Math.Max(sMaxID, keyValuePair.Value.ID);
                if (!LocalizationDic.ContainsKey(key))
                {
                    LocalizationDic.Add(key, new List<string>
                    {
                        keyValuePair.Value.ENGLISH,
                        keyValuePair.Value.CHINESE,
                        keyValuePair.Value.RUSSIAN
                    });
                }
                else
                {
                    BepInExPlugin.Debug("exist key:" + keyValuePair.Key);
                }
            }
        }

        public static string GetContentLocal(string _KEY, object[] pars)
        {
            List<string> list;
            int sp_index = _KEY.IndexOf('(');
            if (LocalizationDic.TryGetValue(_KEY.Trim(), out list))
            {
                return GetContentLocal(list, pars);
            }
            if (sp_index > 0)
            {
                string key = _KEY.Substring(0, sp_index);
                if (LocalizationDic.TryGetValue(key.Trim(), out list))
                {
                    return GetContentLocal(list, pars) + _KEY.Substring(sp_index);
                }
            }
            return _KEY;
        }

        public static string GetContentLocal(List<string> list, object[] pars)
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

    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.InitLocalization))]
    class Localization_InitLocalization_Patch
    {
        private static void Postfix()
        {
            if (BepInExPlugin.modEnabled.Value)
            {
                Localization_Patch.InitLocalizetionText();
            }
        }
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.GetContent))]
    class Localization_GetContent_Patch
    {
        private static bool Prefix(string _KEY, object[] pars, ref string __result)
        {
            if (!BepInExPlugin.modEnabled.Value)
            {
                return true;
            }
            List<string> list;
            if (Localization_Patch.LocalizationDic.TryGetValue(_KEY.Trim(), out list))
            {
                __result = Localization_Patch.GetContentLocal(list, pars);
                return false;
            }
            //按照原始读法
            return true;
        }
    }
}

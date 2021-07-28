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
    [BepInPlugin("caicai.LanguagePatch", "Language Patch", "0.0.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> replaceLocalizetionText;

        public static ConfigEntry<bool> LocalizetionHotkeyEnable;

        public static ConfigEntry<KeyCode> reloadLocalizetionHotKey;

        public static ConfigEntry<KeyCode> saveMissLocalizetionHotKey;

        private static int sMaxID;

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
            BepInExPlugin.nexusID = Config.Bind<int>("General", "NexusID", 35, "Nexus mod ID for updates");

            BepInExPlugin.replaceLocalizetionText = base.Config.Bind<bool>("Options", "ReplaceLocalizetionText", true, "replace localizetion text.");
            BepInExPlugin.LocalizetionHotkeyEnable = base.Config.Bind<bool>("Options", "LocalizetionHotkeyEnable", false, "enable hot key about localizetion.");
            BepInExPlugin.reloadLocalizetionHotKey = base.Config.Bind<KeyCode>("Options", "ReloadLocalizetionHotKey", KeyCode.L, "left Ctrl + hotkey to reload localizetion txt");
            BepInExPlugin.saveMissLocalizetionHotKey = base.Config.Bind<KeyCode>("Options", "SaveMissLocalizetionHotKey", KeyCode.D, "left Ctrl + hotkey to save miss_localizetion txt");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            BepInExPlugin.Debug("Plugin awake", true);
        }

        private void Update()
        {
            if (BepInExPlugin.LocalizetionHotkeyEnable.Value)
            {
                var key = new BepInEx.Configuration.KeyboardShortcut(BepInExPlugin.reloadLocalizetionHotKey.Value, KeyCode.LeftControl);
                if (key.IsDown())
                {
                    //
                    InitLocalizetionText(true);
                    if (Global.code != null && Global.code.uiCombat != null)
                    {
                        Global.code.uiCombat.AddRollHint("Reload Localizetion.txt", Color.white);
                    }
                }
                key = new BepInEx.Configuration.KeyboardShortcut(KeyCode.D, KeyCode.LeftControl);
                if (key.IsDown())
                {
                    SaveUnknownKeys();
                }
            }
        }

        private void SaveUnknownKeys() {
            //
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "miss_Localization.txt");
            List<string> missTextKeys = new List<string>();
            List<string> lines = new List<string>();
            //  "Twitch Dagger": {
            //      "ID": 80072,
            //   "KEY": "Twitch Dagger",
            //    "CHINESE": "扭曲匕首",
            //    "ENGLISH": "Twitch Dagger",
            //    "RUSSIAN": "Twitch Dagger"
            //  },
            foreach (var k in RM.code.allWeapons.items)
            {
                string name = k.GetComponent<Item>().name;
                if (!LocalizationDic.ContainsKey(name))
                {
                    if (!missTextKeys.Contains(name))
                    {
                        missTextKeys.Add(name);
                    }
                }
            }
            foreach (var k in RM.code.allAffixes.items)
            {
                string name = k.GetComponent<Item>().name;
                if (!LocalizationDic.ContainsKey(name))
                {
                    if (!missTextKeys.Contains(name))
                    {
                        missTextKeys.Add(name);
                    }
                }
            }
            foreach (var k in RM.code.allEnemies.items)
            {
                string name = k.GetComponent<Item>().name;
                if (!LocalizationDic.ContainsKey(name))
                {
                    if (!missTextKeys.Contains(name))
                    {
                        missTextKeys.Add(name);
                    }
                }
            }
            foreach (var k in RM.code.allArmors.items)
            {
                string name = k.GetComponent<Item>().name;
                if (!LocalizationDic.ContainsKey(name))
                {
                    if (!missTextKeys.Contains(name))
                    {
                        missTextKeys.Add(name);
                    }
                }
            }
            foreach (var k in RM.code.allLingeries.items)
            {
                string name = k.GetComponent<Item>().name;
                if (!LocalizationDic.ContainsKey(name))
                {
                    if (!missTextKeys.Contains(name))
                    {
                        missTextKeys.Add(name);
                    }
                }
            }
            int count = missTextKeys.Count;
            sMaxID++;
            foreach (string k in missTextKeys)
            {
                if (IsNumber(k))
                {
                    continue;
                }
                string name = k.Replace("_", " ").Trim();
                lines.Add("  \"" + k + "\": {");
                lines.Add("    \"ID\": " + (sMaxID++) + ",");
                lines.Add("    \"KEY\": \"" + k + "\",");
                lines.Add("    \"CHINESE\": \"" + name + "\",");
                lines.Add("    \"ENGLISH\": \"" + name + "\",");
                lines.Add("    \"RUSSIAN\": \"" + name + "\",");
                lines.Add("  },");
            }
            missTextKeys.Clear();
            File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
            if (Global.code != null && Global.code.uiCombat != null)
            {
                Global.code.uiCombat.AddRollHint("Save " + count + " to miss_Localization.txt", Color.white);
            }
        }

        private static bool IsNumber(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            char[] cs = str.ToCharArray();
            foreach (char c in cs)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return true;
        }

        private static bool sInit = false;
        private static Dictionary<string, List<string>> LocalizationDic = new Dictionary<string, List<string>>();

        private static void InitLocalizetionText(bool force = false)
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
                Error("read Localization error:" + path);
                Error(e.Message);
                Error(e.StackTrace);
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
                sMaxID = Math.Max(sMaxID, keyValuePair.Value.ID);
                if (!LocalizationDic.ContainsKey(keyValuePair.Key))
                {
                    LocalizationDic.Add(keyValuePair.Key, new List<string>
                    {
                        keyValuePair.Value.ENGLISH,
                        keyValuePair.Value.CHINESE,
                        keyValuePair.Value.RUSSIAN
                    });
                }
                else
                {
                    Error("exist key:" + keyValuePair.Key);
                }
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
                if (!BepInExPlugin.modEnabled.Value)
                {
                    return true;
                }
                List<string> list;
                if (LocalizationDic.TryGetValue(_KEY, out list))
                {
                    __result = GetContentLocal(list, _KEY, pars);
                    return false;
                }
                //按照原始读法
                return true;
            }
        }
    }
}

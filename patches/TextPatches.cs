using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using System;
using HarmonyLib;
using System.Reflection;
using GnosiaCustomizer.utils;
using coreSystem;
using gnosia;
using System.Collections.Concurrent;

namespace GnosiaCustomizer.patches
{
    internal class TextPatches
    {
        internal static ManualLogSource Logger;
        private const string ConfigFileName = "config.yaml";

        private static readonly ConcurrentDictionary<int, CharacterText> CharacterTexts = new();

        private static readonly Dictionary<string, string> NameReplacements = new();
        internal static int CurrentSpeakerId = 0;
        private static readonly Dictionary<int, Dictionary<string, string>> NicknamesPerCharacter = new();
        private static int[] localToReal = new int[15];

        private static int MapLocalToReal(int localId)
        {
            if (localId < 0 || localId >= localToReal.Length)
                return -1;
            return localToReal[localId];
        }
        private static readonly List<string> NamesToReplace = new()
        {
            "Gina", "SQ", "Raqio", "Stella", "Shigemichi", "Chipie", "Remnan",
            "Comet", "Yuriko", "Jonas", "Setsu", "Otome", "Sha-Ming", "Kukrushka"
        };
        private const string SqueakPrefix = "SQU";

        internal static void Initialize()
        {
            Logger.LogInfo("LoadCustomText called");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            // Read from each character folder asynchronously
            int characterId = 1;
            var skillMap = new ConcurrentDictionary<int, HashSet<string>>();
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                var charaPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder);
                if (!Directory.Exists(charaPath))
                {
                    Logger.LogInfo($"Could not find path {charaPath}");
                    characterId++;
                    continue;
                }

                var yamlPath = Path.Combine(charaPath, ConfigFileName);
                if (File.Exists(yamlPath))
                {
                    try
                    {
                        var character = new CharacterText();
                        character.LoadFromFile(yamlPath);
                        if (characterId != 0)
                        {
                            CharacterTexts[characterId] = character;
                            if (character.Nicknames != null)
                            {
                                var perChar = new Dictionary<string, string>();
                                foreach (var kv in character.Nicknames)
                                {
                                    perChar[kv.Key] = kv.Value;
                                    Logger?.LogInfo($"Nickname replacement loaded for {characterId}: {kv.Key} → {kv.Value}");
                                }
                                if (characterId >= 0)
                                {
                                    NicknamesPerCharacter[characterId] = perChar;
                                }
                            }
                            skillMap[characterId] = character.KnownSkills;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize {charaFolder}: {ex.Message}");
                    }
                }
                characterId++;
            }

            try
            {
                Logger.LogInfo($"Loaded {CharacterTexts.Count}/{Consts.CharaFolderNames.Length} character configs");
                Logger.LogInfo($"Loaded {skillMap.Count}/{Consts.CharaFolderNames.Length} character skills");
                Logger.LogInfo($"LoadCustomText completed in {sw.ElapsedMilliseconds} ms");

                JinroPatches.Initialize(skillMap);
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    Logger.LogError($"Error loading texture: {inner.Message}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Unexpected error loading textures: {e.Message}");
            }
        }

        internal static void UpdateIdMap()
        {
            try
            {
                var dataType = AccessTools.TypeByName("gnosia.Data");
                var gdField = AccessTools.Field(dataType, "gd");
                var gd = gdField.GetValue(null);
                if (gd == null) return;
                var gdType = gd.GetType();
                var personFromIdField = AccessTools.Field(gdType, "personFromId");
                var personFromIdObj = personFromIdField.GetValue(gd);
                if (personFromIdObj == null) return;

                var personFromId = personFromIdObj as int[];
                if (personFromId == null) return;

                for (int i = 0; i < localToReal.Length; i++)
                    localToReal[i] = -1;

                for (int realId = 0; realId < personFromId.Length; realId++)
                {
                    int localId = personFromId[realId];
                    if (localId >= 0 && localId < localToReal.Length)
                        localToReal[localId] = realId;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UpdateIdMap ERROR] {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(GameData), "MakeLoop")]
        public class MakeLoopPatch
        {
            static void Postfix()
            {
                Logger.LogInfo("[MakeLoop] New loop started. Updating ID Map.");
                UpdateIdMap();
            }
        }

        [HarmonyPatch(typeof(GameData), "GetFromBaseData")]
        public class LoadGamePatch
        {
            static void Postfix()
            {
                Logger.LogInfo("[LoadGamePatch] Game loaded from save. Updating ID Map.");
                UpdateIdMap();
            }
        }

        [HarmonyPatch(typeof(GameData), "Initialize")]
        public class InitializeGameDataPatch
        {
            static void Postfix()
            {
                Logger.LogInfo("[InitializeGameDataPatch] GameData initialized. Updating ID Map.");
                UpdateIdMap();
            }
        }

        [HarmonyPatch]
        public class SetCharaDataPatch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("gnosia.Data");
                return AccessTools.Method(type, "SetKukulsika"); // This is called last
            }

            static void Postfix()
            {
                // Uncomment to re-generate original config for the player
                //GenerateOriginalConfig.WriteCharaDataToFile(0);

                foreach (var absoluteId in Consts.CharaFolderIds)
                {
                    // Uncomment to re-generate the original character config
                    //GenerateOriginalConfig.WriteCharaDataToFile(absoluteId);

                    if (CharacterTexts.TryGetValue(absoluteId, out var character))
                    {
                        CharacterSetter.SetChara(Logger, absoluteId, character);

                        CharacterSetter.GetCharaFieldValueAsString(absoluteId, "name", out var name);
                        Logger.LogInfo($"Character ID {absoluteId} name is now: {name}");
                        NameReplacements[NamesToReplace[absoluteId - 1]] = name;
                    }

                    if (CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, "t_skill_dogeza", out var strArray))
                    {
                        Logger.LogInfo($"Dogeza test: Want 2 lines, got: {strArray.Count}");
                    }
                    else
                    {
                        Logger.LogInfo($"Dogeza test: No lines found for character {absoluteId}.");
                    }
                    if (CharacterSetter.GetCharaFieldAs2dStringArray(absoluteId, "t_personal", out var personalArray))
                    {
                        Logger.LogInfo($"Personal lines test: {personalArray[0][5]}");
                    }
                    else
                    {
                        Logger.LogInfo($"Personal lines test: No personal line found for character {absoluteId}.");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ScriptParser), "SetText")]
        public class ScriptParserSetTextPatch
        {
            static void Prefix(ref string message)
            {
                TextPatches.Logger?.LogInfo("[STP] Prefix entered");
                TextPatches.Logger?.LogInfo($"[STP] Original message: {message}");
                // Resolve speaker from Data.gd
                try
                {
                    var dataType = AccessTools.TypeByName("gnosia.Data");
                    var gdField = AccessTools.Field(dataType, "gd");
                    var gd = gdField.GetValue(null);
                    TextPatches.Logger?.LogInfo($"[STP] gdField: {gdField}");
                    TextPatches.Logger?.LogInfo($"[STP] gd: {gd}");
                    var gdType = gd.GetType();
                    TextPatches.Logger?.LogInfo($"[STP] gdType: {gdType}");

                    var actionDoItField = AccessTools.Field(gdType, "actionDoIt");
                    var actionDoItObj = actionDoItField.GetValue(gd);
                    if (actionDoItObj != null)
                    {
                        var mainPField = AccessTools.Field(actionDoItObj.GetType(), "mainP");
                        int dynamicMainP = (int)mainPField.GetValue(actionDoItObj);
                        TextPatches.CurrentSpeakerId = dynamicMainP;
                        TextPatches.Logger?.LogInfo($"[STP] CurrentSpeakerId (from actionDoIt) = {dynamicMainP}");
                    }
                    else
                    {
                        TextPatches.Logger?.LogInfo("[STP] actionDoIt is null - skipping speaker detection");
                    }
                    TextPatches.Logger?.LogInfo("[STP] Speaker resolution complete");
                }
                catch (Exception ex)
                {
                    TextPatches.Logger?.LogError($"[ScreenProcessor Speaker ERROR]: {ex.Message}");
                }

                TextPatches.Logger?.LogInfo("[STP] Checking for empty or substitution prefix");
                // Empty or substitution prefix
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "...";
                }
                else if (message.StartsWith(CharacterSetter.SubstitutionPrefix))
                {
                    var tokens = message.Split(CharacterSetter.Delimiter);
                    if (tokens.Length > 2)
                    {
                        message = tokens[2];
                        TextPatches.Logger?.LogInfo($"[STP] After substitution prefix: {message}");
                    }
                }

                TextPatches.Logger?.LogInfo("[STP] Starting NameReplacements check");
                // Name replacements
                foreach (var name in NameReplacements.Keys)
                {
                    if (message.Contains(name))
                    {
                        if (name.Equals("SQ") && message.Contains("SQU"))
                            continue;
                        message = message.Replace(name, NameReplacements[name]);
                        TextPatches.Logger?.LogInfo($"[STP] Name replaced: {name} -> {NameReplacements[name]}");
                    }
                }

                TextPatches.Logger?.LogInfo("[STP] Checking NicknamesPerCharacter");
                // Nickname replacements (speaker-dependent)
                int realSpeaker = MapLocalToReal(TextPatches.CurrentSpeakerId);
                if (realSpeaker >= 0 && NicknamesPerCharacter.TryGetValue(realSpeaker, out var dict))
                {
                    foreach (var kv in dict)
                    {
                        if (message.Contains(kv.Key))
                        {
                            message = message.Replace(kv.Key, kv.Value);
                            TextPatches.Logger?.LogInfo($"[STP] Nickname replaced: {kv.Key} -> {kv.Value}");
                        }
                    }
                }
                TextPatches.Logger?.LogInfo($"[STP] Final message: {message}");
                TextPatches.Logger?.LogInfo("[STP] Prefix exit");
            }
        }

        [HarmonyPatch]
        public class SetNormalSerifuPatch
        {
            static MethodBase TargetMethod()
            {
                var t = typeof(ScriptParser);
                foreach (var m in AccessTools.GetDeclaredMethods(t))
                {
                    if (m.Name == "SetNormalSerifu")
                    {
                        var ps = m.GetParameters();
                        if (ps.Length >= 1 && ps[0].ParameterType == typeof(int))
                        {
                            return m; // choose overload whose first arg is int main
                        }
                    }
                }
                return null;
            }

            static void Postfix(int main)
            {
                TextPatches.CurrentSpeakerId = main;
                TextPatches.Logger?.LogInfo($"[Postfix] CurrentSpeakerId set to {main}");
            }
        }

        [HarmonyPatch]
        public class ActionDataDoItPatch
        {
            static MethodBase TargetMethod()
            {
                TextPatches.Logger?.LogInfo("[DoItPatch-Debug] TargetMethod entered");

                var t = AccessTools.TypeByName("gnosia.GameData+actionData");
                TextPatches.Logger?.LogInfo($"[DoItPatch-Debug] Type lookup: {t}");

                if (t == null)
                {
                    TextPatches.Logger?.LogError("[DoItPatch-Debug] FAILED: Type gnosia.GameData+actionData not found");
                    return null;
                }

                var m = AccessTools.Method(t, "DoIt");
                TextPatches.Logger?.LogInfo($"[DoItPatch-Debug] Method lookup: {m}");

                if (m == null)
                {
                    TextPatches.Logger?.LogError("[DoItPatch-Debug] FAILED: Method DoIt not found on gnosia.GameData+actionData");
                }
                else
                {
                    TextPatches.Logger?.LogInfo("[DoItPatch-Debug] SUCCESS: DoIt method found and patched");
                }

                return m;
            }

            static void Postfix(object __instance)
            {
                TextPatches.Logger?.LogInfo("[DoItPatch-Debug] Postfix triggered");
                try
                {
                    var dataType = AccessTools.TypeByName("gnosia.Data");
                    var gdField = AccessTools.Field(dataType, "gd");
                    var gd = gdField.GetValue(null);
                    var gdType = gd.GetType();

                    var actionsDidField = AccessTools.Field(gdType, "actionsDid");
                    var actionsDidObj = actionsDidField.GetValue(gd);
                    var list = actionsDidObj as System.Collections.IList;

                    if (list != null && list.Count > 0)
                    {
                        var last = list[list.Count - 1];
                        var mainPField = AccessTools.Field(last.GetType(), "mainP");
                        int mainP = (int)mainPField.GetValue(last);

                        TextPatches.CurrentSpeakerId = mainP;
                        TextPatches.Logger?.LogInfo($"[DoItPatch] CurrentSpeakerId set to {mainP}");
                    }
                    else
                    {
                        TextPatches.Logger?.LogInfo("[DoItPatch] actionsDid empty");
                    }
                }
                catch (Exception ex)
                {
                    TextPatches.Logger?.LogError($"[DoItPatch ERROR] {ex.Message}");
                }
            }
        }
    }
}

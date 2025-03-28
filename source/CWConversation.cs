using HUD;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using RWCustom;
using System;
using Random = UnityEngine.Random;

namespace CWStuff;

public static class NewSoundID
{
    //vol=0.7
    public static SoundID CW_AI_Talk_1 = new(nameof(CW_AI_Talk_1), true), CW_AI_Talk_2 = new(nameof(CW_AI_Talk_2), true), CW_AI_Angry_1 = new(nameof(CW_AI_Angry_1), true), CW_AI_Angry_2 = new(nameof(CW_AI_Angry_2), true);

    internal static void UnregisterValues()
    {
        CW_AI_Talk_1?.Unregister();
        CW_AI_Talk_1 = null!;
        CW_AI_Talk_2?.Unregister();
        CW_AI_Talk_2 = null!;
        CW_AI_Angry_1?.Unregister();
        CW_AI_Angry_1 = null!;
        CW_AI_Angry_2?.Unregister();
        CW_AI_Angry_2 = null!;
    }
}

public static class ConversationID
{
    public static Conversation.ID SL_CWNeuron = new(nameof(SL_CWNeuron), true), CWSpearPearlAfterMoon = new(nameof(CWSpearPearlAfterMoon), true);
        
    internal static void UnregisterValues()
    {
        SL_CWNeuron?.Unregister();
        SL_CWNeuron = null!;
        CWSpearPearlAfterMoon?.Unregister();
        CWSpearPearlAfterMoon = null!;
    }
}

public class CWPearlConversation(Conversation.ID id, OracleBehavior slOracleBehaviorHasMark, SLOracleBehaviorHasMark.MiscItemType describeItem) : SLOracleBehaviorHasMark.MoonConversation(id, slOracleBehaviorHasMark, describeItem)
{
    public delegate void AddEventsHandler(CWPearlConversation self, ref bool runOriginalCode);

    public bool IntroSaid;
    public static event AddEventsHandler? OnAddEvents;

    public override void AddEvents()
    {
        if (id is not ID locID)
            return;
        var run = true;
        OnAddEvents?.Invoke(this, ref run);
        if (!run)
            return;
        if (CWOracleHooks.WorldSaveData.TryGetValue(myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
            ++data.NumberOfConversations;
        if (locID == ID.Moon_Pearl_Misc)
        {
            CWConversation.CWEventsFromFile(this, locID.value, myBehavior is SSOracleBehavior { inspectPearl: not null }, myBehavior.oracle.room.game.StoryCharacter?.value, true, Random.Range(0, 10000));
            return;
        }
        if (CWConversation.CWEventsFromFile(this, locID.value, myBehavior is SSOracleBehavior { inspectPearl: not null }, myBehavior.oracle.room.game.StoryCharacter?.value))
            return;
        base.AddEvents();
        IntroSaid = false;
    }
}

public class CWConversation(SSOracleBehavior owner, SSOracleBehavior.ConversationBehavior convBehav, string convo, DialogBox dialogBox) : SSOracleBehavior.PebblesConversation(owner, convBehav, ID.None, dialogBox)
{
    public delegate void AddEventsHandler(CWConversation self, ref bool runOriginalCode);

    public delegate void SayGoodLuckMessageHandler(CWConversation self, ref bool result);

    public delegate void BigCreatureHandler(CWConversation self, CreatureTemplate.Type type, ref bool result);

    public string Convo = convo;
    public bool GoodLuckAdded;
    public static event AddEventsHandler? OnAddEvents;
    public static event SayGoodLuckMessageHandler? OnSayGoodLuckMessage;
    public static event BigCreatureHandler? OnBigCreature;

    public override void AddEvents()
    {
        var run = true;
        OnAddEvents?.Invoke(this, ref run);
        if (!run)
            return;
        if (CWOracleHooks.WorldSaveData.TryGetValue(owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
            ++data.NumberOfConversations;
        var flag = false;
        if (Convo.Contains("_GetNeuron"))
        {
            if (!CWEventsFromFile(this, Convo))
                CWEventsFromFile(this, "Red_GetNeuron");
        }
        else if (Convo.Contains("_WithNeuron"))
        {
            if (!CWEventsFromFile(this, Convo))
                CWEventsFromFile(this, "Red_FirstEncounter_WithNeuron");
        }
        else if (owner.playerEnteredWithMark)
        {
            if (!CWEventsFromFile(this, Convo + "_AlreadyHadMark"))
                flag = CWEventsFromFile(this, "White_FirstEncounter_AlreadyHadMark");
        }
        else
        {
            if (!CWEventsFromFile(this, Convo + "_MarkGiven"))
                flag = CWEventsFromFile(this, "White_FirstEncounter_MarkGiven");
        }
        if (flag)
            CWEventsFromFile(this, "FirstEncounter_BestLuck");
    }

    public virtual bool ShouldISaySpecialGoodLuck()
    {
        var res = string.Equals(Convo, "White_FirstEncounter", StringComparison.OrdinalIgnoreCase) || string.Equals(Convo, "Yellow_FirstEncounter", StringComparison.OrdinalIgnoreCase) || string.Equals(Convo, "Gourmand_FirstEncounter", StringComparison.OrdinalIgnoreCase);
        OnSayGoodLuckMessage?.Invoke(this, ref res);
        return res;
    }

    public virtual bool IsThisABigCreature(CreatureTemplate.Type type)
    {
        var res = type == CreatureTemplate.Type.Vulture || type == CreatureTemplate.Type.KingVulture || type == CreatureTemplate.Type.BigEel || type == CreatureTemplate.Type.MirosBird || (ModManager.MSC && (type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.MirosVulture || type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs)) || type == CreatureTemplate.Type.RedCentipede || type == CreatureTemplate.Type.Deer || type == CreatureTemplate.Type.TempleGuard || type == CreatureTemplate.Type.DaddyLongLegs || type == CreatureTemplate.Type.BrotherLongLegs;
        OnBigCreature?.Invoke(this, type, ref res);
        return res;
    }

    public override void Update()
    {
        base.Update();
        if (ShouldISaySpecialGoodLuck() && events.Count == 0 && !GoodLuckAdded)
        {
            if (owner.CheckSlugpupsInRoom())
                CWEventsFromFile(this, "FirstEncounter_BestLuckWithFamily");
            else if (owner.CheckStrayCreatureInRoom() is CreatureTemplate.Type type && type != CreatureTemplate.Type.StandardGroundCreature)
            {
                if (IsThisABigCreature(type))
                    CWEventsFromFile(this, "FirstEncounter_BestLuckWithBigCreature");
                else
                    CWEventsFromFile(this, "FirstEncounter_BestLuckWithCreature");
            }
            else
                CWEventsFromFile(this, "FirstEncounter_BestLuck");
            GoodLuckAdded = true;
        }
    }

    public static string[] CWConsolidateLineInstructions(string s)
    {
        var array = Regex.Split(s, " : ");
        if (array.Length <= 1)
            return array;
        List<string> list = [];
        bool flag = false, flag2 = false;
        var text = string.Empty;
        for (var i = 0; i < array.Length; i++)
        {
            var ari = array[i];
            if (i == 0)
            {
                if (ari is "WAIT" or "SPECIAL" or "SPECEVENT" or "PEBBLESWAIT")
                    list.Add(ari);
                else if (int.TryParse(ari, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    list.Add(ari);
                    flag = true;
                }
                else
                    text += ari;
            }
            else if (i != array.Length - 1)
            {
                if (!flag || flag2 || !int.TryParse(ari, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    text = text.Length > 0 ? (text + " : " + ari) : (text + ari);
                    continue;
                }
                list.Add(ari);
                flag2 = true;
            }
            else if (flag && !flag2 && int.TryParse(ari, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                list.Add(text);
                list.Add(ari);
                flag2 = true;
            }
            else
                list.Add(text = text.Length > 0 ? (text + " : " + ari) : (text + ari));
        }
        return [.. list];
    }

    public static bool CWEventsFromFile(Conversation self, string fileName) => CWEventsFromFile(self, fileName, false, null);

    public static bool CWEventsFromFile(Conversation self, string fileName, bool pearlIntro, string? pearlSlugName) => CWEventsFromFile(self, fileName, pearlIntro, pearlSlugName, false, 0);

    public static bool CWEventsFromFile(Conversation self, string fileName, bool pearlIntro, string? pearlSlugName, bool oneRandomLine, int randomSeed)
    {
        var languageID = Custom.rainWorld.inGameTranslator.currentLanguage;
        var lgloc = LocalizationTranslator.LangShort(languageID);
        var egloc = LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English);
        string text, t;
        int i;
        if (pearlSlugName is not null)
        {
            t = Path.DirectorySeparatorChar + "CW_" + pearlSlugName + "_" + fileName + ".txt";
            text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + lgloc + t);
            if (!File.Exists(text))
                text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + egloc + t);
            if (File.Exists(text))
                goto SKIP_NORMAL_LOADING;
        }
        t = Path.DirectorySeparatorChar + "CW_" + fileName + ".txt";
        text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + lgloc + t);
        if (!File.Exists(text))
            text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + egloc + t);
        if (!File.Exists(text))
            return false;
        SKIP_NORMAL_LOADING:
        if (pearlIntro)
            (self as SLOracleBehaviorHasMark.MoonConversation)?.PearlIntro();
        string[] array = File.ReadAllLines(text, Encoding.UTF8), array2;
        try
        {
            if (oneRandomLine)
            {
                List<TextEvent> list = [];
                for (i = 0; i < array.Length; i++)
                {
                    array2 = CWConsolidateLineInstructions(array[i]);
                    if (array2.Length == 3)
                        list.Add(new TextEvent(self, int.Parse(array2[0], NumberStyles.Any, CultureInfo.InvariantCulture), array2[2], int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                    else if (array2.Length == 1 && array2[0].Length > 0)
                        list.Add(new TextEvent(self, 0, array2[0], 0));
                }
                if (list.Count > 0)
                {
                    var state = Random.state;
                    Random.InitState(randomSeed);
                    var item = list[Random.Range(0, list.Count)];
                    Random.state = state;
                    self.events.Add(item);
                }
                return true;
            }
            for (i = 0; i < array.Length; i++)
            {
                array2 = CWConsolidateLineInstructions(array[i]);
                if (array2.Length == 3)
                    self.events.Add(new TextEvent(self, int.Parse(array2[0], NumberStyles.Any, CultureInfo.InvariantCulture), array2[2], int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                else if (array2.Length == 2)
                {
                    if (array2[0] is "SPECIAL" or "SPECEVENT")
                        self.events.Add(new SpecialEvent(self, 0, array2[1]));
                    else if (array2[0] is "WAIT" or "PEBBLESWAIT")
                        self.events.Add(new PauseAndWaitForStillEvent(self, null, int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                }
                else if (array2.Length == 1 && array2[0].Length > 0)
                    self.events.Add(new TextEvent(self, 0, array2[0], 0));
            }
            return true;
        }
        catch
        {
            Custom.LogWarning("TEXT ERROR");
            self.events.Add(new TextEvent(self, 0, "TEXT ERROR", 100));
            return false;
        }
    }

    public static string[]? CWLinesFromFile(string fileName, string? slugName)
    {
        var languageID = Custom.rainWorld.inGameTranslator.currentLanguage;
        var lgloc = LocalizationTranslator.LangShort(languageID);
        var egloc = LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English);
        string text, t;
        if (slugName is not null)
        {
            t = Path.DirectorySeparatorChar + "CW_TextOnly_" + slugName + "_" + fileName + ".txt";
            text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + lgloc + t);
            if (!File.Exists(text))
                text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + egloc + t);
            if (File.Exists(text))
                goto SKIP_NORMAL_LOADING;
        }
        t = Path.DirectorySeparatorChar + "CW_TextOnly_" + fileName + ".txt";
        text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + lgloc + t);
        if (!File.Exists(text))
            text = AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + egloc + t);
        if (!File.Exists(text))
            return null;
        SKIP_NORMAL_LOADING:
        return File.ReadAllLines(text, Encoding.UTF8);
    }

    public static bool CWTextFileExists(string fileNameWithoutCW_, bool textOnly = false)
    {
        var pth = Path.DirectorySeparatorChar + (textOnly ? "CW_TextOnly_" : "CW_") + fileNameWithoutCW_ + ".txt";
        return File.Exists(AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + LocalizationTranslator.LangShort(Custom.rainWorld.inGameTranslator.currentLanguage) + pth)) || File.Exists(AssetManager.ResolveFilePath(CWStuffPlugin.CWTextPath + LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English) + pth));
    }
}

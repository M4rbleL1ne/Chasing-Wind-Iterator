using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using static System.Reflection.BindingFlags;
using Random = UnityEngine.Random;
using MoreSlugcats;
using System.Reflection;
using Menu;
using HUD;
using CoralBrain;

namespace CWStuff;

public static class AbstractPhysicalObjectType
{
    public static AbstractPhysicalObject.AbstractObjectType CWOracleSwarmer = new(nameof(CWOracleSwarmer), true);
    public static AbstractPhysicalObject.AbstractObjectType CWPearl = new(nameof(CWPearl), true);

    internal static void UnregisterValues()
    {
        if (CWOracleSwarmer is not null)
        {
            CWOracleSwarmer.Unregister();
            CWOracleSwarmer = null!;
        }
        if (CWPearl is not null)
        {
            CWPearl.Unregister();
            CWPearl = null!;
        }
    }
}

public static class DataPearlType
{
    public static DataPearl.AbstractDataPearl.DataPearlType CWPearl = new(nameof(CWPearl), true);

    internal static void UnregisterValues()
    {
        if (CWPearl is not null)
        {
            CWPearl.Unregister();
            CWPearl = null!;
        }
    }
}

public static class NewOracleID
{
    public static Oracle.OracleID CW = new(nameof(CW), true);

    internal static void UnregisterValues()
    {
        if (CW is not null)
        {
            CW.Unregister();
            CW = null!;
        }
    }
}

public static class NewTickerID
{
    public static StoryGameStatisticsScreen.TickerID CWPearls = new(nameof(CWPearls), true),
        CWEncounter = new(nameof(CWEncounter), true),
        CWGreenNeuron = new(nameof(CWGreenNeuron), true),
        CWSpearMission = new(nameof(CWSpearMission), true);

    internal static void UnregisterValues()
    {
        if (CWPearls is not null)
        {
            CWPearls.Unregister();
            CWPearls = null!;
        }
        if (CWEncounter is not null)
        {
            CWEncounter.Unregister();
            CWEncounter = null!;
        }
        if (CWGreenNeuron is not null)
        {
            CWGreenNeuron.Unregister();
            CWGreenNeuron = null!;
        }
        if (CWSpearMission is not null)
        {
            CWSpearMission.Unregister();
            CWSpearMission = null!;
        }
    }
}

public static class CWOracleHooks
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class CWOracleWorldSaveData(SLOrcacleState oracleState)
    {
        public SLOrcacleState OracleState = oracleState;
        public int NumberOfConversations, AnnoyedCounter;
        public bool SeenGreenNeuron, ScavImmunity, SeenSpearmasterTaggedPearl, AdditionalRedCycles;
    }

    public delegate void CWSpecialEvent(SSOracleBehavior self, string eventName, ref bool runGiftCode);

    public delegate void CWGiftEvent(SSOracleBehavior self, string eventName, ref GiftStates gifts);

    public delegate void CWEvent(SSOracleBehavior self, ref bool runOriginalCode);

    public delegate void SLEvent(SLOracleBehaviorHasMark self, ref bool result);

    public delegate void CWPearlEvent(SLOracleBehaviorHasMark.MoonConversation self, ref bool runOriginalCode);

    public static ConditionalWeakTable<MiscWorldSaveData, CWOracleWorldSaveData> WorldSaveData = new();
    public static ConditionalWeakTable<SlugcatSelectMenu.SaveGameData, StrongBox<bool>> GameData = new();
    public static MethodInfo CWWorldRedCyclesInfo = typeof(CWOracleHooks).GetMethod(nameof(CWWorldRedCycles)),
        CWGameRedCyclesInfo = typeof(CWOracleHooks).GetMethod(nameof(CWGameRedCycles));
    //public static ConditionalWeakTable<PlayerProgression.MiscProgressionData, HashSet<DataPearl.AbstractDataPearl.DataPearlType>> DecipheredPearls = new();
    public static event SLEvent? OnPebblesIsDying;
    public static event CWPearlEvent? OnPearlIntro;
    public static event CWSpecialEvent? OnCustomEvent;
    public static event CWGiftEvent? OnCustomGift;
    public static event CWEvent? OnTakeNeuron, OnReleaseNeuron, OnResumePausedPearlConversation, OnInterruptPearlMessagePlayerLeaving, OnUnconciousUpdate, OnReactToHitWeapon,
        OnSlugcatEnterRoomReaction, OnNewAction, OnSeePlayer;
    public static event Action<SSOracleBehavior>? OnMove, OnUpdate;

    public static int AdditionalCycles => ModManager.MMF && MMF.cfgHunterBonusCycles is Configurable<int> cfg ? cfg.Value : 5;

    internal static void Apply()
    {
        On.Room.ReadyForAI += On_Room_ReadyForAI;
        On.AbstractPhysicalObject.Realize += On_AbstractPhysicalObject_Realize;
        On.Player.StomachGlowLightColor += On_Player_StomachGlowLightColor;
        On.OracleGraphics.ArmJointGraphics.ctor += On_ArmJointGraphics_ctor;
        On.OracleGraphics.Gown.Color += On_Gown_Color;
        On.OracleGraphics.ctor += On_OracleGraphics_ctor;
        On.OracleGraphics.Update += On_OracleGraphics_Update;
        On.OracleGraphics.InitiateSprites += On_OracleGraphics_InitiateSprites;
        On.OracleGraphics.AddToContainer += On_OracleGraphics_AddToContainer;
        On.OracleGraphics.DrawSprites += On_OracleGraphics_DrawSprites;
        On.OracleGraphics.ApplyPalette += On_OracleGraphics_ApplyPalette;
        On.OracleChatLabel.AddToContainer += On_OracleChatLabel_AddToContainer;
        On.OracleChatLabel.DrawSprites += On_OracleChatLabel_DrawSprites;
        On.SLOracleBehaviorHasMark.RejectDiscussItem += On_SLOracleBehaviorHasMark_RejectDiscussItem;
        On.CoralBrain.CoralNeuronSystem.PlaceSwarmers += On_CoralNeuronSystem_PlaceSwarmers;
        IL.Oracle.OracleArm.Joint.Update += IL_Joint_Update;
        On.Oracle.OracleArm.ctor += On_OracleArm_ctor;
        IL.Oracle.OracleArm.Update += IL_OracleArm_Update;
        IL.Oracle.ctor += IL_Oracle_ctor;
        On.Oracle.CreateMarble += On_Oracle_CreateMarble;
        On.Oracle.HitByWeapon += On_Oracle_HitByWeapon;
        On.OracleBehavior.AlreadyDiscussedItemString += On_OracleBehavior_AlreadyDiscussedItemString;
        On.OracleBehavior.FindPlayer += On_OracleBehavior_FindPlayer;
        On.SSOracleBehavior.InitStoryPearlCollection += On_SSOracleBehavior_InitStoryPearlCollection;
        On.SaveState.AbstractPhysicalObjectFromString += On_SaveState_AbstractPhysicalObjectFromString;
        On.ItemSymbol.SymbolDataFromItem += On_ItemSymbol_SymbolDataFromItem;
        On.ItemSymbol.ColorForItem += On_ItemSymbol_ColorForItem;
        On.ItemSymbol.SpriteNameForItem += On_ItemSymbol_SpriteNameForItem;
        On.MoreSlugcats.SSSwarmerSpawner.SpawnSwarmer += On_SSSwarmerSpawner_SpawnSwarmer;
        On.DataPearl.PearlIsNotMisc += On_DataPearl_PearlIsNotMisc;
        IL.DataPearl.Update += IL_DataPearl_Update;
        On.DataPearl.ApplyPalette += On_DataPearl_ApplyPalette;
        On.SSOracleBehavior.ctor += On_SSOracleBehavior_ctor;
        On.SSOracleBehavior.InitateConversation += On_SSOracleBehavior_InitateConversation;
        On.SSOracleBehavior.SeePlayer += On_SSOracleBehavior_SeePlayer;
        new Hook(typeof(SSOracleBehavior).GetMethod("get_HasSeenGreenNeuron", Public | NonPublic | Instance), On_SSOracleBehavior_get_HasSeenGreenNeuron);
        On.SSOracleBehavior.Update += On_SSOracleBehavior_Update;
        On.SSOracleBehavior.NewAction += On_SSOracleBehavior_NewAction;
        On.SSOracleBehavior.Move += On_SSOracleBehavior_Move;
        On.SSOracleBehavior.HandTowardsPlayer += On_SSOracleBehavior_HandTowardsPlayer;
        On.SSOracleBehavior.CreatureJokeDialog += On_SSOracleBehavior_CreatureJokeDialog;
        On.SSOracleBehavior.ReactToHitWeapon += On_SSOracleBehavior_ReactToHitWeapon;
        On.SSOracleBehavior.UnconciousUpdate += On_SSOracleBehavior_UnconciousUpdate;
        On.SSOracleBehavior.StartItemConversation += On_SSOracleBehavior_StartItemConversation;
        On.SSOracleBehavior.InterruptPearlMessagePlayerLeaving += On_SSOracleBehavior_InterruptPearlMessagePlayerLeaving;
        On.SSOracleBehavior.ResumePausedPearlConversation += On_SSOracleBehavior_ResumePausedPearlConversation;
        On.MiscWorldSaveData.ctor += On_MiscWorldSaveData_ctor;
        On.SaveState.ctor += On_SaveState_ctor;
        On.MiscWorldSaveData.FromString += On_MiscWorldSaveData_FromString;
        On.MiscWorldSaveData.ToString += On_MiscWorldSaveData_ToString;
        On.SSOracleBehavior.SpecialEvent += On_SSOracleBehavior_SpecialEvent;
        On.Oracle.SetUpMarbles += On_Oracle_SetUpMarbles;
        On.SSOracleBehavior.SlugcatEnterRoomReaction += On_SSOracleBehavior_SlugcatEnterRoomReaction;
        IL.SLOracleBehaviorHasMark.GrabObject += IL_SLOracleBehaviorHasMark_GrabObject;
        IL.SLOracleBehavior.Update += IL_SLOracleBehavior_Update;
        On.SLOracleBehavior.Update += On_SLOracleBehavior_Update;
        On.SLOracleBehaviorHasMark.MoonConversation.PearlIntro += On_MoonConversation_PearlIntro;
        new Hook(typeof(SLOracleBehaviorHasMark.MoonConversation).GetMethod("get_State", Public | NonPublic | Instance), On_MoonConversation_get_State);
        On.Menu.StoryGameStatisticsScreen.GetDataFromGame += On_StoryGameStatisticsScreen_GetDataFromGame;
        On.Menu.StoryGameStatisticsScreen.TickerIsDone += On_StoryGameStatisticsScreen_TickerIsDone;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += On_MoonConversation_AddEvents;
        On.ScavengerAI.PlayerRelationship += On_ScavengerAI_PlayerRelationship;
        On.ScavengerOutpost.ScavengerReportTransgression += On_ScavengerOutpost_ScavengerReportTransgression;
        On.Scavenger.PlayerHasImmunity += On_Scavenger_PlayerHasImmunity;
        On.SaveState.LoadGame += On_SaveState_LoadGame;
        new ILHook(typeof(SaveState).GetMethod("get_SlowFadeIn", Public | NonPublic | Instance), IL_get_SlowFadeIn);
        new ILHook(typeof(StoryGameSession).GetMethod("get_RedIsOutOfCycles", Public | NonPublic | Instance), IL_get_RedIsOutOfCycles);
        IL.Player.ctor += IL_Player_ctor;
        IL.HUD.SubregionTracker.Update += IL_SubregionTracker_Update;
        On.Menu.SlugcatSelectMenu.MineForSaveData += On_SlugcatSelectMenu_MineForSaveData;
        IL.ProcessManager.CreateValidationLabel += IL_ProcessManager_CreateValidationLabel;
        IL.Menu.DialogBackupSaveInfo.PopulateSaveSlotInfoDisplay += IL_DialogBackupSaveInfo_PopulateSaveSlotInfoDisplay;
        IL.Menu.SlugcatSelectMenu.ctor += IL_SlugcatSelectMenu_ctor;
        IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += IL_SlugcatPageContinue_ctor;
        IL.HUD.Map.CycleLabel.UpdateCycleText += IL_CycleLabel_UpdateCycleText;
        IL.HUD.TextPrompt.Update += IL_TextPrompt_Update;
        On.SSOracleBehavior.UpdateStoryPearlCollection += On_SSOracleBehavior_UpdateStoryPearlCollection;
    }

    static void On_SSOracleBehavior_UpdateStoryPearlCollection(On.SSOracleBehavior.orig_UpdateStoryPearlCollection orig, SSOracleBehavior self)
    {
        if (self.oracle is Oracle o && o.IsCW())
        {
            if (o.room is Room rm)
            {
                var list = new List<DataPearl.AbstractDataPearl>();
                int num = 0, i;
                var orbits = self.readDataPearlOrbits;
                var glyphs = self.readPearlGlyphs;
                for (i = 0; i < orbits.Count; i++)
                {
                    var orbitPearl = orbits[i];
                    if (orbitPearl.realizedObject is not DataPearl pearl)
                        continue;
                    if (pearl.grabbedBy.Count > 0)
                    {
                        list.Add(orbitPearl);
                        continue;
                    }
                    var fc = pearl.firstChunk;
                    if (!glyphs.ContainsKey(orbitPearl))
                    {
                        glyphs.Add(orbitPearl, new(fc.pos, GlyphLabel.RandomString(1, 1, 12842 + orbitPearl.dataPearlType.Index, false)));
                        rm.AddObject(glyphs[orbitPearl]);
                    }
                    else
                        glyphs[orbitPearl].setPos = fc.pos;
                    fc.pos = Custom.MoveTowards(fc.pos, self.storedPearlOrbitLocation(num), 2.5f);
                    fc.vel *= .99f;
                    ++num;
                }
                for (i = 0; i < list.Count; i++)
                {
                    var pearl = list[i];
                    //Custom.Log($"stored pearl grabbed, releasing from storage {item}");
                    if (glyphs.TryGetValue(pearl, out var glyph))
                    {
                        glyph.Destroy();
                        glyphs.Remove(pearl);
                        orbits.Remove(pearl);
                    }
                }
            }
        }
        else
            orig(self);
    }

    static SlugcatSelectMenu.SaveGameData? On_SlugcatSelectMenu_MineForSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
    {
        var res = orig(manager, slugcat);
        if (res is not null && slugcat == SlugcatStats.Name.Red)
        {
            var flag = false;
            if (manager.rainWorld.progression.currentSaveState is SaveState save && save.saveStateNumber == slugcat)
                flag = save.miscWorldSaveData is MiscWorldSaveData worldData && WorldSaveData.TryGetValue(worldData, out var wData) && wData.AdditionalRedCycles;
            else
            {
                var progLinesFromMemory = manager.rainWorld.progression.GetProgLinesFromMemory();
                for (var i = 0; i < progLinesFromMemory.Length; i++)
                {
                    var array = Regex.Split(progLinesFromMemory[i], "<progDivB>");
                    if (array.Length != 2 || array[0] != "SAVE STATE" || BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) != slugcat)
                        continue;
                    flag = FindCWRedCycles(array[1]);
                    break;
                }
            }
            if (!GameData.TryGetValue(res, out var result))
                GameData.Add(res, new(flag));
            else
                result.Value = flag;
        }
        return res;
    }

    static void IL_TextPrompt_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((int cycles, TextPrompt self) =>
             {
                 var rm = (self.hud.owner as Player)!.room;
                 var saveState = rm.game.GetStorySession.saveState;
                 var nm = rm.abstractRoom.name;
                 if (string.Equals("SS_AI", nm, StringComparison.OrdinalIgnoreCase))
                 {
                     if (saveState.miscWorldSaveData is MiscWorldSaveData worldData && WorldSaveData.TryGetValue(worldData, out var data) && data.AdditionalRedCycles)
                         cycles += AdditionalCycles;
                 }
                 else if (string.Equals("CW_AI", nm, StringComparison.OrdinalIgnoreCase))
                 {
                     if (saveState.redExtraCycles)
                         cycles += AdditionalCycles;
                 }
                 return cycles;
             });
        }
    }

    static void IL_CycleLabel_UpdateCycleText(ILContext il)
    {
        var import = il.Import(CWWorldRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        VariableDefinition? loc = null;
        var vars = il.Body.Variables;
        for (var i = 0; i < vars.Count; i++)
        {
            var vr = vars[i];
            if (vr.VariableType.Name.Contains("Player"))
                loc = vr;
        }
        if (loc is null)
        {
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook CycleLabel.UpdateCycleText! (local not found)");
            return;
        }
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldloc, loc)
                 .Emit<Creature>(OpCodes.Callvirt, "get_abstractCreature")
                 .Emit<AbstractWorldEntity>(OpCodes.Ldfld, nameof(AbstractWorldEntity.world))
                 .Emit<World>(OpCodes.Callvirt, "get_game")
                 .Emit<RainWorldGame>(OpCodes.Callvirt, "get_GetStorySession")
                 .Emit<StoryGameSession>(OpCodes.Ldfld, nameof(StoryGameSession.saveState))
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_SlugcatPageContinue_ctor(ILContext il)
    {
        var import = il.Import(CWGameRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldarg_0)
                 .Emit<SlugcatSelectMenu.SlugcatPageContinue>(OpCodes.Callvirt, "get_saveGameData")
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static bool FindCWRedCycles(string saveStateString)
    {
        var num = saveStateString.IndexOf("M4R_CW_redCycles<mwA>");
        if (num < 0)
            return false;
        var ri = num + 21;
        return ri < saveStateString.Length && saveStateString[ri] == 'Y';
    }

    static void IL_SlugcatSelectMenu_ctor(ILContext il)
    {
        var import = il.Import(CWGameRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((SlugcatSelectMenu self) => self.saveGameData[SlugcatStats.Name.Red]);
                c.Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_DialogBackupSaveInfo_PopulateSaveSlotInfoDisplay(ILContext il)
    {
        var import = il.Import(CWGameRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        VariableDefinition? loc = null;
        var vars = il.Body.Variables;
        for (var i = 0; i < vars.Count; i++)
        {
            var vr = vars[i];
            if (vr.VariableType.Name.Contains("SaveGameData"))
                loc = vr;
        }
        if (loc is null)
        {
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook DialogBackupSaveInfo.PopulateSaveSlotInfoDisplay! (local not found)");
            return;
        }
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldloc, loc)
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_ProcessManager_CreateValidationLabel(ILContext il)
    {
        var import = il.Import(CWGameRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        VariableDefinition? loc = null;
        var vars = il.Body.Variables;
        for (var i = 0; i < vars.Count; i++)
        {
            var vr = vars[i];
            if (vr.VariableType.Name.Contains("SaveGameData"))
                loc = vr;
        }
        if (loc is null)
        {
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook ProcessManager.CreateValidationLabel! (local not found)");
            return;
        }
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldloc, loc)
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_SubregionTracker_Update(ILContext il)
    {
        var import = il.Import(CWWorldRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        VariableDefinition? loc = null;
        var vars = il.Body.Variables;
        for (var i = 0; i < vars.Count; i++)
        {
            var vr = vars[i];
            if (vr.VariableType.Name.Contains("Player"))
                loc = vr;
        }
        if (loc is null)
        {
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook SubregionTracker.Update! (local not found)");
            return;
        }
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldloc, loc)
                 .Emit<UpdatableAndDeletable>(OpCodes.Ldfld, nameof(UpdatableAndDeletable.room))
                 .Emit<Room>(OpCodes.Ldfld, nameof(Room.game))
                 .Emit<RainWorldGame>(OpCodes.Callvirt, "get_GetStorySession")
                 .Emit<StoryGameSession>(OpCodes.Ldfld, nameof(StoryGameSession.saveState))
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_Player_ctor(ILContext il)
    {
        var import = il.Import(CWWorldRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldarg_1)
                 .Emit<AbstractWorldEntity>(OpCodes.Ldfld, nameof(AbstractWorldEntity.world))
                 .Emit<World>(OpCodes.Callvirt, "get_game")
                 .Emit<RainWorldGame>(OpCodes.Callvirt, "get_GetStorySession")
                 .Emit<StoryGameSession>(OpCodes.Ldfld, nameof(StoryGameSession.saveState))
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_get_SlowFadeIn(ILContext il)
    {
        var import = il.Import(CWWorldRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldarg_0)
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    static void IL_get_RedIsOutOfCycles(ILContext il)
    {
        var import = il.Import(CWWorldRedCyclesInfo);
        var c = new ILCursor(il);
        var ins = il.Body.Instructions;
        for (var i = 0; i < ins.Count; i++)
        {
            if (ins[i].MatchCall<RedsIllness>(nameof(RedsIllness.RedsCycles)))
            {
                c.Goto(i, MoveType.After)
                 .Emit(OpCodes.Ldarg_0)
                 .Emit<StoryGameSession>(OpCodes.Ldfld, nameof(StoryGameSession.saveState))
                 .Emit(OpCodes.Call, import);
            }
        }
    }

    public static int CWWorldRedCycles(int cycles, SaveState save)
    {
        if (save.miscWorldSaveData is MiscWorldSaveData dt && WorldSaveData.TryGetValue(dt, out var cwData) && cwData.AdditionalRedCycles)
            cycles += AdditionalCycles;
        return cycles;
    }

    public static int CWGameRedCycles(int cycles, SlugcatSelectMenu.SaveGameData save)
    {
        if (GameData.TryGetValue(save, out var dt) && dt.Value)
            cycles += AdditionalCycles;
        return cycles;
    }

    static void On_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        if (self.miscWorldSaveData is MiscWorldSaveData dt && WorldSaveData.TryGetValue(dt, out var cwData))
        {
            cwData.ScavImmunity = false;
            cwData.AdditionalRedCycles = false;
        }
        orig(self, str, game);
    }

    static bool On_Scavenger_PlayerHasImmunity(On.Scavenger.orig_PlayerHasImmunity orig, Scavenger self, Player player)
    {
        return orig(self, player) || (self.room is Room rm && rm.game?.session is StoryGameSession ses && WorldSaveData.TryGetValue(ses.saveState.miscWorldSaveData, out var data) && data.ScavImmunity && rm.world?.name is string s && CWStuffPlugin.ScavImmuRegions.Contains(s));
    }

    static void On_ScavengerOutpost_ScavengerReportTransgression(On.ScavengerOutpost.orig_ScavengerReportTransgression orig, ScavengerOutpost self, Player player)
    {
        if (self.room is Room rm && rm.game?.session is StoryGameSession ses && WorldSaveData.TryGetValue(ses.saveState.miscWorldSaveData, out var data) && data.ScavImmunity && rm.world?.name is string s && CWStuffPlugin.ScavImmuRegions.Contains(s))
            return;
        orig(self, player);
    }

    static CreatureTemplate.Relationship On_ScavengerAI_PlayerRelationship(On.ScavengerAI.orig_PlayerRelationship orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
    {
        var res = orig(self, dRelation);
        if (!self.scavenger.King && dRelation.trackerRep.representedCreature.realizedCreature is Player p && self.scavenger.PlayerHasImmunity(p))
        {
            res.type = CreatureTemplate.Relationship.Type.Afraid;
            res.intensity = 1f;
        }
        return res;
    }

    /*static void On_MiscProgressionData_ctor(On.PlayerProgression.MiscProgressionData.orig_ctor orig, PlayerProgression.MiscProgressionData self, PlayerProgression owner)
    {
        orig(self, owner);
        if (!DecipheredPearls.TryGetValue(self, out _))
            DecipheredPearls.Add(self, []);
    }*/

    public static bool PebblesIsDying(this SLOracleBehaviorHasMark self)
    {
        bool res;
        if (ModManager.MSC)
        {
            var save = self.oracle.room.game.GetStorySession.saveStateNumber;
            res = save == MoreSlugcatsEnums.SlugcatStatsName.Saint || save == MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
        }
        else
            res = false;
        OnPebblesIsDying?.Invoke(self, ref res);
        return res;
    }

    static void On_MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);
        if (self.id == ConversationID.SL_CWNeuron)
        {
            if (self.myBehavior is not SLOracleBehaviorHasMark hm)
                return;
            var neuronsLeft = self.State.neuronsLeft;
            if (neuronsLeft - 1 > 2 && hm.respondToNeuronFromNoSpeakMode)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("You... Strange thing. Now this?"), 10));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I will accept your gift..."), 10));
            }
            switch (neuronsLeft - 1)
            {
                case -1 or 0:
                    break;
                case 1:
                    self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You!"), 10));
                    self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("...you...killed..."), 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, "...", 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...me"), 10));
                    break;
                case 2:
                    self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("...thank you... better..."), 10));
                    self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("still, very... bad."), 10));
                    break;
                case 3:
                    self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Thank you... That is a little better. Thank you, creature."), 10));
                    if (!hm.respondToNeuronFromNoSpeakMode)
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Maybe this is asking too much... But, would you bring me another one?"), 0));
                    break;
                default:
                    if (hm.respondToNeuronFromNoSpeakMode)
                    {
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Thank you. I do wonder what you want."), 10));
                        break;
                    }
                    if (self.State.neuronGiveConversationCounter == 0)
                    {
                        if (neuronsLeft == 5)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("After all this time, a lifeline. Thank you."), 10));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I'll never feel the power I once had, but this is something to sustain an old soul."), 10));
                        }
                        else if (hm.PebblesIsDying())
                            self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You get these at Five Pebbles'?<LINE>Thank you <little creature>, but please, leave Five Pebbles be."), 10));
                        else
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I am grateful - the relief is indescribable!"), 10));
                    }
                    else if (self.State.neuronGiveConversationCounter == 1)
                    {
                        if (hm.PebblesIsDying())
                        {
                            self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you again <little creature>, but please, leave Five Pebbles be."), 10));
                            self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Neither of us are well. We have both lost more than we could ever recover from."), 10));
                            self.events.Add(new Conversation.TextEvent(self, 10, "...", 0));
                            self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It is for the best that our circumstances are not agitated."), 0));
                        }
                        else
                        {
                            self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You get these at Five Pebbles'?<LINE>Thank you so much. I'm sure he won't mind."), 10));
                            self.events.Add(new Conversation.TextEvent(self, 10, "...", 0));
                            self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Or actually I'm sure he would, but he has so many of these~<LINE>it doesn't do him any difference.<LINE>For me though, it does! Thank you, little creature!"), 0));
                        }
                    }
                    else
                    {
                        switch (Random.Range(0, 4))
                        {
                            case 0:
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you, again. I feel wonderful."), 10));
                                break;
                            case 1:
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you so very much!"), 10));
                                break;
                            case 2:
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("It is strange... I'm remembering myself, but also... someone else."), 10));
                                break;
                            default:
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you... Sincerely."), 10));
                                break;
                        }
                    }
                    ++self.State.neuronGiveConversationCounter;
                    break;
            }
            hm.respondToNeuronFromNoSpeakMode = false;
        }
    }

    static void On_SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
    {
        orig(self, eu);
        if (self.holdingObject is SSOracleSwarmer ssw && ssw.abstractPhysicalObject.type == AbstractPhysicalObjectType.CWOracleSwarmer && self.oracle.Consious && ssw.grabbedBy.Count == 0 && (self.oracle.room.game.cameras[0].hud.dialogBox is not DialogBox box || box.messages.Count == 0))
        {
            ssw.firstChunk.MoveFromOutsideMyUpdate(eu, self.oracle.firstChunk.pos + new Vector2(-18f, -7f));
            ssw.firstChunk.vel *= 0f;
            ++self.convertSwarmerCounter;
            if (self.convertSwarmerCounter > 40)
            {
                var pos = ssw.firstChunk.pos;
                ssw.Destroy();
                self.holdingObject = null;
                var sLOracleSwarmer = new SLOracleSwarmer(new(self.oracle.room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null, self.oracle.room.GetWorldCoordinate(pos), self.oracle.room.game.GetNewID()), self.oracle.room.world);
                self.oracle.room.abstractRoom.entities.Add(sLOracleSwarmer.abstractPhysicalObject);
                sLOracleSwarmer.firstChunk.HardSetPosition(pos);
                self.oracle.room.AddObject(sLOracleSwarmer);
                ++self.State.neuronsLeft;
                self.State.InfluenceLike(.65f);
                if (self.oracle.room.game.session is StoryGameSession sess)
                    sess.saveState.miscWorldSaveData.playerGuideState.angryWithPlayer = false;
                if (self is SLOracleBehaviorHasMark hm)
                {
                    ++self.State.totNeuronsGiven;
                    self.State.increaseLikeOnSave = true;
                    if (self.reelInSwarmer is null && (hm.currentConversation is not Conversation c || c.id != Conversation.ID.MoonRecieveSwarmer) && self.State.SpeakingTerms)
                        hm.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(ConversationID.SL_CWNeuron, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                }
                if (!self.moonActive && self.InSitPosition && self.dontHoldKnees < 1 && Random.value < .025f && (self.player is null || !Custom.DistLess(self.oracle.firstChunk.pos, self.player.DangerPos, 50f)) && !self.protest && self.oracle.health >= 1f)
                    self.holdKnees = true;
            }
        }
    }

    static void IL_SLOracleBehavior_Update(ILContext il)
    {
        var c = new ILCursor(il);
        ILLabel? label = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<SLOracleBehavior>("holdingObject"),
            x => x.MatchIsinst<SSOracleSwarmer>(),
            x => x.MatchBrfalse(out label))
        && label is not null)
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((SLOracleBehavior self) => (self.holdingObject as SSOracleSwarmer)?.abstractPhysicalObject.type == AbstractPhysicalObjectType.CWOracleSwarmer);
            c.Emit(OpCodes.Brtrue, label);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook SLOracleBehavior.Update!");
    }

    static void On_StoryGameStatisticsScreen_TickerIsDone(On.Menu.StoryGameStatisticsScreen.orig_TickerIsDone orig, StoryGameStatisticsScreen self, StoryGameStatisticsScreen.Ticker ticker)
    {
        orig(self, ticker);
        if (ticker.ID == NewTickerID.CWEncounter)
            self.scoreKeeper.AddScoreAdder(ticker.getToValue, 60);
        else if (ticker.ID == NewTickerID.CWPearls)
            self.scoreKeeper.AddScoreAdder(ticker.getToValue, 25);
        else if (ticker.ID == NewTickerID.CWGreenNeuron)
            self.scoreKeeper.AddScoreAdder(ticker.getToValue, 150);
        else if (ticker.ID == NewTickerID.CWSpearMission)
            self.scoreKeeper.AddScoreAdder(ticker.getToValue, 100);
    }

    static void On_StoryGameStatisticsScreen_GetDataFromGame(On.Menu.StoryGameStatisticsScreen.orig_GetDataFromGame orig, StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if (!WorldSaveData.TryGetValue(package.saveState.miscWorldSaveData, out var data))
            return;
        var vector = new Vector2(self.ContinueAndExitButtonsXPos - 160f, 535f);
        if (data.NumberOfConversations > 0)
        {
            var ticker = new StoryGameStatisticsScreen.Popper(self, self.pages[0], vector + new Vector2(0f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Met Chasing Wind") + ">", NewTickerID.CWEncounter);
            self.allTickers.Add(ticker);
            self.pages[0].subObjects.Add(ticker);
        }
        if (ModManager.MSC && package.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            if (data.SeenSpearmasterTaggedPearl)
            {
                var ticker = new StoryGameStatisticsScreen.Popper(self, self.pages[0], vector + new Vector2(-30f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Brought Moon's message to Chasing Wind") + ">", NewTickerID.CWSpearMission);
                self.allTickers.Add(ticker);
                self.pages[0].subObjects.Add(ticker);
            }
        }
        else if (data.SeenGreenNeuron)
        {
            var ticker = new StoryGameStatisticsScreen.Popper(self, self.pages[0], vector + new Vector2(-30f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Brought the slag keys to Chasing Wind") + ">", NewTickerID.CWGreenNeuron);
            self.allTickers.Add(ticker);
            self.pages[0].subObjects.Add(ticker);
        }
        var count = data.OracleState.significantPearls.Count;
        if (count > 0)
        {
            var ticker = new StoryGameStatisticsScreen.LabelTicker(self, self.pages[0], vector + new Vector2(-90f, -30f * (-3 + self.allTickers.Count)), count, NewTickerID.CWPearls, self.Translate("Unique pearls read by Chasing Wind : "));
            ticker.numberLabel.pos.x += 130f;
            self.allTickers.Add(ticker);
            self.pages[0].subObjects.Add(ticker);
        }
    }

    static SLOrcacleState On_MoonConversation_get_State(Func<SLOracleBehaviorHasMark.MoonConversation, SLOrcacleState> orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self is CWPearlConversation && self.myBehavior.oracle.room.game.session is StoryGameSession sess && WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var data))
            return data.OracleState;
        return orig(self);
    }

    static void On_MoonConversation_PearlIntro(On.SLOracleBehaviorHasMark.MoonConversation.orig_PearlIntro orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self is not CWPearlConversation cwp || self.myBehavior is not SSOracleBehavior bhv || bhv.oracle.room.game is not RainWorldGame game)
            orig(self);
        else if (!cwp.IntroSaid)
        {
            cwp.IntroSaid = true;
            var run = true;
            OnPearlIntro?.Invoke(self, ref run);
            if (!run)
                return;
            if (self.myBehavior.isRepeatedDiscussion && (cwp.id != ConversationID.CWSpearPearlAfterMoon || !WorldSaveData.TryGetValue(game.GetStorySession.saveState.miscWorldSaveData, out var data) || !data.SeenSpearmasterTaggedPearl))
                self.events.Add(new Conversation.TextEvent(self, 0, self.myBehavior.AlreadyDiscussedItemString(true), 10));
            else
            {
                if (ModManager.MSC && (cwp.id == ConversationID.CWSpearPearlAfterMoon || cwp.id == MoreSlugcatsEnums.ConversationID.Moon_Spearmaster_Pearl))
                {
                    if (bhv.currSubBehavior is CWGeneralConversation gen)
                        gen.LockPaths = true;
                    else if (bhv.currSubBehavior is CWNoSubBehavior ns)
                        ns.LockPaths = true;
                }
                if (CWConversation.CWLinesFromFile("PearlIntro", game.StoryCharacter?.value) is string[] lns)
                {
                    var l = lns.Length;
                    if (l > 0 && l % 2 == 0)
                    {
                        var rd = Random.Range(0, l / 2);
                        self.events.Add(new Conversation.TextEvent(self, 0, lns[rd * 2], 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, lns[rd * 2 + 1], 10));
                    }
                }
            }
        }
    }

    static void IL_SLOracleBehaviorHasMark_GrabObject(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
            x => x.MatchLdarg(1),
            x => x.MatchIsinst<DataPearl>(),
            x => x.MatchCallOrCallvirt<DataPearl>("get_AbstractPearl"),
            x => x.MatchLdfld<DataPearl.AbstractDataPearl>("dataPearlType"),
            x => x.MatchLdsfld<DataPearl.AbstractDataPearl.DataPearlType>("PebblesPearl")))
        {
            ++c.Index;
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((PhysicalObject obj, SLOracleBehaviorHasMark self) =>
            {
                if (obj is DataPearl d && d.AbstractPearl.dataPearlType == DataPearlType.CWPearl)
                {
                    self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.Moon_Pebbles_Pearl, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                    return true;
                }
                return false;
            });
            var label = c.DefineLabel();
            var mmfc = 0;
            var ins = il.Instrs;
            for (var i = 0; i < ins.Count; i++)
            {
                if (ins[i].MatchLdsfld<ModManager>("MMF"))
                {
                    ++mmfc;
                    if (mmfc == 3)
                    {
                        label.Target = ins[i];
                        break;
                    }
                }
            }
            c.Emit(OpCodes.Brtrue, label)
             .Emit(OpCodes.Ldarg_1);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook SLOracleBehaviorHasMark.GrabObject!");
    }

    static void On_SSOracleBehavior_SlugcatEnterRoomReaction(On.SSOracleBehavior.orig_SlugcatEnterRoomReaction orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            if (self.conversation is null && self.pearlConversation is null)
            {
                var run = true;
                OnSlugcatEnterRoomReaction?.Invoke(self, ref run);
                if (run && (self.currSubBehavior is not CWGeneralConversation cv || !cv.SeenPlayer) && (self.currSubBehavior is not CWNoSubBehavior ns || !ns.SeenPlayer))
                {
                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("SlugcatEnterRoomReaction", self.oracle.room.game.StoryCharacter?.value) is string[] lns && lns.Length > 0)
                        self.dialogBox.NewMessage(lns[Random.Range(0, lns.Length)], 0);
                    if (self.currSubBehavior is CWGeneralConversation cv2)
                        cv2.SeenPlayer = true;
                    else if (self.currSubBehavior is CWNoSubBehavior ns2)
                        ns2.SeenPlayer = true;
                }
            }
        }
    }

    static void On_Oracle_SetUpMarbles(On.Oracle.orig_SetUpMarbles orig, Oracle self)
    {
        if (!self.IsCW())
            orig(self);
        else
        {
            var vector = self.room?.game?.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase) ? new Vector2(180f, 180f) : new Vector2(200f, 100f);
            PhysicalObject physicalObject = self;
            for (var i = 0; i < 4; i++)
            {
                var ps = new Vector2(vector.x + 300f, vector.y + 200f) + Custom.RNV() * 20f;
                var color = i switch
                {
                    5 => 2,
                    2 or 3 => 1,
                    _ => 0,
                };
                self.CreateMarble(physicalObject, ps, 0, 35f, color);
            }
            for (var j = 0; j < 4; j++)
                self.CreateMarble(physicalObject, new Vector2(vector.x + 300f, vector.y + 200f) + Custom.RNV() * 20f, 1, 100f, j == 1 ? 2 : 0);
            self.CreateMarble(null, new(vector.x + 60f, vector.y + 200f), 0, 0f, 1);
            Vector2 vector2 = new(vector.x + 80f, vector.y + 30f), vector3 = Custom.DegToVec(-32.7346f), vector4 = Custom.PerpendicularVector(vector3);
            for (var k = 0; k < 3; k++)
            {
                for (var l = 0; l < 3; l++)
                {
                    if (k != 2 || l != 2)
                        self.CreateMarble(null, vector2 + vector4 * k * 17f + vector3 * l * 17f, 0, 0f, (k != 2 || l != 0) && (k != 1 || l != 3) ? 1 : 2);
                    else
                        self.CreateMarble(null, vector2 + vector4 * k * 17f + vector3 * l * 17f, 0, 0f, 0);
                }
            }
            self.CreateMarble(null, new(vector.x + 487f, vector.y + 218f), 0, 0f, 1);
            self.CreateMarble(self.marbles[self.marbles.Count - 1], new(vector.x + 487f, vector.y + 218f), 0, 18f, 0);
            self.CreateMarble(null, new(vector.x + 450f, vector.y + 167f), 0, 0f, 2);
            self.CreateMarble(self.marbles[self.marbles.Count - 1], new(vector.x + 440f, vector.y + 177f), 0, 38f, 1);
            self.CreateMarble(self.marbles[self.marbles.Count - 2], new(vector.x + 440f, vector.y + 177f), 0, 38f, 2);
            self.CreateMarble(self.marbles[self.marbles.Count - 1], new(vector.x + 109f, vector.y + 352f), 0, 42f, 1);
            self.marbles[self.marbles.Count - 1].orbitSpeed = .8f;
            self.CreateMarble(self.marbles[self.marbles.Count - 1], new(vector.x + 109f, vector.y + 352f), 0, 12f, 0);
            for (var d = 0f; d < 2f; d++)
            {
                for (var e = 0f; e < 3f; e++)
                {
                    for (var f = 0f; f < 3f; f++)
                        self.CreateMarble(null, new(vector.x + 100f + e * 30f + d * 320f, vector.y + 400f + f * 30f), 0, 0f, d == 0 ? (e == f ? 2 : ((e == 0f && f == 2f) || (e == 2f && f == 0f)) ? 0 : 1) : (e == f ? 0 : ((e == 0f && f == 2f) || (e == 2f && f == 0f)) ? 2 : 1));
                }
            }
        }
    }

    static void On_SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
    {
        if (!self.oracle.IsCW())
            orig(self, eventName);
        else
        {
            if (string.Equals(eventName, "SHOWBRAINPIC", StringComparison.OrdinalIgnoreCase) || string.Equals(eventName, "unlock", StringComparison.OrdinalIgnoreCase))
            {
                if (self.conversation is not null)
                    self.conversation.paused = true;
                self.inActionCounter = 0;
                self.NewAction(SSOracleBehavior.Action.MeetWhite_Images);
                self.oracle.room.PlaySound(Random.value < .5f ? NewSoundID.CW_AI_Talk_1 : NewSoundID.CW_AI_Talk_2, self.oracle.firstChunk).requireActiveUpkeep = false;
            }
            else if (string.Equals(eventName, "TAKENEURON", StringComparison.OrdinalIgnoreCase))
            {
                self.inActionCounter = 0;
                if (self.greenNeuron is NSHSwarmer swn && !swn.slatedForDeletetion && swn.room == self.oracle.room)
                    TakeNeuron(self, swn);
                else if (self.player?.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                    self.player.Regurgitate();
                    var objLists = self.oracle.room.physicalObjects;
                    for (var i = 0; i < objLists.Length; i++)
                    {
                        var objs = objLists[i];
                        for (var j = 0; j < objs.Count; j++)
                        {
                            if (objs[j] is NSHSwarmer sw)
                            {
                                self.greenNeuron = sw;
                                break;
                            }
                        }
                    }
                    if (self.greenNeuron is NSHSwarmer nsw)
                    {
                        nsw.firstChunk.vel *= 0f;
                        TakeNeuron(self, nsw);
                    }
                }
            }
            else if (string.Equals(eventName, "RELEASENEURON", StringComparison.OrdinalIgnoreCase))
            {
                var run = true;
                OnReleaseNeuron?.Invoke(self, ref run);
                if (!run)
                    return;
                if (self.greenNeuron is NSHSwarmer sw && self.player is not null)
                {
                    sw.firstChunk.HardSetPosition(sw.firstChunk.pos);
                    sw.storyFly = false;
                    sw.firstChunk.vel *= 0f;
                    sw.direction *= 0f;
                    sw.lastDirection *= 0f;
                    sw.firstChunk.mass = .2f;
                    self.greenNeuron = null;
                }
                self.inActionCounter = 0;
                self.action = SSOracleBehavior.Action.MeetWhite_Curious;
            }
            else if (string.Equals(eventName, "UNLOCKPATHS", StringComparison.OrdinalIgnoreCase))
            {
                if (self.currSubBehavior is CWGeneralConversation cv)
                    cv.LockPaths = false;
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                    ns.LockPaths = false;
                self.NewAction(SSOracleBehavior.Action.General_Idle);
                self.conversation?.Destroy();
                self.conversation = null;
            }
            else if (string.Equals(eventName, "LOCKPATHS", StringComparison.OrdinalIgnoreCase))
            {
                if (self.currSubBehavior is CWGeneralConversation cv)
                    cv.LockPaths = true;
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                    ns.LockPaths = true;
            }
            else if (string.Equals(eventName, "GRAV", StringComparison.OrdinalIgnoreCase))
            {
                if (self.currSubBehavior is CWGeneralConversation cv)
                {
                    cv.GravOn = true;
                    cv.PartialGravity = 1f;
                }
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                {
                    ns.GravOn = true;
                    ns.PartialGravity = 1f;
                }
                self.working = 0f;
                self.getToWorking = 0f;
            }
            else if (string.Equals(eventName, "PARTIALGRAV", StringComparison.OrdinalIgnoreCase))
            {
                if (self.currSubBehavior is CWGeneralConversation cv)
                {
                    cv.GravOn = true;
                    cv.PartialGravity = .1f;
                }
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                {
                    ns.GravOn = true;
                    ns.PartialGravity = .1f;
                }
                self.working = 1f;
                self.getToWorking = 1f;
            }
            else
            {
                var run = true;
                OnCustomEvent?.Invoke(self, eventName, ref run);
                if (run && self.currSubBehavior is CWGeneralConversation cv)
                {
                    if (self.conversation is not null)
                        self.conversation.paused = true;
                    self.inActionCounter = 0;
                    cv.Gifts = GiftStates.None;
                    var events = eventName.Split('+');
                    for (var i = 0; i < events.Length; i++)
                    {
                        var ev = events[i];
                        if (string.Equals(ev, "SCAVIMMU", StringComparison.OrdinalIgnoreCase))
                            cv.Gifts |= GiftStates.ScavImmu;
                        else if (string.Equals(ev, "CURE", StringComparison.OrdinalIgnoreCase))
                            cv.Gifts |= GiftStates.Cure;
                        else if (string.Equals(ev, "FOODMAX", StringComparison.OrdinalIgnoreCase))
                            cv.Gifts |= GiftStates.FoodMax;
                        else if (string.Equals(ev, "KARMA10", StringComparison.OrdinalIgnoreCase) || string.Equals(ev, "karma", StringComparison.OrdinalIgnoreCase))
                            cv.Gifts |= GiftStates.Karma10;
                        else if (string.Equals(ev, "MARK", StringComparison.OrdinalIgnoreCase))
                            cv.Gifts |= GiftStates.Mark;
                        else
                            OnCustomGift?.Invoke(self, ev, ref cv.Gifts);
                    }
                    self.NewAction(SSOracleBehavior.Action.General_GiveMark);
                }
            }
        }
    }

    static void TakeNeuron(SSOracleBehavior self, NSHSwarmer gn)
    {
        var run = true;
        OnTakeNeuron?.Invoke(self, ref run);
        if (run && self.currSubBehavior is CWGeneralConversation cv)
        {
            cv.CurrentLookPoint = gn.firstChunk.pos;
            self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            if (gn.grabbedBy.Count > 0)
            {
                for (var num = gn.grabbedBy.Count - 1; num >= 0; num--)
                    gn.grabbedBy[num]?.Release();
                gn.firstChunk.vel.y = 7f;
                var room = self.oracle.room;
                for (var j = 0; j < 7; j++)
                    room.AddObject(new Spark(gn.firstChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 16f, Random.value), gn.myColor, null, 9, 40));
            }
            gn.storyFly = true;
            gn.storyFlyTarget = cv.GrabPos;
            gn.firstChunk.mass = .000001f;
            cv.ActiveNeuronMovement = true;
            if (ModManager.CoopAvailable)
                self.StunCoopPlayers(30);
            else
                self.player?.Stun(30);
        }
    }

    static void On_Oracle_HitByWeapon(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
    {
        if (!self.IsCW())
            orig(self, weapon);
        else if (self.Consious)
            (self.oracleBehavior as SSOracleBehavior)!.ReactToHitWeapon();
    }

    static void On_OracleBehavior_FindPlayer(On.OracleBehavior.orig_FindPlayer orig, OracleBehavior self)
    {
        if (self.oracle.IsCW())
        {
            if (!ModManager.CoopAvailable || self.oracle.room is not Room rm || rm.game.rainWorld.safariMode)
                return;
            var flag = false;
            if (WorldSaveData.TryGetValue(rm.game.GetStorySession.saveState.miscWorldSaveData, out var data) && !data.SeenGreenNeuron)
            {
                if (self.PlayerWithNeuronInStomach is Player pl)
                {
                    flag = true;
                    self.player = pl;
                }
            }
            else
                self.player = rm.game.Players[0]?.realizedCreature as Player;
            if (self.player is not Player p || p.room != rm || p.inShortcut)
            {
                var inrm = self.PlayersInRoom;
                self.player = inrm.Count > 0 ? inrm[0] : null;
                if (self.player is not null)
                {
                    var num = 1;
                    while (!flag && self.player.inShortcut && num < inrm.Count)
                    {
                        self.player = inrm[num];
                        num++;
                    }
                }
            }
            if (self.PlayersInRoom.Count > 0 && self.PlayersInRoom[0].dead && self.player == self.PlayersInRoom[0])
                self.player = null;
            if (self.player is not null)
                rm.game.cameras[0].EnterCutsceneMode(self.player.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
        }
        else
            orig(self);
    }

    static string On_MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
    {
        if (WorldSaveData.TryGetValue(self, out var data))
        {
            var strs = self.unrecognizedSaveStrings;
            int i, j;
            if ((i = strs.IndexOf("M4R_CW_seenGreenNeuron")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.SeenGreenNeuron.ToString());
                else
                    strs[i + 1] = data.SeenGreenNeuron.ToString();
                while ((j = strs.LastIndexOf("M4R_CW_seenGreenNeuron")) != i)
                {
                    if (j != strs.Count - 1)
                        strs.RemoveAt(j + 1);
                    strs.RemoveAt(j);
                }
            }
            else
            {
                strs.Add("M4R_CW_seenGreenNeuron");
                strs.Add(data.SeenGreenNeuron.ToString());
            }
            if ((i = strs.IndexOf("M4R_CW_annoyedCnt")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.AnnoyedCounter.ToString(CultureInfo.InvariantCulture));
                else
                    strs[i + 1] = data.AnnoyedCounter.ToString(CultureInfo.InvariantCulture);
                while ((j = strs.LastIndexOf("M4R_CW_annoyedCnt")) != i)
                {
                    if (j != strs.Count - 1)
                        strs.RemoveAt(j + 1);
                    strs.RemoveAt(j);
                }
            }
            else
            {
                strs.Add("M4R_CW_annoyedCnt");
                strs.Add(data.AnnoyedCounter.ToString(CultureInfo.InvariantCulture));
            }
            if ((i = strs.IndexOf("M4R_CW_numberOfConversations")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.NumberOfConversations.ToString(CultureInfo.InvariantCulture));
                else
                    strs[i + 1] = data.NumberOfConversations.ToString(CultureInfo.InvariantCulture);
                while ((j = strs.LastIndexOf("M4R_CW_numberOfConversations")) != i)
                {
                    if (j != strs.Count - 1)
                        strs.RemoveAt(j + 1);
                    strs.RemoveAt(j);
                }
            }
            else
            {
                strs.Add("M4R_CW_numberOfConversations");
                strs.Add(data.NumberOfConversations.ToString(CultureInfo.InvariantCulture));
            }
            if ((i = strs.IndexOf("M4R_CW_state")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.OracleState.ToString());
                else
                    strs[i + 1] = data.OracleState.ToString();
                while ((j = strs.LastIndexOf("M4R_CW_state")) != i)
                {
                    if (j != strs.Count - 1)
                        strs.RemoveAt(j + 1);
                    strs.RemoveAt(j);
                }
            }
            else
            {
                strs.Add("M4R_CW_state");
                strs.Add(data.OracleState.ToString());
            }
            if (ModManager.MSC && self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                if ((i = strs.IndexOf("M4R_CW_tagSprPrl")) != -1)
                {
                    if (i == strs.Count - 1)
                        strs.Add(data.SeenSpearmasterTaggedPearl.ToString());
                    else
                        strs[i + 1] = data.SeenSpearmasterTaggedPearl.ToString();
                    while ((j = strs.LastIndexOf("M4R_CW_tagSprPrl")) != i)
                    {
                        if (j != strs.Count - 1)
                            strs.RemoveAt(j + 1);
                        strs.RemoveAt(j);
                    }
                }
                else
                {
                    strs.Add("M4R_CW_tagSprPrl");
                    strs.Add(data.SeenSpearmasterTaggedPearl.ToString());
                }
            }
            var scavImmu = data.ScavImmunity ? "Y" : "N";
            if ((i = strs.IndexOf("M4R_CW_scavImmu")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(scavImmu);
                else
                    strs[i + 1] = scavImmu;
                while ((j = strs.LastIndexOf("M4R_CW_scavImmu")) != i)
                {
                    if (j != strs.Count - 1)
                        strs.RemoveAt(j + 1);
                    strs.RemoveAt(j);
                }
            }
            else
            {
                strs.Add("M4R_CW_scavImmu");
                strs.Add(scavImmu);
            }
            if (self.saveStateNumber == SlugcatStats.Name.Red)
            {
                var redCycles = data.AdditionalRedCycles ? "Y" : "N";
                if ((i = strs.IndexOf("M4R_CW_redCycles")) != -1)
                {
                    if (i == strs.Count - 1)
                        strs.Add(redCycles);
                    else
                        strs[i + 1] = redCycles;
                    while ((j = strs.LastIndexOf("M4R_CW_redCycles")) != i)
                    {
                        if (j != strs.Count - 1)
                            strs.RemoveAt(j + 1);
                        strs.RemoveAt(j);
                    }
                }
                else
                {
                    strs.Add("M4R_CW_redCycles");
                    strs.Add(redCycles);
                }
            }
        }
        return orig(self);
    }

    static void On_MiscWorldSaveData_FromString(On.MiscWorldSaveData.orig_FromString orig, MiscWorldSaveData self, string s)
    {
        orig(self, s);
        if (WorldSaveData.TryGetValue(self, out var data))
        {
            var flag = ModManager.MSC && self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear;
            var flag2 = self.saveStateNumber == SlugcatStats.Name.Red;
            var unrec = self.unrecognizedSaveStrings;
            for (var i = 0; i < unrec.Count - 1; i++)
            {
                var str = unrec[i];
                if (string.Equals(str, "M4R_CW_seenGreenNeuron", StringComparison.OrdinalIgnoreCase))
                    bool.TryParse(unrec[i + 1], out data.SeenGreenNeuron);
                else if (string.Equals(str, "M4R_CW_annoyedCnt", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(unrec[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out data.AnnoyedCounter);
                else if (string.Equals(str, "M4R_CW_numberOfConversations", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(unrec[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out data.NumberOfConversations);
                else if (string.Equals(str, "M4R_CW_state", StringComparison.OrdinalIgnoreCase))
                    data.OracleState.FromString(unrec[i + 1]);
                else if (flag && string.Equals(str, "M4R_CW_tagSprPrl", StringComparison.OrdinalIgnoreCase))
                    bool.TryParse(unrec[i + 1], out data.SeenSpearmasterTaggedPearl);
                else if (string.Equals(str, "M4R_CW_scavImmu", StringComparison.OrdinalIgnoreCase))
                    data.ScavImmunity = string.Equals(unrec[i + 1], "Y", StringComparison.OrdinalIgnoreCase);
                else if (flag2 && string.Equals(str, "M4R_CW_redCycles", StringComparison.OrdinalIgnoreCase))
                    data.AdditionalRedCycles = string.Equals(unrec[i + 1], "Y", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    static void On_SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
    {
        orig(self, saveStateNumber, progression);
        if (ModManager.Expedition && Custom.rainWorld.ExpeditionMode && WorldSaveData.TryGetValue(self.miscWorldSaveData, out var data))
            data.NumberOfConversations = 1;
    }

    static void On_MiscWorldSaveData_ctor(On.MiscWorldSaveData.orig_ctor orig, MiscWorldSaveData self, SlugcatStats.Name saveStateNumber)
    {
        orig(self, saveStateNumber);
        if (!WorldSaveData.TryGetValue(self, out _))
            WorldSaveData.Add(self, new(new(false, saveStateNumber)));
    }

    static void On_SSOracleBehavior_ResumePausedPearlConversation(On.SSOracleBehavior.orig_ResumePausedPearlConversation orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            var run = true;
            OnResumePausedPearlConversation?.Invoke(self, ref run);
            if (!run || CWConversation.CWLinesFromFile("ResumePausedPearlConversation", self.oracle.room.game.StoryCharacter?.value) is not string[] lns || lns.Length == 0)
                return;
            self.pearlConversation.Interrupt(lns[Random.Range(0, lns.Length)], 10);
            self.restartConversationAfterCurrentDialoge = true;
        }
    }

    static void On_SSOracleBehavior_InterruptPearlMessagePlayerLeaving(On.SSOracleBehavior.orig_InterruptPearlMessagePlayerLeaving orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            var run = true;
            OnInterruptPearlMessagePlayerLeaving?.Invoke(self, ref run);
            if (!run || CWConversation.CWLinesFromFile("InterruptPearlMessagePlayerLeaving", self.oracle.room.game.StoryCharacter?.value) is not string[] lns || lns.Length == 0)
                return;
            self.pearlConversation.Interrupt(lns[Random.Range(0, lns.Length)], 10);
        }
    }

    static void On_SSOracleBehavior_StartItemConversation(On.SSOracleBehavior.orig_StartItemConversation orig, SSOracleBehavior self, DataPearl item)
    {
        if (!self.oracle.IsCW())
            orig(self, item);
        else
        {
            if (!WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
                return;
            self.isRepeatedDiscussion = data.OracleState.alreadyTalkedAboutItems.Contains(item.abstractPhysicalObject.ID);
            if (self.pearlConversation is CWPearlConversation mc)
            {
                mc.Interrupt("...", 0);
                mc.Destroy();
                self.pearlConversation = null;
            }
            if (self.conversation is CWConversation co)
            {
                co.Interrupt("...", 0);
                co.Destroy();
                self.conversation = null;
            }
            var dataPearlType = item.AbstractPearl.dataPearlType;
            if (ModManager.MSC && item.AbstractPearl is SpearMasterPearl.AbstractSpearMasterPearl sp)
            {
                Conversation.ID id;
                if (sp.broadcastTagged)
                {
                    if (data.SeenSpearmasterTaggedPearl)
                        id = Conversation.ID.None;
                    else
                    {
                        id = ConversationID.CWSpearPearlAfterMoon;
                        data.SeenSpearmasterTaggedPearl = true;
                    }
                }
                else
                {
                    if (self.isRepeatedDiscussion)
                        id = Conversation.ID.None;
                    else
                        id = MoreSlugcatsEnums.ConversationID.Moon_Spearmaster_Pearl;
                }
                if (!data.OracleState.significantPearls.Contains(dataPearlType))
                    data.OracleState.significantPearls.Add(dataPearlType);
                self.pearlConversation = new CWPearlConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                ++data.OracleState.totalPearlsBrought;
            }
            else if (dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc || dataPearlType.Index == -1)
                self.pearlConversation = new CWPearlConversation(Conversation.ID.Moon_Pearl_Misc, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            else if (dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc2)
                self.pearlConversation = new CWPearlConversation(Conversation.ID.Moon_Pearl_Misc2, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            else if (ModManager.MSC && dataPearlType == MoreSlugcatsEnums.DataPearlType.BroadcastMisc)
                self.pearlConversation = new CWPearlConversation(MoreSlugcatsEnums.ConversationID.Moon_Pearl_BroadcastMisc, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            else if (dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl)
                self.pearlConversation = new CWPearlConversation(Conversation.ID.Moon_Pebbles_Pearl, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            else
            {
                var id = Conversation.DataPearlToConversation(dataPearlType);
                if (!data.OracleState.significantPearls.Contains(dataPearlType))
                    data.OracleState.significantPearls.Add(dataPearlType);
                self.pearlConversation = new CWPearlConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                ++data.OracleState.totalPearlsBrought;
            }
            if (!self.isRepeatedDiscussion)
            {
                ++data.OracleState.totalItemsBrought;
                data.OracleState.AddItemToAlreadyTalkedAbout(item.abstractPhysicalObject.ID);
            }
            self.talkedAboutThisSession.Add(item.abstractPhysicalObject.ID);
        }
    }

    static void On_SSOracleBehavior_UnconciousUpdate(On.SSOracleBehavior.orig_UnconciousUpdate orig, SSOracleBehavior self)
    {
        orig(self);
        if (self.oracle.IsCW())
        {
            var run = true;
            OnUnconciousUpdate?.Invoke(self, ref run);
            if (!run)
                return;
            var cams = self.oracle.room.game.cameras;
            for (var i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam.room == self.oracle.room && !cam.AboutToSwitchRoom)
                    cam.ChangeBothPalettes(10, 26, .51f + Mathf.Sin(self.unconciousTick * .257079631f) * .35f);
            }
            self.unconciousTick += 1f;
        }
    }

    static void On_SSOracleBehavior_ReactToHitWeapon(On.SSOracleBehavior.orig_ReactToHitWeapon orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            var res = true;
            OnReactToHitWeapon?.Invoke(self, ref res);
            if (!res)
                return;
            self.oracle.room.PlaySound(Random.value < .5f ? NewSoundID.CW_AI_Angry_1 : NewSoundID.CW_AI_Angry_2, self.oracle.firstChunk).requireActiveUpkeep = false;
            if (self.conversation is not null || self.pearlConversation is not null)
            {
                if (self.conversation is not null)
                    self.conversation.paused = true;
                if (self.pearlConversation is not null)
                    self.pearlConversation.paused = true;
                self.restartConversationAfterCurrentDialoge = true;
                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("ReactToHitWeaponWhileTalking", self.oracle.room.game.StoryCharacter?.value) is string[] lns && lns.Length > 0)
                    self.dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
            }
            else if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("ReactToHitWeapon", self.oracle.room.game.StoryCharacter?.value) is string[] lns && lns.Length > 0)
                self.dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
            if (WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data) && data.AnnoyedCounter < 4)
                ++data.AnnoyedCounter;
            else
                self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
        }
    }

    static void On_SSOracleBehavior_CreatureJokeDialog(On.SSOracleBehavior.orig_CreatureJokeDialog orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
    }

    static bool On_SSOracleBehavior_HandTowardsPlayer(On.SSOracleBehavior.orig_HandTowardsPlayer orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            return orig(self);
        if ((self.currSubBehavior is not CWThrowOut th || !th.telekinThrowOut || self.player is not Player p || p.dead) && (self.action != SSOracleBehavior.Action.General_GiveMark || self.inActionCounter <= 30 || self.inActionCounter >= 300) && self.action != SSOracleBehavior.Action.ThrowOut_KillOnSight)
            return false;
        return true;
    }

    static void On_SSOracleBehavior_Move(On.SSOracleBehavior.orig_Move orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Idle)
            {
                self.invstAngSpeed = 1f;
                if (self.investigateMarble is null && self.oracle.marbles.Count > 0)
                    self.investigateMarble = self.oracle.marbles[Random.Range(0, self.oracle.marbles.Count)];
                if (self.investigateMarble is not null && (self.investigateMarble.orbitObj == self.oracle || Custom.DistLess(new(250f, 150f), self.investigateMarble.firstChunk.pos, 100f)))
                    self.investigateMarble = null;
                if (self.investigateMarble is not null)
                {
                    self.lookPoint = self.investigateMarble.firstChunk.pos;
                    if (Custom.DistLess(self.nextPos, self.investigateMarble.firstChunk.pos, 100f))
                    {
                        self.floatyMovement = true;
                        self.nextPos = self.investigateMarble.firstChunk.pos - Custom.DegToVec(self.investigateAngle) * 50f;
                    }
                    else
                        self.SetNewDestination(self.investigateMarble.firstChunk.pos - Custom.DegToVec(self.investigateAngle) * 50f);
                    if (self.pathProgression == 1f && Random.value < .005f)
                        self.investigateMarble = null;
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate)
            {
                if (self.nextPos != self.oracle.room.MiddleOfTile(24, 17))
                    self.SetNewDestination(self.oracle.room.MiddleOfTile(24, 17));
                self.investigateAngle = 0f;
                self.lookPoint = self.oracle.firstChunk.pos + new Vector2(0f, -40f);
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.KeepDistance)
            {
                if (self.player is null)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    self.lookPoint = self.player.DangerPos;
                    var vector = new Vector2(Random.value * self.oracle.room.PixelWidth, Random.value * self.oracle.room.PixelHeight);
                    if (!self.oracle.room.GetTile(vector).Solid && self.oracle.room.aimap.getTerrainProximity(vector) > 2 && Vector2.Distance(vector, self.player.DangerPos) > Vector2.Distance(self.nextPos, self.player.DangerPos) + 100f)
                        self.SetNewDestination(vector);
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Investigate)
            {
                if (self.player is null)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    self.lookPoint = self.player.DangerPos;
                    if (self.investigateAngle < -90f || self.investigateAngle > 90f || self.oracle.room.aimap.getTerrainProximity(self.nextPos) < 2f)
                    {
                        self.investigateAngle = Mathf.Lerp(-70f, 70f, Random.value);
                        self.invstAngSpeed = Mathf.Lerp(.4f, .8f, Random.value) * (Random.value < .5f ? (-1f) : 1f);
                    }
                    var vector = self.player.DangerPos + Custom.DegToVec(self.investigateAngle) * 150f;
                    if (self.oracle.room.aimap.getTerrainProximity(vector) >= 2f)
                    {
                        if (self.pathProgression > .9f)
                        {
                            if (Custom.DistLess(self.oracle.firstChunk.pos, vector, 30f))
                                self.floatyMovement = true;
                            else if (!Custom.DistLess(self.nextPos, vector, 30f))
                                self.SetNewDestination(vector);
                        }
                        self.nextPos = vector;
                    }
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Talk)
            {
                if (self.player is null)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    self.lookPoint = self.player.DangerPos;
                    var vector = new Vector2(Random.value * self.oracle.room.PixelWidth, Random.value * self.oracle.room.PixelHeight);
                    if (self.CommunicatePosScore(vector) + 40f < self.CommunicatePosScore(self.nextPos) && !Custom.DistLess(vector, self.nextPos, 30f))
                        self.SetNewDestination(vector);
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.ShowMedia)
            {
                if (self.currSubBehavior is CWGeneralConversation sbhv)
                    sbhv.ShowMediaMovementBehavior();
            }
            if (self.currSubBehavior?.LookPoint is Vector2 vec)
                self.lookPoint = vec;
            ++self.consistentBasePosCounter;
            if (self.oracle.room.readyForAI)
            {
                var vector = new Vector2(Random.value * self.oracle.room.PixelWidth, Random.value * self.oracle.room.PixelHeight);
                if (!self.oracle.room.GetTile(vector).Solid && self.BasePosScore(vector) + 40f < self.BasePosScore(self.baseIdeal))
                {
                    self.baseIdeal = vector;
                    self.consistentBasePosCounter = 0;
                }
            }
            else
                self.baseIdeal = self.nextPos;
            OnMove?.Invoke(self);
        }
    }

    static void On_SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
    {
        if (!self.oracle.IsCW())
            orig(self, nextAction);
        else
        {
            var run = true;
            OnNewAction?.Invoke(self, ref run);
            if (!run || nextAction == self.action)
                return;
            var subbhv = SSOracleBehavior.SubBehavior.SubBehavID.General;
            if (nextAction is not null)
            {
                if (nextAction.value.Contains("MeetWhite"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.MeetWhite;
                else if (nextAction.value.Contains("GetNeuron"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.GetNeuron;
                else if (nextAction.value.Contains("ThrowOut"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.ThrowOut;
            }
            self.currSubBehavior?.NewAction(self.action, nextAction);
            if (subbhv != SSOracleBehavior.SubBehavior.SubBehavID.General && subbhv != self.currSubBehavior?.ID)
            {
                SSOracleBehavior.SubBehavior? subBehavior = null;
                for (var i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == subbhv)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }
                if (subBehavior is null)
                {
                    if (subbhv == SSOracleBehavior.SubBehavior.SubBehavID.MeetWhite)
                    {
                        if ((self.greenNeuron is not null || self.player.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer) && !self.HasSeenGreenNeuron)
                        {
                            if (WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
                                data.SeenGreenNeuron = true;
                            var s = self.oracle.room.game.StoryCharacter.value + "_FirstEncounter_WithNeuron";
                            var id = new Conversation.ID("E") { value = s, valueHash = s.GetHashCode() };
                            var bhv = new CWGeneralConversation(self, id);
                            subBehavior = bhv;
                            self.InitateConversation(id, bhv);
                        }
                        else
                        {
                            var s = self.oracle.room.game.StoryCharacter.value + "_FirstEncounter";
                            var id = new Conversation.ID("E") { value = s, valueHash = s.GetHashCode() };
                            var bhv = new CWGeneralConversation(self, id);
                            subBehavior = bhv;
                            self.InitateConversation(id, bhv);
                        }
                    }
                    else if (subbhv == SSOracleBehavior.SubBehavior.SubBehavID.ThrowOut)
                        subBehavior = new CWThrowOut(self);
                    else if (subbhv == SSOracleBehavior.SubBehavior.SubBehavID.GetNeuron)
                    {
                        var s = self.oracle.room.game.StoryCharacter.value + "_GetNeuron";
                        var id = new Conversation.ID("E") { value = s, valueHash = s.GetHashCode() };
                        var bhv = new CWGeneralConversation(self, id);
                        subBehavior = bhv;
                        self.InitateConversation(id, bhv);
                    }
                    self.allSubBehaviors.Add(subBehavior);
                }
                subBehavior?.Activate(self.action, nextAction);
                if (subBehavior is CWGeneralConversation cv)
                {
                    if (self.currSubBehavior is CWNoSubBehavior ns)
                        cv.SeenPlayer = ns.SeenPlayer;
                    else if (self.currSubBehavior is CWGeneralConversation cv2)
                        cv.SeenPlayer = cv2.SeenPlayer;
                }
                self.currSubBehavior?.Deactivate();
                self.currSubBehavior = subBehavior;
            }
            self.inActionCounter = 0;
            self.action = nextAction;
        }
    }

    static void On_SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        if (!self.oracle.IsCW())
            orig(self, eu);
        else
        {
            if (self.oracle.room is not Room rm)
                return;
            var sess = rm.game.GetStorySession;
            var deathData = sess.saveState.deathPersistentSaveData;
            int i, j;
            Creature.Grasp[] gs;
            List<Player> players;
            if (self.inspectPearl is DataPearl p)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
                for (i = 0; i < p.grabbedBy.Count; i++)
                {
                    if (p.grabbedBy[i]?.grabber is Creature c)
                    {
                        gs = c.grasps;
                        for (j = 0; j < gs.Length; j++)
                        {
                            if (gs[j]?.grabbed == p)
                                c.ReleaseGrasp(j);
                        }
                    }
                }
                var fs = p.firstChunk;
                var vector = self.oracle.firstChunk.pos - fs.pos;
                var num = Custom.Dist(self.oracle.firstChunk.pos, fs.pos);
                fs.vel += Vector2.ClampMagnitude(vector, 40f) / 40f * Mathf.Clamp(2f - num / 200f * 2f, .5f, 2f);
                if (fs.vel.magnitude < 1f && num < 16f)
                    fs.vel = Custom.RNV() * 8f;
                if (fs.vel.magnitude > 8f)
                    fs.vel /= 2f;
                if (num < 100f && self.pearlConversation is null && self.conversation is null)
                    self.StartItemConversation(p);
            }
            self.UpdateStoryPearlCollection();
            if (self.timeSinceSeenPlayer >= 0)
                ++self.timeSinceSeenPlayer;
            if (self.pearlPickupReaction && self.timeSinceSeenPlayer > 300 && deathData.theMark && self.currSubBehavior is not CWThrowOut)
            {
                var flag = false;
                if (self.player is Player player)
                {
                    gs = player.grasps;
                    for (i = 0; i < gs.Length; i++)
                    {
                        if (gs[i]?.grabbed is PebblesPearl prl && prl.AbstractPearl.type == AbstractPhysicalObjectType.CWPearl)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag && !self.lastPearlPickedUp && (self.conversation is not CWConversation conv || (conv.age > 300 && !conv.paused)))
                {
                    if (self.conversation is not null)
                    {
                        self.conversation.paused = true;
                        self.restartConversationAfterCurrentDialoge = true;
                    }
                    if (CWConversation.CWLinesFromFile("PearlPickupReaction", self.oracle.room.game.StoryCharacter?.value) is string[] lns && lns.Length > 0)
                        self.dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
                    self.pearlPickupReaction = false;
                }
                self.lastPearlPickedUp = flag;
            }
            if (self.conversation is CWConversation cwc)
            {
                if (self.restartConversationAfterCurrentDialoge && cwc.paused && self.action != SSOracleBehavior.Action.General_GiveMark && self.dialogBox.messages.Count == 0 && self.player?.room == rm)
                {
                    cwc.paused = false;
                    self.restartConversationAfterCurrentDialoge = false;
                    cwc.RestartCurrent();
                }
            }
            else if (self.pearlConversation is CWPearlConversation pconv)
            {
                if (pconv.slatedForDeletion)
                {
                    self.pearlConversation = null;
                    if (self.inspectPearl is DataPearl pcp && self.player is Player pl)
                    {
                        pcp.firstChunk.vel = Custom.DirVec(pcp.firstChunk.pos, pl.mainBodyChunk.pos) * 3f;
                        self.readDataPearlOrbits.Add(pcp.AbstractPearl);
                        self.inspectPearl = null;
                    }
                }
                else
                {
                    pconv.Update();
                    if (self.player?.room != rm || (ModManager.CoopAvailable && self.PlayersInRoom.Count == 0))
                    {
                        if (!pconv.paused)
                        {
                            pconv.paused = true;
                            self.InterruptPearlMessagePlayerLeaving();
                        }
                    }
                    else if (pconv.paused && !self.restartConversationAfterCurrentDialoge)
                        self.ResumePausedPearlConversation();
                    if (pconv.paused && self.restartConversationAfterCurrentDialoge && self.dialogBox.messages.Count == 0)
                    {
                        pconv.paused = false;
                        self.restartConversationAfterCurrentDialoge = false;
                        pconv.RestartCurrent();
                    }
                }
            }
            else
                self.restartConversationAfterCurrentDialoge = false;
            if (self.voice is ChunkSoundEmitter voice)
            {
                voice.alive = true;
                if (voice.slatedForDeletetion)
                    self.voice = null;
            }
            if (ModManager.MSC && rm.game.rainWorld.safariMode is true)
            {
                self.safariCreature = null;
                var num = float.MaxValue;
                var crits = rm.abstractRoom.creatures;
                for (i = 0; i < crits.Count; i++)
                {
                    if (crits[i].realizedCreature is Creature rlc)
                    {
                        var num2 = Custom.Dist(self.oracle.firstChunk.pos, rlc.mainBodyChunk.pos);
                        if (num2 < num)
                        {
                            num = num2;
                            self.safariCreature = rlc;
                        }
                    }
                }
            }
            self.FindPlayer();
            var cams = rm.game.cameras;
            for (i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam.room == rm)
                    cam.virtualMicrophone.volumeGroups[2] = 1f - rm.gravity;
                else
                    cam.virtualMicrophone.volumeGroups[2] = 1f;
            }
            if (!self.oracle.Consious)
                return;
            self.unconciousTick = 0f;
            self.currSubBehavior?.Update();
            if (self.oracle.slatedForDeletetion)
                return;
            self.conversation?.Update();
            if (self.currSubBehavior?.CurrentlyCommunicating is false or null && self.pearlConversation is null)
                self.pathProgression = Mathf.Min(1f, self.pathProgression + 1f / Mathf.Lerp(40f + self.pathProgression * 80f, Vector2.Distance(self.lastPos, self.nextPos) / 5f, .5f));
            self.currentGetTo = Custom.Bezier(self.lastPos, self.ClampVectorInRoom(self.lastPos + self.lastPosHandle), self.nextPos, self.ClampVectorInRoom(self.nextPos + self.nextPosHandle), self.pathProgression);
            self.floatyMovement = false;
            self.investigateAngle += self.invstAngSpeed;
            ++self.inActionCounter;
            if (self.player?.room != rm || (ModManager.CoopAvailable && self.PlayersInRoom.Count == 0))
            {
                self.killFac = 0f;
                ++self.playerOutOfRoomCounter;
            }
            if (self.pathProgression >= 1f && self.consistentBasePosCounter > 100 && !self.oracle.arm.baseMoving)
                ++self.allStillCounter;
            else
                self.allStillCounter = 0;
            self.lastKillFac = self.killFac;
            if (self.action == SSOracleBehavior.Action.General_Idle)
            {
                if (self.movementBehavior != SSOracleBehavior.MovementBehavior.Idle && self.movementBehavior != SSOracleBehavior.MovementBehavior.Meditate)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                self.throwOutCounter = 0;
                if (self.player?.room == rm)
                {
                    ++self.discoverCounter;
                    if (rm.GetTilePosition(self.player.mainBodyChunk.pos).y < 32 && (self.discoverCounter > 220 || Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.firstChunk.pos, 150f) || !Custom.DistLess(self.player.mainBodyChunk.pos, rm.MiddleOfTile(rm.ShortcutLeadingToNode(1).StartTile), 150f)))
                        self.SeePlayer();
                }
            }
            else if (self.action == SSOracleBehavior.Action.General_GiveMark)
            {
                if (self.currSubBehavior is not CWGeneralConversation cv || self.player is not Player pl)
                    return;
                self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                if (self.inActionCounter > 30 && self.inActionCounter < 300)
                {
                    if (self.inActionCounter < 300)
                    {
                        if (ModManager.CoopAvailable)
                            self.StunCoopPlayers(20);
                        else
                            pl.Stun(20);
                    }
                    var vector2 = Vector2.ClampMagnitude(rm.MiddleOfTile(24, 14) - pl.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, self.inActionCounter);
                    if (ModManager.CoopAvailable)
                    {
                        players = self.PlayersInRoom;
                        for (i = 0; i < players.Count; i++)
                            players[i].mainBodyChunk.vel += vector2;
                    }
                    else
                        pl.mainBodyChunk.vel += vector2;
                }
                if (self.inActionCounter == 30)
                    rm.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                if (self.inActionCounter == 300)
                {
                    pl.mainBodyChunk.vel += Custom.RNV() * 10f;
                    pl.bodyChunks[1].vel += Custom.RNV() * 10f;
                    if ((cv.Gifts & GiftStates.FoodMax) == GiftStates.FoodMax)
                        pl.AddFood(pl.MaxFoodInStomach);
                    if (ModManager.CoopAvailable)
                        self.StunCoopPlayers(40);
                    else
                        pl.Stun(40);
                    if ((cv.Gifts & GiftStates.Mark) == GiftStates.Mark)
                        deathData.theMark = true;
                    if ((cv.Gifts & GiftStates.Cure) == GiftStates.Cure)
                    {
                        if (WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var dt))
                            dt.AdditionalRedCycles = true;
                        if (rm.game.cameras[0].hud is HUD.HUD hud)
                        {
                            if (hud.textPrompt is TextPrompt tpr)
                                tpr.cycleTick = 0;
                            hud.map?.cycleLabel?.UpdateCycleText();
                        }
                        if (ModManager.CoopAvailable)
                        {
                            var aliveP = rm.game.AlivePlayers;
                            for (i = 0; i < aliveP.Count; i++)
                            {
                                var apl = aliveP[i];
                                if (apl.Room == rm.abstractRoom)
                                    (apl.realizedCreature as Player)?.redsIllness?.GetBetter();
                            }
                        }
                        else
                            pl.redsIllness?.GetBetter();
                    }
                    if ((cv.Gifts & GiftStates.ScavImmu) == GiftStates.ScavImmu && WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var data))
                        data.ScavImmunity = true;
                    else if ((cv.Gifts & GiftStates.Karma10) == GiftStates.Karma10)
                        deathData.karma = deathData.karmaCap = 9;
                    for (i = 0; i < cams.Length; i++)
                        cams[i].hud.karmaMeter?.UpdateGraphic();
                    Vector2 ps;
                    if (ModManager.CoopAvailable)
                    {
                        players = self.PlayersInRoom;
                        for (i = 0; i < players.Count; i++)
                        {
                            ps = players[i].mainBodyChunk.pos;
                            for (j = 0; j < 20; j++)
                                rm.AddObject(new Spark(ps, Custom.RNV() * Random.value * 40f, Color.white, null, 30, 120));
                        }
                    }
                    else
                    {
                        ps = pl.mainBodyChunk.pos;
                        for (i = 0; i < 20; i++)
                            rm.AddObject(new Spark(ps, Custom.RNV() * Random.value * 40f, Color.white, null, 30, 120));
                    }
                    rm.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);
                }
                if (self.inActionCounter > 300 && self.player?.graphicsModule is PlayerGraphics pgr && (cv.Gifts & GiftStates.Mark) == GiftStates.Mark)
                    pgr.markAlpha = Mathf.Max(pgr.markAlpha, Mathf.InverseLerp(500f, 300f, self.inActionCounter));
                if (self.inActionCounter >= 500 && self.conversation is Conversation co)
                    co.paused = false;
            }
            self.Move();
            if (self.player?.room == rm && self.conversation is null && self.inspectPearl is null)
            {
                var physicalObjects = rm.physicalObjects;
                for (i = 0; i < physicalObjects.Length; i++)
                {
                    var list = physicalObjects[i];
                    for (j = 0; j < list.Count; j++)
                    {
                        if (self.inspectPearl is not null)
                            goto DOUBLE_BREAK;
                        if (list[j] is DataPearl dpearl && dpearl.grabbedBy.Count == 0 && dpearl.AbstractPearl.dataPearlType != DataPearlType.CWPearl && !self.readDataPearlOrbits.Contains(dpearl.AbstractPearl) && deathData.theMark && !self.talkedAboutThisSession.Contains(dpearl.abstractPhysicalObject.ID))
                            self.inspectPearl = dpearl;
                    }
                }
            }
        DOUBLE_BREAK:
            if (self.working != self.getToWorking)
                self.working = Custom.LerpAndTick(self.working, self.getToWorking, .05f, 1f / 30f);
            if (self.currSubBehavior?.LowGravity >= 0f)
                rm.gravity = Custom.LerpAndTick(rm.gravity, self.currSubBehavior.LowGravity, .05f, .02f);
            else
            {
                if (self.currSubBehavior?.Gravity is false)
                    rm.gravity = Custom.LerpAndTick(rm.gravity, 0f, .05f, .02f);
                else
                    rm.gravity = 1f - self.working;
            }
            if (rm.gravity < .1f)
                rm.gravity = .1f;
            for (i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam.room == rm && !cam.AboutToSwitchRoom && cam.paletteBlend != 1f - rm.gravity)
                    cam.ChangeBothPalettes(25, 26, 1f - rm.gravity);
            }
            OnUpdate?.Invoke(self);
        }
    }

    static bool On_SSOracleBehavior_get_HasSeenGreenNeuron(Func<SSOracleBehavior, bool> orig, SSOracleBehavior self)
    {
        if (self.oracle.IsCW())
            return WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data) && data.SeenGreenNeuron;
        return orig(self);
    }

    static void On_SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        if (!self.oracle.IsCW())
            orig(self);
        else
        {
            var msc = ModManager.MSC;
            var rm = self.oracle.room;
            if (self.timeSinceSeenPlayer < 0)
                self.timeSinceSeenPlayer = 0;
            int i, j;
            if (ModManager.CoopAvailable && self.timeSinceSeenPlayer < 5)
            {
                Player? player = null;
                foreach (Player item in from x in rm.game.NonPermaDeadPlayers
                                        where x.Room != rm.abstractRoom
                                        select x.realizedCreature as Player into x
                                        orderby x.slugOnBack is not null
                                        select x)
                {
                    item.slugOnBack?.DropSlug();
                    //JollyCoop.JollyCustom.Log($"Warping player to CW room, {item} - back occupied?{item.slugOnBack}");
                    try
                    {
                        var worldCoordinate = rm.LocalCoordinateOfNode(1);
                        JollyCoop.JollyCustom.MovePlayerWithItems(item, item.room, rm.abstractRoom.name, worldCoordinate);
                        var down = Vector2.down;
                        var chs = item.bodyChunks;
                        var md = rm.MiddleOfTile(worldCoordinate);
                        for (i = 0; i < chs.Length; i++)
                        {
                            var ch = chs[i];
                            ch.HardSetPosition(md - down * (-.5f + i) * 5f);
                            ch.vel = down * 2f;
                        }
                    }
                    catch (Exception ex)
                    {
                        JollyCoop.JollyCustom.Log("Failed to move player " + ex, true);
                    }
                    if (player is null && item.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
                    {
                        player = item;
                        //JollyCoop.JollyCustom.Log($"Found player with neuron in stomach, focusing ... {item}");
                    }
                }
                if (player is not null)
                    self.player = player;
            }
            self.greenNeuron = null;
            var objLists = rm.physicalObjects;
            for (i = 0; i < objLists.Length; i++)
            {
                var objs = objLists[i];
                for (j = 0; j < objs.Count; j++)
                {
                    if (objs[j] is NSHSwarmer sw)
                    {
                        self.greenNeuron = sw;
                        break;
                    }
                }
            }
            var run = true;
            OnSeePlayer?.Invoke(self, ref run);
            if (!run)
                return;
            var dataFlag = WorldSaveData.TryGetValue(rm.game.GetStorySession.saveState.miscWorldSaveData, out var data);
            var car = rm.game.StoryCharacter;
            if (dataFlag && data.NumberOfConversations == 0)
            {
                if (self.currSubBehavior is CWGeneralConversation cv)
                    cv.SeenPlayer = true;
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                    ns.SeenPlayer = true;
                self.NewAction(SSOracleBehavior.Action.MeetWhite_Shocked);
            }
            else if ((self.greenNeuron is not null || self.player.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer) && !self.HasSeenGreenNeuron && dataFlag)
            {
                data.SeenGreenNeuron = true;
                if (self.currSubBehavior is CWGeneralConversation cv)
                    cv.SeenPlayer = true;
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                    ns.SeenPlayer = true;
                self.NewAction(SSOracleBehavior.Action.GetNeuron_Init);
            }
            else
            {
                if (dataFlag && data.AnnoyedCounter < 4)
                    self.SlugcatEnterRoomReaction();
                else
                {
                    if (self.currSubBehavior is CWGeneralConversation cv)
                        cv.SeenPlayer = true;
                    else if (self.currSubBehavior is CWNoSubBehavior ns)
                        ns.SeenPlayer = true;
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                }
            }
        }
    }

    static void On_SSOracleBehavior_InitateConversation(On.SSOracleBehavior.orig_InitateConversation orig, SSOracleBehavior self, Conversation.ID convoId, SSOracleBehavior.ConversationBehavior convBehav)
    {
        if (self.oracle.IsCW())
        {
            if (self.conversation is CWConversation conv)
            {
                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                    conv.Interrupt("...", 0);
                conv.Destroy();
            }
            if (convoId?.value is string s)
                self.conversation = new CWConversation(self, convBehav, s, self.dialogBox);
        }
        else
            orig(self, convoId, convBehav);
    }

    static void On_SSOracleBehavior_ctor(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.IsCW())
        {
            self.InitStoryPearlCollection();
            self.currSubBehavior.Deactivate();
            self.allSubBehaviors.RemoveAt(self.allSubBehaviors.Count - 1);
            self.allSubBehaviors.Add(self.currSubBehavior = new CWNoSubBehavior(self));
        }
    }

    static void On_DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.AbstractPearl.dataPearlType == DataPearlType.CWPearl)
        {
            var num = Random.Range(0, 3);
            if (rCam.room.world.game.IsStorySession)
                num = (self.abstractPhysicalObject as PebblesPearl.AbstractPebblesPearl)!.color;
            self.color = Mathf.Abs(num) switch
            {
                1 => new(.8f, .8f, .8f),
                2 => new(.01f, .01f, .01f),
                _ => new(85f / 255f, 172f / 255f, 238f / 255f),
            };
            self.darkness = 0f;
        }
    }

    static void IL_DataPearl_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<DataPearl.AbstractDataPearl.DataPearlType>("PebblesPearl"),
            x => x.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, DataPearl self) =>
             {
                 var fl = self.AbstractPearl.dataPearlType == DataPearlType.CWPearl;
                 if (fl)
                     self.gravity = .8f;
                 return flag && !fl;
             });
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook DataPearl.Update!");
    }

    static bool On_DataPearl_PearlIsNotMisc(On.DataPearl.orig_PearlIsNotMisc orig, DataPearl.AbstractDataPearl.DataPearlType pearlType) => orig(pearlType) && pearlType != DataPearlType.CWPearl;

    static void On_SSSwarmerSpawner_SpawnSwarmer(On.MoreSlugcats.SSSwarmerSpawner.orig_SpawnSwarmer orig, SSSwarmerSpawner self)
    {
        if (self.room?.world?.name is CWStuffPlugin.CW)
        {
            if (self.room.SwarmerCount < self.maxSwamers)
                new AbstractPhysicalObject(self.room.world, AbstractPhysicalObjectType.CWOracleSwarmer, null, self.room.ToWorldCoordinate(self.spawnPos), self.room.game.GetNewID()).RealizeInRoom();
        }
        else
            orig(self);
    }

    static string On_ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        if (itemType == AbstractPhysicalObjectType.CWPearl)
            return "Symbol_Pearl";
        if (itemType == AbstractPhysicalObjectType.CWOracleSwarmer)
            return "Symbol_Neuron";
        return orig(itemType, intData);
    }

    static Color On_ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        if (itemType == AbstractPhysicalObjectType.CWPearl)
        {
            if (intData == 1)
                return new(.8f, .8f, .8f);
            if (intData == 2)
                return Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            return new(85f / 255f, 172f / 255f, 238f / 255f);
        }
        if (itemType == AbstractPhysicalObjectType.CWOracleSwarmer)
            return new(85f / 255f, 172f / 255f, 238f / 255f);
        return orig(itemType, intData);
    }

    static IconSymbol.IconSymbolData? On_ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
    {
        if (item.type == AbstractPhysicalObjectType.CWPearl)
            return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, (item as PebblesPearl.AbstractPebblesPearl)!.color);
        return orig(item);
    }

    static AbstractPhysicalObject On_SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
    {
        try
        {
            var array = Regex.Split(objString, "<oA>");
            var tp = new AbstractPhysicalObject.AbstractObjectType(array[1]);
            if (tp == AbstractPhysicalObjectType.CWPearl)
                return new PebblesPearl.AbstractPebblesPearl(world, null, WorldCoordinate.FromString(array[2]), EntityID.FromString(array[0]), int.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture), int.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture), null, int.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture), int.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture))
                {
                    unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 8),
                    type = AbstractPhysicalObjectType.CWPearl,
                    dataPearlType = DataPearlType.CWPearl
                };
        }
        catch { }
        return orig(world, objString);
    }

    static void On_SSOracleBehavior_InitStoryPearlCollection(On.SSOracleBehavior.orig_InitStoryPearlCollection orig, SSOracleBehavior self)
    {
        if (self.oracle.IsCW())
        {
            var prls = self.readDataPearlOrbits = [];
            self.readPearlGlyphs = [];
            var rm = self.oracle.room;
            var ents = rm.abstractRoom.entities;
            for (var i = 0; i < ents.Count; i++)
            {
                if (ents[i] is DataPearl.AbstractDataPearl p)
                {
                    if (p.type != AbstractPhysicalObjectType.CWPearl)
                        prls.Add(p);
                }
            }
            var num = 0;
            for (var i = 0; i < prls.Count; i++)
            {
                var pos = self.storedPearlOrbitLocation(num);
                if (prls[i].realizedObject is PhysicalObject obj)
                    obj.firstChunk.pos = pos;
                else
                    prls[i].pos.Tile = rm.GetTilePosition(pos);
                ++num;
            }
            self.inspectPearl = null;
        }
        else
            orig(self);
    }

    static string On_OracleBehavior_AlreadyDiscussedItemString(On.OracleBehavior.orig_AlreadyDiscussedItemString orig, OracleBehavior self, bool pearl)
    {
        if (self.oracle.IsCW())
        {
            if (pearl && CWConversation.CWLinesFromFile("AlreadyDiscussedPearl", self.oracle.room.game.StoryCharacter?.value) is string[] lns && lns.Length > 0)
                return lns[Random.Range(0, lns.Length)];
            else if (CWConversation.CWLinesFromFile("AlreadyDiscussedItem", self.oracle.room.game.StoryCharacter?.value) is string[] lns2 && lns2.Length > 0)
                return lns2[Random.Range(0, lns2.Length)];
            return string.Empty;
        }
        return orig(self, pearl);
    }

    static void On_Oracle_CreateMarble(On.Oracle.orig_CreateMarble orig, Oracle self, PhysicalObject orbitObj, Vector2 ps, int circle, float dist, int color)
    {
        if (self.IsCW())
        {
            if (self.pearlCounter == 0)
                self.pearlCounter = 1;
            var abstractPhysicalObject = new PebblesPearl.AbstractPebblesPearl(self.room.world, null, self.room.GetWorldCoordinate(ps), self.room.game.GetNewID(), -1, -1, null, color, self.pearlCounter)
            {
                type = AbstractPhysicalObjectType.CWPearl,
                dataPearlType = DataPearlType.CWPearl
            };
            ++self.pearlCounter;
            self.room.abstractRoom.entities.Add(abstractPhysicalObject);
            var pearl = new PebblesPearl(abstractPhysicalObject, self.room.world)
            {
                oracle = self,
                orbitObj = orbitObj,
                orbitCircle = circle,
                orbitDistance = dist,
                marbleColor = abstractPhysicalObject.color,
                marbleIndex = self.marbles.Count
            };
            pearl.firstChunk.HardSetPosition(ps);
            if (orbitObj is null)
                pearl.hoverPos = ps;
            self.room.AddObject(pearl);
            self.marbles.Add(pearl);
        }
        else
            orig(self, orbitObj, ps, circle, dist, color);
    }

    static void IL_Oracle_ctor(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<Oracle.OracleID>("SS"),
            x => x.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Oracle self) => flag || self.IsCW());
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Oracle.ctor (part 1)!");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchStfld<Oracle>("ID")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .Emit(OpCodes.Ldarg_2)
             .EmitDelegate((Oracle self, Room room) =>
             {
                 if (string.Equals(room.abstractRoom.name, "CW_AI", StringComparison.OrdinalIgnoreCase))
                     self.ID = NewOracleID.CW;
             });
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Oracle.ctor (part 2)!");
        for (var i = 3; i <= 6; i++)
        {
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<Oracle.OracleID>("SS"),
                x => x.MatchCall(out _)))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Oracle self) => flag || self.IsCW());
            }
            else
                CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Oracle.ctor (part {i})!");
        }
    }

    static void IL_OracleArm_Update(ILContext il)
    {
        var c = new ILCursor(il);
        for (var i = 1; i <= 3; i++)
        {
            if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<Oracle.OracleID>("SS"),
            x => x.MatchCall(out _)))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Oracle.OracleArm self) => flag || self.oracle.IsCW());
            }
            else
                CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Oracle.OracleArm.Update (part {i})!");
        }
    }

    static void On_OracleArm_ctor(On.Oracle.OracleArm.orig_ctor orig, Oracle.OracleArm self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.IsCW())
        {
            self.baseMoveSoundLoop = new(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
            if (self.oracle?.room is Room rm && rm.game?.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase))
            {
                self.cornerPositions[0] = rm.MiddleOfTile(9, 35);
                self.cornerPositions[1] = rm.MiddleOfTile(37, 35);
                self.cornerPositions[2] = rm.MiddleOfTile(37, 7);
                self.cornerPositions[3] = rm.MiddleOfTile(9, 7);
            }
        }
    }

    static void IL_Joint_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<Oracle.OracleID>("SS"),
            x => x.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Oracle.OracleArm.Joint self) => flag || self.arm.oracle.IsCW());
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Oracle.OracleArm.Joint.Update!");
    }

    static void On_CoralNeuronSystem_PlaceSwarmers(On.CoralBrain.CoralNeuronSystem.orig_PlaceSwarmers orig, CoralNeuronSystem self)
    {
        if (self.room is Room rm && rm.world?.name is CWStuffPlugin.CW)
        {
            var dark = rm.roomSettings.Palette == 24 || (rm.roomSettings.fadePalette?.palette == 24);
            var accessableTiles = rm.aimap.CreatureSpecificAImap(StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly)).accessableTiles;
            var num = (int)(accessableTiles.Length * .05f * rm.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.SSSwarmers));
            List<IntVector2> list = [];
            for (var i = 0; i < num; i++)
            {
                if (accessableTiles.Length == 0)
                    break;
                list.Add(accessableTiles[Random.Range(0, accessableTiles.Length)]);
            }
            SSOracleSwarmer.Behavior behavior = default;
            for (var j = 0; j < list.Count; j++)
            {
                var sw = new SSOracleSwarmer(new(rm.world, AbstractPhysicalObjectType.CWOracleSwarmer, null, rm.GetWorldCoordinate(list[j]), rm.game.GetNewID()) { destroyOnAbstraction = true }, rm.world)
                {
                    system = self,
                    waitToFindOthers = j,
                    dark = dark
                };
                sw.firstChunk.HardSetPosition(rm.MiddleOfTile(list[j]));
                if (behavior == default)
                    behavior = sw.currentBehavior;
                else
                    sw.currentBehavior = behavior;
                rm.abstractRoom.AddEntity(sw.abstractPhysicalObject);
                rm.AddObject(sw);
                sw.NewRoom(rm);
            }
        }
        else
            orig(self);
    }

    static bool On_SLOracleBehaviorHasMark_RejectDiscussItem(On.SLOracleBehaviorHasMark.orig_RejectDiscussItem orig, SLOracleBehaviorHasMark self)
    {
        if (self.moveToAndPickUpItem.abstractPhysicalObject.type == AbstractPhysicalObjectType.CWOracleSwarmer)
        {
            self.throwAwayObjects = false;
            return false;
        }
        return orig(self);
    }

    static void On_OracleChatLabel_DrawSprites(On.OracleChatLabel.orig_DrawSprites orig, OracleChatLabel self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (!self.slatedForDeletetion && self.room == rCam.room && self.visible && self.oracleBehav?.oracle?.IsCW() is true)
        {
            var sprs = sLeaser.sprites;
            for (var j = 0; j < sprs.Length; j++)
                sprs[j].color = self.color;
        }
    }

    static void On_OracleChatLabel_AddToContainer(On.OracleChatLabel.orig_AddToContainer orig, OracleChatLabel self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        if (self.oracleBehav?.oracle?.IsCW() is true)
            newContainer = rCam.ReturnFContainer("BackgroundShortcuts");
        orig(self, sLeaser, rCam, newContainer);
    }

    static void On_OracleGraphics_ApplyPalette(On.OracleGraphics.orig_ApplyPalette orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.oracle.IsCW())
        {
            var color = new Color(237f / 255f, 230f / 255f, 1f);
            var lt = self.oracle.bodyChunks.Length;
            var sprs = sLeaser.sprites;
            for (var j = 0; j < lt; j++)
                sprs[self.firstBodyChunkSprite + j].color = color;
            sprs[self.neckSprite].color = color;
            sprs[self.HeadSprite].color = color;
            sprs[self.ChinSprite].color = color;
            for (var k = 0; k < 2; k++)
            {
                sprs[self.HandSprite(k, 0)].color = color;
                if (self.gown is null)
                    sprs[self.HandSprite(k, 1)].color = color;
                sprs[self.FootSprite(k, 0)].color = color;
                sprs[self.FootSprite(k, 1)].color = color;
            }
        }
    }

    static void On_OracleGraphics_DrawSprites(On.OracleGraphics.orig_DrawSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.oracle is not Oracle or || or.room is not Room rm || or.slatedForDeletetion || rm != rCam.room || self.dispose)
            return;
        if (or.IsCW() && or.oracleBehavior is SSOracleBehavior behav)
        {
            Vector2 vector13f = Vector2.Lerp(or.bodyChunks[1].lastPos, or.bodyChunks[1].pos, timeStacker),
                vector = Vector2.Lerp(or.firstChunk.lastPos, or.firstChunk.pos, timeStacker),
                vector2 = Custom.DirVec(vector13f, vector),
                vector3 = Custom.PerpendicularVector(vector2);
            var ks = sLeaser.sprites[self.killSprite];
            if (behav.killFac > 0f)
            {
                ks.isVisible = true;
                if (behav.player is Player p)
                {
                    ks.x = Mathf.Lerp(p.mainBodyChunk.lastPos.x, p.mainBodyChunk.pos.x, timeStacker) - camPos.x;
                    ks.y = Mathf.Lerp(p.mainBodyChunk.lastPos.y, p.mainBodyChunk.pos.y, timeStacker) - camPos.y;
                }
                var f = Mathf.Lerp(behav.lastKillFac, behav.killFac, timeStacker);
                ks.scale = Mathf.Lerp(200f, 2f, Mathf.Pow(f, .5f));
                ks.alpha = Mathf.Pow(f, 3f);
            }
            else
                ks.isVisible = false;
            var num = Mathf.Lerp(self.lastEyesOpen, self.eyesOpen, timeStacker);
            for (var k = 0; k < 2; k++)
            {
                sLeaser.sprites[self.EyeSprite(k)].scaleY = Mathf.Lerp(1f, 2.5f, num);
                var num2 = k == 1 ? -1f : 1f;
                Vector2 vector12 = Vector2.Lerp(self.hands[k].lastPos, self.hands[k].pos, timeStacker),
                    vector13 = vector + vector3 * 4f * num2,
                    cB = vector12 + Custom.DirVec(vector12, vector13) * 3f + vector2,
                    cA = vector13 + vector3 * 5f * num2,
                    vector14 = vector13 - vector3 * 2f * num2;
                for (var m = 0; m < 7; m++)
                {
                    Vector2 vector15 = Custom.Bezier(vector13, cA, vector12, cB, m / 6f),
                        vector16 = Custom.DirVec(vector14, vector15),
                        vector17 = Custom.PerpendicularVector(vector16) * (k == 0 ? -1f : 1f);
                    var num4 = Vector2.Distance(vector14, vector15);
                    var hand = (sLeaser.sprites[self.HandSprite(k, 1)] as TriangleMesh)!;
                    hand.MoveVertice(m * 4, vector15 - vector16 * num4 * .3f - vector17 * 4f - camPos);
                    hand.MoveVertice(m * 4 + 1, vector15 - vector16 * num4 * .3f + vector17 * 4f - camPos);
                    hand.MoveVertice(m * 4 + 2, vector15 - vector17 * 4f - camPos);
                    hand.MoveVertice(m * 4 + 3, vector15 + vector17 * 4f - camPos);
                    vector14 = vector15;
                }
                vector12 = Vector2.Lerp(self.feet[k].lastPos, self.feet[k].pos, timeStacker);
                Vector2 b = Vector2.Lerp(self.knees[k, 1], self.knees[k, 0], timeStacker);
                cB = Vector2.Lerp(vector12, b, .9f);
                cA = Vector2.Lerp(vector13f, b, .9f);
                vector14 = vector13f - vector3 * 2f * num2;
                var num5 = 4f;
                for (var n = 0; n < 7; n++)
                {
                    Vector2 vector18 = Custom.Bezier(vector13f, cA, vector12, cB, n / 6f),
                        vector19 = Custom.DirVec(vector14, vector18),
                        vector20 = Custom.PerpendicularVector(vector19) * (k == 0 ? -1f : 1f);
                    var num6 = Vector2.Distance(vector14, vector18);
                    var foot = (sLeaser.sprites[self.FootSprite(k, 1)] as TriangleMesh)!;
                    foot.MoveVertice(n * 4, vector18 - vector19 * num6 * .3f - vector20 * (num5 + 2f) * .5f - camPos);
                    foot.MoveVertice(n * 4 + 1, vector18 - vector19 * num6 * .3f + vector20 * (num5 + 2f) * .5f - camPos);
                    foot.MoveVertice(n * 4 + 2, vector18 - vector20 * 2f - camPos);
                    foot.MoveVertice(n * 4 + 3, vector18 + vector20 * 2f - camPos);
                    vector14 = vector18;
                    num5 = 2f;
                }
            }
        }
    }

    static void On_OracleGraphics_AddToContainer(On.OracleGraphics.orig_AddToContainer orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        if (self.oracle.IsCW())
            sLeaser.sprites[self.killSprite] ??= new("Futile_White")
            {
                shader = rCam.game.rainWorld.Shaders["FlatLight"]
            };
        orig(self, sLeaser, rCam, newContainer);
    }

    static void On_OracleGraphics_InitiateSprites(On.OracleGraphics.orig_InitiateSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.oracle.IsCW())
        {
            var sprs = sLeaser.sprites;
            sprs[self.neckSprite].scaleX = 3f;
            sprs[self.fadeSprite].color = Color.black;
            sprs[self.fadeSprite].alpha = .5f;
            for (var k = 0; k < 2; k++)
                sprs[self.EyeSprite(k)].color = new(0f, 0f, 150f / 255f);
            sprs[self.killSprite] ??= new("Futile_White")
            {
                shader = rCam.game.rainWorld.Shaders["FlatLight"]
            };
        }
    }

    static void On_OracleGraphics_Update(On.OracleGraphics.orig_Update orig, OracleGraphics self)
    {
        orig(self);
        if (self.oracle.IsCW() && self.oracle.room is Room rm)
        {
            if (!rm.game.cameras[0].AboutToSwitchRoom || self.lightsource is null)
            {
                if (self.lightsource is null)
                {
                    rm.AddObject(self.lightsource = new(self.oracle.firstChunk.pos, false, Custom.HSL2RGB(200f / 360f, 1f, .5f), self.oracle)
                    {
                        affectedByPaletteDarkness = 0f
                    });
                    return;
                }
                self.lightsource.setAlpha = Math.Max(1f - rm.gravity, .01f);
                self.lightsource.setRad = 300f;
                self.lightsource.setPos = self.oracle.firstChunk.pos;
            }
        }
    }

    static void On_OracleGraphics_ctor(On.OracleGraphics.orig_ctor orig, OracleGraphics self, PhysicalObject ow)
    {
        orig(self, ow);
        if (self.oracle.IsCW())
        {
            self.totalSprites -= self.armBase.totalSprites;
            self.killSprite = self.totalSprites;
            ++self.totalSprites;
            self.armBase.firstSprite = self.firstArmBaseSprite = self.totalSprites;
            self.totalSprites += self.armBase.totalSprites;
        }
    }

    static Color On_Gown_Color(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
    {
        if (self.owner.oracle.IsCW())
            return Custom.HSL2RGB(234f / 360f, Mathf.Lerp(.46f, .58f, f), Mathf.Lerp(.23f, .25f, f));
        return orig(self, f);
    }

    static void On_ArmJointGraphics_ctor(On.OracleGraphics.ArmJointGraphics.orig_ctor orig, OracleGraphics.ArmJointGraphics self, OracleGraphics owner, Oracle.OracleArm.Joint myJoint, int firstSprite)
    {
        orig(self, owner, myJoint, firstSprite);
        if (owner.oracle.IsCW())
            self.armJointSound.soundID = SoundID.SS_AI_Arm_Joint_LOOP;
    }

    static Color? On_Player_StomachGlowLightColor(On.Player.orig_StomachGlowLightColor orig, Player self)
    {
        var obj = self.AI is not null ? (self.State as PlayerNPCState)!.StomachObject : self.objectInStomach;
        if (obj?.type == AbstractPhysicalObjectType.CWOracleSwarmer)
            return new(1f, 1f, 1f, .35f);
        return orig(self);
    }

    static void On_AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
    {
        if (self.realizedObject is null)
        {
            if (self.type == AbstractPhysicalObjectType.CWOracleSwarmer)
            {
                self.realizedObject = new SSOracleSwarmer(self, self.world);
                for (var i = 0; i < self.stuckObjects.Count; i++)
                {
                    var stuckObj = self.stuckObjects[i];
                    if (stuckObj.A.realizedObject is null && stuckObj.A != self)
                        stuckObj.A.Realize();
                    if (stuckObj.B.realizedObject is null && stuckObj.B != self)
                        stuckObj.B.Realize();
                }
                return;
            }
            if (self.type == AbstractPhysicalObjectType.CWPearl)
            {
                self.realizedObject = new PebblesPearl(self, self.world);
                for (var i = 0; i < self.stuckObjects.Count; i++)
                {
                    var stuckObj = self.stuckObjects[i];
                    if (stuckObj.A.realizedObject is null && stuckObj.A != self)
                        stuckObj.A.Realize();
                    if (stuckObj.B.realizedObject is null && stuckObj.B != self)
                        stuckObj.B.Realize();
                }
                return;
            }
        }
        orig(self);
    }

    static void On_Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
    {
        orig(self);
        if (self.game?.session is StoryGameSession && string.Equals(self.abstractRoom.name, "CW_AI", StringComparison.OrdinalIgnoreCase))
        {
            self.AddObject(new Oracle(new(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self));
            self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
        }
    }

    /*public static bool SetCWPearlDeciphered(this PlayerProgression.MiscProgressionData self, DataPearl.AbstractDataPearl.DataPearlType pearlType, bool forced = false)
    {
        if (pearlType is not null && !forced)
        {
            if (num != -1 && !Conversation.EventsFileExists(self.owner.rainWorld, num, SlugcatStats.Name.White))
                return SetPearlDeciphered(pearlType);
        }
        if (!DecipheredPearls.TryGetValue(self, out var pearls) || pearlType is null || GetCWPearlDeciphered(pearlType, pearls))
            return false;
        pearls.Add(pearlType);
        self.owner.SaveProgression(false, true);
        return true;
    }

    public static bool GetCWPearlDeciphered(this PlayerProgression.MiscProgressionData self, DataPearl.AbstractDataPearl.DataPearlType pearlType)
    {
        return DecipheredPearls.TryGetValue(self, out var pearls) && GetCWPearlDeciphered(pearlType, pearls);
    }

    public static bool GetCWPearlDeciphered(DataPearl.AbstractDataPearl.DataPearlType pearlType, HashSet<DataPearl.AbstractDataPearl.DataPearlType> decipheredPearls)
    {
        if (pearlType is not null)
            return decipheredPearls.Contains(pearlType);
        return false;
    }*/

    public static bool IsCW(this Oracle self) => self.ID == NewOracleID.CW;
}

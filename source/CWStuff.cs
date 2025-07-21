using BepInEx;
using BepInEx.Logging;
using CoralBrain;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Music;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
using static System.Reflection.BindingFlags;
using Random = UnityEngine.Random;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace CWStuff;

[BepInPlugin("lb-fgf-m4r-ik.chatoyant-waterfalls-but-real", "CWStuff", "10.0.0")]
public sealed class CWStuffPlugin : BaseUnityPlugin
{
    public static RoomSettings.RoomEffect.Type CWDarkerTubes = new(nameof(CWDarkerTubes), true);
    public const string CW = "CW";
    [AllowNull] internal static ManualLogSource s_logger;
    public static string CWTextPath = "cw_text" + Path.DirectorySeparatorChar + "Text_";
    [AllowNull] public static HashSet<string> ScavImmuRegions;
    public static ConditionalWeakTable<AbstractPhysicalObject, StrongBox<bool>> CWVariant = new();

    public void OnEnable()
    {
        s_logger = Logger;
        //prevents an exception
        On.SoundLoader.LoadSounds += On_SoundLoader_LoadSounds;
        On.Music.MusicPlayer.RequestSSSong += On_MusicPlayer_RequestSSSong;
        On.SlimeMold.CosmeticSlimeMold.ApplyPalette += On_CosmeticSlimeMold_ApplyPalette;
        On.SlimeMold.ApplyPalette += On_SlimeMold_ApplyPalette;
        On.BubbleGrass.ApplyPalette += On_BubbleGrass_ApplyPalette;
        On.BubbleGrass.UpdateLumpColors += On_BubbleGrass_UpdateLumpColors;
        On.DaddyCorruption.CorruptionTube.TubeGraphic.ApplyPalette += On_TubeGraphic_ApplyPalette;
        On.RainWorld.PostModsInit += On_RainWorld_PostModsInit;
        On.RainWorld.UnloadResources += On_RainWorld_UnloadResources;
        On.RainWorld.OnModsDisabled += On_RainWorld_OnModsDisabled;
        new Hook(typeof(Inspector).GetMethod("get_OwneriteratorColor", Instance | Public | NonPublic), On_Inspector_get_OwneriteratorColor);
        new Hook(typeof(Inspector).GetMethod("get_TrueColor", Instance | Public | NonPublic), On_Inspector_get_TrueColor);
        On.RoomCamera.DrawUpdate += On_RoomCamera_DrawUpdate;
        On.FliesRoomAI.CreateFlyInHive += On_FliesRoomAI_CreateFlyInHive;
        On.AbstractPhysicalObject.Realize += On_AbstractPhysicalObject_Realize;
        On.Player.StomachGlowLightColor += On_Player_StomachGlowLightColor;
        On.SLOracleBehaviorHasMark.RejectDiscussItem += On_SLOracleBehaviorHasMark_RejectDiscussItem;
        On.CoralBrain.CoralNeuronSystem.PlaceSwarmers += On_CoralNeuronSystem_PlaceSwarmers;
        On.SaveState.AbstractPhysicalObjectFromString += On_SaveState_AbstractPhysicalObjectFromString;
        On.ItemSymbol.SymbolDataFromItem += On_ItemSymbol_SymbolDataFromItem;
        On.ItemSymbol.ColorForItem += On_ItemSymbol_ColorForItem;
        On.ItemSymbol.SpriteNameForItem += On_ItemSymbol_SpriteNameForItem;
        On.MoreSlugcats.SSSwarmerSpawner.SpawnSwarmer += On_SSSwarmerSpawner_SpawnSwarmer;
        On.DataPearl.PearlIsNotMisc += On_DataPearl_PearlIsNotMisc;
        IL.DataPearl.Update += IL_DataPearl_Update;
        On.DataPearl.ApplyPalette += On_DataPearl_ApplyPalette;
        IL.SLOracleBehaviorHasMark.GrabObject += IL_SLOracleBehaviorHasMark_GrabObject;
        IL.SLOracleBehavior.Update += IL_SLOracleBehavior_Update;
        On.SLOracleBehavior.Update += On_SLOracleBehavior_Update;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += On_MoonConversation_AddEvents;
        On.AbstractPhysicalObject.ctor += On_AbstractPhysicalObject_ctor;
        On.BubbleGrass.AbstractBubbleGrass.ToString += On_AbstractBubbleGrass_ToString;
        On.AbstractConsumable.ToString += On_AbstractConsumable_ToString;
        IL.Room.Loaded += IL_Room_Loaded;
        CWOracleHooks.Apply();
        CWWater.Apply();
    }

    static void IL_Room_Loaded(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<AbstractPhysicalObject.AbstractObjectType>("SlimeMold"))
         && c.TryGotoNext(MoveType.After,
            x => x.MatchNewobj<AbstractConsumable>()))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((AbstractConsumable cons, Room self) =>
             {
                 if (self.world?.name == CW && CWVariant.TryGetValue(cons, out var box))
                     box.Value = true;
                 return cons;
             });
        }
        else
            s_logger.LogError("Couldn't ILHook Room.Loaded (part 1)!");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchNewobj<BubbleGrass.AbstractBubbleGrass>()))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((BubbleGrass.AbstractBubbleGrass cons, Room self) =>
             {
                 if (self.world?.name == CW && CWVariant.TryGetValue(cons, out var box))
                     box.Value = true;
                 return cons;
             });
        }
        else
            s_logger.LogError("Couldn't ILHook Room.Loaded (part 2)!");
    }

    static string On_AbstractConsumable_ToString(On.AbstractConsumable.orig_ToString orig, AbstractConsumable self)
    {
        var res = orig(self);
        if (CWVariant.TryGetValue(self, out var box) && box.Value)
            res += "<oA>CWVariant";
        return res;
    }

    static string On_AbstractBubbleGrass_ToString(On.BubbleGrass.AbstractBubbleGrass.orig_ToString orig, BubbleGrass.AbstractBubbleGrass self)
    {
        var res = orig(self);
        if (CWVariant.TryGetValue(self, out var box) && box.Value)
            res += "<oA>CWVariant";
        return res;
    }

    static void On_AbstractPhysicalObject_ctor(On.AbstractPhysicalObject.orig_ctor orig, AbstractPhysicalObject self, World world, AbstractPhysicalObject.AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, type, realizedObject, pos, ID);
        if (!CWVariant.TryGetValue(self, out _) && (type == AbstractPhysicalObject.AbstractObjectType.BubbleGrass || type == AbstractPhysicalObject.AbstractObjectType.SlimeMold))
            CWVariant.Add(self, new());
    }

    static void On_SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
    {
        _ = NewSoundID.CW_AI_Talk_1;
        orig(self);
    }

    static void On_MusicPlayer_RequestSSSong(On.Music.MusicPlayer.orig_RequestSSSong orig, MusicPlayer self)
    {
        if (self.manager is ProcessManager mag && mag.currentMainLoop is RainWorldGame g && g.IsStorySession && g.world.name == CW && self.song is not SSSong && self.nextSong is not SSSong && mag.rainWorld.setup.playMusic)
        {
            var song = new SSSong(self, "Chatoyant Gods");
            if (self.song is null)
            {
                self.song = song;
                song.playWhenReady = true;
            }
            else
            {
                self.nextSong = song;
                song.playWhenReady = false;
            }
        }
        else
            orig(self);
    }

    static void On_CosmeticSlimeMold_ApplyPalette(On.SlimeMold.CosmeticSlimeMold.orig_ApplyPalette orig, SlimeMold.CosmeticSlimeMold self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.room?.world?.name == CW)
        {
            Color b = new(33f / 255f + .1f, 149f / 255f + .1f, 1f), a = Color.Lerp(palette.blackColor, palette.fogColor, .15f + .1f * palette.fogAmount);
            var l = self.positions.Length;
            var bri = self.brightnesses;
            var sprites = sLeaser.sprites;
            for (var i = 0; i < l; i++)
                sprites[i].color = Color.Lerp(a, b, bri[i]);
        }
    }

    static void On_SlimeMold_ApplyPalette(On.SlimeMold.orig_ApplyPalette orig, SlimeMold self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        // already works for originRoom = -1
        if (CWVariant.TryGetValue(self.abstractPhysicalObject, out var box) && box.Value)
            self.color = new(33f / 255f + .1f, 149f / 255f + .1f, 1f);
    }

    static void On_BubbleGrass_ApplyPalette(On.BubbleGrass.orig_ApplyPalette orig, BubbleGrass self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        // already works for originRoom = -1
        if (CWVariant.TryGetValue(self.abstractPhysicalObject, out var box) && box.Value)
        {
            self.color = Color.Lerp(Color.Lerp(palette.blackColor, new(1f, .7f, .2f), .6f), palette.fogColor, .2f);
            if (self.blink > 1 && Random.value < .5f)
                self.color = Color.white;
            var verts = (sLeaser.sprites[self.StalkSprite] as TriangleMesh)!.verticeColors;
            for (var i = 0; i < verts.Length; i++)
                verts[i] = self.StalkColor(Mathf.InverseLerp(verts.Length - 1, 0f, i));
            self.UpdateLumpColors(sLeaser);
        }
    }

    static void On_BubbleGrass_UpdateLumpColors(On.BubbleGrass.orig_UpdateLumpColors orig, BubbleGrass self, RoomCamera.SpriteLeaser sLeaser)
    {
        orig(self, sLeaser);
        // already works for originRoom = -1
        if (CWVariant.TryGetValue(self.abstractPhysicalObject, out var box) && box.Value)
        {
            var sprites = sLeaser.sprites;
            var l = self.lumps.GetLength(0);
            var c = Color.Lerp(self.blackColor, new(1f, .7f, .2f), .6f);
            for (var i = 0; i < l; i++)
                sprites[self.LumpSprite(i, 2)].color = Color.Lerp(Color.Lerp(self.blackColor, self.color, .2f + .8f * Mathf.InverseLerp(i, i + 1, self.oxygen * l)), c, .6f);
        }
    }

    static void On_TubeGraphic_ApplyPalette(On.DaddyCorruption.CorruptionTube.TubeGraphic.orig_ApplyPalette orig, DaddyCorruption.CorruptionTube.TubeGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.owner is DaddyCorruption.CorruptionTube tube && tube.room is Room rm && rm.world?.name == CW)
        {
            var sprites = sLeaser.sprites;
            var ef = 1f - rm.roomSettings.GetEffectAmount(CWDarkerTubes);
            var color = tube.EffectColor;
            if (self is DaddyCorruption.NeuronFilledLeg.LegGraphic)
                color = Color.Lerp(color, new(.13f, 0f, .19f), .2f);
            var spr = (sprites[self.firstSprite] as TriangleMesh)!;
            var verts = spr.vertices;
            for (var i = 0; i < verts.Length; i++)
            {
                var floatPos = (float)i / (verts.Length - 1);
                spr.verticeColors[i] = Color.Lerp(palette.blackColor, color, self.OnTubeEffectColorFac(floatPos) + .5f * ef);
            }
            var num = 0;
            var bumps = self.bumps;
            for (var j = 0; j < bumps.Length; j++)
            {
                var bump = bumps[j];
                sprites[self.firstSprite + 1 + j].color = Color.Lerp(palette.blackColor, color, self.OnTubeEffectColorFac(bump.pos.y) + .5f * ef);
                if (bump.eyeSize > 0f)
                {
                    sprites[self.firstSprite + 1 + bumps.Length + num].color = Color.Lerp(palette.blackColor, color, self.OnTubeEffectColorFac(bump.pos.y) + .6f * ef);
                    num++;
                }
            }
        }
    }

    static void On_RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        _ = AbstractPhysicalObjectType.CWOracleSwarmer;
        _ = DataPearlType.CWPearl;
        _ = ConversationID.SL_CWNeuron;
        _ = NewOracleID.CW;
        _ = SubBehavID.GetOYBot;
        _ = ActionID.GetOYBot_Init;
        _ = CWDarkerTubes;
        try
        {
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg1"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg1", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg1" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg2"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg2", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg2" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg2_Seer"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg2_Seer", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg2_Seer" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg3a"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg3a", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg3a" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg3b"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg3b", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg3b" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            if (!Futile.atlasManager.DoesContainAtlas("CW_AIimg3b_Spear"))
                Futile.atlasManager.ActuallyLoadAtlasOrImage("CW_AIimg3b_Spear", "Illustrations" + Path.DirectorySeparatorChar + "CW_AIimg3b_Spear" + Futile.resourceSuffix, string.Empty).texture.wrapMode = TextureWrapMode.Clamp;
            ScavImmuRegions = [.. File.ReadAllText(AssetManager.ResolveFilePath("cw_text" + Path.DirectorySeparatorChar + "scavengerPacifyingRegions.txt"), Encoding.UTF8).Split(',')];
        }
        catch (Exception e)
        {
            s_logger.LogError("Error while loading atlases or scavenger immunity regions file! You should restart the game to hopefully fix this.");
            s_logger.LogError(e);
        }
    }

    static void On_RainWorld_UnloadResources(On.RainWorld.orig_UnloadResources orig, RainWorld self)
    {
        orig(self);
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg1"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg1");
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg2"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg2");
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg2_Seer"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg2_Seer");
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg3a"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg3a");
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg3b"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg3b");
        if (Futile.atlasManager.DoesContainAtlas("CW_AIimg3b_Spear"))
            Futile.atlasManager.UnloadAtlas("CW_AIimg3b_Spear");
    }

    static void On_RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
    {
        orig(self, newlyDisabledMods);
        for (var i = 0; i < newlyDisabledMods.Length; i++)
        {
            if (newlyDisabledMods[i].id == "lb-fgf-m4r-ik.chatoyant-waterfalls-but-real")
            {
                CWDarkerTubes?.Unregister();
                CWDarkerTubes = null!;
                AbstractPhysicalObjectType.UnregisterValues();
                NewOracleID.UnregisterValues();
                DataPearlType.UnregisterValues();
                ConversationID.UnregisterValues();
                NewTickerID.UnregisterValues();
                NewSoundID.UnregisterValues();
                SubBehavID.UnregisterValues();
                ActionID.UnregisterValues();
                break;
            }
        }
    }

    static Color On_Inspector_get_OwneriteratorColor(Func<Inspector, Color> orig, Inspector self)
    {
        if (self.abstractCreature.world?.name == CW)
            return new(.4f, .4f, .4f);
        return orig(self);
    }

    static Color On_Inspector_get_TrueColor(Func<Inspector, Color> orig, Inspector self)
    {
        if (self.abstractCreature.world?.name == CW)
            return self.OwneriteratorColor;
        return orig(self);
    }

    static void On_RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
    {
        orig(self, timeStacker, timeSpeed);
        if (self.room is Room rm && rm.world?.name == CW && self.fullScreenEffect is FSprite spr)
        {
            var ef = self.lightBloomAlphaEffect;
            if (ef == RoomSettings.RoomEffect.Type.Bloom || ef == RoomSettings.RoomEffect.Type.SkyBloom || ef == RoomSettings.RoomEffect.Type.SkyAndLightBloom || ef == RoomSettings.RoomEffect.Type.LightBurn)
                spr.alpha = self.lightBloomAlpha = rm.roomSettings.GetEffectAmount(ef);
        }
    }

    static void On_MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);
        if (self.id == ConversationID.SL_CWNeuron)
        {
            if (self.myBehavior is not SLOracleBehaviorHasMark hm)
                return;
            var state = self.State;
            var neuronsLeft = state.neuronsLeft;
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
                    if (state.neuronGiveConversationCounter == 0)
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
                    else if (state.neuronGiveConversationCounter == 1)
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
                    ++state.neuronGiveConversationCounter;
                    break;
            }
            hm.respondToNeuronFromNoSpeakMode = false;
        }
    }

    static void On_SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
    {
        orig(self, eu);
        if (self.holdingObject is SSOracleSwarmer ssw && ssw.abstractPhysicalObject.type == AbstractPhysicalObjectType.CWOracleSwarmer && self.oracle.Consious && ssw.grabbedBy.Count == 0 && self.oracle.room is Room rm && (rm.game.cameras[0].hud.dialogBox is not DialogBox box || box.messages.Count == 0))
        {
            var fc = self.oracle.firstChunk;
            var sswch = ssw.firstChunk;
            sswch.MoveFromOutsideMyUpdate(eu, fc.pos + new Vector2(-18f, -7f));
            sswch.vel *= 0f;
            ++self.convertSwarmerCounter;
            if (self.convertSwarmerCounter > 40)
            {
                var pos = sswch.pos;
                ssw.Destroy();
                self.holdingObject = null;
                var sLOracleSwarmer = new SLOracleSwarmer(new(rm.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null, rm.GetWorldCoordinate(pos), rm.game.GetNewID()), rm.world);
                rm.abstractRoom.entities.Add(sLOracleSwarmer.abstractPhysicalObject);
                sLOracleSwarmer.firstChunk.HardSetPosition(pos);
                rm.AddObject(sLOracleSwarmer);
                var state = self.State;
                ++state.neuronsLeft;
                state.InfluenceLike(.65f);
                if (rm.game.session is StoryGameSession sess)
                    sess.saveState.miscWorldSaveData.playerGuideState.angryWithPlayer = false;
                if (self is SLOracleBehaviorHasMark hm)
                {
                    ++state.totNeuronsGiven;
                    state.increaseLikeOnSave = true;
                    if (self.reelInSwarmer is null && (hm.currentConversation is not Conversation c || c.id != Conversation.ID.MoonRecieveSwarmer) && state.SpeakingTerms)
                        hm.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(ConversationID.SL_CWNeuron, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                }
                if (!self.moonActive && self.InSitPosition && self.dontHoldKnees < 1 && Random.value < .025f && (self.player is null || !Custom.DistLess(fc.pos, self.player.DangerPos, 50f)) && !self.protest && self.oracle.health >= 1f)
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
            s_logger.LogError("Couldn't ILHook SLOracleBehavior.Update!");
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
            s_logger.LogError("Couldn't ILHook SLOracleBehaviorHasMark.GrabObject!");
    }

    static void On_FliesRoomAI_CreateFlyInHive(On.FliesRoomAI.orig_CreateFlyInHive orig, FliesRoomAI self)
    {
        if (self.room is Room rm && rm.abstractRoom.name is string nm && string.Equals("CW_C13", nm, StringComparison.OrdinalIgnoreCase))
        {
            var cnt = 0;
            var crits = rm.abstractRoom.creatures;
            for (var j = 0; j < crits.Count; j++)
            {
                if (crits[j]?.creatureTemplate.type == CreatureTemplate.Type.Fly)
                    ++cnt;
            }
            if (cnt >= 10)
                return;
        }
        orig(self);
    }

    static void On_DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.AbstractPearl.dataPearlType == DataPearlType.CWPearl)
        {
            var num = Random.Range(0, 3);
            if (rCam.game?.session is StoryGameSession)
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
            s_logger.LogError("Couldn't ILHook DataPearl.Update!");
    }

    static bool On_DataPearl_PearlIsNotMisc(On.DataPearl.orig_PearlIsNotMisc orig, DataPearl.AbstractDataPearl.DataPearlType pearlType) => orig(pearlType) && pearlType != DataPearlType.CWPearl;

    static void On_SSSwarmerSpawner_SpawnSwarmer(On.MoreSlugcats.SSSwarmerSpawner.orig_SpawnSwarmer orig, SSSwarmerSpawner self)
    {
        if (self.room is Room rm && rm.world is World w && w.name == CW)
        {
            if (rm.SwarmerCount < self.maxSwamers)
                new AbstractPhysicalObject(w, AbstractPhysicalObjectType.CWOracleSwarmer, null, rm.ToWorldCoordinate(self.spawnPos), rm.game.GetNewID()).RealizeInRoom();
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
        if (intData == 319)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.BubbleGrass)
                return new(1f, .7f, .2f);
            if (itemType == AbstractPhysicalObject.AbstractObjectType.SlimeMold)
                return new(33f / 255f + .1f, 149f / 255f + .1f, 1f);
        }
        return orig(itemType, intData);
    }

    static IconSymbol.IconSymbolData? On_ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
    {
        if (item.type == AbstractPhysicalObjectType.CWPearl)
            return new(CreatureTemplate.Type.StandardGroundCreature, item.type, (item as PebblesPearl.AbstractPebblesPearl)!.color);
        var res = orig(item);
        if (CWVariant.TryGetValue(item, out var box) && box.Value && res.HasValue)
            res = res.Value with { intData = 319 };
        return res;
    }

    static AbstractPhysicalObject? On_SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
    {
        try
        {
            var array = objString.Split(["<oA>"], StringSplitOptions.None);
            var tp = new AbstractPhysicalObject.AbstractObjectType(array[1]);
            if (tp == AbstractPhysicalObjectType.CWPearl)
            {
                var rippleLayer = 0;
                EntityID iD;
                if (array[0].Contains("<oB>"))
                {
                    var array2 = array[0].Split(["<oB>"], StringSplitOptions.None);
                    iD = EntityID.FromString(array2[0]);
                    int.TryParse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture, out rippleLayer);
                }
                else
                    iD = EntityID.FromString(array[0]);
                return new PebblesPearl.AbstractPebblesPearl(world, null, WorldCoordinate.FromString(array[2]), iD, int.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture), int.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture), null, int.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture), int.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture))
                {
                    unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 8),
                    type = AbstractPhysicalObjectType.CWPearl,
                    dataPearlType = DataPearlType.CWPearl,
                    rippleLayer = rippleLayer
                };
            }
        }
        catch { }
        var res = orig(world, objString);
        if (res?.unrecognizedAttributes is string[] sAr && (res.type == AbstractPhysicalObject.AbstractObjectType.BubbleGrass || res.type == AbstractPhysicalObject.AbstractObjectType.SlimeMold) && CWVariant.TryGetValue(res, out var box))
        {
            box.Value = false;
            for (var i = 0; i < sAr.Length; i++)
            {
                if (sAr[i] == "CWVariant")
                {
                    box.Value = true;
                    break;
                }
            }
        }
        return res;
    }

    static void On_CoralNeuronSystem_PlaceSwarmers(On.CoralBrain.CoralNeuronSystem.orig_PlaceSwarmers orig, CoralNeuronSystem self)
    {
        if (self.room is Room rm && rm.world?.name == CWStuffPlugin.CW)
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
            AbstractPhysicalObject a, b;
            List<AbstractPhysicalObject.AbstractObjectStick> objs;
            if (self.type == AbstractPhysicalObjectType.CWOracleSwarmer)
            {
                self.realizedObject = new SSOracleSwarmer(self, self.world);
                objs = self.stuckObjects;
                for (var i = 0; i < objs.Count; i++)
                {
                    var stuckObj = objs[i];
                    a = stuckObj.A;
                    b = stuckObj.B;
                    if (a.realizedObject is null && a != self)
                        a.Realize();
                    if (b.realizedObject is null && b != self)
                        b.Realize();
                }
                return;
            }
            if (self.type == AbstractPhysicalObjectType.CWPearl)
            {
                self.realizedObject = new PebblesPearl(self, self.world);
                objs = self.stuckObjects;
                for (var i = 0; i < objs.Count; i++)
                {
                    var stuckObj = objs[i];
                    a = stuckObj.A;
                    b = stuckObj.B;
                    if (a.realizedObject is null && a != self)
                        a.Realize();
                    if (b.realizedObject is null && b != self)
                        b.Realize();
                }
                return;
            }
        }
        orig(self);
    }

    public void OnDisable()
    {
        s_logger = null;
        ScavImmuRegions = null!;
        CWTextPath = null!;
        CWVariant = null!;
        CWOracleHooks.WorldSaveData = null!;
        CWOracleHooks.CWWorldRedCyclesInfo = null!;
        CWOracleHooks.GameData = null!;
        CWOracleHooks.CWGameRedCyclesInfo = null!;
    }
}
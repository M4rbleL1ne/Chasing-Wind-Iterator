using BepInEx;
using System.Security.Permissions;
using System.Security;
using UnityEngine;
using System.Runtime.CompilerServices;
using Music;
using BepInEx.Logging;
using System.Diagnostics.CodeAnalysis;
using MonoMod.RuntimeDetour;
using static System.Reflection.BindingFlags;
using System;
using Random = UnityEngine.Random;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MoreSlugcats;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace CWStuff;

[BepInPlugin("lb-fgf-m4r-ik.chatoyant-waterfalls-but-real", "CWStuff", "1.0.3")]
public sealed class CWStuffPlugin : BaseUnityPlugin
{
    public static RoomSettings.RoomEffect.Type CWDarkerTubes = new(nameof(CWDarkerTubes), true);
    public const string CW = "CW";
    static ConditionalWeakTable<AbstractPhysicalObject, BornInCW> s_bornInCW = new();
    static BornInCW s_born = new();
    [AllowNull] internal static ManualLogSource s_logger;
    public static string CWTextPath = "cw_text" + Path.DirectorySeparatorChar + "Text_";
    [AllowNull] public static HashSet<string> ScavImmuRegions;

    sealed class BornInCW { }

    public void OnEnable()
    {
        s_logger = Logger;
        //prevents an exception
        On.SoundLoader.LoadSounds += (orig, self) =>
        {
            _ = NewSoundID.CW_AI_Talk_1;
            orig(self);
        };
        On.Music.MusicPlayer.RequestSSSong += (orig, self) =>
        {
            if (self.manager is ProcessManager mag && mag.currentMainLoop is RainWorldGame g && g.IsStorySession && g.world.name == CW && self.song is not SSSong sss && self.nextSong is not SSSong nsss && mag.rainWorld.setup.playMusic)
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
        };
        On.AbstractPhysicalObject.ctor += (orig, self, world, type, realizedObject, pos, ID) =>
        {
            orig(self, world, type, realizedObject, pos, ID);
            if (world?.name is CW && (self.type == AbstractPhysicalObject.AbstractObjectType.BubbleGrass || self.type == AbstractPhysicalObject.AbstractObjectType.SlimeMold) && !s_bornInCW.TryGetValue(self, out _))
                s_bornInCW.Add(self, s_born);
        };
        On.SlimeMold.CosmeticSlimeMold.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (self.room?.world?.name is CW)
            {
                Color b = new(33f / 255f + .1f, 149f / 255f + .1f, 1f), a = Color.Lerp(palette.blackColor, palette.fogColor, .15f + .1f * palette.fogAmount);
                var l = self.positions.Length;
                var bri = self.brightnesses;
                var sprites = sLeaser.sprites;
                for (var i = 0; i < l; i++)
                    sprites[i].color = Color.Lerp(a, b, bri[i]);
            }
        };
        On.SlimeMold.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (s_bornInCW.TryGetValue(self.abstractPhysicalObject, out _))
                self.color = new(33f / 255f + .1f, 149f / 255f + .1f, 1f);
        };
        On.BubbleGrass.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (s_bornInCW.TryGetValue(self.abstractPhysicalObject, out _))
            {
                self.color = Color.Lerp(Color.Lerp(palette.blackColor, new(1f, .7f, .2f), .6f), palette.fogColor, .2f);
                if (self.blink > 1 && Random.value < .5f)
                    self.color = Color.white;
                var verts = (sLeaser.sprites[self.StalkSprite] as TriangleMesh)!.verticeColors;
                for (var i = 0; i < verts.Length; i++)
                    verts[i] = self.StalkColor(Mathf.InverseLerp(verts.Length - 1, 0f, i));
                self.UpdateLumpColors(sLeaser);
            }
        };
        On.BubbleGrass.UpdateLumpColors += (orig, self, sLeaser) =>
        {
            orig(self, sLeaser);
            if (s_bornInCW.TryGetValue(self.abstractPhysicalObject, out _))
            {
                var sprites = sLeaser.sprites;
                var l = self.lumps.GetLength(0);
                var c = Color.Lerp(self.blackColor, new(1f, .7f, .2f), .6f);
                for (var i = 0; i < l; i++)
                    sprites[self.LumpSprite(i, 2)].color = Color.Lerp(Color.Lerp(self.blackColor, self.color, .2f + .8f * Mathf.InverseLerp(i, i + 1, self.oxygen * l)), c, .6f);
            }
        };
        On.DaddyCorruption.CorruptionTube.TubeGraphic.ApplyPalette += (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (self.owner?.room is Room rm && rm.world?.name is CW)
            {
                var sprites = sLeaser.sprites;
                var ef = 1f - rm.roomSettings.GetEffectAmount(CWDarkerTubes);
                var color = self.owner.EffectColor;
                if (ModManager.MSC && self is DaddyCorruption.NeuronFilledLeg.LegGraphic)
                    color = Color.Lerp(color, new(.13f, 0f, .19f), .2f);
                var spr = (sprites[self.firstSprite] as TriangleMesh)!;
                var verts = spr.vertices;
                var vec = spr.GetPosition();
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
        };
        On.RainWorld.PostModsInit += (orig, self) =>
        {
            orig(self);
            _ = AbstractPhysicalObjectType.CWOracleSwarmer;
            _ = DataPearlType.CWPearl;
            _ = ConversationID.SL_CWNeuron;
            _ = NewOracleID.CW;
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
        };
        On.RainWorld.UnloadResources += (orig, self) =>
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
        };
        On.RainWorld.OnModsDisabled += (orig, self, newlyDisabledMods) =>
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
                    break;
                }
            }
        };
        new Hook(typeof(Inspector).GetMethod("get_OwneriteratorColor", Instance | Public | NonPublic), (Func<Inspector, Color> orig, Inspector self) =>
        {
            if (self.abstractCreature.world?.name is CW)
                return new(.4f, .4f, .4f);
            return orig(self);
        });
        new Hook(typeof(Inspector).GetMethod("get_TrueColor", Instance | Public | NonPublic), (Func<Inspector, Color> orig, Inspector self) =>
        {
            if (self.abstractCreature.world?.name is CW)
                return self.OwneriteratorColor;
            return orig(self);
        });
        On.RoomCamera.DrawUpdate += (orig, self, timeStacker, timeSpeed) =>
        {
            orig(self, timeStacker, timeSpeed);
            if (self.room is Room rm && rm.world?.name is CW && self.fullScreenEffect is FSprite spr)
            {
                var ef = self.lightBloomAlphaEffect;
                if (ef == RoomSettings.RoomEffect.Type.Bloom || ef == RoomSettings.RoomEffect.Type.SkyBloom || ef == RoomSettings.RoomEffect.Type.SkyAndLightBloom || ef == RoomSettings.RoomEffect.Type.LightBurn)
                    spr.alpha = self.lightBloomAlpha = rm.roomSettings.GetEffectAmount(ef);
            }
        };
        CWOracleHooks.Apply();
        CWWater.Apply();
    }

    public void OnDisable()
    {
        s_born = null!;
        s_bornInCW = null!;
        s_logger = null;
        ScavImmuRegions = null!;
        CWTextPath = null!;
        CWOracleHooks.WorldSaveData = null!;
        CWOracleHooks.CWWorldRedCyclesInfo = null!;
        CWOracleHooks.GameData = null!;
        CWOracleHooks.CWGameRedCyclesInfo = null!;
    }
}
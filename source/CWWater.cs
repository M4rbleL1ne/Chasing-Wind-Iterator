using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System;
using UnityEngine;
using MoreSlugcats;

namespace CWStuff;

static class CWWater
{
    internal static void Apply()
    {
        IL.BodyChunk.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, BodyChunk self) => flag || self.owner?.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook BodyChunk.Update!");
        };
        IL.LightSource.DrawSprites += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, LightSource self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook LightSource.DrawSprites!");
        };
        IL.Bubble.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Bubble self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Bubble.Update!");
        };
        IL.VirtualMicrophone.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, VirtualMicrophone self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook VirtualMicrophone.Update!");
        };
        IL.VirtualMicrophone.PositionedSound.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, VirtualMicrophone.PositionedSound self) => flag || self.mic?.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook VirtualMicrophone.PositionedSound.Update!");
        };
        IL.RoomCamera.DrawUpdate += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, RoomCamera self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook RoomCamera.DrawUpdate!");
        };
        On.Room.ctor += (orig, self, game, world, abstractRoom) =>
        {
            orig(self, game, world, abstractRoom);
            if (game is not null && world.name is CWStuffPlugin.CW)
            {
                if (MoreSlugcatsEnums.RoomEffectType.InvertedWater is not RoomSettings.RoomEffect.Type t || t.Index == -1)
                    MoreSlugcatsEnums.RoomEffectType.InvertedWater = new(nameof(MoreSlugcatsEnums.RoomEffectType.InvertedWater), true);
                if (self.roomSettings.GetEffect(MoreSlugcatsEnums.RoomEffectType.InvertedWater) is not null)
                    self.waterInverted = true;
            }
        };
        On.Room.PointSubmerged_Vector2 += (orig, self, pos) =>
        {
            if (self.world?.name is CWStuffPlugin.CW && self.waterInverted)
            {
                if (self.waterObject is Water w)
                    return pos.y > w.DetailedWaterLevel(pos.x);
                return pos.y > self.floatWaterLevel;
            }
            return orig(self, pos);
        };
        On.Room.PointSubmerged_Vector2_float += (orig, self, pos, yDisplacement) =>
        {
            if (self.world?.name is CWStuffPlugin.CW && self.waterInverted)
            {
                if (self.waterObject is Water w)
                    return pos.y > w.DetailedWaterLevel(pos.x) + yDisplacement;
                return pos.y > self.floatWaterLevel + yDisplacement;
            }
            return orig(self, pos, yDisplacement);
        };
        On.Room.AddWater += (orig, self) =>
        {
            var flag = self.waterObject is null;
            orig(self);
            if (flag && self.world?.name is CWStuffPlugin.CW && self.waterInverted && self.defaultWaterLevel > 0)
                self.waterInverted = false;
        };
        IL.RoomRain.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, RoomRain self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook RoomRain.Update!");
        };
        new Hook(typeof(RoomRain).GetMethod("get_FloodLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), (Func<RoomRain, float> orig, RoomRain self) => self.room is Room rm && rm.world?.name is CWStuffPlugin.CW && rm.waterFlux is Room.WaterFluxController flux ? flux.fluxWaterLevel : orig(self));
        new Hook(typeof(BodyChunk).GetMethod("get_submersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), (Func<BodyChunk, float> orig, BodyChunk self) =>
        {
            if (self.owner?.room is Room rm && rm.world?.name is CWStuffPlugin.CW && rm.waterInverted)
                return 1f - Mathf.InverseLerp(self.pos.y - self.rad, self.pos.y + self.rad, rm.FloatWaterLevel(self.pos.x));
            return orig(self);
        });
        IL.Player.MovementUpdate += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate! (part 1)");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate! (part 2)");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate (part 3)!");
        };
        IL.Player.UpdateBodyMode += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.UpdateBodyMode (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.UpdateBodyMode (part 2)!");
        };
        IL.Player.LungUpdate += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)))
             && c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.LungUpdate (part 1)!");
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.LungUpdate (part 2)!");
        };
        On.Water.ctor += (orig, self, room, waterLevel) =>
        {
            orig(self, room, waterLevel);
            if (room.world?.name is CWStuffPlugin.CW && room.waterInverted)
                self.waterSounds = new(self.waterSoundObject, new(0f, room.PixelHeight - (room.defaultWaterLevel - 1) * 20f, room.PixelWidth, room.PixelHeight - room.defaultWaterLevel * 20f), room);
        };
        IL.Water.Update += il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .EmitDelegate((bool flag, Water self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
            }
            else
                CWStuffPlugin.s_logger.LogError("Couldn't ILHook Water.Update!");
        };
        On.Water.shiftWithInversion += (orig, self, shift) =>
        {
            if (self.room is Room rm && rm.world?.name is CWStuffPlugin.CW && rm.waterInverted)
                return shift with { y = -shift.y };
            return orig(self, shift);
        };
        IL.Water.DrawSprites += il =>
        {
            var c = new ILCursor(il);
            for (var i = 1; i <= 6; i++)
            {
                if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
                {
                    c.Emit(OpCodes.Ldarg_0)
                     .EmitDelegate((bool flag, Water self) => flag || self.room?.world?.name is CWStuffPlugin.CW);
                }
                else
                    CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Water.DrawSprites! (part {i})");
            }
        };
    }
}
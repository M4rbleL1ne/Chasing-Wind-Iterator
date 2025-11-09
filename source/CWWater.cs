using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System;
using UnityEngine;

namespace CWStuff;

static class CWWater
{
    internal static void Apply()
    {
        IL.BodyChunk.Update += IL_BodyChunk_Update;
        IL.LightSource.DrawSprites += IL_LightSource_DrawSprites;
        IL.Bubble.Update += IL_Bubble_Update;
        IL.VirtualMicrophone.Update += IL_VirtualMicrophone_Update;
        IL.VirtualMicrophone.PositionedSound.Update += IL_PositionedSound_Update;
        IL.RoomCamera.DrawUpdate += IL_RoomCamera_DrawUpdate;
        On.Room.ctor += On_Room_ctor;
        On.Room.PointSubmerged_Vector2 += On_Room_PointSubmerged_Vector2;
        On.Room.PointSubmerged_Vector2_float += On_Room_PointSubmerged_Vector2_float;
        On.Room.AddWater += On_Room_AddWater;
        IL.RoomRain.Update += IL_RoomRain_Update;
        new Hook(typeof(RoomRain).GetMethod("get_FloodLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), On_RoomRain_get_FloodLevel);
        new Hook(typeof(BodyChunk).GetMethod("get_submersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), On_BodyChunk_get_submersion);
        IL.Player.MovementUpdate += IL_Player_MovementUpdate;
        IL.Player.UpdateBodyMode += IL_Player_UpdateBodyMode;
        IL.Player.LungUpdate += IL_Player_LungUpdate;
        On.Water.ctor += On_Water_ctor;
        IL.Water.Update += IL_Water_Update;
        On.Water.shiftWithInversion += IL_Water_shiftWithInversion;
        IL.Water.DrawSprites += IL_Water_DrawSprites;
    }

    static void IL_BodyChunk_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, BodyChunk self) => flag || self.owner?.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook BodyChunk.Update!");
    }

    static void IL_LightSource_DrawSprites(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, LightSource self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook LightSource.DrawSprites!");
    }

    static void IL_Bubble_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Bubble self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Bubble.Update!");
    }

    static void IL_VirtualMicrophone_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, VirtualMicrophone self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook VirtualMicrophone.Update!");
    }

    static void IL_PositionedSound_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, VirtualMicrophone.PositionedSound self) => flag || self.mic?.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook VirtualMicrophone.PositionedSound.Update!");
    }

    static void IL_RoomCamera_DrawUpdate(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, RoomCamera self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook RoomCamera.DrawUpdate!");
    }

    static void On_Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom, bool devUI)
    {
        orig(self, game, world, abstractRoom, devUI);
        if (game is not null && world.name == CWStuffPlugin.CW)
        {
            if (DLCSharedEnums.RoomEffectType.InvertedWater is not RoomSettings.RoomEffect.Type t || t.Index == -1)
                DLCSharedEnums.RoomEffectType.InvertedWater = new(nameof(DLCSharedEnums.RoomEffectType.InvertedWater), true);
            if (self.roomSettings.GetEffect(DLCSharedEnums.RoomEffectType.InvertedWater) is not null)
                self.waterInverted = true;
        }
    }

    static bool On_Room_PointSubmerged_Vector2(On.Room.orig_PointSubmerged_Vector2 orig, Room self, Vector2 pos)
    {
        if (self.world?.name == CWStuffPlugin.CW && self.waterInverted)
        {
            if (self.waterObject is Water w)
                return pos.y > w.DetailedWaterLevel(pos);
            return pos.y > self.FloatWaterLevel();
        }
        return orig(self, pos);
    }

    static bool On_Room_PointSubmerged_Vector2_float(On.Room.orig_PointSubmerged_Vector2_float orig, Room self, Vector2 pos, float yDisplacement)
    {
        if (self.world?.name == CWStuffPlugin.CW && self.waterInverted)
        {
            if (self.waterObject is Water w)
                return pos.y > w.DetailedWaterLevel(pos) + yDisplacement;
            return pos.y > self.FloatWaterLevel() + yDisplacement;
        }
        return orig(self, pos, yDisplacement); throw new NotImplementedException();
    }

    static void On_Room_AddWater(On.Room.orig_AddWater orig, Room self)
    {
        var flag = self.waterObject is null;
        orig(self);
        if (flag && self.world?.name == CWStuffPlugin.CW && self.waterInverted && self.DefaultWaterLevel() > 0)
            self.waterInverted = false;
    }

    static void IL_RoomRain_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared"))
         && c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, RoomRain self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook RoomRain.Update!");
    }

    static float On_RoomRain_get_FloodLevel(Func<RoomRain, float> orig, RoomRain self) => self.room is Room rm && rm.world?.name == CWStuffPlugin.CW && rm.waterFlux is Room.WaterFluxController flux ? flux.fluxWaterLevel : orig(self);

    static float On_BodyChunk_get_submersion(Func<BodyChunk, float> orig, BodyChunk self)
    {
        if (self.owner?.room is Room rm && rm.world?.name == CWStuffPlugin.CW && rm.waterInverted)
            return 1f - Mathf.InverseLerp(self.pos.y - self.rad, self.pos.y + self.rad, rm.FloatWaterLevel(self.pos));
        return orig(self);
    }

    static void IL_Player_MovementUpdate(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate! (part 1)");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate! (part 2)");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.MovementUpdate (part 3)!");
    }

    static void IL_Player_UpdateBodyMode(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.UpdateBodyMode (part 1)!");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF))))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.UpdateBodyMode (part 2)!");
    }

    static void IL_Player_LungUpdate(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.LungUpdate (part 1)!");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Player self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Player.LungUpdate (part 2)!");
    }

    static void On_Water_ctor(On.Water.orig_ctor orig, Water self, Room room, int waterLevel)
    {
        orig(self, room, waterLevel);
        if (room.world?.name == CWStuffPlugin.CW && room.waterInverted)
            self.waterSounds = new(self.waterSoundObject, new(0f, room.PixelHeight - (room.DefaultWaterLevel() - 1) * 20f, room.PixelWidth, room.PixelHeight - room.DefaultWaterLevel() * 20f), room);
    }

    static void IL_Water_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<ModManager>("get_DLCShared")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Water self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Water.Update!");
    }

    static Vector2 IL_Water_shiftWithInversion(On.Water.orig_shiftWithInversion orig, Water self, Vector2 shift)
    {
        if (self.room is Room rm && rm.world?.name == CWStuffPlugin.CW && rm.waterInverted)
            return shift with { y = -shift.y };
        return orig(self, shift);
    }

    static void IL_Water_DrawSprites(ILContext il)
    {
        var c = new ILCursor(il);
        for (var i = 1; i <= 8; i++)
        {
            if (i != 7)
            {
                if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<ModManager>("get_DLCShared")))
                {
                    c.Emit(OpCodes.Ldarg_0)
                     .EmitDelegate((bool flag, Water self) => flag || self.room?.world?.name == CWStuffPlugin.CW);
                }
                else
                    CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Water.DrawSprites! (part {i})");
            }
        }
    }

    public static int DefaultWaterLevel(this Room self) => self.defaultWaterLevel;

    public static float FloatWaterLevel(this Room self) => self.floatWaterLevel;
}
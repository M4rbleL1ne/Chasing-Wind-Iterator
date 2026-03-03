using HUD;
using JollyCoop;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using static System.Reflection.BindingFlags;
using Random = UnityEngine.Random;

namespace CWStuff;

public static class CWOracleHooks
{
    public class CWOracle : Oracle
    {
        public CWOracle(AbstractPhysicalObject abstractPhysicalObject, Room room) : base(abstractPhysicalObject, room)
        {
            room.AddObject(myScreen = new(room, oracleBehavior = new CWOracleBehavior(this)));
            marbles = [];
            SetUpMarbles();
            room.gravity = 0f;
            var uads = room.updateList;
            for (var num = 0; num < uads.Count; num++)
            {
                if (uads[num] is AntiGravity a)
                {
                    a.active = false;
                    break;
                }
            }
            arm = new CWOracleArm(this);
        }

        public override void InitiateGraphicsModule() => graphicsModule ??= new CWOracleGraphics(this);

        public override void HitByWeapon(Weapon weapon)
        {
            if (Consious)
                (oracleBehavior as CWOracleBehavior)!.ReactToHitWeapon();
        }
    }

    public class CWOracleBehavior : SSOracleBehavior
    {
        public PhysicalObject? OYBot;

        public virtual Player? PlayerWithNeuronInStomach => PlayersInRoom?.Find(x => x.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer);

        public virtual Player? PlayerWithBotInStomach => PlayersInRoom?.Find(x => x.objectInStomach?.type?.value == "OYOrbitalRobot");

        public virtual bool HasSeenOYBot => oracle.room.game.session is StoryGameSession sess && WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var data) && data.SeenOYBot;

        public CWOracleBehavior(CWOracle oracle) : base(oracle)
        {
            InitStoryPearlCollection();
            currSubBehavior.Deactivate();
            allSubBehaviors.RemoveAt(allSubBehaviors.Count - 1);
            allSubBehaviors.Add(currSubBehavior = new CWNoSubBehavior(this));
        }

        public override void Update(bool eu)
        {
            if (oracle.room is not Room rm || rm.game.session is not StoryGameSession sess)
                return;
            var deathData = sess.saveState.deathPersistentSaveData;
            int i, j;
            Creature.Grasp[] gs;
            List<Player> players;
            var oracleFc = oracle.firstChunk;
            if (inspectPearl is DataPearl p)
            {
                movementBehavior = MovementBehavior.Meditate;
                var grabbers = p.grabbedBy;
                for (i = 0; i < grabbers.Count; i++)
                {
                    if (grabbers[i]?.grabber is Creature c)
                    {
                        gs = c.grasps;
                        for (j = 0; j < gs.Length; j++)
                        {
                            if (gs[j]?.grabbed == p)
                                c.ReleaseGrasp(j);
                        }
                    }
                }
                var playerFc = p.firstChunk;
                var pos = oracleFc.pos - playerFc.pos;
                var dist = Custom.Dist(oracleFc.pos, playerFc.pos);
                playerFc.vel += Vector2.ClampMagnitude(pos, 40f) / 40f * Mathf.Clamp(2f - dist / 200f * 2f, .5f, 2f);
                if (playerFc.vel.magnitude < 1f && dist < 16f)
                    playerFc.vel = Custom.RNV() * 8f;
                if (playerFc.vel.magnitude > 8f)
                    playerFc.vel /= 2f;
                if (dist < 100f && pearlConversation is null && conversation is null)
                    StartItemConversation(p);
            }
            UpdateStoryPearlCollection();
            if (timeSinceSeenPlayer >= 0)
                ++timeSinceSeenPlayer;
            if (pearlPickupReaction && timeSinceSeenPlayer > 300 && deathData.theMark && currSubBehavior is not CWThrowOut)
            {
                var flag = false;
                if (player is Player play)
                {
                    gs = play.grasps;
                    for (i = 0; i < gs.Length; i++)
                    {
                        if (gs[i]?.grabbed is PebblesPearl prl && prl.AbstractPearl.type == AbstractPhysicalObjectType.CWPearl)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag && !lastPearlPickedUp && (conversation is not CWConversation conv || (conv.age > 300 && !conv.paused)))
                {
                    if (conversation is not null)
                    {
                        conversation.paused = true;
                        restartConversationAfterCurrentDialoge = true;
                    }
                    if (CWConversation.CWLinesFromFile("PearlPickupReaction", sess.saveStateNumber?.value) is string[] lns && lns.Length > 0)
                        dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
                    pearlPickupReaction = false;
                }
                lastPearlPickedUp = flag;
            }
            if (conversation is CWConversation cwc)
            {
                if (restartConversationAfterCurrentDialoge && cwc.paused && action != SSOracleBehavior.Action.General_GiveMark && dialogBox.messages.Count == 0 && player?.room == rm)
                {
                    cwc.paused = false;
                    restartConversationAfterCurrentDialoge = false;
                    cwc.RestartCurrent();
                }
            }
            else if (pearlConversation is CWPearlConversation pconv)
            {
                if (pconv.slatedForDeletion)
                {
                    pearlConversation = null;
                    if (inspectPearl is DataPearl pcp && player is Player pl)
                    {
                        var pfc = pcp.firstChunk;
                        pfc.vel = Custom.DirVec(pfc.pos, pl.mainBodyChunk.pos) * 3f;
                        readDataPearlOrbits.Add(pcp.AbstractPearl);
                        inspectPearl = null;
                    }
                }
                else
                {
                    pconv.Update();
                    if (player?.room != rm || (ModManager.CoopAvailable && PlayersInRoom.Count == 0))
                    {
                        if (!pconv.paused)
                        {
                            pconv.paused = true;
                            InterruptPearlMessagePlayerLeaving();
                        }
                    }
                    else if (pconv.paused && !restartConversationAfterCurrentDialoge)
                        ResumePausedPearlConversation();
                    if (pconv.paused && restartConversationAfterCurrentDialoge && dialogBox.messages.Count == 0)
                    {
                        pconv.paused = false;
                        restartConversationAfterCurrentDialoge = false;
                        pconv.RestartCurrent();
                    }
                }
            }
            else
                restartConversationAfterCurrentDialoge = false;
            if (voice is ChunkSoundEmitter vce)
            {
                vce.alive = true;
                if (vce.slatedForDeletetion)
                    voice = null;
            }
            if (rm.game.rainWorld.safariMode)
            {
                safariCreature = null;
                var minDist = float.MaxValue;
                var crits = rm.abstractRoom.creatures;
                for (i = 0; i < crits.Count; i++)
                {
                    if (crits[i].realizedCreature is Creature rlc)
                    {
                        var tempDist = Custom.Dist(oracleFc.pos, rlc.mainBodyChunk.pos);
                        if (tempDist < minDist)
                        {
                            minDist = tempDist;
                            safariCreature = rlc;
                        }
                    }
                }
            }
            FindPlayer();
            var cams = rm.game.cameras;
            for (i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                cam.virtualMicrophone.volumeGroups[2] = cam.room == rm ? 1f - rm.gravity : 1f;
            }
            if (!oracle.Consious)
                return;
            unconciousTick = 0f;
            currSubBehavior?.Update();
            if (oracle.slatedForDeletetion)
                return;
            conversation?.Update();
            if (currSubBehavior?.CurrentlyCommunicating is false or null && pearlConversation is null)
                pathProgression = Math.Min(1f, pathProgression + 1f / Mathf.Lerp(40f + pathProgression * 80f, Vector2.Distance(lastPos, nextPos) / 5f, .5f));
            currentGetTo = Custom.Bezier(lastPos, ClampVectorInRoom(lastPos + lastPosHandle), nextPos, ClampVectorInRoom(nextPos + nextPosHandle), pathProgression);
            floatyMovement = false;
            investigateAngle += invstAngSpeed;
            ++inActionCounter;
            if (player?.room != rm || (ModManager.CoopAvailable && PlayersInRoom.Count == 0))
            {
                killFac = 0f;
                ++playerOutOfRoomCounter;
            }
            if (pathProgression >= 1f && consistentBasePosCounter > 100 && !oracle.arm.baseMoving)
                ++allStillCounter;
            else
                allStillCounter = 0;
            lastKillFac = killFac;
            if (action == Action.General_Idle)
            {
                if (movementBehavior != MovementBehavior.Idle && movementBehavior != MovementBehavior.Meditate)
                    movementBehavior = MovementBehavior.Idle;
                throwOutCounter = 0;
                if (player is Player pl && pl.room == rm)
                {
                    var mbcPos = pl.mainBodyChunk.pos;
                    ++discoverCounter;
                    if (rm.GetTilePosition(mbcPos).y < 32 && (discoverCounter > 220 || Custom.DistLess(mbcPos, oracleFc.pos, 150f) || !Custom.DistLess(mbcPos, rm.MiddleOfTile(rm.ShortcutLeadingToNode(1).StartTile), 150f)))
                        SeePlayer();
                }
            }
            else if (action == Action.General_GiveMark)
            {
                if (currSubBehavior is not CWGeneralConversation cv || player is not Player pl)
                    return;
                movementBehavior = MovementBehavior.KeepDistance;
                if (inActionCounter > 30 && inActionCounter < 300)
                {
                    if (inActionCounter < 300)
                    {
                        if (ModManager.CoopAvailable)
                            StunCoopPlayers(20);
                        else
                            pl.Stun(20);
                    }
                    if (ModManager.CoopAvailable)
                    {
                        players = PlayersInRoom;
                        for (i = 0; i < players.Count; i++)
                        {
                            var play = players[i];
                            play.mainBodyChunk.vel += Vector2.ClampMagnitude(rm.MiddleOfTile(24, 14) - play.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, inActionCounter);
                        }
                    }
                    else
                        pl.mainBodyChunk.vel += Vector2.ClampMagnitude(rm.MiddleOfTile(24, 14) - pl.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, inActionCounter);
                }
                if (inActionCounter == 30)
                    rm.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                if (inActionCounter == 300)
                {
                    if (ModManager.CoopAvailable)
                    {
                        players = PlayersInRoom;
                        for (i = 0; i < players.Count; i++)
                        {
                            var play = players[i];
                            play.mainBodyChunk.vel += Custom.RNV() * 10f;
                            play.bodyChunks[1].vel += Custom.RNV() * 10f;
                        }
                    }
                    else
                    {
                        pl.mainBodyChunk.vel += Custom.RNV() * 10f;
                        pl.bodyChunks[1].vel += Custom.RNV() * 10f;
                    }
                    if ((cv.Gifts & GiftStates.FoodMax) == GiftStates.FoodMax)
                        pl.AddFood(pl.MaxFoodInStomach);
                    if (ModManager.CoopAvailable)
                        StunCoopPlayers(40);
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
                        players = PlayersInRoom;
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
                if ((cv.Gifts & GiftStates.Mark) == GiftStates.Mark)
                {
                    if (ModManager.CoopAvailable)
                    {
                        players = PlayersInRoom;
                        for (i = 0; i < players.Count; i++)
                        {
                            var play = players[i];
                            if (inActionCounter > 300 && play.graphicsModule is PlayerGraphics pgr)
                                pgr.markAlpha = Mathf.Max(pgr.markAlpha, Mathf.InverseLerp(500f, 300f, inActionCounter));
                        }
                    }
                    else if (inActionCounter > 300 && player?.graphicsModule is PlayerGraphics pgr)
                        pgr.markAlpha = Mathf.Max(pgr.markAlpha, Mathf.InverseLerp(500f, 300f, inActionCounter));
                }
                if (inActionCounter >= 500 && conversation is Conversation co)
                    co.paused = false;
            }
            Move();
            if (player?.room == rm && conversation is null && inspectPearl is null)
            {
                var physicalObjects = rm.physicalObjects;
                for (i = 0; i < physicalObjects.Length; i++)
                {
                    var list = physicalObjects[i];
                    for (j = 0; j < list.Count; j++)
                    {
                        if (inspectPearl is not null)
                            goto DOUBLE_BREAK;
                        if (list[j] is DataPearl dpearl && dpearl.grabbedBy.Count == 0 && dpearl.AbstractPearl.dataPearlType != DataPearlType.CWPearl && !readDataPearlOrbits.Contains(dpearl.AbstractPearl) && deathData.theMark && !talkedAboutThisSession.Contains(dpearl.abstractPhysicalObject.ID))
                            inspectPearl = dpearl;
                    }
                }
            }
        DOUBLE_BREAK:
            if (working != getToWorking)
                working = Custom.LerpAndTick(working, getToWorking, .05f, 1f / 30f);
            if (currSubBehavior?.LowGravity >= 0f)
                rm.gravity = Custom.LerpAndTick(rm.gravity, currSubBehavior.LowGravity, .05f, .02f);
            else
            {
                if (currSubBehavior?.Gravity is false)
                    rm.gravity = Custom.LerpAndTick(rm.gravity, 0f, .05f, .02f);
                else
                    rm.gravity = 1f - working;
            }
            if (rm.gravity < .1f)
                rm.gravity = .1f;
            for (i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam.room == rm && !cam.AboutToSwitchRoom && cam.paletteBlend != 1f - rm.gravity)
                    cam.ChangeBothPalettes(25, 26, 1f - rm.gravity);
            }
            OnUpdate?.Invoke(this);
        }

        public override void UnconciousUpdate()
        {
            base.UnconciousUpdate();
            var run = true;
            OnUnconciousUpdate?.Invoke(this, ref run);
            if (!run)
                return;
            var rm = oracle.room;
            var cams = rm.game.cameras;
            for (var i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam.room == rm && !cam.AboutToSwitchRoom)
                    cam.ChangeBothPalettes(10, 26, .51f + Mathf.Sin(unconciousTick * .257079631f) * .35f);
            }
            unconciousTick += 1f;
        }

        public virtual void TakeNeuron(NSHSwarmer gn)
        {
            var run = true;
            OnTakeNeuron?.Invoke(this, ref run);
            if (run && currSubBehavior is CWGeneralConversation cv)
            {
                var fc = gn.firstChunk;
                cv.CurrentLookPoint = fc.pos;
                movementBehavior = MovementBehavior.KeepDistance;
                var grbs = gn.grabbedBy;
                if (grbs.Count > 0)
                {
                    for (var num = grbs.Count - 1; num >= 0; num--)
                        grbs[num]?.Release();
                    fc.vel.y = 7f;
                    var room = oracle.room;
                    for (var j = 0; j < 7; j++)
                        room.AddObject(new Spark(fc.pos, Custom.RNV() * Mathf.Lerp(4f, 16f, Random.value), gn.myColor, null, 9, 40));
                }
                gn.storyFly = true;
                gn.storyFlyTarget = cv.GrabPos;
                fc.mass = .000001f;
                cv.ActiveNeuronMovement = true;
                if (ModManager.CoopAvailable)
                    StunCoopPlayers(30);
                else
                    player?.Stun(30);
            }
        }

        public virtual void TakeBot(PhysicalObject bot)
        {
            var run = true;
            OnTakeBot?.Invoke(this, ref run);
            if (run && currSubBehavior is CWGeneralConversation cv)
            {
                var fc = bot.firstChunk;
                cv.CurrentLookPoint = fc.pos;
                movementBehavior = MovementBehavior.KeepDistance;
                var grbs = bot.grabbedBy;
                if (grbs.Count > 0)
                {
                    for (var num = grbs.Count - 1; num >= 0; num--)
                        grbs[num]?.Release();
                    fc.vel.y = 7f;
                    var room = oracle.room;
                    for (var j = 0; j < 7; j++)
                        room.AddObject(new Spark(fc.pos, Custom.RNV() * Mathf.Lerp(4f, 16f, Random.value), Color.green, null, 9, 40));
                }
                fc.mass = .000001f;
                cv.ActiveBotMovement = true;
                if (ModManager.CoopAvailable)
                    StunCoopPlayers(30);
                else
                    player?.Stun(30);
            }
        }
    }

    public class CWOracleGraphics : OracleGraphics
    {
        public CWOracleGraphics(CWOracle ow) : base(ow)
        {
            totalSprites -= armBase.totalSprites;
            killSprite = totalSprites;
            ++totalSprites;
            armBase.firstSprite = firstArmBaseSprite = totalSprites;
            totalSprites += armBase.totalSprites;
        }

        public override void Update()
        {
            base.Update();
            var ow = oracle;
            if (ow.room is Room rm)
            {
                if (!rm.game.cameras[0].AboutToSwitchRoom || lightsource is null)
                {
                    if (lightsource is null)
                    {
                        rm.AddObject(lightsource = new(ow.firstChunk.pos, false, Custom.HSL2RGB(200f / 360f, 1f, .5f), ow)
                        {
                            affectedByPaletteDarkness = 0f
                        });
                        return;
                    }
                    lightsource.setAlpha = Math.Max(1f - rm.gravity, .01f);
                    lightsource.setRad = 300f;
                    lightsource.setPos = ow.firstChunk.pos;
                }
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            var sprs = sLeaser.sprites;
            sprs[neckSprite].scaleX = 3f;
            sprs[fadeSprite].color = Color.black;
            sprs[fadeSprite].alpha = .5f;
            for (var k = 0; k < 2; k++)
                sprs[EyeSprite(k)].color = new(0f, 0f, 150f / 255f);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[killSprite] ??= new("Futile_White")
            {
                shader = Custom.rainWorld.Shaders["FlatLight"]
            };
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (oracle is not CWOracle ow || ow.room is not Room rm || ow.slatedForDeletetion || rm != rCam.room || dispose || ow.oracleBehavior is not CWOracleBehavior behav)
                return;
            var sprites = sLeaser.sprites;
            var ch1 = ow.bodyChunks[1];
            var fc = ow.firstChunk;
            Vector2 ch1Pos = Vector2.Lerp(ch1.lastPos, ch1.pos, timeStacker),
                fcPos = Vector2.Lerp(fc.lastPos, fc.pos, timeStacker),
                dir = Custom.DirVec(ch1Pos, fcPos),
                perp = Custom.PerpendicularVector(dir);
            var ks = sprites[killSprite];
            if (behav.killFac > 0f)
            {
                ks.isVisible = true;
                if (behav.player is Player p)
                {
                    var mbc = p.mainBodyChunk;
                    ks.SetPosition(Vector2.Lerp(mbc.lastPos, mbc.pos, timeStacker) - camPos);
                }
                var f = Mathf.Lerp(behav.lastKillFac, behav.killFac, timeStacker);
                ks.scale = Mathf.Lerp(200f, 2f, Mathf.Pow(f, .5f));
                ks.alpha = Mathf.Pow(f, 3f);
            }
            else
                ks.isVisible = false;
            var openEyesFac = Mathf.Lerp(lastEyesOpen, eyesOpen, timeStacker);
            var hds = hands;
            for (var k = 0; k < hds.Length; k++)
            {
                sprites[EyeSprite(k)].scaleY = Mathf.Lerp(1f, 2.5f, openEyesFac);
                var num2 = k == 1 ? -1f : 1f;
                var hand1 = hds[k];
                Vector2 handPos = Vector2.Lerp(hand1.lastPos, hand1.pos, timeStacker),
                    adjPos1 = fcPos + perp * 4f * num2,
                    cB = handPos + Custom.DirVec(handPos, adjPos1) * 3f + dir,
                    cA = adjPos1 + perp * 5f * num2,
                    adjPos2 = adjPos1 - perp * 2f * num2;
                for (var m = 0; m < 7; m++)
                {
                    Vector2 handBez = Custom.Bezier(adjPos1, cA, handPos, cB, m / 6f),
                        handDir = Custom.DirVec(adjPos2, handBez),
                        handPerp = Custom.PerpendicularVector(handDir) * (k == 0 ? -1f : 1f);
                    var handDist = Vector2.Distance(adjPos2, handBez);
                    var hand = (sprites[HandSprite(k, 1)] as TriangleMesh)!;
                    hand.MoveVertice(m * 4, handBez - handDir * handDist * .3f - handPerp * 4f - camPos);
                    hand.MoveVertice(m * 4 + 1, handBez - handDir * handDist * .3f + handPerp * 4f - camPos);
                    hand.MoveVertice(m * 4 + 2, handBez - handPerp * 4f - camPos);
                    hand.MoveVertice(m * 4 + 3, handBez + handPerp * 4f - camPos);
                    adjPos2 = handBez;
                }
                var ft = feet[k];
                handPos = Vector2.Lerp(ft.lastPos, ft.pos, timeStacker);
                Vector2 b = Vector2.Lerp(knees[k, 1], knees[k, 0], timeStacker);
                cB = Vector2.Lerp(handPos, b, .9f);
                cA = Vector2.Lerp(ch1Pos, b, .9f);
                adjPos2 = ch1Pos - perp * 2f * num2;
                var footWidthFac = 4f;
                for (var n = 0; n < 7; n++)
                {
                    Vector2 footBez = Custom.Bezier(ch1Pos, cA, handPos, cB, n / 6f),
                        footDir = Custom.DirVec(adjPos2, footBez),
                        footPerp = Custom.PerpendicularVector(footDir) * (k == 0 ? -1f : 1f);
                    var footDist = Vector2.Distance(adjPos2, footBez);
                    var foot = (sprites[FootSprite(k, 1)] as TriangleMesh)!;
                    foot.MoveVertice(n * 4, footBez - footDir * footDist * .3f - footPerp * (footWidthFac + 2f) * .5f - camPos);
                    foot.MoveVertice(n * 4 + 1, footBez - footDir * footDist * .3f + footPerp * (footWidthFac + 2f) * .5f - camPos);
                    foot.MoveVertice(n * 4 + 2, footBez - footPerp * 2f - camPos);
                    foot.MoveVertice(n * 4 + 3, footBez + footPerp * 2f - camPos);
                    adjPos2 = footBez;
                    footWidthFac = 2f;
                }
            }
        }
    }

    public class CWOracleArm : Oracle.OracleArm
    {
        public CWOracleArm(CWOracle oracle) : base(oracle)
        {
            var rm = oracle.room;
            baseMoveSoundLoop = new(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, rm, 1f, 1f);
            if (rm.game?.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase))
            {
                cornerPositions[0] = rm.MiddleOfTile(9, 35);
                cornerPositions[1] = rm.MiddleOfTile(37, 35);
                cornerPositions[2] = rm.MiddleOfTile(37, 7);
                cornerPositions[3] = rm.MiddleOfTile(9, 7);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class CWOracleWorldSaveData(SLOrcacleState oracleState)
    {
        public SLOrcacleState OracleState = oracleState;
        public int NumberOfConversations, AnnoyedCounter;
        public bool SeenGreenNeuron, ScavImmunity, SeenSpearmasterTaggedPearl, AdditionalRedCycles, SeenOYBot;
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
    public static event CWEvent? OnTakeNeuron, OnReleaseNeuron, OnTakeBot, OnReleaseBot, OnResumePausedPearlConversation, OnInterruptPearlMessagePlayerLeaving, OnUnconciousUpdate, OnReactToHitWeapon,
        OnSlugcatEnterRoomReaction, OnNewAction, OnSeePlayer;
    public static event Action<SSOracleBehavior>? OnMove, OnUpdate;

    public static int AdditionalCycles => ModManager.MMF && MMF.cfgHunterBonusCycles is Configurable<int> cfg ? cfg.Value : 5;

    internal static void Apply()
    {
        On.Room.ReadyForAI += On_Room_ReadyForAI;
        On.OracleGraphics.ArmJointGraphics.ctor += On_ArmJointGraphics_ctor;
        On.OracleGraphics.Gown.Color += On_Gown_Color;
        On.OracleGraphics.SkinColor += On_OracleGraphics_SkinColor;
        On.OracleChatLabel.AddToContainer += On_OracleChatLabel_AddToContainer;
        On.OracleChatLabel.DrawSprites += On_OracleChatLabel_DrawSprites;
        IL.Oracle.OracleArm.Joint.Update += IL_Joint_Update;
        IL.Oracle.OracleArm.Update += IL_OracleArm_Update;
        IL.Oracle.ctor += IL_Oracle_ctor;
        On.Oracle.CreateMarble += On_Oracle_CreateMarble;
        On.OracleBehavior.AlreadyDiscussedItemString += On_OracleBehavior_AlreadyDiscussedItemString;
        On.OracleBehavior.FindPlayer += On_OracleBehavior_FindPlayer;
        On.SSOracleBehavior.InitStoryPearlCollection += On_SSOracleBehavior_InitStoryPearlCollection;
        On.SSOracleBehavior.InitateConversation += On_SSOracleBehavior_InitateConversation;
        On.SSOracleBehavior.SeePlayer += On_SSOracleBehavior_SeePlayer;
        new Hook(typeof(SSOracleBehavior).GetMethod("get_HasSeenGreenNeuron", Public | NonPublic | Instance), On_SSOracleBehavior_get_HasSeenGreenNeuron);
        On.SSOracleBehavior.NewAction += On_SSOracleBehavior_NewAction;
        On.SSOracleBehavior.Move += On_SSOracleBehavior_Move;
        On.SSOracleBehavior.HandTowardsPlayer += On_SSOracleBehavior_HandTowardsPlayer;
        On.SSOracleBehavior.CreatureJokeDialog += On_SSOracleBehavior_CreatureJokeDialog;
        On.SSOracleBehavior.ReactToHitWeapon += On_SSOracleBehavior_ReactToHitWeapon;
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
        On.SLOracleBehaviorHasMark.MoonConversation.PearlIntro += On_MoonConversation_PearlIntro;
        new Hook(typeof(SLOracleBehaviorHasMark.MoonConversation).GetMethod("get_State", Public | NonPublic | Instance), On_MoonConversation_get_State);
        //On.Menu.StoryGameStatisticsScreen.GetDataFromGame += On_StoryGameStatisticsScreen_GetDataFromGame;
        IL.Menu.StoryGameStatisticsScreen.GetDataFromGame += IL_StoryGameStatisticsScreen_GetDataFromGame;
        On.Menu.StoryGameStatisticsScreen.TickerIsDone += On_StoryGameStatisticsScreen_TickerIsDone;
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
        On.Player.ThrowObject += On_Player_ThrowObject;
        IL.RegionState.AdaptWorldToRegionState += IL_RegionState_AdaptWorldToRegionState;
    }

    static void IL_RegionState_AdaptWorldToRegionState(ILContext il)
    {
        var c = new ILCursor(il);
        var loc1 = 0;
        ILLabel? label = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out loc1),
            x => x.MatchLdfld<AbstractRoom>("name"),
            x => x.MatchLdstr("SS_AI"),
            x => x.MatchCall<string>("op_Equality"),
            x => x.MatchBrtrue(out label)))
        {
            c.Emit(OpCodes.Ldloc, loc1)
             .Emit<AbstractRoom>(OpCodes.Ldfld, "name")
             .Emit(OpCodes.Ldstr, "CW_AI")
             .Emit<string>(OpCodes.Call, "op_Equality")
             .Emit(OpCodes.Brtrue, label);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook RegionState.AdaptWorldToRegionState!");
    }

    static void On_Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (self.grasps[grasp]?.grabbed is SingularityBomb && self.room?.abstractRoom.name is string nm && string.Equals("CW_AI", nm, StringComparison.OrdinalIgnoreCase))
            return;
        orig(self, grasp, eu);
    }

    static Color On_OracleGraphics_SkinColor(On.OracleGraphics.orig_SkinColor orig, OracleGraphics self) => self is CWOracleGraphics ? new(237f / 255f, 230f / 255f, 1f) : orig(self);

    static void On_SSOracleBehavior_UpdateStoryPearlCollection(On.SSOracleBehavior.orig_UpdateStoryPearlCollection orig, SSOracleBehavior self)
    {
        if (self is CWOracleBehavior && self.oracle is Oracle o)
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
            var save = self.oracle.room.game.StoryCharacter;
            //keeping manual checks because it was said that the AtOrBeforeTimeline method doesn't rly work, but ppl can subscribe to OnPebblesIsDying anyway
            res = save == MoreSlugcatsEnums.SlugcatStatsName.Saint || save == MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
        }
        else
            res = false;
        OnPebblesIsDying?.Invoke(self, ref res);
        return res;
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

    static void IL_StoryGameStatisticsScreen_GetDataFromGame(ILContext il)
    {
        var c = new ILCursor(il);
        int vectorVar = 0, numVar = 0;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloca(out vectorVar))
         && c.TryGotoNext(MoveType.After,
            x => x.MatchLdcI4(2))
         && c.TryGotoNext(MoveType.After,
            x => x.MatchStloc(out numVar)))
        {
            var vars = il.Body.Variables;
            c.Emit(OpCodes.Ldloc, vars[vectorVar])
             .Emit(OpCodes.Ldloca, vars[numVar])
             .Emit(OpCodes.Ldarg_0)
             .Emit(OpCodes.Ldarg_1)
             .EmitDelegate((Vector2 ogPos, ref int num, StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package) =>
             {
                 if (!WorldSaveData.TryGetValue(package.saveState.miscWorldSaveData, out var data))
                     return;
                 var firstPage = self.pages[0];
                 if (data.NumberOfConversations > 0)
                 {
                     var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(0f, -30f * num), "< " + self.Translate("Met Chasing Wind") + ">", NewTickerID.CWEncounter);
                     self.allTickers.Add(ticker);
                     firstPage.subObjects.Add(ticker);
                     ++num;
                 }
                 if (ModManager.MSC && package.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
                 {
                     if (data.SeenSpearmasterTaggedPearl)
                     {
                         var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(-30f, -30f * num), "< " + self.Translate("Brought Moon's message to Chasing Wind") + ">", NewTickerID.CWSpearMission);
                         self.allTickers.Add(ticker);
                         firstPage.subObjects.Add(ticker);
                         ++num;
                     }
                 }
                 else if (data.SeenGreenNeuron)
                 {
                     var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(-30f, -30f * num), "< " + self.Translate("Brought the slag keys to Chasing Wind") + ">", NewTickerID.CWGreenNeuron);
                     self.allTickers.Add(ticker);
                     firstPage.subObjects.Add(ticker);
                     ++num;
                 }
                 var count = data.OracleState.significantPearls.Count;
                 if (count > 0)
                 {
                     var ticker = new StoryGameStatisticsScreen.LabelTicker(self, firstPage, ogPos + new Vector2(-90f, -30f * num), count, NewTickerID.CWPearls, self.Translate("Unique pearls read by Chasing Wind : "));
                     ticker.numberLabel.pos.x += 130f;
                     self.allTickers.Add(ticker);
                     firstPage.subObjects.Add(ticker);
                     ++num;
                 }
             });
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook StoryGameStatisticsScreen.GetDataFromGame!");
    }

    /*static void On_StoryGameStatisticsScreen_GetDataFromGame(On.Menu.StoryGameStatisticsScreen.orig_GetDataFromGame orig, StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if (!WorldSaveData.TryGetValue(package.saveState.miscWorldSaveData, out var data))
            return;
        var ogPos = new Vector2(self.ContinueAndExitButtonsXPos - 160f, 535f);
        var firstPage = self.pages[0];
        if (data.NumberOfConversations > 0)
        {
            var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(0f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Met Chasing Wind") + ">", NewTickerID.CWEncounter);
            self.allTickers.Add(ticker);
            firstPage.subObjects.Add(ticker);
        }
        if (ModManager.MSC && package.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            if (data.SeenSpearmasterTaggedPearl)
            {
                var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(-30f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Brought Moon's message to Chasing Wind") + ">", NewTickerID.CWSpearMission);
                self.allTickers.Add(ticker);
                firstPage.subObjects.Add(ticker);
            }
        }
        else if (data.SeenGreenNeuron)
        {
            var ticker = new StoryGameStatisticsScreen.Popper(self, firstPage, ogPos + new Vector2(-30f, -30f * (-3 + self.allTickers.Count)), "< " + self.Translate("Brought the slag keys to Chasing Wind") + ">", NewTickerID.CWGreenNeuron);
            self.allTickers.Add(ticker);
            firstPage.subObjects.Add(ticker);
        }
        var count = data.OracleState.significantPearls.Count;
        if (count > 0)
        {
            var ticker = new StoryGameStatisticsScreen.LabelTicker(self, firstPage, ogPos + new Vector2(-90f, -30f * (-3 + self.allTickers.Count)), count, NewTickerID.CWPearls, self.Translate("Unique pearls read by Chasing Wind : "));
            ticker.numberLabel.pos.x += 130f;
            self.allTickers.Add(ticker);
            firstPage.subObjects.Add(ticker);
        }
    }*/

    static SLOrcacleState On_MoonConversation_get_State(Func<SLOracleBehaviorHasMark.MoonConversation, SLOrcacleState> orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self is CWPearlConversation && self.myBehavior.oracle.room.game.session is StoryGameSession sess && WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var data))
            return data.OracleState;
        return orig(self);
    }

    static void On_MoonConversation_PearlIntro(On.SLOracleBehaviorHasMark.MoonConversation.orig_PearlIntro orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self is not CWPearlConversation cwp || self.myBehavior is not CWOracleBehavior bhv || bhv.oracle.room.game is not RainWorldGame game)
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

    static void On_SSOracleBehavior_SlugcatEnterRoomReaction(On.SSOracleBehavior.orig_SlugcatEnterRoomReaction orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior)
            orig(self);
        else
        {
            if (self.conversation is null && self.pearlConversation is null)
            {
                var run = true;
                OnSlugcatEnterRoomReaction?.Invoke(self, ref run);
                if (run && (self.currSubBehavior is not CWGeneralConversation cv || !cv.SeenPlayer) && (self.currSubBehavior is not CWNoSubBehavior ns || !ns.SeenPlayer))
                {
                    var sv = self.oracle.room.game.GetStorySession.saveState;
                    if (sv.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("SlugcatEnterRoomReaction", sv.saveStateNumber?.value) is string[] lns && lns.Length > 0)
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
        if (self is not CWOracle)
            orig(self);
        else
        {
            var basePos = self.room?.game?.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase) ? new Vector2(180f, 180f) : new Vector2(200f, 100f);
            PhysicalObject physicalObject = self;
            for (var i = 0; i < 4; i++)
            {
                var ps = new Vector2(basePos.x + 300f, basePos.y + 200f) + Custom.RNV() * 20f;
                var color = i switch
                {
                    5 => 2,
                    2 or 3 => 1,
                    _ => 0,
                };
                self.CreateMarble(physicalObject, ps, 0, 35f, color);
            }
            for (var j = 0; j < 4; j++)
                self.CreateMarble(physicalObject, new Vector2(basePos.x + 300f, basePos.y + 200f) + Custom.RNV() * 20f, 1, 100f, j == 1 ? 2 : 0);
            self.CreateMarble(null, new(basePos.x + 60f, basePos.y + 200f), 0, 0f, 1);
            Vector2 vector2 = new(basePos.x + 80f, basePos.y + 30f), vector3 = Custom.DegToVec(-32.7346f), vector4 = Custom.PerpendicularVector(vector3);
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
            var marbles = self.marbles;
            self.CreateMarble(null, new(basePos.x + 487f, basePos.y + 218f), 0, 0f, 1);
            self.CreateMarble(marbles[marbles.Count - 1], new(basePos.x + 487f, basePos.y + 218f), 0, 18f, 0);
            self.CreateMarble(null, new(basePos.x + 450f, basePos.y + 167f), 0, 0f, 2);
            self.CreateMarble(marbles[marbles.Count - 1], new(basePos.x + 440f, basePos.y + 177f), 0, 38f, 1);
            self.CreateMarble(marbles[marbles.Count - 2], new(basePos.x + 440f, basePos.y + 177f), 0, 38f, 2);
            self.CreateMarble(marbles[marbles.Count - 1], new(basePos.x + 109f, basePos.y + 352f), 0, 42f, 1);
            marbles[marbles.Count - 1].orbitSpeed = .8f;
            self.CreateMarble(marbles[marbles.Count - 1], new(basePos.x + 109f, basePos.y + 352f), 0, 12f, 0);
            for (var d = 0f; d < 2f; d++)
            {
                for (var e = 0f; e < 3f; e++)
                {
                    for (var f = 0f; f < 3f; f++)
                        self.CreateMarble(null, new(basePos.x + 100f + e * 30f + d * 320f, basePos.y + 400f + f * 30f), 0, 0f, d == 0 ? (e == f ? 2 : ((e == 0f && f == 2f) || (e == 2f && f == 0f)) ? 0 : 1) : (e == f ? 0 : ((e == 0f && f == 2f) || (e == 2f && f == 0f)) ? 2 : 1));
                }
            }
        }
    }

    static void On_SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
    {
        if (self is not CWOracleBehavior cwbehav)
            orig(self, eventName);
        else
        {
            if (string.Equals(eventName, "SHOWBRAINPIC", StringComparison.OrdinalIgnoreCase) || string.Equals(eventName, "unlock", StringComparison.OrdinalIgnoreCase))
            {
                self.conversation?.paused = true;
                self.inActionCounter = 0;
                self.NewAction(SSOracleBehavior.Action.MeetWhite_Images);
                self.oracle.room.PlaySound(Random.value < .5f ? NewSoundID.CW_AI_Talk_1 : NewSoundID.CW_AI_Talk_2, self.oracle.firstChunk).requireActiveUpkeep = false;
            }
            else if (string.Equals(eventName, "TAKENEURON", StringComparison.OrdinalIgnoreCase))
            {
                if (self.greenNeuron is NSHSwarmer swn && !swn.slatedForDeletetion && swn.room == self.oracle.room)
                    cwbehav.TakeNeuron(swn);
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
                        cwbehav.TakeNeuron(nsw);
                    }
                }
            }
            else if (string.Equals(eventName, "TAKEBOT", StringComparison.OrdinalIgnoreCase))
            {
                if (cwbehav.OYBot is PhysicalObject bot && !bot.slatedForDeletetion && bot.room == self.oracle.room)
                    cwbehav.TakeBot(bot);
                else if (self.player?.objectInStomach?.type?.value == "OYOrbitalRobot")
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                    self.player.Regurgitate();
                    var objLists = self.oracle.room.physicalObjects;
                    for (var i = 0; i < objLists.Length; i++)
                    {
                        var objs = objLists[i];
                        for (var j = 0; j < objs.Count; j++)
                        {
                            if (objs[j] is PhysicalObject obj && obj.abstractPhysicalObject.type?.value == "OYOrbitalRobot" && obj.firstChunk.mass < 50f)
                            {
                                cwbehav.OYBot = obj;
                                break;
                            }
                        }
                    }
                    if (cwbehav.OYBot is PhysicalObject bot2)
                    {
                        bot2.firstChunk.vel *= 0f;
                        cwbehav.TakeBot(bot2);
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
            else if (string.Equals(eventName, "RELEASEBOT", StringComparison.OrdinalIgnoreCase))
            {
                var run = true;
                OnReleaseBot?.Invoke(self, ref run);
                if (!run)
                    return;
                if (cwbehav.OYBot is PhysicalObject bot && self.player is not null)
                {
                    bot.firstChunk.HardSetPosition(bot.firstChunk.pos);
                    bot.firstChunk.vel *= 0f;
                    bot.firstChunk.mass = .07f;
                    cwbehav.OYBot = null;
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
                    self.conversation?.paused = true;
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

    static void On_OracleBehavior_FindPlayer(On.OracleBehavior.orig_FindPlayer orig, OracleBehavior self)
    {
        if (self is CWOracleBehavior cwbehav)
        {
            if (self.oracle.room is not Room rm || rm.game.rainWorld.safariMode)
                return;
            var flag = false;
            if (WorldSaveData.TryGetValue(rm.game.GetStorySession.saveState.miscWorldSaveData, out var data))
            {
                if (!data.SeenGreenNeuron && cwbehav.PlayerWithNeuronInStomach is Player pl0)
                {
                    flag = true;
                    self.player = pl0;
                }
                else if (!data.SeenOYBot && cwbehav.PlayerWithBotInStomach is Player pl2)
                {
                    flag = true;
                    self.player = pl2;
                }
            }
            else
                self.player = rm.game.Players[0]?.realizedCreature as Player;
            if (self.player is not Player p || p.room != rm || p.inShortcut)
            {
                var inrm = self.PlayersInRoom;
                self.player = inrm.Count > 0 ? inrm[0] : null;
                if (self.player is Player pl1)
                {
                    var num = 1;
                    while (!flag && pl1.inShortcut && num < inrm.Count)
                    {
                        self.player = inrm[num];
                        ++num;
                    }
                }
            }
            if (self.PlayersInRoom.Count > 0 && self.player == self.PlayersInRoom[0] && self.player.dead)
                self.player = null;
            if (self.player is Player pl)
                rm.game.cameras[0].EnterCutsceneMode(pl.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
        }
        else
            orig(self);
    }

    // removed save cleaner since the issue has been fixed for a while
    static string On_MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
    {
        if (WorldSaveData.TryGetValue(self, out var data))
        {
            var strs = self.unrecognizedSaveStrings;
            int i;
            if ((i = strs.IndexOf("M4R_CW_seenGreenNeuron")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.SeenGreenNeuron.ToString());
                else
                    strs[i + 1] = data.SeenGreenNeuron.ToString();
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
                }
                else
                {
                    strs.Add("M4R_CW_redCycles");
                    strs.Add(redCycles);
                }
            }
            if ((i = strs.IndexOf("M4R_CW_seenOYBot")) != -1)
            {
                if (i == strs.Count - 1)
                    strs.Add(data.SeenOYBot.ToString());
                else
                    strs[i + 1] = data.SeenOYBot.ToString();
            }
            else
            {
                strs.Add("M4R_CW_seenOYBot");
                strs.Add(data.SeenOYBot.ToString());
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
                else if (string.Equals(str, "M4R_CW_seenOYBot", StringComparison.OrdinalIgnoreCase))
                    bool.TryParse(unrec[i + 1], out data.SeenOYBot);
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
        if (self is not CWOracleBehavior)
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
        if (self is not CWOracleBehavior)
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
        if (self is not CWOracleBehavior)
            orig(self, item);
        else
        {
            if (!WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
                return;
            var state = data.OracleState;
            self.isRepeatedDiscussion = state.alreadyTalkedAboutItems.Contains(item.abstractPhysicalObject.ID);
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
            if (item.AbstractPearl is SpearMasterPearl.AbstractSpearMasterPearl sp)
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
                if (!state.significantPearls.Contains(dataPearlType))
                    state.significantPearls.Add(dataPearlType);
                self.pearlConversation = new CWPearlConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                ++state.totalPearlsBrought;
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
                if (!state.significantPearls.Contains(dataPearlType))
                    state.significantPearls.Add(dataPearlType);
                self.pearlConversation = new CWPearlConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                ++state.totalPearlsBrought;
            }
            if (!self.isRepeatedDiscussion)
            {
                ++state.totalItemsBrought;
                state.AddItemToAlreadyTalkedAbout(item.abstractPhysicalObject.ID);
            }
            self.talkedAboutThisSession.Add(item.abstractPhysicalObject.ID);
        }
    }

    static void On_SSOracleBehavior_ReactToHitWeapon(On.SSOracleBehavior.orig_ReactToHitWeapon orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior)
            orig(self);
        else
        {
            var res = true;
            OnReactToHitWeapon?.Invoke(self, ref res);
            if (!res)
                return;
            var rm = self.oracle.room;
            var sv = rm.game.GetStorySession.saveState;
            rm.PlaySound(Random.value < .5f ? NewSoundID.CW_AI_Angry_1 : NewSoundID.CW_AI_Angry_2, self.oracle.firstChunk).requireActiveUpkeep = false;
            if (self.conversation is not null || self.pearlConversation is not null)
            {
                self.conversation?.paused = true;
                self.pearlConversation?.paused = true;
                self.restartConversationAfterCurrentDialoge = true;
                if (sv.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("ReactToHitWeaponWhileTalking", sv.saveStateNumber?.value) is string[] lns && lns.Length > 0)
                    self.dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
            }
            else if (sv.deathPersistentSaveData.theMark && CWConversation.CWLinesFromFile("ReactToHitWeapon", sv.saveStateNumber?.value) is string[] lns && lns.Length > 0)
                self.dialogBox.Interrupt(lns[Random.Range(0, lns.Length)], 10);
            if (WorldSaveData.TryGetValue(sv.miscWorldSaveData, out var data) && data.AnnoyedCounter < 4)
                ++data.AnnoyedCounter;
            else
                self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
        }
    }

    static void On_SSOracleBehavior_CreatureJokeDialog(On.SSOracleBehavior.orig_CreatureJokeDialog orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior)
            orig(self);
    }

    static bool On_SSOracleBehavior_HandTowardsPlayer(On.SSOracleBehavior.orig_HandTowardsPlayer orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior)
            return orig(self);
        if ((self.currSubBehavior is not CWThrowOut th || !th.telekinThrowOut || self.player is not Player p || p.dead) && (self.action != SSOracleBehavior.Action.General_GiveMark || self.inActionCounter <= 30 || self.inActionCounter >= 300) && self.action != SSOracleBehavior.Action.ThrowOut_KillOnSight)
            return false;
        return true;
    }

    static void On_SSOracleBehavior_Move(On.SSOracleBehavior.orig_Move orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior)
            orig(self);
        else
        {
            var rm = self.oracle.room;
            Vector2 dgp;
            if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Idle)
            {
                self.invstAngSpeed = 1f;
                var marbles = self.oracle.marbles;
                if (self.investigateMarble is null && marbles.Count > 0)
                    self.investigateMarble = marbles[Random.Range(0, marbles.Count)];
                if (self.investigateMarble is PebblesPearl pe && (pe.orbitObj == self.oracle || Custom.DistLess(new(250f, 150f), pe.firstChunk.pos, 100f)))
                    self.investigateMarble = null;
                if (self.investigateMarble is PebblesPearl pe2)
                {
                    var pePos = pe2.firstChunk.pos;
                    self.lookPoint = pePos;
                    if (Custom.DistLess(self.nextPos, pePos, 100f))
                    {
                        self.floatyMovement = true;
                        self.nextPos = pePos - Custom.DegToVec(self.investigateAngle) * 50f;
                    }
                    else
                        self.SetNewDestination(pePos - Custom.DegToVec(self.investigateAngle) * 50f);
                    if (self.pathProgression == 1f && Random.value < .005f)
                        self.investigateMarble = null;
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate)
            {
                var tl = self.oracle.room.MiddleOfTile(24, 17);
                if (self.nextPos != tl)
                    self.SetNewDestination(tl);
                self.investigateAngle = 0f;
                self.lookPoint = self.oracle.firstChunk.pos + new Vector2(0f, -40f);
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.KeepDistance)
            {
                if (self.player is not Player pl)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    dgp = self.lookPoint = pl.DangerPos;
                    var vector = new Vector2(Random.value * rm.PixelWidth, Random.value * rm.PixelHeight);
                    if (!rm.GetTile(vector).Solid && rm.aimap.getTerrainProximity(vector) > 2 && Vector2.Distance(vector, dgp) > Vector2.Distance(self.nextPos, dgp) + 100f)
                        self.SetNewDestination(vector);
                }
            }
            else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Investigate)
            {
                if (self.player is not Player pl)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    dgp = self.lookPoint = pl.DangerPos;
                    if (self.investigateAngle < -90f || self.investigateAngle > 90f || rm.aimap.getTerrainProximity(self.nextPos) < 2f)
                    {
                        self.investigateAngle = Mathf.Lerp(-70f, 70f, Random.value);
                        self.invstAngSpeed = Mathf.Lerp(.4f, .8f, Random.value) * (Random.value < .5f ? (-1f) : 1f);
                    }
                    var vector = dgp + Custom.DegToVec(self.investigateAngle) * 150f;
                    if (rm.aimap.getTerrainProximity(vector) >= 2f)
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
                if (self.player is not Player pl)
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                else
                {
                    self.lookPoint = pl.DangerPos;
                    var vector = new Vector2(Random.value * rm.PixelWidth, Random.value * rm.PixelHeight);
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
            if (rm.readyForAI)
            {
                var vector = new Vector2(Random.value * rm.PixelWidth, Random.value * rm.PixelHeight);
                if (!rm.GetTile(vector).Solid && self.BasePosScore(vector) + 40f < self.BasePosScore(self.baseIdeal))
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
        if (self is not CWOracleBehavior cwbehav)
            orig(self, nextAction);
        else
        {
            var run = true;
            OnNewAction?.Invoke(self, ref run);
            if (!run || nextAction == self.action)
                return;
            var subbhv = SSOracleBehavior.SubBehavior.SubBehavID.General;
            if (nextAction?.value is string s1)
            {
                if (s1.Contains("MeetWhite"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.MeetWhite;
                else if (s1.Contains("GetNeuron"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.GetNeuron;
                else if (s1.Contains("GetOYBot"))
                    subbhv = SubBehavID.GetOYBot;
                else if (s1.Contains("ThrowOut"))
                    subbhv = SSOracleBehavior.SubBehavior.SubBehavID.ThrowOut;
            }
            self.currSubBehavior?.NewAction(self.action, nextAction);
            if (subbhv != SSOracleBehavior.SubBehavior.SubBehavID.General && subbhv != self.currSubBehavior?.ID)
            {
                SSOracleBehavior.SubBehavior? subBehavior = null;
                var subs = self.allSubBehaviors;
                for (var i = 0; i < subs.Count; i++)
                {
                    var subTemp = subs[i];
                    if (subTemp.ID == subbhv)
                    {
                        subBehavior = subTemp;
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
                        else if ((cwbehav.OYBot is not null || self.player.objectInStomach?.type?.value == "OYOrbitalRobot") && !cwbehav.HasSeenOYBot)
                        {
                            if (WorldSaveData.TryGetValue(self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData, out var data))
                                data.SeenOYBot = true;
                            var s = self.oracle.room.game.StoryCharacter.value + "_FirstEncounter_WithBot";
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
                    else if (subbhv == SubBehavID.GetOYBot)
                    {
                        var s = self.oracle.room.game.StoryCharacter.value + "_GetBot";
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

    static bool On_SSOracleBehavior_get_HasSeenGreenNeuron(Func<SSOracleBehavior, bool> orig, SSOracleBehavior self)
    {
        if (self is CWOracleBehavior)
            return self.oracle.room.game.session is StoryGameSession sess && WorldSaveData.TryGetValue(sess.saveState.miscWorldSaveData, out var data) && data.SeenGreenNeuron;
        return orig(self);
    }

    static void On_SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        if (self is not CWOracleBehavior cwbehav)
            orig(self);
        else
        {
            var rm = self.oracle.room;
            if (self.timeSinceSeenPlayer < 0)
                self.timeSinceSeenPlayer = 0;
            int i, j;
            if (ModManager.CoopAvailable && self.timeSinceSeenPlayer < 5)
            {
                Player? player = null;
                foreach (Player item in from x in rm.game.NonPermaDeadPlayers
                                        where x.Room != rm.abstractRoom && x.realizedCreature is not null
                                        select x.realizedCreature as Player into x
                                        orderby x.slugOnBack is not null
                                        select x)
                {
                    item.slugOnBack?.DropSlug();
                    //JollyCoop.JollyCustom.Log($"Warping player to CW room, {item} - back occupied?{item.slugOnBack}");
                    try
                    {
                        if (item.inShortcut || item.room is null)
                        {
                            //JollyCustom.Log($"Player is currently in a pipe, waiting for them to start iterator sequence ...{item}");
                            self.timeSinceSeenPlayer = 0;
                            continue;
                        }
                        var worldCoordinate = rm.LocalCoordinateOfNode(1);
                        JollyCustom.MovePlayerWithItems(item, rm.abstractRoom.name, worldCoordinate);
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
                        JollyCustom.Log("Failed to move player " + ex, true);
                    }
                    if (item.abstractPhysicalObject.Room.name != rm.abstractRoom.name)
                        continue;
                    if (player is null && item.objectInStomach is AbstractPhysicalObject obj && (obj.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer || obj.type?.value == "OYOrbitalRobot"))
                    {
                        player = item;
                        //JollyCoop.JollyCustom.Log($"Found player with OYBot in stomach, focusing ... {item}");
                    }
                }
                if (player is not null)
                    self.player = player;
            }
            self.greenNeuron = null;
            cwbehav.OYBot = null;
            var objLists = rm.physicalObjects;
            for (i = 0; i < objLists.Length; i++)
            {
                var objs = objLists[i];
                for (j = 0; j < objs.Count; j++)
                {
                    if (objs[j] is NSHSwarmer sw)
                        self.greenNeuron = sw;
                    else if (objs[j] is PhysicalObject obj && obj.abstractPhysicalObject.type?.value == "OYOrbitalRobot" && obj.firstChunk.mass < 50f)
                        cwbehav.OYBot = obj;
                }
            }
            var run = true;
            OnSeePlayer?.Invoke(self, ref run);
            if (!run)
                return;
            var dataFlag = WorldSaveData.TryGetValue(rm.game.GetStorySession.saveState.miscWorldSaveData, out var data);
            var charac = rm.game.StoryCharacter;
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
            else if ((cwbehav.OYBot is not null || self.player.objectInStomach?.type?.value == "OYOrbitalRobot") && !cwbehav.HasSeenOYBot && dataFlag)
            {
                data.SeenOYBot = true;
                if (self.currSubBehavior is CWGeneralConversation cv)
                    cv.SeenPlayer = true;
                else if (self.currSubBehavior is CWNoSubBehavior ns)
                    ns.SeenPlayer = true;
                self.NewAction(ActionID.GetOYBot_Init);
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
        if (self is CWOracleBehavior)
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

    static void On_SSOracleBehavior_InitStoryPearlCollection(On.SSOracleBehavior.orig_InitStoryPearlCollection orig, SSOracleBehavior self)
    {
        if (self is CWOracleBehavior)
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
                var prl = prls[i];
                var pos = self.storedPearlOrbitLocation(num);
                if (prl.realizedObject is PhysicalObject obj)
                    obj.firstChunk.pos = pos;
                else
                    prl.pos.Tile = rm.GetTilePosition(pos);
                ++num;
            }
            self.inspectPearl = null;
        }
        else
            orig(self);
    }

    static string On_OracleBehavior_AlreadyDiscussedItemString(On.OracleBehavior.orig_AlreadyDiscussedItemString orig, OracleBehavior self, bool pearl)
    {
        if (self is CWOracleBehavior)
        {
            var charac = self.oracle.room.game.StoryCharacter?.value;
            if (pearl && CWConversation.CWLinesFromFile("AlreadyDiscussedPearl", charac) is string[] lns && lns.Length > 0)
                return lns[Random.Range(0, lns.Length)];
            else if (CWConversation.CWLinesFromFile("AlreadyDiscussedItem", charac) is string[] lns2 && lns2.Length > 0)
                return lns2[Random.Range(0, lns2.Length)];
            return string.Empty;
        }
        return orig(self, pearl);
    }

    static void On_Oracle_CreateMarble(On.Oracle.orig_CreateMarble orig, Oracle self, PhysicalObject orbitObj, Vector2 ps, int circle, float dist, int color)
    {
        if (self is CWOracle)
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
            x => x.MatchLdsfld<Oracle.OracleID>("SS"))
         && c.TryGotoNext(MoveType.After,
            x => x.MatchStfld<Oracle>("ID")))
        {
            c.Emit(OpCodes.Ldarg_0)
             .Emit(OpCodes.Ldarg_2)
             .EmitDelegate((Oracle self, Room room) =>
             {
                 if (self is CWOracle)
                     self.ID = NewOracleID.CW;
             });
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Oracle.ctor (part 1)!");
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<Oracle.OracleID>("SS"),
            x => x.MatchCall(out _)))
        {
            c.Emit(OpCodes.Ldarg_0)
             .EmitDelegate((bool flag, Oracle self) => flag || self is CWOracle);
        }
        else
            CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Oracle.ctor (part 2)!");
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
                 .EmitDelegate((bool flag, Oracle.OracleArm self) => flag || self is CWOracleArm);
            }
            else
                CWStuffPlugin.s_logger.LogError($"Couldn't ILHook Oracle.OracleArm.Update (part {i})!");
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
             .EmitDelegate((bool flag, Oracle.OracleArm.Joint self) => flag || self.arm is CWOracleArm);
        }
        else
            CWStuffPlugin.s_logger.LogError("Couldn't ILHook Oracle.OracleArm.Joint.Update!");
    }

    static void On_OracleChatLabel_DrawSprites(On.OracleChatLabel.orig_DrawSprites orig, OracleChatLabel self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (!self.slatedForDeletetion && self.room == rCam.room && self.visible && self.oracleBehav is CWOracleBehavior)
        {
            var sprs = sLeaser.sprites;
            for (var j = 0; j < sprs.Length; j++)
                sprs[j].color = self.color;
        }
    }

    static void On_OracleChatLabel_AddToContainer(On.OracleChatLabel.orig_AddToContainer orig, OracleChatLabel self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        if (self.oracleBehav is CWOracleBehavior)
            newContainer = rCam.ReturnFContainer("BackgroundShortcuts");
        orig(self, sLeaser, rCam, newContainer);
    }

    static Color On_Gown_Color(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
    {
        if (self.owner is CWOracleGraphics)
            return Custom.HSL2RGB(234f / 360f, Mathf.Lerp(.46f, .58f, f), Mathf.Lerp(.23f, .25f, f));
        return orig(self, f);
    }

    static void On_ArmJointGraphics_ctor(On.OracleGraphics.ArmJointGraphics.orig_ctor orig, OracleGraphics.ArmJointGraphics self, OracleGraphics owner, Oracle.OracleArm.Joint myJoint, int firstSprite)
    {
        orig(self, owner, myJoint, firstSprite);
        if (owner is CWOracleGraphics)
            self.armJointSound.soundID = SoundID.SS_AI_Arm_Joint_LOOP;
    }

    static void On_Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
    {
        orig(self);
        if (self.game?.session is StoryGameSession && string.Equals(self.abstractRoom.name, "CW_AI", StringComparison.OrdinalIgnoreCase))
        {
            self.AddObject(new CWOracle(new(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self));
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

    // for backwards compat
    public static bool IsCW(this Oracle self) => self is CWOracle;
}
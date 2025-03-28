using UnityEngine;
using RWCustom;
using MoreSlugcats;
using System;
using Random = UnityEngine.Random;

namespace CWStuff;

public class CWThrowOut(SSOracleBehavior owner) : SSOracleBehavior.ThrowOutBehavior(owner)
{
    public override bool Gravity => false;

    public override float LowGravity => .1f;

    public override Vector2? LookPoint => player?.firstChunk.pos;

    public override void Activate(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
    {
        NewAction(oldAction, newAction);
        owner.pearlPickupReaction = false;
    }

    public override void Deactivate() { }

    public override void Update()
    {
        if (player is not Player p)
            return;
        var mbc = p.mainBodyChunk;
        var orc = oracle;
        var oroom = orc.room;
        var crits = oroom.abstractRoom.creatures;
        owner.UnlockShortcuts();
        if (ModManager.MSC && action == MoreSlugcatsEnums.SSOracleBehaviorAction.ThrowOut_Singularity)
        {
            if (inActionCounter == 10)
            {
                if (owner.conversation is CWConversation conv)
                {
                    conv.Destroy();
                    owner.conversation = null;
                    if (oroom.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                        dialogBox.Interrupt(". . . !", 0);
                }
                else if (oroom.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                    dialogBox.NewMessage(". . . !", 0);
            }
            owner.getToWorking = 1f;
            var grs = p.grasps;
            if (p.room != oroom && !p.inShortcut)
            {
                for (var i = 0; i < grs.Length; i++)
                {
                    if (grs[i]?.grabbed is SingularityBomb bomb)
                    {
                        var ps = p.firstChunk.pos;
                        bomb.Thrown(p, ps, null, new(0, -1), 1f, true);
                        bomb.ignited = true;
                        bomb.activateSucktion = true;
                        bomb.counter = 50f;
                        bomb.firstChunk.pos = bomb.floatLocation = ps;
                    }

                }
                p.Stun(200);
                owner.NewAction(SSOracleBehavior.Action.General_Idle);
                return;
            }
            movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            var seer = oroom.game.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase);
            var tlPos = seer ? oroom.MiddleOfTile(7, 21) : oroom.MiddleOfTile(8, 17);
            mbc.vel += Custom.DirVec(mbc.pos, tlPos) * 1.3f;
            mbc.pos = Vector2.Lerp(mbc.pos, tlPos, .08f);
            if (!p.enteringShortCut.HasValue && mbc.pos.x < 560f && mbc.pos.y > 630f)
                mbc.pos.y = 630f;
            if (owner.dangerousSingularity is SingularityBomb b)
            {
                var fc = b.firstChunk;
                b.activateSucktion = false;
                fc.vel += Custom.DirVec(fc.pos, mbc.pos) * 1.3f;
                fc.pos = Vector2.Lerp(fc.pos, mbc.pos, .1f);
                if (Vector2.Distance(fc.pos, mbc.pos) < 10f)
                {
                    if (grs[0] is null)
                        p.SlugcatGrab(b, 0);
                    else if (grs[1] is null)
                        p.SlugcatGrab(b, 1);
                }
            }
            if (orc.room.GetTilePosition(mbc.pos) == (seer ? new IntVector2(7, 21) : new IntVector2(8, 17)) && !p.enteringShortCut.HasValue)
            {
                var flag = false;
                var danger = owner.dangerousSingularity;
                if (grs[0] is Creature.Grasp g0 && g0.grabbed == danger)
                    flag = true;
                else if (grs[1] is Creature.Grasp g1 && g1.grabbed == danger)
                    flag = true;
                if (flag)
                {
                    p.enteringShortCut = oroom.ShortcutLeadingToNode(0).StartTile;
                    return;
                }
                p.ReleaseGrasp(0);
                p.ReleaseGrasp(1);
            }
            return;
        }
        if (p.room == oroom || crits.Count > 0)
        {
            if (owner.greenNeuron is NSHSwarmer sw && sw.room is null)
                owner.greenNeuron = null;
        }
        if ((!p.dead || owner.killFac > .5f) && p.room == oroom)
        {
            movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            if (orc.graphicsModule is OracleGraphics gr)
                gr.eyesOpen = 1f;
            owner.killFac += .025f;
            if (owner.killFac >= 1f)
            {
                mbc.vel += Custom.RNV() * 12f;
                for (var k = 0; k < 20; k++)
                    oroom.AddObject(new Spark(mbc.pos, Custom.RNV() * Random.value * 40f, Color.white, null, 30, 120));
                p.Die();
                owner.killFac = 0f;
            }
        }
    }
}
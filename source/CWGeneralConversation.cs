global using static CWStuff.CWGeneralConversation;
using System;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Random = UnityEngine.Random;

namespace CWStuff;

public class CWNoSubBehavior(SSOracleBehavior owner) : SSOracleBehavior.NoSubBehavior(owner)
{
    public float PartialGravity;
    public bool SeenPlayer, LockPaths, GravOn;

    public override void Update()
    {
        if (LockPaths)
            owner.LockShortcuts();
        else
            owner.UnlockShortcuts();
    }

    public override float LowGravity => GravOn ? PartialGravity : -1f;

    public override bool Gravity => GravOn;
}

public class CWGeneralConversation(SSOracleBehavior owner, Conversation.ID convoID) : SSOracleBehavior.ConversationBehavior(owner, SubBehavID.MeetWhite, convoID)
{
    [Flags]
    public enum GiftStates
    {
        None = 0b_0000_0000,
        Mark = 0b_0000_0001,
        FoodMax = 0b_0000_0010,
        Karma10 = 0b_0000_0100,
        Cure = 0b_0000_1000,
        ScavImmu = 0b_0001_0000
    }

    public float PartialGravity;
    public Vector2? CurrentLookPoint;
    public GiftStates Gifts;
    public bool GravOn, LockPaths, SeenPlayer, ChangePassiveBehavior, ActiveNeuronMovement;
    public ProjectedImage? ShowImage;
    public Vector2 IdealShowMediaPos, ShowMediaPos;
    public int ConsistentShowMediaPosCounter, ImageCounter, ChangeCouter;

    public virtual Vector2 GrabPos
    {
        get
        {
            var orc = oracle;
            if (orc.graphicsModule is not OracleGraphics gr)
                return orc.firstChunk.pos;
            return gr.hands[1].pos;
        }
    }

    public override float LowGravity => GravOn ? PartialGravity : -1f;

    public override bool Gravity => GravOn;

    public virtual bool SpearImage => ModManager.MSC && oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear;

    public virtual bool SeerImage => oracle.room.game.StoryCharacter?.value is string s && string.Equals(s, "seer", StringComparison.OrdinalIgnoreCase);

    public override Vector2? LookPoint => CurrentLookPoint;

    public override void Update()
    {
        var ownr = owner;
        if (LockPaths)
        {
            var flag = ownr.pearlConversation is null;
            if (flag)
                CurrentLookPoint = ownr.player?.firstChunk.pos;
            ownr.LockShortcuts();
            if (!flag)
                return;
            if (ownr.action == SSOracleBehavior.Action.MeetWhite_Images)
            {
                ownr.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                var img = ShowImage ??= ownr.oracle.myScreen.AddImage(["CW_AIimg1", "CW_AIimg1"], 15);
                var imgs = img.imageNames;
                if (ImageCounter >= 700)
                {
                    img.setAlpha = 1f;
                    imgs[0] = "CW_AIimg3a";
                    imgs[1] = SpearImage ? "CW_AIimg3b_Spear" : "CW_AIimg3b";
                }
                else if (ImageCounter >= 650)
                    img.setAlpha = 0f;
                else if (ImageCounter >= 350)
                {
                    img.setAlpha = 1f;
                    imgs[1] = imgs[0] = SeerImage ? "CW_AIimg2_Seer" : "CW_AIimg2";
                }
                else if (ImageCounter >= 300)
                    img.setAlpha = 0f;
                else
                {
                    img.setAlpha = 1f;
                    imgs[1] = imgs[0] = "CW_AIimg1";
                }
                img.pos = ShowMediaPos;
                if (ImageCounter < 1000)
                    ++ImageCounter;
                else
                {
                    if (ownr.conversation is CWConversation conv)
                        conv.paused = false;
                    ImageCounter = 0;
                    ownr.NewAction(SSOracleBehavior.Action.MeetWhite_Curious);
                }
            }
            else
            {
                ownr.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                if (ShowImage is not null)
                {
                    ShowImage.Destroy();
                    ShowImage = null;
                }
            }
            if (ActiveNeuronMovement && ownr.greenNeuron is NSHSwarmer sw1 && !sw1.slatedForDeletetion && sw1.room == ownr.oracle.room)
            {
                if (Custom.DistLess(GrabPos, sw1.firstChunk.pos, 20f))
                {
                    sw1.storyFly = false;
                    ActiveNeuronMovement = false;
                    ownr.action = SSOracleBehavior.Action.GetNeuron_InspectNeuron;
                }
            }
            if (ownr.action == SSOracleBehavior.Action.GetNeuron_InspectNeuron)
            {
                if (ownr.greenNeuron is NSHSwarmer sw)
                {
                    sw.firstChunk.pos = GrabPos;
                    CurrentLookPoint = sw.firstChunk.pos;
                }
                else
                    CurrentLookPoint = null;
                ownr.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            }
        }
        else
        {
            CurrentLookPoint = null;
            ownr.UnlockShortcuts();
            if (ChangeCouter > 0)
                --ChangeCouter;
            else
                ChangePassiveBehavior = Random.value < .001f;
            if (ownr.movementBehavior == SSOracleBehavior.MovementBehavior.Talk)
            {
                ChangeCouter = 1500;
                ownr.NewAction(SSOracleBehavior.Action.General_Idle);
                ownr.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
            }
            if (ChangePassiveBehavior)
            {
                if (ownr.movementBehavior != SSOracleBehavior.MovementBehavior.Meditate)
                    ownr.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
                else
                    ownr.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                ChangePassiveBehavior = false;
                ChangeCouter = 1500;
            }
        }
    }
    public virtual void ShowMediaMovementBehavior()
    {
        var ownr = owner;
        var rm = ownr.oracle.room;
        var vector = new Vector2(Random.value * rm.PixelWidth, Random.value * rm.PixelHeight);
        if (ownr.CommunicatePosScore(vector) + 40f < ownr.CommunicatePosScore(ownr.nextPos) && !Custom.DistLess(vector, ownr.nextPos, 30f))
            ownr.SetNewDestination(vector);
        ConsistentShowMediaPosCounter += (int)Custom.LerpMap(Vector2.Distance(ShowMediaPos, IdealShowMediaPos), 0f, 200f, 1f, 10f);
        vector = new(Random.value * rm.PixelWidth, Random.value * rm.PixelHeight);
        if (ShowMediaScore(vector) + 40f < ShowMediaScore(IdealShowMediaPos))
        {
            IdealShowMediaPos = vector;
            ConsistentShowMediaPosCounter = 0;
        }
        vector = IdealShowMediaPos + Custom.RNV() * Random.value * 40f;
        if (ShowMediaScore(vector) + 20f < ShowMediaScore(IdealShowMediaPos))
        {
            IdealShowMediaPos = vector;
            ConsistentShowMediaPosCounter = 0;
        }
        if (ConsistentShowMediaPosCounter > 300)
        {
            ShowMediaPos = Vector2.Lerp(ShowMediaPos, IdealShowMediaPos, .1f);
            ShowMediaPos = Custom.MoveTowards(ShowMediaPos, IdealShowMediaPos, 10f);
        }
    }

    public override void Deactivate()
    {
        if (ShowImage is not null)
        {
            ShowImage.Destroy();
            ShowImage = null;
        }
        base.Deactivate();
    }

    public virtual float ShowMediaScore(Vector2 tryPos)
    {
        var orc = oracle;
        if (orc.room.GetTile(tryPos).Solid || player is not Player p)
            return float.MaxValue;
        var num = Mathf.Abs(Vector2.Distance(tryPos, p.DangerPos) - 250f);
        num -= Math.Min(orc.room.aimap.getTerrainProximity(tryPos), 9f) * 30f;
        num -= Vector2.Distance(tryPos, owner.nextPos) * .5f;
        var joints = orc.arm.joints;
        for (var i = 0; i < joints.Length; i++)
            num -= Mathf.Min(Vector2.Distance(tryPos, joints[i].pos), 100f) * 10f;
        if (orc.graphicsModule is OracleGraphics gr)
        {
            var umbCoord = gr.umbCord.coord;
            var lgt = umbCoord.GetLength(0);
            for (var j = 0; j < lgt; j += 3)
                num -= Mathf.Min(Vector2.Distance(tryPos, umbCoord[j, 0]), 100f);
        }
        return num;
    }
}
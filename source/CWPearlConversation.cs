using UnityEngine;

namespace CWStuff;

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
        var sv = myBehavior.oracle.room.game.GetStorySession.saveState;
        if (CWOracleHooks.WorldSaveData.TryGetValue(sv.miscWorldSaveData, out var data))
            ++data.NumberOfConversations;
        if (locID == ID.Moon_Pearl_Misc)
        {
            CWConversation.CWEventsFromFile(this, locID.value, myBehavior is CWOracleHooks.CWOracleBehavior { inspectPearl: not null }, sv.saveStateNumber?.value, true, Random.Range(0, 10000));
            return;
        }
        if (CWConversation.CWEventsFromFile(this, locID.value, myBehavior is CWOracleHooks.CWOracleBehavior { inspectPearl: not null }, sv.saveStateNumber?.value))
            return;
        base.AddEvents();
        IntroSaid = false;
    }
}
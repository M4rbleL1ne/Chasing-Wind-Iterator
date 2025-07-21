using Menu;

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

public static class SubBehavID
{
    public static SSOracleBehavior.SubBehavior.SubBehavID GetOYBot = new(nameof(GetOYBot), true);

    internal static void UnregisterValues()
    {
        if (GetOYBot is not null)
        {
            GetOYBot.Unregister();
            GetOYBot = null!;
        }
    }
}

public static class ActionID
{
    public static SSOracleBehavior.Action GetOYBot_Init = new(nameof(GetOYBot_Init), true),
        GetOYBot_Inspect = new(nameof(GetOYBot_Inspect), true);

    internal static void UnregisterValues()
    {
        if (GetOYBot_Init is not null)
        {
            GetOYBot_Init.Unregister();
            GetOYBot_Init = null!;
        }
        if (GetOYBot_Inspect is not null)
        {
            GetOYBot_Inspect.Unregister();
            GetOYBot_Inspect = null!;
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
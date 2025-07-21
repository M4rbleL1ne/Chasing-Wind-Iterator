namespace CWStuff;

public class CWNoSubBehavior(SSOracleBehavior ow) : SSOracleBehavior.NoSubBehavior(ow)
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
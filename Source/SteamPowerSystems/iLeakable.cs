namespace ArkhamEstate
{
    public interface ILeakable
    {
        float CurLeakRate();
        void AdjustLeakRate(float amt);
    }
}

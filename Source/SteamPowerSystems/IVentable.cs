namespace ArkhamEstate
{
    public interface IVentable
    {
        void Vent();
        bool ShouldVentNow { get; set; }
    }
}

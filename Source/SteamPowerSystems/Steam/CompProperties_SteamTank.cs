using RimWorld;

namespace ArkhamEstate
{
    public class CompProperties_SteamTank : CompProperties_Steam
    {
        public CompProperties_SteamTank()
        {
            this.compClass = typeof(CompSteamTank);
        }

        public float storedSteamMax = 200f; //Using pressure instead of litres now

        public float efficiency = 0.5f;
    }
}
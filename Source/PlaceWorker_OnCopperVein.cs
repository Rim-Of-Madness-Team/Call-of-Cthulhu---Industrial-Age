using RimWorld;
using Verse;

namespace ArkhamEstate
{
    public class PlaceWorker_OnCopperVein : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            Thing thing = map.thingGrid.ThingAt(loc, ThingDef.Named("Estate_VeinCopper"));
            if (thing == null || thing.Position != loc)
            {
                return "Estate_MustPlaceOnCopperVein".Translate();
            }
            return true;
        }

        public override bool ForceAllowPlaceOver(BuildableDef otherDef)
        {
            return otherDef == ThingDef.Named("Estate_VeinCopper");
        }
    }
}

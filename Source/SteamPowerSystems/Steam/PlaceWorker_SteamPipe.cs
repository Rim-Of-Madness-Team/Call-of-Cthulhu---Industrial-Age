using System.Collections.Generic;
using Verse;

namespace ArkhamEstate
{
    public class PlaceWorker_SteamPipe : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            List<Thing> thingList = loc.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].def.EverTransmitsSteam())
                {
                    return false;
                }
                if (thingList[i].def.entityDefToBuild != null)
                {
                    ThingDef thingDef = thingList[i].def.entityDefToBuild as ThingDef;
                    if (thingDef != null && thingDef.EverTransmitsSteam())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}

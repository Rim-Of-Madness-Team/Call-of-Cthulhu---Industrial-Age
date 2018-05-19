using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArkhamEstate
{
    public static class WaterNetMaker
    {
        private static IEnumerable<CompWater> ContiguousWaterBuildings(Building root)
        {
            WaterNetMaker.closedSet.Clear();
            WaterNetMaker.currentSet.Clear();
            WaterNetMaker.openSet.Add(root);
            do
            {
                foreach (Building item in WaterNetMaker.openSet)
                {
                    WaterNetMaker.closedSet.Add(item);
                }
                HashSet<Building> hashSet = WaterNetMaker.currentSet;
                WaterNetMaker.currentSet = WaterNetMaker.openSet;
                WaterNetMaker.openSet = hashSet;
                WaterNetMaker.openSet.Clear();
                foreach (Building building in WaterNetMaker.currentSet)
                {
                    foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal(building))
                    {
                        if (c.InBounds(building.Map))
                        {
                            List<Thing> thingList = c.GetThingList(building.Map);
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                Building building2 = thingList[i] as Building;
                                if (building2 != null)
                                {
                                    if (building2.TransmitsWaterNow())
                                    {
                                        if (!WaterNetMaker.openSet.Contains(building2) && !WaterNetMaker.currentSet.Contains(building2) && !WaterNetMaker.closedSet.Contains(building2))
                                        {
                                            WaterNetMaker.openSet.Add(building2);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (WaterNetMaker.openSet.Count > 0);
            return from b in WaterNetMaker.closedSet
                select b.GetComp<CompWater>();
        }

        public static WaterNet NewWaterNetStartingFrom(Building root)
        {
            return new WaterNet(WaterNetMaker.ContiguousWaterBuildings(root));
        }

        public static void UpdateVisualLinkagesFor(WaterNet net)
        {
        }

        private static HashSet<Building> closedSet = new HashSet<Building>();

        private static HashSet<Building> openSet = new HashSet<Building>();

        private static HashSet<Building> currentSet = new HashSet<Building>();
    }
}

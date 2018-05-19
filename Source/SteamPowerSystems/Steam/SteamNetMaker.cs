using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArkhamEstate
{
    public static class SteamNetMaker
    {
        private static IEnumerable<CompSteam> ContiguousSteamBuildings(Building root)
        {
            SteamNetMaker.closedSet.Clear();
            SteamNetMaker.currentSet.Clear();
            SteamNetMaker.openSet.Add(root);
            do
            {
                foreach (Building item in SteamNetMaker.openSet)
                {
                    SteamNetMaker.closedSet.Add(item);
                }
                HashSet<Building> hashSet = SteamNetMaker.currentSet;
                SteamNetMaker.currentSet = SteamNetMaker.openSet;
                SteamNetMaker.openSet = hashSet;
                SteamNetMaker.openSet.Clear();
                foreach (Building building in SteamNetMaker.currentSet)
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
                                    if (building2.TransmitsSteamNow())
                                    {
                                        if (!SteamNetMaker.openSet.Contains(building2) && !SteamNetMaker.currentSet.Contains(building2) && !SteamNetMaker.closedSet.Contains(building2))
                                        {
                                            SteamNetMaker.openSet.Add(building2);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (SteamNetMaker.openSet.Count > 0);
            return from b in SteamNetMaker.closedSet
                select b.GetComp<CompSteam>();
        }

        public static SteamNet NewSteamNetStartingFrom(Building root)
        {
            return new SteamNet(SteamNetMaker.ContiguousSteamBuildings(root));
        }

        public static void UpdateVisualLinkagesFor(SteamNet net)
        {
        }

        private static HashSet<Building> closedSet = new HashSet<Building>();

        private static HashSet<Building> openSet = new HashSet<Building>();

        private static HashSet<Building> currentSet = new HashSet<Building>();
    }
}

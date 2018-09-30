using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace IndustrialAge.Objects
{
    public class ListenBuildingUtility
    {

        public static bool TryFindBestListenCell(Thing toListen, Pawn pawn, bool desireSit, out IntVec3 result, out Building chair)
        {
            IntVec3 intVec = IntVec3.Invalid;
            Building_Gramophone musicBuilding = toListen as Building_Gramophone;
            IEnumerable<IntVec3> cells = musicBuilding.ListenableCells;
            var random = new Random();
            IEnumerable<IntVec3> cellsRandom = cells.OrderBy(order => random.Next()).ToList();

            foreach (IntVec3 current in cellsRandom)
            {
                bool flag = false;
                Building building = null;
                if (desireSit)
                {
                    building = current.GetEdifice(pawn.Map);
                    if (building != null && building.def.building.isSittable && pawn.CanReserve(building, 1))
                    {
                        flag = true;
                    }
                }
                else if (!current.IsForbidden(pawn) && pawn.CanReserve(current, 1))
                {
                    flag = true;
                }
                if (flag)
                {
                    result = current;
                    chair = building;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            chair = null;
            return false;
        }

        // RimWorld.WatchBuildingUtility
        public static bool CanListenFromBed(Pawn pawn, Building_Bed bed, Thing toListen)
        {
            if (!pawn.Position.Standable(pawn.Map) || (pawn.Position.GetEdifice(pawn.Map) is Building_Bed))
            {
                return false;
            }
            Building_Gramophone musicBuilding = toListen as Building_Gramophone;
            IEnumerable<IntVec3> cells = musicBuilding.ListenableCells;
            foreach (IntVec3 current in cells)
            {
                if (current == pawn.Position)
                {
                    return true;
                }
            }
            return false;
        }
    }

}

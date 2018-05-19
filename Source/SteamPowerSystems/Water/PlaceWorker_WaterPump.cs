using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    public class PlaceWorker_WaterPump : PlaceWorker
    {

        public IEnumerable<IntVec3> CellsForWater(ThingDef def, IntVec3 center, Rot4 rot)
        {
            var rightRot = rot;
            rightRot.Rotate(RotationDirection.Clockwise);

            var elOne = 1;
            var elTwo = 3;
            if (rot == Rot4.West)
            {
                elOne = 3;
                elTwo = 2;
            }
            else if (rot == Rot4.South)
            {
                elOne = 0;
                elTwo = 2;
            }
            else if (rot == Rot4.East)
            {
                elOne = 0;
                elTwo = 1;
            }
            var cellAbove = GenAdj.OccupiedRect(center, rot, def.size).Corners.ElementAt(elOne) +  rightRot.FacingCell;
            var cellAdj = GenAdj.OccupiedRect(center, rot, def.size).Corners.ElementAt(elTwo) + rightRot.FacingCell;
            return new List<IntVec3>{cellAbove, cellAdj};
        }
        
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            Map visibleMap = Find.VisibleMap;
            HashSet<IntVec3> cells = new HashSet<IntVec3>(GenRadial.RadialCellsAround(center, 9.6f, true));
            foreach (var cell in CellsForWater(def, center, rot))
            {
                cells.Remove(cell);
            }
            GenDraw.DrawFieldEdges(cells.ToList(), Color.white);//GenTemperature.ColorRoomHot);
            GenDraw.DrawFieldEdges(new List<IntVec3>(CellsForWater(def, center, rot)), Color.blue);
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            //Next to water.
            var checkCells = new HashSet<IntVec3>(CellsForWater(checkingDef as ThingDef, loc, rot));
            foreach (var checkCell in checkCells)
            {
                if (!checkCell.InBounds(map))
                {
                    return false;
                }
                TerrainDef terrainDef = checkCell.GetTerrain(map);
                if (terrainDef == null || terrainDef?.IsWater() == false)
                {
                    return "Estate_WaterPlaceWorker_NeedSource".Translate();
                }
            }
            HashSet<IntVec3> cells = new HashSet<IntVec3>(GenRadial.RadialCellsAround(loc, 9.6f, true));
            if (cells.Any(x => x.GetFirstThing(map, ThingDef.Named(checkingDef.defName)) != null))
            {
                return "Estate_WaterPlaceWorker_TwoPumps".Translate();
            }
            return true;
        }
    }
}

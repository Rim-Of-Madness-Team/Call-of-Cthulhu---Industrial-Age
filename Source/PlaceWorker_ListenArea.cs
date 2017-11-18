using System;
using System.Linq;
using Verse;

namespace ArkhamEstate
{
    public class PlaceWorker_ListenArea : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            Map visibleMap = Find.VisibleMap;
            GenDraw.DrawFieldEdges(Building_Gramophone.ListenableCellsAround(center, visibleMap));
        }
    }
}

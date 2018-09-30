using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace IndustrialAge.Objects
{
    public class JoyGiver_ListenToBuilding : JoyGiver_InteractBuilding
    {
        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
        {
            if (!base.CanInteractWith(pawn, t, inBed))
            {
                return false;
            }
            if (inBed)
            {
                Building_Bed layingDownBed = pawn.CurrentBed();

                return ListenBuildingUtility.CanListenFromBed(pawn, layingDownBed, t);
            }
            return true;
        }

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            IntVec3 vec;
            Building t2;
            if (!ListenBuildingUtility.TryFindBestListenCell(t, pawn, this.def.desireSit, out vec, out t2))
            {
                if (!ListenBuildingUtility.TryFindBestListenCell(t, pawn, false, out vec, out t2))
                {
                    return null;
                }
            }
            if (t2 != null)
            {
                if (vec == t2.Position)
                {
                    if (!pawn.Map.reservationManager.CanReserve(pawn, t2))
                        return null;
                }
            }

            return new Job(this.def.jobDef, t, vec, t2);
        }
    }
}

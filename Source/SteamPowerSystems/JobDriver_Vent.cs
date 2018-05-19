using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArkhamEstate
{
    public class JobDriver_Vent : JobDriver
    {

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil repair = new Toil();
            repair.initAction = delegate
            {
                var ventable = (IVentable)((ThingWithComps) TargetThingA).AllComps.FirstOrDefault(x => x is IVentable y && y.ShouldVentNow);
                if (ventable != null)
                {
                    ventable.ShouldVentNow = false;
                    ventable.Vent();
                }
                else
                {
                    Log.Error("Vent failed. Null Iventable.");
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return repair;
            yield break;
        }
    }
}

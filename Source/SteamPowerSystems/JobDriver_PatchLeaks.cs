using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArkhamEstate
{
    public class JobDriver_PatchLeaks : JobDriver
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
                 ticksToNextRepair = 250f;
            };
            repair.tickAction = delegate
            {
               Pawn actor = repair.actor;
                actor.skills.Learn(SkillDefOf.Construction, 0.275f, false);
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                 ticksToNextRepair -= statValue;
                if ( ticksToNextRepair <= 0f)
                {
                     ticksToNextRepair += 250f;
                    
                    var leakable = (ILeakable)((ThingWithComps) TargetThingA).AllComps.FirstOrDefault(x => x is ILeakable y);
                    if (leakable != null)
                    {
                        leakable?.AdjustLeakRate(-1);
                        //Map.listerBuildingsRepairable.Notify_BuildingRepaired((Building) TargetThingA);
                    }
                    if (leakable == null || leakable.CurLeakRate() < 1f)
                    {
                        actor.records.Increment(RecordDefOf.ThingsRepaired);
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            yield return repair;
            yield break;
        }

        protected float ticksToNextRepair;

        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;
    }
}

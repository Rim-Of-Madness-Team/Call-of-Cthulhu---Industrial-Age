using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace IndustrialAge.Objects
{
    public class JobDriver_ListenToGramophone : JobDriver
    {
        public override bool TryMakePreToilReservations(bool debug)
        {
            return true;
        }
        private string report = "";
        public override string GetReport()
        {
            if (report != "")
            {
                return base.ReportStringProcessed(report);
            }
            return base.GetReport();
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail Checks
            
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);   //If we don't exist, exit.

            if (this.job.targetA.Thing is Building_Radio) report = "Listening to the radio.";


            //yield return Toils_Reserve.Reserve(TargetIndex.A, base.CurJob.def.joyMaxParticipants); //Can we reserve?

            //yield return Toils_Reserve.Reserve(TargetIndex.B, 1);   //Reserve

            bool flag = base.TargetC.HasThing && base.TargetC.Thing is Building_Bed;   
            Toil toil;
            if (flag)   //If we have a bed, do something else.
            {
                this.KeepLyingDown(TargetIndex.C);
                yield return Toils_Reserve.Reserve(TargetIndex.C, ((Building_Bed)base.TargetC.Thing).SleepingSlotsCount);
                yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.C, TargetIndex.None);
                yield return Toils_Bed.GotoBed(TargetIndex.C);
                toil = Toils_LayDown.LayDown(TargetIndex.C, true, false, true, true);
                toil.AddFailCondition(() => !this.pawn.Awake());
                
            }
            else
            {
                if (base.TargetC.HasThing)
                {
                    yield return Toils_Reserve.Reserve(TargetIndex.C, 1);
                }
                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
                toil = new Toil();
                
            }
            toil.AddPreTickAction(delegate
            {
                if (this.job.targetA.Thing is Building_Radio) report = "Listening to the radio.";
                this.ListenTickAction();
            });
            toil.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = base.job.def.joyDuration * 2;
            yield return toil;
            yield break;
        }

        protected virtual void ListenTickAction()
        {
            Building_Gramophone gramo = base.TargetA.Thing as Building_Gramophone;
            if (!gramo.IsOn())
            {
                base.EndJobWith(JobCondition.Incompletable);
                return;
            }
            this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
            this.pawn.GainComfortFromCellIfPossible();
            float statValue = base.TargetThingA.GetStatValue(StatDefOf.JoyGainFactor, true);
            float extraJoyGainFactor = statValue;
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
        }

    }
}

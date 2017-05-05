using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace ArkhamEstate
{

    public class JobDriver_AutoPlayListenGramophone : JobDriver
    {

        public Building_Gramophone Gramophone
        {
            get
            {
                Building_Gramophone result = this.pawn.jobs.curJob.GetTarget(TargetIndex.A).Thing as Building_Gramophone;
                if (result == null)
                {
                    throw new InvalidOperationException("Gramophone is missing.");
                }
                return result;
            }
        }

        public TargetIndex GramophoneIndex = TargetIndex.A;
        public TargetIndex ChairIndex = TargetIndex.B;
        public TargetIndex BedIndex = TargetIndex.C;

        //How long will it take to wind up the gramophone?
        private int duration = 400;
        protected int Duration
        {
            get
            {
                return duration;
            }
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

        //What should we do?
        protected override IEnumerable<Toil> MakeNewToils()
        {

            //Check it out. Can we go there?
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            //Wait a minute, is this thing already playing?
            if (!Gramophone.IsOn())
            {
                if (this.CurJob.targetA.Thing is Building_Radio) report = "playing the radio.";

                // Toil 1:
                // Reserve Target (TargetPack A is selected (It has the info where the target cell is))
                yield return Toils_Reserve.Reserve(TargetIndex.A, 1);

                // Toil 2:
                // Go to the thing.
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

                // Toil 3:
                // Wind up the gramophone
                Toil wind = new Toil();
                wind.defaultCompleteMode = ToilCompleteMode.Delay;
                wind.defaultDuration = this.Duration;
                wind.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
                if (this.CurJob.targetA.Thing is Building_Radio)
                {
                    wind.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_RadioSeeking"));
                }
                else
                {
                    wind.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_GramophoneWindup"));
                }
                wind.initAction = delegate
                {
                    Gramophone.StopMusic();
                };
                yield return wind;

                // Toil 4:
                // Play music.

                Toil toilPlayMusic = new Toil();
                toilPlayMusic.defaultCompleteMode = ToilCompleteMode.Instant;
                toilPlayMusic.initAction = delegate
                {
                    Gramophone.PlayMusic(this.pawn);
                };
                yield return toilPlayMusic;

            }

            Toil toil;
            if (base.TargetC.HasThing && base.TargetC.Thing is Building_Bed)   //If we have a bed, lie in bed to listen.
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
                this.ListenTickAction();
                if (this.CurJob.targetA.Thing is Building_Radio) report = "Listening to the radio.";
            });
            toil.AddFinishAction(delegate
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = base.CurJob.def.joyDuration;
            yield return toil;
            yield break;
        }

        protected virtual void ListenTickAction()
        {
            if (!Gramophone.IsOn())
            {
                base.EndJobWith(JobCondition.Incompletable);
                return;
            }
            this.pawn.Drawer.rotator.FaceCell(base.TargetA.Cell);
            this.pawn.GainComfortFromCellIfPossible();
            float statValue = base.TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor, true);
            float extraJoyGainFactor = statValue;
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
        }
    }
}




/*

This is the needed XML file to make a real Job from the JobDriver
     
<?xml version="1.0" encoding="utf-8" ?>
<JobDefs>
<!--========= Job ============-->
	<JobDef>
	<defName>PlayGramophone</defName>
	<driverClass>ArkhamEstate.JobDriver_PlayGramophone</driverClass>
	<reportString>Winding up gramophone.</reportString>
	</JobDef>
</JobDefs>
     
*/

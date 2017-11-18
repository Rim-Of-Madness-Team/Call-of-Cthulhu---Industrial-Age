// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation

namespace RimWorld
{
    public class PlaceWorker_IsUnderRoof : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            if (map.roofGrid.Roofed(loc))
            {
                return true;
            }
            return new AcceptanceReport("Must Place Under Roof");
        }
    }

    #region FireOverlays

    /////////////////////////// Override Classes


    /// <summary>
    /// A nested override.
    /// </summary>
    public class ThingCompCandelabra : ThingComp
    {
        private bool fireOnInt;

        public bool ShouldBeFireNow
        {
            get
            {
                if (!this.parent.Spawned)
                {
                    return false;
                }
                CompRefuelable compRefuelable = this.parent.TryGetComp<CompRefuelable>();
                if (compRefuelable != null && !compRefuelable.HasFuel)
                {
                    return false;
                }
                CompFlickable compFlickable = this.parent.TryGetComp<CompFlickable>();
                return compFlickable == null || compFlickable.SwitchIsOn;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.fireOnInt, "fireOn", false, false);
        }

    }

    /// /////////////Fire Overlay 2

    public class CompProperties_Fire2Overlay : CompProperties
    {
        public float flameSize = 1f;

        public Vector3 offset;

        public CompProperties_Fire2Overlay()
        {
            this.compClass = typeof(Comp2FireOverlay);
        }
    }

    [StaticConstructorOnStartup]
    public class Comp2FireOverlay : ThingCompCandelabra
    {
        private Graphic FireGraphic2;


        public Comp2FireOverlay fireOverlay;
        public float fireSize_fromXML;


        public CompProperties_Fire2Overlay Props
        {
            get
            {
                return (CompProperties_Fire2Overlay)this.props;
            }
        }

        public override void PostSpawnSetup(bool bla)
        {

            this.fireOverlay = ThingCompUtility.TryGetComp<Comp2FireOverlay>(this.parent);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Vector3 drawPos = this.parent.DrawPos;
            Vector2 drawSize = new Vector2(this.fireOverlay.Props.flameSize, this.fireOverlay.Props.flameSize);

            drawPos += this.fireOverlay.Props.offset;

            if (this.ShouldBeFireNow)
            {
                this.FireGraphic2 = GraphicDatabase.Get<Graphic_Flicker>("Things/Special/Candle", ShaderDatabase.TransparentPostLight, drawSize, Color.white);
                this.FireGraphic2.Draw(Vector3Utility.RotatedBy(drawPos, this.parent.Rotation.AsAngle), Rot4.North, this.parent);
            }
        }
    }


    /// /////////////Fire Overlay 3


    public class CompProperties_Fire3Overlay : CompProperties
    {
        public float flameSize = 1f;

        public Vector3 offset;

        public CompProperties_Fire3Overlay()
        {
            this.compClass = typeof(Comp3FireOverlay);
        }
    }

    [StaticConstructorOnStartup]
    public class Comp3FireOverlay : ThingCompCandelabra
    {
        private Graphic FireGraphic3;


        public Comp3FireOverlay fireOverlay;
        public float fireSize_fromXML;

        public CompProperties_Fire3Overlay Props
        {
            get
            {
                return (CompProperties_Fire3Overlay)this.props;
            }
        }


        public override void PostSpawnSetup(bool bla)
        {

            this.fireOverlay = ThingCompUtility.TryGetComp<Comp3FireOverlay>(this.parent);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Vector3 drawPos = this.parent.DrawPos;
            Vector2 drawSize = new Vector2(this.fireOverlay.Props.flameSize, this.fireOverlay.Props.flameSize);

            drawPos += this.fireOverlay.Props.offset;

            if (this.ShouldBeFireNow)
            {
                this.FireGraphic3 = GraphicDatabase.Get<Graphic_Flicker>("Things/Special/Candle", ShaderDatabase.TransparentPostLight, drawSize, Color.white);
                this.FireGraphic3.Draw(Vector3Utility.RotatedBy(drawPos, this.parent.Rotation.AsAngle), Rot4.North, this.parent);
            }
        }
    }


    /// /////////////Fire Overlay 4


    public class CompProperties_Fire4Overlay : CompProperties
    {
        public float flameSize = 1f;

        public Vector3 offset;

        public CompProperties_Fire4Overlay()
        {
            this.compClass = typeof(Comp4FireOverlay);
        }
    }

    [StaticConstructorOnStartup]
    public class Comp4FireOverlay : ThingCompCandelabra
    {
        private Graphic FireGraphic4;


        public Comp4FireOverlay fireOverlay;
        public float fireSize_fromXML;


        public CompProperties_Fire4Overlay Props
        {
            get
            {
                return (CompProperties_Fire4Overlay)this.props;
            }
        }


        public override void PostSpawnSetup(bool bla)
        {

            this.fireOverlay = ThingCompUtility.TryGetComp<Comp4FireOverlay>(this.parent);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Vector3 drawPos = this.parent.DrawPos;
            Vector2 drawSize = new Vector2(this.fireOverlay.Props.flameSize, this.fireOverlay.Props.flameSize);

            drawPos += this.fireOverlay.Props.offset;

            if (this.ShouldBeFireNow)
            {
                this.FireGraphic4 = GraphicDatabase.Get<Graphic_Flicker>("Things/Special/Candle", ShaderDatabase.TransparentPostLight, drawSize, Color.white);
                this.FireGraphic4.Draw(Vector3Utility.RotatedBy(drawPos, this.parent.Rotation.AsAngle), Rot4.North, this.parent);
            }
        }
    }

    #endregion FireOverlay

}
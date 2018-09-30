using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace IndustrialAge.Objects
{
    class WorldComponent_Tunes : WorldComponent
    {
        private bool AreTunesReady = false;
        public List<TuneDef> TuneDefCache = new List<TuneDef>();

        public WorldComponent_Tunes(World world) : base(world)
        {
        }

        public TuneDef GetCache(TuneDef tune)
        {
            TuneDef result;
            if (TuneDefCache == null)
                TuneDefCache = new List<TuneDef>();

            foreach (TuneDef current in TuneDefCache)
                if (current == tune)
                {
                    result = current;
                    return result;
                }

            TuneDef tuneDef = tune;
            TuneDefCache.Add(tune);
            result = tune;
            return result;
        }
        


        public void GenerateTunesList()
        {
            if (!AreTunesReady)
            {
                foreach (TuneDef current in DefDatabase<TuneDef>.AllDefs)
                {
                    GetCache(current);
                }
                AreTunesReady = true;
            }
            return;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            GenerateTunesList();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look<TuneDef>(ref this.TuneDefCache, "TuneDefCache", LookMode.Def, new object[0]);
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GenerateTunesList();
            }
        }

    }
}

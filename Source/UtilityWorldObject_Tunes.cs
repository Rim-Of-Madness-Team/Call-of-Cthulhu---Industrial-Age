using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
    class UtilityWorldObject_Tunes : Cthulhu.UtilityWorldObject
    {
        private bool AreTunesReady = false;
        public List<TuneDef> TuneDefCache = new List<TuneDef>();

        public TuneDef GetCache(TuneDef tune)
        {
            TuneDef result;
            bool flag1 = TuneDefCache == null;
            if (flag1)
            {
                TuneDefCache = new List<TuneDef>();
            }

            foreach (TuneDef current in TuneDefCache)
            {
                if (current == tune)
                {
                    result = current;
                    return result;
                }
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
        
        public override void Tick()
        {
            base.Tick();
            GenerateTunesList();
            //Log.Message("UtilityWorldObject Tunes Started");
            
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookList<TuneDef>(ref this.TuneDefCache, "TuneDefCache", LookMode.Def, new object[0]);
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GenerateTunesList();
            }
        }

    }
}

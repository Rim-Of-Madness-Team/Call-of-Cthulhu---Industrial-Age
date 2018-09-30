using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace IndustrialAge.Objects
{
    public class TuneDef : SoundDef
    {
        private string version = "0";
        public string artist;
        public float durationTime;
        public bool instrumentOnly;
        public List<ThingDef> instrumentDefs = new List<ThingDef>();

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.LabelCap + " - " + this.artist);
            return s.ToString();
        }

        public int Version
        {
            get
            {
                int x = 0;
                if (Int32.TryParse(version, out x))
                {
                    return x;
                }
                return 0;
            }
        }
    }
}

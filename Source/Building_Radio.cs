using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace IndustrialAge.Objects
{
    public class Building_Radio : Building_Gramophone
    {
        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            isRadio = true;
        }
    }
}

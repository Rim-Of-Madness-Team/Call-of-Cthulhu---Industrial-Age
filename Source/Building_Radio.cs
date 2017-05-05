using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace ArkhamEstate
{
    public class Building_Radio : Building_Gramophone
    {
        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);
            isRadio = true;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    public class CompProperties_Steam : CompProperties
    {
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string err in base.ConfigErrors(parentDef))
            {
                yield return err;
            }
            yield break;
        }

        public bool transmitsSteam;

        public bool isSteam;

        //public bool connectsToSteam = true;
        
        public float baseSteamConsumption;
        public float baseWaterConsumption;

        public float toxicGasVentRate;
        public float toxicGasGenPct = 0.0f;

        public int ticksPerSteamVent = -1;
        public IntRange ticksOnTime = new IntRange(-1, -1);
        public int ticksOffTime = -1;
        public Vector3 steamOffset = new Vector3(0,0,0);
        
        public bool canBurst;

        public float pressureBuildRate = 0.001f; //per tick

        public float maximumPressure = 9840; //psi to break a hard drawn copper tube 
        
        //public bool startElectricalFires;

        //public bool shortCircuitInRain = true;

        public SoundDef soundSteamOn;

        public SoundDef soundSteamOff;

        public SoundDef soundAmbientSteamed;
    }
}

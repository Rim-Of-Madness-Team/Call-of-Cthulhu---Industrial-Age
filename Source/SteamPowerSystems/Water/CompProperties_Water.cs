using System.Collections.Generic;
using Verse;

namespace ArkhamEstate
{
    public class CompProperties_Water : CompProperties
    {
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string err in base.ConfigErrors(parentDef))
            {
                yield return err;
            }
            yield break;
        }

        public bool transmitsWater;

        public bool isSteam;

        //public bool connectsToWater = true;
        
        public float baseWaterConsumption;

        public bool canBurst;

        public float pressureBuildRate = 0.001f; //per tick

        public float maximumPressure = 9840; //psi to break a hard drawn copper tube 
        
        //public bool startElectricalFires;

        //public bool shortCircuitInRain = true;

        public SoundDef soundWaterOn;

        public SoundDef soundWaterOff;

        public SoundDef soundAmbientWatered;
    }
}

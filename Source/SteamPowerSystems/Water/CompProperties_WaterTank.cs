using RimWorld;
using UnityEngine;

namespace ArkhamEstate
{
    public class CompProperties_WaterTank : CompProperties_Water
    {
        public CompProperties_WaterTank()
        {
            this.compClass = typeof(CompWaterTank);
        }

        public float storedWaterMax = 22_700f; //We're using litres, but this is about 5000 gallons.

        public float efficiency = 0.5f;
        
        public Vector3 indicatorOffset = Vector3.zero;

        public Vector2 indicatorDrawSize = Vector2.one;
    }
}
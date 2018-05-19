using System;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    internal class TexButton
    {
        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop", true);
        public static readonly Texture2D Estate_DesireWater = ContentFinder<Texture2D>.Get("UI/Commands/Estate_DesireWater", true);
        public static readonly Texture2D Estate_DesireSteam = ContentFinder<Texture2D>.Get("UI/Commands/Estate_DesireSteam", true);
        public static readonly Texture2D Estate_VentSteam = ContentFinder<Texture2D>.Get("UI/Commands/Estate_VentSteam", true);
        public static readonly Texture2D Estate_VentWater = ContentFinder<Texture2D>.Get("UI/Commands/Estate_VentWater", true);

    }
}

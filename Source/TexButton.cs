using System;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    internal class TexButton
    {
        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop", true);

    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    public class Building_Bookcase : Building_InternalStorage
    {
        public override string GetInspectString()
        {
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "")
                s.AppendLine(baseStr);
            if (this.innerContainer.Count > 0)
                s.AppendLine("Estate_ContainsXBooks".Translate(this.innerContainer.Count));
            s.AppendLine("Estate_XSlotsForBooks".Translate(CompStorageGraphic.Props.countFullCapacity));
            return s.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;
            if (this.innerContainer.Count > 0)
            {
                yield return new Command_Action()
                {
                  defaultLabel = "Estate_RetrieveBook".Translate(),
                  icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                  defaultDesc = "Estate_RetrieveBookDesc".Translate(),
                  action = delegate
                  {
                      ProcessInput();
                  }
                    
    
                };
            }
        }

        public void ProcessInput()
        {

            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = this.Map;
            if (innerContainer.Count != 0)
            {
                foreach (ThingBook current in innerContainer)
                {
                    string text = current.Label;
                    if (current.TryGetComp<CompArt>() is CompArt compArt)
                        text = "Estate_BookTitle".Translate(new object[] { compArt.Title, compArt.AuthorName });
                    List<FloatMenuOption> arg_121_0 = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                    arg_121_0.Add(new FloatMenuOption(text, delegate
                    {
                        base.TryDrop(current);
                    }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }
    }
}

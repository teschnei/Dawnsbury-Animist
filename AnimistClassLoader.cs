using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Classes.Animist.Apparitions;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using HarmonyLib;

namespace Dawnsbury.Mods.Classes.Animist;

public static class AnimistClassLoader
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FeatGeneratorAttribute : Attribute
    {
        public int Level
        {
            get; set;
        }
        public FeatGeneratorAttribute(int level)
        {
            Level = level;
        }
    }

    static IEnumerable<MethodInfo> GetFeatGenerators()
    {
        var a = typeof(AnimistClassLoader).Assembly.GetTypes().Where(x => x.IsClass).SelectMany(x => x.GetMethods())
        .Where(x => x.GetCustomAttributes(typeof(FeatGeneratorAttribute), false).FirstOrDefault() != null)
        .OrderBy(x => (x.GetCustomAttributes(typeof(FeatGeneratorAttribute), false).First() as FeatGeneratorAttribute)!.Level);
        return a;
    }

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.AssertV3();

        foreach (var featGenerator in GetFeatGenerators())
        {
            foreach (var feat in (featGenerator.Invoke(null, null) as IEnumerable<Feat>)!)
            {
                ModManager.AddFeat(feat);
            }
        }

        var harmony = new Harmony("junabell.dawnsburydays.animist");
        harmony.PatchAll();

        CreatureStatblock.CreatureStatblockSectionGenerators.Insert(CreatureStatblock.CreatureStatblockSectionGenerators.FindIndex(i => i.Name == "Impulses"),
            new("Apparitions", cr => String.Join("\n",
                String.Join("\n",
                    from f in cr.PersistentCharacterSheet?.Calculated.AllFeats ?? []
                    where f.HasTrait(AnimistTrait.ApparitionPrimary)
                    select $"{{b}}{f.DisplayName(cr.PersistentCharacterSheet!)}{{/b}}"
                ),
                String.Join("\n",
                    from f in cr.PersistentCharacterSheet?.Calculated.AllFeats ?? []
                    where f.HasTrait(AnimistTrait.ApparitionAttuned) && cr.PersistentCharacterSheet?.Calculated.AllFeats.Where(p => p is Apparition apparition && apparition.AttunedFeat == f).Count() == 0
                    select $"{f.DisplayName(cr.PersistentCharacterSheet!)}"
                )
            ))
        );
    }
}

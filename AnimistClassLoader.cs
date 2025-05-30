using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;
using HarmonyLib;

namespace Dawnsbury.Mods.Classes.Animist;

public static class AnimistClassLoader
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FeatGeneratorAttribute : Attribute
    {
    }

    static IEnumerable<MethodInfo> GetFeatGenerators()
    {
        var a = typeof(AnimistClassLoader).Assembly.GetTypes().Where(x => x.IsClass).SelectMany(x => x.GetMethods())
        .Where(x => x.GetCustomAttributes(typeof(FeatGeneratorAttribute), false).FirstOrDefault() != null);
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
    }
}

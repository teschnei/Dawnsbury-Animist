using System;
using System.Linq;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Animist;

public static class AnimistClassLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.AssertV3();
        foreach (var feat in Animist.CreateFeats())
        {
            ModManager.AddFeat(feat);
        }
    }
}

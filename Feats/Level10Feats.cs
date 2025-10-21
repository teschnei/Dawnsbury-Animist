using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using Dawnsbury.Mods.Familiars;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level10
{
    [FeatGenerator(10)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.IncredibleFamiliar, 10,
                "Your connection to your apparition and your mastery of spiritual magic reach a new threshold that allows you to channel even more power into the physical form you allot it to take on as your familiar.",
                "You can select a base of six familiar or master abilities each day, instead of four.",
                [AnimistTrait.Animist])
            .WithPrerequisite(AnimistFeat.EnhancedFamiliar, "Enhanced Familiar")
            .WithEquivalent(sheet => sheet.HasFeat(Familiars.ClassFeats.FNIncredibleFamiliar))
            .WithOnSheet(sheet =>
            {
                var index = sheet.SelectionOptions.FindIndex(o => o.Key.EndsWith("FamiliarAbilities"));
                if (index > 0)
                {
                    sheet.SelectionOptions[index] = FamiliarFeats.CreateFamiliarFeatsSelectionOption(sheet);
                }
            });
    }
}

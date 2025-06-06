using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Practices;

public static class Practice
{
    [FeatGenerator(0)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new Feat(AnimistFeat.InvocationOfSight, "", "", [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.ApparitionSense);
            })
            .WithPermanentQEffect("You shift your eyes easily to the spirit world, intuiting the needs of apparitions and other spiritual entities based on how they appear to you. You gain the Apparition Sense feat. You also gain a +1 status bonus to saving throws and AC against the effects of haunts and the abilities of spirits and incorporeal undead.",
                q =>
                {
                    q.BonusToDefenses = (qe, action, defense) =>
                    {
                        if ((action != null) && ((action.Owner.HasTrait(Trait.Undead) && action.Owner.HasTrait(Trait.Incorporeal)) ||
                                (ModManager.TryParse<Trait>("Haunt", out var haunt) && (action.Owner.HasTrait(haunt) || action.HasTrait(haunt)))))
                        {
                            return new Bonus(1, BonusType.Status, "Invocation of Sight", true);
                        }
                        return null;
                    };
                }
            );
        yield return new Feat(AnimistFeat.SongOfInvocation, "", "", [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.CircleOfSpirits);
            })
            .WithPermanentQEffect("You can sing out to the spirits to have them spin and twirl around you. You gain the Circle of Spirits feat.", delegate { });
        yield return new Feat(AnimistFeat.InvocationOfUnity, "", "", [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.RelinquishControl);
            })
            .WithPermanentQEffect("The lines between your body and your apparition are blurry. You gain the Relinquish Control feat.", delegate { });
    }

    public static IEnumerable<Feat> GetPractices()
    {
        yield return new Feat(AnimistFeat.Liturgist,
                "You draw forth your apparitions through the power of song and dance, connecting the spiritual to the physical. These performances can be of your own creation or follow the specific rites of your religion.",
                "{b}Song of Invocation (1st):{/b} You can sing out to the spirits to have them spin and twirl around you. You gain the Circle of Spirits feat.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.SongOfInvocation);
            });
        yield return new Feat(AnimistFeat.Medium,
                "You are particularly good at acting as a conduit for spiritual energy and tend to associate more freely with a wide array of apparitions, though you tend not to form the deep bond with a single apparition that other animists often develop.",
                "{b}Invocation of Unity (1st):{/b} The lines between your body and your apparition are blurry. You gain the Relinquish Control feat.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.InvocationOfUnity);
            });
        yield return new Feat(AnimistFeat.Seer,
                "You are particularly sensitive to the presence and influence of spirits and undead. You can detect lingering spirits, offering you some defense against them.",
                "{b}Invocation of Sight (1st):{/b} You shift your eyes easily to the spirit world, intuiting the needs of apparitions and other spiritual entities based on how they appear to you. You gain the Apparition Sense feat. You also gain a +1 status bonus to saving throws and AC against the effects of haunts and the abilities of spirits and incorporeal undead.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.InvocationOfSight);
            });
        /* TODO: familiars must come first
    yield return new Practice(AnimistFeat.Shaman,
            "You form close bonds with your apparitions that allow you to invest them with the rare ability to take on a material form and directly affect the physical world.",
            "{b}Invocation of Embodiment (1st):{/b} You allow your apparition to inhabit a physical form. You gain the Spirit Familiar feat. At 2nd level, you gain the Enhanced Familiar feat.",
            [], null)
        .WithOnSheet(sheet =>
        {
            sheet.GrantFeat(AnimistFeat.SpiritFamiliar);
            sheet.AddAtLevel(2, action =>
            {
                action.GrantFeat(AnimistFeat.EnhancedFamiliar);
            });
        });
        */
    }
}

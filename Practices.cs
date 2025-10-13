using System;
using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.Classes.Animist.Apparitions;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Practices;

public static class Practice
{
    [FeatGenerator(0)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new Feat(AnimistFeat.SongOfInvocation, "You can sing out to the spirits to have them spin and twirl around you.", "You gain the Circle of Spirits feat.", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.CircleOfSpirits);
            });
        yield return new Feat(AnimistFeat.DancingInvocation, "The movement of your body grants power to your magic.", "When you Leap, Step, or Tumble Through, you also Sustain an apparition spell or vessel spell.", [AnimistTrait.Invocation], null)
            .WithPermanentQEffect(null,
                q =>
                {
                    q.AfterYouTakeAction = async (q, action) =>
                    {
                        if (action.ActionId == ActionId.Leap || action.ActionId == ActionId.Step || action.ActionId == ActionId.TumbleThrough)
                        {
                            var spells = Possibilities.Create(q.Owner).Filter(possibility =>
                            {
                                if (!(possibility.CombatAction.HasTrait(Trait.SustainASpell) && possibility.CombatAction.ReferencedQEffect?.ReferencedSpell?.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition))
                                {
                                    return false;
                                }
                                possibility.CombatAction.ActionCost = 0;
                                possibility.RecalculateUsability();
                                return true;
                            });
                            if (spells.ActionCount > 0)
                            {
                                var active = q.Owner.Battle.ActiveCreature;
                                q.Owner.Battle.ActiveCreature = q.Owner;
                                q.Owner.Possibilities = spells;
                                var options = await q.Owner.Battle.GameLoop.CreateActions(q.Owner, q.Owner.Possibilities, null);
                                q.Owner.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                                await q.Owner.Battle.GameLoop.OfferOptions(q.Owner, options, true);
                                q.Owner.Battle.ActiveCreature = active;
                            }
                        }
                    };
                });

        yield return new Feat(AnimistFeat.InvocationOfUnity, "The lines between your body and your apparition are blurry.", "You gain the Relinquish Control feat.", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.RelinquishControl);
            });
        yield return new Feat(AnimistFeat.DualInvocation, "You can build powerful bonds with multiple apparitions.", "You can select two of your attuned apparitions to be your primary apparitions. The number of Focus Points in your focus pool is equal to the number of focus spells you have or the number of primary apparitions you are attuned to, whichever is higher (maximum 3).", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                var primaryIndex = sheet.SelectionOptions.FindIndex(option => option.Name == "Primary Apparitions");
                sheet.SelectionOptions[primaryIndex] = new MultipleFeatSelectionOption("AnimistPrimaryApparition", "Primary Apparitions", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, (ft, values) =>
                {
                    if (ft is Apparition apparition)
                    {
                        return values.HasFeat(apparition.AttunedFeat);
                    }
                    return false;
                }, 2);
                sheet.AtEndOfRecalculation += sheet2 => sheet2.FocusPointCount = Math.Max(sheet2.FocusPointCount, 2);
            });
        yield return new Feat(AnimistFeat.InvocationOfSight, "You shift your eyes easily to the spirit world, intuiting the needs of apparitions and other spiritual entities based on how they appear to you.", "You gain the Apparition Sense feat. You also gain a +1 status bonus to saving throws and AC against the effects of haunts and the abilities of spirits and incorporeal undead.", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.ApparitionSense);
            })
            .WithPermanentQEffect(null,
                q =>
                {
                    q.BonusToDefenses = (qe, action, defense) =>
                    {
                        if ((action != null) && ((action.Owner.HasTrait(Trait.Undead) && action.Owner.HasTrait(Trait.Incorporeal)) ||
                                (action.Owner.HasTrait(Trait.Haunt) || action.HasTrait(Trait.Haunt))))
                        {
                            return new Bonus(1, BonusType.Status, "Invocation of Sight", true);
                        }
                        return null;
                    };
                }
            );
        yield return new Feat(AnimistFeat.InvocationOfProtection, "Your status as an intermediary across planar boundaries grants you further defenses against spiritual ailments", "You gain spirit resistance and void resistance equal to half your level, and your status bonus to saving throws and AC against the effect of haunts and the abilities of spirits and incorporeal undead increases to +2.", [AnimistTrait.Invocation], null)
            .WithPermanentQEffect(null,
                q =>
                {
                    q.BonusToDefenses = (qe, action, defense) =>
                    {
                        if ((action != null) && ((action.Owner.HasTrait(Trait.Undead) && action.Owner.HasTrait(Trait.Incorporeal)) ||
                                (action.Owner.HasTrait(Trait.Haunt) || action.HasTrait(Trait.Haunt))))
                        {
                            return new Bonus(2, BonusType.Status, "Invocation of Protection", true);
                        }
                        return null;
                    };
                    q.StateCheck = qe =>
                    {
                        var amount = (qe.Owner.Level + 1) / 2;
                        qe.Owner.WeaknessAndResistance.AddResistance(DamageKind.Positive, amount);
                        qe.Owner.WeaknessAndResistance.AddResistance(DamageKind.Negative, amount);
                    };
                }
            );
        yield return new Feat(AnimistFeat.InvocationOfEmbodiment, "You allow your apparition to inhabit a physical form.", "You gain the Spirit Familiar feat. At 2nd level, you gain the Enhanced Familiar feat.", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.SpiritFamiliar);
                sheet.AddAtLevel(2, action => action.GrantFeat(AnimistFeat.EnhancedFamiliar));
            });
        yield return new Feat(AnimistFeat.InvocationOfGrowth, "Your bond with the physical form of your chosen apparition grows stronger.", "You gain the Incredible Familiar feat.", [AnimistTrait.Invocation], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.IncredibleFamiliar);
            });
    }

    public static IEnumerable<Feat> GetPractices()
    {
        yield return new Feat(AnimistFeat.Liturgist,
                "You draw forth your apparitions through the power of song and dance, connecting the spiritual to the physical. These performances can be of your own creation or follow the specific rites of your religion.",
                "{b}Song of Invocation (1st):{/b} You can sing out to the spirits to have them spin and twirl around you. You gain the Circle of Spirits feat.\n{b}Dancing Invocation (9th):{/b} The movement of your body grants power to your magic. When you Leap, Step, or Tumble Through, you also Sustain an apparition spell or vessel spell.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.SongOfInvocation);
                sheet.AddAtLevel(9, sheet2 => sheet2.GrantFeat(AnimistFeat.DancingInvocation));
            });
        yield return new Feat(AnimistFeat.Medium,
                "You are particularly good at acting as a conduit for spiritual energy and tend to associate more freely with a wide array of apparitions, though you tend not to form the deep bond with a single apparition that other animists often develop.",
                "{b}Invocation of Unity (1st):{/b} The lines between your body and your apparition are blurry. You gain the Relinquish Control feat.\n{b}Dual Invocation (9th):{/b} You can build powerful bonds with multiple apparitions. You can select two of your attuned apparitions to be your primary apparitions. The number of Focus Points in your focus pool is equal to the number of focus spells you have or the number of primary apparitions you are attuned to, whichever is higher (maximum 3).",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.InvocationOfUnity);
                sheet.AddAtLevel(9, sheet2 => sheet2.GrantFeat(AnimistFeat.DualInvocation));
            });
        yield return new Feat(AnimistFeat.Seer,
                "You are particularly sensitive to the presence and influence of spirits and undead. You can detect lingering spirits, offering you some defense against them.",
                "{b}Invocation of Sight (1st):{/b} You shift your eyes easily to the spirit world, intuiting the needs of apparitions and other spiritual entities based on how they appear to you. You gain the Apparition Sense feat. You also gain a +1 status bonus to saving throws and AC against the effects of haunts and the abilities of spirits and incorporeal undead.\n{b}Invocation of Protection (9th):{/b} Your status as an intermediary across planar boundaries grants you further defenses against spiritual ailments. You gain spirit resistance and void resistance equal to half your level, and your status bonus to saving throws and AC against the effect of haunts and the abilities of spirits and incorporeal undead increases to +2.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.InvocationOfSight);
                sheet.AddAtLevel(9, sheet2 => sheet2.GrantFeat(AnimistFeat.InvocationOfProtection));
            });
        yield return new Feat(AnimistFeat.Shaman,
                "You form close bonds with your apparitions that allow you to invest them with the rare ability to take on a material form and directly affect the physical world.",
                "{b}Invocation of Embodiment (1st):{/b} You allow your apparition to inhabit a physical form. You gain the Spirit Familiar feat. At 2nd level, you gain the Enhanced Familiar feat.\n{b}Invocation of Growth (9th):{/b} Your bond with the physical form of your chosen apparition grows stronger. You gain the Incredible Familiar feat.",
                [], null)
            .WithOnSheet(sheet =>
            {
                sheet.GrantFeat(AnimistFeat.InvocationOfEmbodiment);
                sheet.AddAtLevel(9, action => action.GrantFeat(AnimistFeat.InvocationOfGrowth));
            });
    }
}

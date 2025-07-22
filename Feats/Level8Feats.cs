using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level8
{
    [FeatGenerator(8)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.ApparitionsReflection, 8,
                "Your apparition infuses your body with additional power.",
                "You regain one expended apparition spell slot that is at least 2 ranks lower than your highest-rank spell slot and takes 1, 2, or 3 actions to Cast. You then immediately cast an apparition spell that can be cast using that slot. The number of actions required for Apparition's Reflection is equal to the action cost of the spell cast. Maintaining control after such a surge is difficult, however; after casting the spell, you're confused until the end of your next turn.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, AnimistTrait.Spellshape])
            .WithActionCost(-1)
            .WithPermanentQEffectAndSameRulesText(qe =>
            {
                qe.Id = AnimistQEffects.AnimistsReflectionUnholiness;
                qe.MetamagicProvider = new MetamagicProvider("Apparition's Reflection", spell =>
                {
                    if (spell.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition && !qe.Owner.HasEffect(AnimistQEffects.AnimistsReflectionUsed))
                    {
                        var spellSlots = qe.Owner.Spellcasting?.GetSourceByOrigin(AnimistTrait.Apparition)?.SpontaneousSpellSlots.Index().Where(slot => slot.Item > 0);
                        var usedSpellSlots = qe.Owner.PersistentUsedUpResources.GetSpellcasting(AnimistTrait.Apparition)?.SpontaneousSpellSlotsUsedUp.Index().Where(slot => slot.Item > 0);
                        var highestSpellSlot = spellSlots?.Count() > 0 ? spellSlots.MaxBy(slot => slot.Index).Index : 0;
                        var highestUsedSpellSlot = usedSpellSlots?.Count() > 0 ? usedSpellSlots.MaxBy(slot => slot.Index).Index : 0;

                        var allSpellSlots = qe.Owner.Spellcasting!.GetSourceByOrigin(AnimistTrait.Apparition)!.SpontaneousSpellSlots;
                        //Time for some unholiness to get the calling function to include spells where there are no spell slots left
                        if (qe.Tag == null)
                        {
                            qe.Tag = allSpellSlots.Clone();
                            for (int i = 0; i < Math.Max(highestSpellSlot, highestUsedSpellSlot) - 2; ++i)
                            {
                                allSpellSlots[i] = 1;
                            }
                        }
                        if (spell.SpellLevel > 0 && spell.SpellLevel <= Math.Max(highestSpellSlot, highestUsedSpellSlot) - 2 && spell.ActionCost > 0)
                        {
                            CombatAction metamagicSpell = Spell.DuplicateSpell(spell).CombatActionSpell;
                            metamagicSpell.Name = "Apparition's Reflection: " + metamagicSpell.Name;
                            metamagicSpell.Traits.Add(Trait.AtWill);
                            metamagicSpell.EffectOnChosenTargets = Delegates.SmartCombineDelegates(metamagicSpell.EffectOnChosenTargets, async (action, self, chosenTargets) =>
                            {
                                self.AddQEffect(new QEffect()
                                {
                                    AfterYouTakeAction = async (q, action) =>
                                    {
                                        q.ExpiresAt = ExpirationCondition.Immediately;
                                        QEffect confusion = QEffect.Confused(false, spell);
                                        confusion.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                                        confusion.CannotExpireThisTurn = true;
                                        q.Owner.AddQEffect(confusion);
                                        q.Owner.AddQEffect(new QEffect()
                                        {
                                            Id = AnimistQEffects.AnimistsReflectionUsed
                                        });
                                    }
                                });
                            });
                            return metamagicSpell;
                        }
                    }
                    return null;
                });
            });
        yield return new TrueFeat(AnimistFeat.InstinctiveManeuvers, 8,
                "When you allow an apparition control over your body, it might vent its fury against your foes.",
                "When you Relinquish Control, you add Grapple, Shove, and Trip to the list of actions you can take. You gain a +2 status bonus to the Athletics checks to attempt these actions.",
                [AnimistTrait.Animist, AnimistTrait.Apparition])
            .WithPrerequisite(AnimistFeat.RelinquishControl, "Relinquish Control")
            .WithPermanentQEffectAndSameRulesText(q =>
            {
                q.Id = AnimistQEffects.InstinctiveManeuvers;
            });
        // Exploration feat, maybe replace it with something else
        /*
        yield return new TrueFeat(AnimistFeat.SpiritWalk, 8,
                "",
                "",
                [AnimistTrait.Animist, AnimistTrait.Apparition])
            ;
        */
        // No Aerial Form spell yet :(
        /*
        yield return new TrueFeat(AnimistFeat.WindSeeker, 8,
                "",
                "",
                [AnimistTrait.Animist])
            .WithOnSheet(sheet => sheet.SpellRepertoires[AnimistTrait.Apparition].SpellsKnown.AddRange(
                    from spellLevel in Enumerable.Range(1, sheet.MaximumSpellLevel)
                    select AllSpells.CreateModernSpellTemplate(SpellId.AerialForm, AnimistTrait.Apparition, spellLevel)
        ));
    */
    }
}

using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level4
{
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.ApparitionsEnhancement, 4,
                "Spiritual power encases your weapon or unarmed attack.",
                "Until the end of your turn, one wielded weapon or unarmed attack you have deals an extra 1d6 force damage and gains the divine trait, if it didn't have it already.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Divine, Trait.Force])
            .WithActionCost(0)
            .WithPermanentQEffect("Your apparition enhances one of your weapons using the residual power of a recently cast spell.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.MagicWeapon, "Apparition's Enhancement", [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Divine, Trait.Force],
                                "Spiritual power encases your weapon or unarmed attack. Until the end of your turn, one wielded weapon or unarmed attack you have deals an extra 1d6 force damage and gains the divine trait, if it didn't have it already.",
                                Target.Self(null).WithAdditionalRestriction(self =>
                                {
                                    var lastAction = self.Actions.ActionHistoryThisEncounter.Where(a => a.SpentActions > 0 || a.ActionCost > 0).LastOrDefault();
                                    if (lastAction == null)
                                    {
                                        return "You haven't acted yet.";
                                    }
                                    if (lastAction.SpellLevel == 0)
                                    {
                                        return "Your last action wasn't to cast a non-cantrip spell.";
                                    }
                                    return null;
                                }))
                        .WithActionCost(0)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            //TODO: include item art in buttons
                            var weaponName = await self.AskForChoiceAmongButtons(action.Illustration, "Which weapon would you like to enhance?", self.Weapons.Select(w => w.Name).ToArray());
                            var weapon = self.Weapons.Where(w => w.Name == weaponName.Caption).FirstOrDefault();
                            if (weapon != null)
                            {
                                var oldTraits = new List<Trait>(weapon.Traits);
                                if (!weapon.Traits.Contains(Trait.Divine))
                                {
                                    weapon.Traits.Add(Trait.Divine);
                                }
                                self.AddQEffect(new QEffect("Apparition's Enhancement", "Your weapon is enhanced by your apparition.", ExpirationCondition.ExpiresAtEndOfYourTurn, self, weapon.Illustration)
                                {
                                    AddExtraWeaponDamage = item =>
                                    {
                                        if (item == weapon)
                                        {
                                            return (DiceFormula.FromText("1d6"), DamageKind.Force);
                                        }
                                        return null;
                                    },
                                    WhenExpires = q =>
                                    {
                                        weapon.Traits = oldTraits;
                                    }
                                });
                            }
                        })
                    );
                };
            });
        yield return new TrueFeat(AnimistFeat.ChanneledProtection, 4,
                "Your apparition uses excess energy to protect you.",
                "You and all adjacent allies gain a +1 status bonus to your AC and to your Reflex saving throws until the start of your next turn.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Aura])
            .WithActionCost(1)
            .WithPrerequisite(AnimistFeat.ChannelersStance, "Channeler's Stance")
            .WithPermanentQEffect("Your apparition uses excess energy from its spell to protect you.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.MagicWeapon, "Channeled Protection", [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Aura],
                                "You enter a stance that grants a status bonus equal to the spell's rank to apparition spells or vessel spells that deal energy damage, and to spells that have the vitality or void traits that restore Hit Points.",
                                Target.Self(null).WithAdditionalRestriction(self =>
                                {
                                    var lastAction = self.Actions.ActionHistoryThisEncounter.Where(a => a.SpentActions > 0 || a.ActionCost > 0).LastOrDefault();
                                    if (lastAction == null)
                                    {
                                        return "You haven't acted yet.";
                                    }
                                    if (lastAction.SpellcastingSource?.ClassOfOrigin != AnimistTrait.Animist || lastAction.SpellLevel == 0)
                                    {
                                        return "Your last action wasn't to cast a spell from your spell slots.";
                                    }
                                    if (!self.HasEffect(AnimistQEffects.ChannelersStance))
                                    {
                                        return "You aren't in Channeler's Stance.";
                                    }
                                    return null;
                                }))
                        .WithActionCost(1)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            self.AddQEffect(new QEffect("Channeled Protection",
                                        "Your apparition grants you and adjacent allies a +1 status bonus to your AC and to your Reflex saving throws.",
                                        ExpirationCondition.ExpiresAtStartOfYourTurn,
                                        self,
                                        IllustrationName.ShieldSpell)
                                .AddGrantingOfTechnical(cr => cr.FriendOf(self) && cr.DistanceTo(self) <= 1, q =>
                                {
                                    q.BonusToDefenses = (q, action, defense) => (defense == Defense.AC || defense == Defense.Reflex) ? new Bonus(1, BonusType.Status, "Channeled Protection", true) : null;
                                })
                            );
                        })
                    );
                };
            });
        yield return new TrueFeat(AnimistFeat.WalkTheWilds, 4,
                "You know the ways of birds and beasts and have gained the right to wear their form.",
                $"You add {AllSpells.CreateModernSpellTemplate(SpellId.AnimalForm, AnimistTrait.Animist).ToSpellLink()} to your apparition spell repertoire, allowing you to cast it with your apparition spellcasting.",
                [AnimistTrait.Animist])
            .WithOnSheet(sheet => sheet.SpellRepertoires[AnimistTrait.Apparition].SpellsKnown.AddRange(
                    from spellLevel in Enumerable.Range(1, sheet.MaximumSpellLevel)
                    select AllSpells.CreateModernSpellTemplate(SpellId.AnimalForm, AnimistTrait.Apparition, spellLevel)
            ));
    }
}

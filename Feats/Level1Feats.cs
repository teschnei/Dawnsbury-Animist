using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.Classes.Animist.Apparitions;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level1
{
    [FeatGenerator(1)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.ApparitionSense, 1,
                "You can see and interact with things others can’t.",
                "You have apparition sight, an imprecise sense that allows you to detect the presence of invisible or hidden spirits, haunts, and undead within 30 feet of you.",
                [AnimistTrait.Animist, Trait.Divine])
            .WithPermanentQEffect("You have apparition sight, an imprecise sense that allows you to detect the presence of invisible or hidden spirits, haunts, and undead within 30 feet of you.", q =>
            {
            });
        yield return new TrueFeat(AnimistFeat.ChannelersStance, 1,
                "You enter a stance that allows power to flow through you.",
                "While in this stance, whenever you cast or Sustain an apparition spell or vessel spell that deals energy damage, you gain a status bonus to the spell’s damage equal to the spell’s rank. Each time you Cast a Spell that has the vitality or void traits and that restores Hit Points while in this stance, the spells’ targets gain a status bonus to the initial amount of healing received equal to the spell’s rank. This bonus healing does not apply to healing over time effects (such as fast healing or regeneration).",
                [AnimistTrait.Animist, Trait.Stance])
            .WithActionCost(1)
            .WithPermanentQEffect("You enter a stance that grants a status bonus equal to the spell's rank to apparition spells or vessel spells that deal energy damage, and to spells that have the vitality or void traits that restore Hit Points.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.MagicWeapon, "Channeler's Stance", [AnimistTrait.Animist, Trait.Stance],
                            "You enter a stance that grants a status bonus equal to the spell's rank to apparition spells or vessel spells that deal energy damage, and to spells that have the vitality or void traits that restore Hit Points.",
                            Target.Self(null).WithAdditionalRestriction(self => self.HasEffect(AnimistQEffects.ChannelersStance) ? "You're already in this stance." : null))
                        .WithActionCost(1)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            var stance = KineticistCommonEffects.EnterStance(self, action.Illustration, "Channeler's Stance", "Your apparition and vessel spells that deal energy damage deal bonus damage, and your spells that have the vitality or void traits that restore Hit Points heal bonus Hit Points.", AnimistQEffects.ChannelersStance);
                            stance.BonusToDamage = (qe, action, target) =>
                            {
                                if (action.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition &&
                                    (action.HasTrait(Trait.Acid) ||
                                     action.HasTrait(Trait.Fire) ||
                                     action.HasTrait(Trait.Cold) ||
                                     action.HasTrait(Trait.Electricity) ||
                                     action.HasTrait(Trait.Sonic)))
                                {
                                    return new Bonus(action.SpellLevel, BonusType.Status, "Channeler's Stance", true);
                                }
                                return null;
                            };
                            stance.AddGrantingOfTechnical(cr => cr.FriendOf(self), qe =>
                            {
                                qe.BonusToSelfHealing = (qe, action) =>
                                {
                                    if (action != null && (action.Owner == self) &&
                                        (action.HasTrait(Trait.Positive) || action.HasTrait(Trait.Negative)))
                                    {
                                        return new Bonus(action.SpellLevel, BonusType.Status, "Channeler's Stance", true);
                                    }
                                    return null;
                                };
                            });
                        })
                    );
                };
            });
        yield return new TrueFeat(AnimistFeat.CircleOfSpirits, 1,
                "With a thought, word, or gesture, you reach your mind out to another spirit.",
                "Choose another apparition from among those you’ve attuned to; it becomes your primary apparition, replacing your current one.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Concentrate])
            .WithActionCost(1)
            .WithPermanentQEffect("You instantly replace your primary apparition with another attuned apparition.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.CircleOfProtection, "Circle Of Spirits", [AnimistTrait.Animist, Trait.Concentrate],
                            "Choose another apparition from among those you’ve attuned to; it becomes your primary apparition, replacing your current one.",
                            Target.Self(null))
                        .WithActionCost(1)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            var choice = await self.AskForChoiceAmongButtons(IllustrationName.CircleOfProtection, "Choose a new primary apparition",
                                   [.. from feat in self.PersistentCharacterSheet?.Calculated.AllFeats where feat.HasTrait(AnimistTrait.ApparitionAttuned) select feat.Name, "Cancel"]);
                            if (choice.Caption == "Cancel")
                            {
                                action.RevertRequested = true;
                            }
                            else
                            {
                                Apparition? chosenApparition = AllFeats.All.Where(feat => feat.Name == choice.Caption && feat is Apparition).FirstOrDefault() as Apparition;
                                if (chosenApparition != null)
                                {
                                    self.Spellcasting?.GetSourceByOrigin(AnimistTrait.Apparition)?.FocusSpells.RemoveAll(spell => true);
                                    self.Spellcasting?.GetSourceByOrigin(AnimistTrait.Apparition)?.WithSpells([chosenApparition.VesselSpell], self.PersistentCharacterSheet?.Calculated.MaximumSpellLevel ?? 0);
                                }
                            }
                        })
                    );
                };
            });
        yield return new TrueFeat(AnimistFeat.RelinquishControl, 1,
                "Your apparition takes over and shields you from outside influence.",
                "Until the start of your next turn, you gain a +4 status bonus on saves against spells and effects that give you the controlled condition or attempt to influence your actions (such as charm, command, or a nosoi’s haunting melody). However, the only actions you can take are to Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action that has the apparition trait.\n{b}Special{/b} This feat requires a particularly strong bond with a specific apparition to learn. Choose one apparition you have access to; once you learn this feat, you must always choose that apparition as one of the apparitions you attune to each day.",
                [AnimistTrait.Animist, AnimistTrait.Apparition],
                null)
            .WithActionCost(0)
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOptionRightNow(new SingleFeatSelectionOption("RelinquishControlApparition", "Bonded Apparition", 1, ft => ft.HasTrait(AnimistTrait.ApparitionAttuned)));
                sheet.SelectionOptions.RemoveAll(option => option.Name == "Attuned Apparitions");
                sheet.AddSelectionOption(new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionAttuned), sheet.CurrentLevel >= 7 ? 2 : 1));
            })
            .WithPermanentQEffect("You gain a +4 status bonus on saves against controlling effects, but you can only take the Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action with the apparition trait.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.CircleOfProtection, "Relinquish Control", [AnimistTrait.Animist, AnimistTrait.Apparition],
                            "Until the start of your next turn, you gain a +4 status bonus on saves against spells and effects that give you the controlled condition or attempt to influence your actions (such as charm, command, or a nosoi’s haunting melody). However, the only actions you can take are to Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action that has the apparition trait.",
                            Target.Self(null))
                        .WithActionCost(0)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            self.AddQEffect(new QEffect("Relinquished Control", "You gain a +4 status bonus on saves against controlling effects, but you can only take the Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action with the apparition trait.",
                                ExpirationCondition.ExpiresAtStartOfYourTurn, self, action.Illustration)
                            {
                                PreventTakingAction = action =>
                                {
                                    if (action.ActionId == ActionId.Step || action.ActionId == ActionId.Stride || action.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition ||
                                        (action.ReferencedQEffect?.ReferencedSpell?.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition) ||
                                        (self.HasEffect(AnimistQEffects.InstinctiveManeuvers) && (action.ActionId == ActionId.Grapple || action.ActionId == ActionId.Shove || action.ActionId == ActionId.Trip)))
                                    {
                                        return null;
                                    }
                                    return "You have relinquished control to your apparition and cannot take this action.";
                                },
                                BonusToDefenses = (qe, action, defense) =>
                                {
                                    //TODO: figure out all controlling effects?
                                    if (action?.SpellId == SpellId.Command)
                                    {
                                        return new Bonus(4, BonusType.Status, "Relinquish Control", true);
                                    }
                                    return null;
                                },
                                BonusToSkillChecks = (skill, action, target) =>
                                {
                                    if (self.HasEffect(AnimistQEffects.InstinctiveManeuvers) && (action.ActionId == ActionId.Grapple || action.ActionId == ActionId.Shove || action.ActionId == ActionId.Trip) && skill == Skill.Athletics)
                                    {
                                        return new Bonus(2, BonusType.Status, "Instinctive Maneuvers", true);
                                    }
                                    return null;
                                }
                            });
                        })
                    );
                };
            });
        /*
                yield return new TrueFeat(AnimistFeat.SpiritFamiliar, 1,
                        "You can dedicate a small amount of your life force to allow one of your apparitions to physically manifest as a familiar",
                        "When you attune to your apparitions during your daily preparations, you can choose to dedicate a small amount of your life force to allow one of them to physically manifest as a familiar, which gains the spirit trait. If your familiar is slain or destroyed, you lose all other benefits from the apparition until you remanifest the familiar during your next daily preparations. If you disperse the apparition you have manifested as a familiar, the familiar is destroyed.",
                        [AnimistTrait.Animist])
                    .WithPermanentQEffect("You can dedicate a small amount of your life force to allow one of your apparitions to physically manifest as a familiar", q =>
                    {

                    });
        */
    }
}

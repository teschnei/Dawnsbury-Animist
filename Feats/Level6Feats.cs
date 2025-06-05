using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level6
{
    [FeatGenerator(6)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.WanderingFeat, 6,
                "Your apparitions power strengthens you during your morning preparations.",
                "During your morning preparations, you can select any Wandering feat at the level you choose this feat or lower.",
                [AnimistTrait.Animist, AnimistTrait.Apparition])
            .WithMultipleSelection()
            .WithOnSheet(sheet =>
            {
                if (!sheet.SelectionOptions.Any(option => option.Name.StartsWith("Wandering Feat")))
                {
                    var wanderingFeats = sheet.AllFeatGrants.Where(fg => fg.GrantedFeat.FeatName == AnimistFeat.WanderingFeat);
                    int choices = wanderingFeats.Where(fg => fg.AtLevel < 12).Count();
                    if (choices > 0)
                    {
                        sheet.AddSelectionOption(new MultipleFeatSelectionOption("WanderingFeat6", "Wandering Feat (level 6)", SelectionOption.MORNING_PREPARATIONS_LEVEL, ft => ft.HasTrait(AnimistTrait.Wandering), choices));
                    }
                }
            });
        yield return new TrueFeat(AnimistFeat.ApparitionStabilization, 6,
                "Your attuned apparition ensures that even if you would be distracted or disrupted, your magic does not go to waste.",
                "If a reaction would disrupt your spellcasting action, attempt a DC 15 flat check. If you succeed your action isn't disrupted. When you gain the third apparition class feature, this DC is reduced to 13.",
                [AnimistTrait.Apparition, AnimistTrait.Wandering])
            .WithPermanentQEffect("Your attuned apparition gives you a chance to ignore your spells being disrupted.", q =>
            {
                //Implemented in AoOPatch
                q.Id = AnimistQEffects.ApparitionStabilization;
            });
        //TODO: once per 10 minutes text
        yield return new TrueFeat(AnimistFeat.BlazingSpirit, 6,
                "Your apparition grants fiery defenses.",
                "When a creature damages you with a melee attack, you gain resistance equal to your level against the triggering damage, and the triggering creature takes 1d6 fire damage and 1 persistent fire damage.",
                [AnimistTrait.Apparition, Trait.Divine, Trait.Fire, AnimistTrait.Wandering])
            .WithActionCost(-2)
            .WithPrerequisite(new Prerequisite(sheet => sheet.HasFeat(AnimistFeat.StewardOfStoneAndFire) || sheet.HasFeat(AnimistFeat.WitnessToAncientBattles), "You must be attuned to either the Steward of Stone and Fire, or the Witness to Ancient Battles."))
            .WithPermanentQEffect("Your apparition grants you a fiery retaliation when you are damaged by a melee attack.", q =>
            {
                q.YouAreDealtDamageEvent = async (qe, damageEvent) =>
                {
                    if (!qe.Owner.HasEffect(AnimistQEffects.BlazingSpiritUsed))
                    {
                        if (damageEvent.CombatAction != null && damageEvent.Source.Occupies != null &&
                            damageEvent.CombatAction.HasTrait(Trait.Melee) && damageEvent.CombatAction.HasTrait(Trait.Attack))
                        {
                            int maximumDamageReduction = 0;
                            DamageKind reductionType = DamageKind.Untyped;
                            foreach (var damage in damageEvent.KindedDamages)
                            {
                                int reduced = Math.Min(damage.ResolvedDamage, qe.Owner.Level);
                                if (reduced > maximumDamageReduction)
                                {
                                    maximumDamageReduction = reduced;
                                    reductionType = damage.DamageKind;
                                }
                            }
                            if (qe.Owner.HasEffect(AnimistQEffects.StoreTimeReaction) && !qe.Owner.HasEffect(AnimistQEffects.StoreTimeReactionUsed))
                            {
                                if (await qe.Owner.Battle.AskForConfirmation(qe.Owner, IllustrationName.Reaction, $"You're about to be hit by {damageEvent.CombatAction.Name}.\nUse Blazing Spirit to reduce the damage by {maximumDamageReduction} and deal damage in retaliation?", "{icon:Reaction} Take reaction", "Pass"))
                                {
                                    await ApplyEffect();
                                    qe.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                    {
                                        Id = AnimistQEffects.StoreTimeReactionUsed
                                    });
                                }
                            }
                            else if (await qe.Owner.Battle.AskToUseReaction(qe.Owner, $"You're about to be hit by {damageEvent.CombatAction.Name}.\nUse Blazing Spirit to reduce the damage by {maximumDamageReduction} and deal damage in retaliation?"))
                            {
                                await ApplyEffect();
                            }
                            async Task ApplyEffect()
                            {
                                damageEvent.KindedDamages.Where(k => k.DamageKind == reductionType).FirstOrDefault()!.ResolvedDamage -= maximumDamageReduction;
                                CombatAction action = new CombatAction(qe.Owner, IllustrationName.None, "Blazing Spirit", [Trait.Divine, Trait.Fire], "", Target.Uncastable());
                                await CommonSpellEffects.DealBasicDamage(action, qe.Owner, damageEvent.Source, CheckResult.Failure, "1d6", DamageKind.Fire);
                                await CommonSpellEffects.DealBasicPersistentDamage(damageEvent.Source, CheckResult.Failure, "1", DamageKind.Fire);
                                qe.Owner.AddQEffect(new QEffect()
                                {
                                    Id = AnimistQEffects.BlazingSpiritUsed
                                });
                            }
                        }
                    }
                };
            });
        yield return new TrueFeat(AnimistFeat.GrudgeStrike, 6,
                "You channel the spiritual power of spiteful grudges.",
                "Make a melee Strike against a creature within your reach. You gain a +2 circumstance bonus to your attack roll and deal an additional 2d6 void damage to the target; if the target is undead or otherwise has void healing, this Strike instead deals an additional 2d6 vitality damage. This ability gains the vitality trait if it deals vitality damage, or the void trait if it deals void damage.",
                [AnimistTrait.Apparition, Trait.Divine, AnimistTrait.Wandering])
            .WithActionCost(2)
            .WithPrerequisite(new Prerequisite(sheet => sheet.HasFeat(AnimistFeat.ImposterInHiddenPlaces) || sheet.HasFeat(AnimistFeat.WitnessToAncientBattles), "You must be attuned to either the Imposter in Hidden Places, or the Witness to Ancient Battles."))
            .WithPermanentQEffect("You can channel the spiritual power of spiteful grudges into an attack that deals bonus damage.", q =>
            {
                q.ProvideStrikeModifier = item =>
                {
                    if (item.HasTrait(Trait.Melee))
                    {
                        var strikeModifiers = new StrikeModifiers()
                        {
                            AdditionalBonusesToAttackRoll = [new Bonus(2, BonusType.Circumstance, "Grudge Strike", true)],
                            QEffectForStrike = new QEffect()
                            {
                                AddExtraKindedDamageOnStrike = (action, target) =>
                                {
                                    DamageKind kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe([DamageKind.Negative, DamageKind.Positive]);

                                    return new KindedDamage(DiceFormula.FromText("2d6"), kind);
                                }
                            }
                        };

                        var action = q.Owner.CreateStrike(item, -1, strikeModifiers);
                        action.Name = "Grudge Strike";
                        action.ActionCost = 2;
                        action.Illustration = new SideBySideIllustration(action.Illustration, IllustrationName.StarHit);
                        action.Description = StrikeRules.CreateBasicStrikeDescription4(action.StrikeModifiers, null, "\nThis attack deals an additional 2d6 void or vitality damage, whichever is more effective.", null, null, null, null, false, null);
                        return action;
                    }
                    return null;
                };
            });
        yield return new TrueFeat(AnimistFeat.MediumsAwareness, 6,
                "Your apparitions watch over you.",
                "You gain a +2 status bonus to Perception checks made to Seek and when using Perception for your initiative roll.",
                [AnimistTrait.Apparition, Trait.Divine, AnimistTrait.Wandering])
            .WithPermanentQEffect("Your apparitions grant you a status bonus to Perception checks.", q =>
            {
                q.BonusToInitiative = q => new Bonus(2, BonusType.Status, "Medium's Awareness", true);
                q.BonusToPerception = q => new Bonus(2, BonusType.Status, "Medium's Awareness", true);
            });
        yield return new TrueFeat(AnimistFeat.RoaringHeart, 6,
                "You surge forward inexorably.",
                "You Stride twice. At any point during this movement, you can Shove up to two creatures you pass adjacent to. When you end the movement, the turbulent spirits you're attuned to reward you for acting in an appropriately fierce manner: you and each ally in a 30-foot emanation gain temporary Hit Points equal to half your level if you successfully Shoved at least one enemy, or equal to your level if you succeeded at Shoving both.",
                [AnimistTrait.Apparition, Trait.Divine, AnimistTrait.Wandering])
            .WithActionCost(2)
            .WithPrerequisite(new Prerequisite(sheet => sheet.HasFeat(AnimistFeat.StewardOfStoneAndFire) || sheet.HasFeat(AnimistFeat.VanguardOfRoaringWaters), "You must be attuned to either the Steward of Stone and Fire, or the Vanguard of Roaring Waters."))
            .WithPermanentQEffect("Your apparition lets you Stride twice, attempt two Shoves, and rewards you with temporary Hit Points on successful Shoves.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.FleetStep, "Roaring Heart", [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Divine, Trait.Move],
                            "You Stride twice. At any point during this movement, you can Shove up to two creatures you pass adjacent to. When you end the movement, the turbulent spirits you're attuned to reward you for acting in an appropriately fierce manner: you and each ally in a 30-foot emanation gain temporary Hit Points equal to half your level if you successfully Shoved at least one enemy, or equal to your level if you succeeded at Shoving both.",
                            Target.Self())
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            var shoveQEffect = new QEffect()
                            {
                                Value = 0,
                                Tag = q.Owner.Occupies,
                                StateCheckWithVisibleChanges = async q =>
                                {
                                    var shoveWeapon = q.Owner.Weapons.Where(w => w.HasTrait(Trait.Shove)).FirstOrDefault();
                                    int attempts = q.Value & 0x03;
                                    int successes = (q.Value & 0x0C) >> 2;
                                    if ((shoveWeapon != null || q.Owner.HasFreeHand) && (attempts < 2) && q.Owner.Occupies != q.Tag)
                                    {
                                        q.Tag = q.Owner.Occupies;
                                        var target = shoveWeapon != null ? Target.Reach(shoveWeapon) : Target.Touch();
                                        foreach (var cr in target.GetLegalTargetCreatures(q.Owner))
                                        {
                                            if (attempts < 2 && await q.Owner.Battle.AskForConfirmation(q.Owner, IllustrationName.Shove, $"Shove {cr.Name}?", "Yes", "No"))
                                            {
                                                var checkResult = CommonSpellEffects.RollCheck("Shove", new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics), TaggedChecks.DefenseDC(Defense.Fortitude)), q.Owner, cr);
                                                if (checkResult >= CheckResult.Success)
                                                {
                                                    successes++;
                                                    int squareCount = ((checkResult != CheckResult.CriticalSuccess) ? 1 : 2);
                                                    await q.Owner.PushCreature(cr, squareCount);
                                                }
                                                q.Value = (successes << 2) | (attempts + 1);
                                            }
                                        }
                                    }
                                }
                            };
                            self.AddQEffect(shoveQEffect);
                            if (!(await self.StrideAsync("Choose where to Stride with Roaring Heart. (1/2)", allowCancel: true)))
                            {
                                action.RevertRequested = true;
                            }
                            else
                            {
                                await self.StrideAsync("Choose where to Stride with Roaring Heart. (2/2)", allowPass: true);
                                var temphp = ((self.Level + 1) / 2) * ((shoveQEffect.Value & 0x0C) >> 2);
                                if (temphp > 0)
                                {
                                    foreach (var cr in q.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(self) <= 6 && cr.FriendOf(self) && !cr.HasTrait(Trait.Object)))
                                    {
                                        cr.GainTemporaryHP(temphp);
                                    }
                                }
                            }
                            self.RemoveAllQEffects(q => q == shoveQEffect);
                        })
                    );
                };
            });
    }
}



using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;

namespace Dawnsbury.Mods.Classes.Animist.Feats;

public static class Level2
{
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(AnimistFeat.ConcealSpell, 2,
                "You speak with the unheard voice of the spirits.",
                "If the next action you use is to Cast a Spell, the spell gains the subtle trait, hiding the shining runes, sparks of magic, and other manifestations that would usually give away your spellcasting. The trait hides only the spell’s spellcasting actions and manifestations, not its effects, so an observer might still see a ray streak out from you or see you vanish into thin air.",
                [AnimistTrait.Animist, Trait.Concentrate, AnimistTrait.Spellshape])
            .WithActionCost(1)
            .WithPermanentQEffect("You can cast spells without provoking an Attack of Opportunity", qe =>
            {
                qe.MetamagicProvider = new MetamagicProvider("Conceal Spell", spell =>
                {
                    CombatAction metamagicSpell = Spell.DuplicateSpell(spell).CombatActionSpell;
                    if (metamagicSpell.ActionCost == 3 || metamagicSpell.ActionCost == -2)
                    {
                        return (CombatAction?)null;
                    }
                    metamagicSpell.Name = "Conceal " + metamagicSpell.Name;
                    CommonSpellEffects.IncreaseActionCostByOne(metamagicSpell);
                    metamagicSpell.Traits.Remove(Trait.Manipulate);
                    return metamagicSpell;
                });
            });
        yield return new TrueFeat(AnimistFeat.EmbodimentOfTheBalance, 2,
                "Your place in the balance between the forces of life and entropy expands the spells you can pull from the spirit realms.",
                "You add heal and harm to your apparition spell repertoire, allowing you to cast them with your apparition spellcasting.",
                [AnimistTrait.Animist])
            .WithOnSheet(sheet => sheet.SpellRepertoires[AnimistTrait.Apparition].SpellsKnown.AddRange(
                    from spellid in new List<SpellId> { SpellId.Heal, SpellId.Harm }
                    from spellLevel in Enumerable.Range(1, sheet.MaximumSpellLevel)
                    select AllSpells.CreateModernSpellTemplate(spellid, AnimistTrait.Apparition, spellLevel)
            ));
        /*
        yield return new TrueFeat(AnimistFeat.EnhancedFamiliar, 2,
                "You are able to materialize more of your attuned apparition’s essence, creating a more powerful vessel for it to inhabit and aid you with.",
                "You can select four familiar or master abilities, instead of two.",
                [AnimistTrait.Animist]);
        */
        yield return new TrueFeat(AnimistFeat.GraspingSpiritsSpell, 2,
                "Gaining substance from your magic, your apparitions increase the range of your spells, which then pull your enemy closer.",
                "If the next action you use is to Cast a Spell that has a range and targets one creature, increase that spell’s range by 30 feet. As is standard for increasing spell ranges, if the spell normally has a range of touch, you extend its range to 30 feet. In addition to the normal effects of the spell, your apparitions briefly take on semi-physical forms and attempt to drag the target toward you. The target must attempt a Fortitude saving throw against your spell DC; on a failure, it is pulled up to 30 feet directly toward you.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Concentrate, AnimistTrait.Spellshape])
            .WithActionCost(1)
            .WithPermanentQEffect("You can increase the range of your spells, which then pull your enemy closer.", qe =>
            {
                qe.MetamagicProvider = new MetamagicProvider("Grasping Spirits spell", delegate (CombatAction spell)
                {
                    CombatAction metamagicSpell = Spell.DuplicateSpell(spell).CombatActionSpell;
                    if (metamagicSpell.ActionCost == 3 || metamagicSpell.ActionCost == -2)
                    {
                        return null;
                    }
                    if (!IncreaseTargetLine(metamagicSpell.Target))
                    {
                        return null;
                    }
                    metamagicSpell.Name = "Grasping " + metamagicSpell.Name;
                    CommonSpellEffects.IncreaseActionCostByOne(metamagicSpell);
                    int num3 = metamagicSpell.Target.ToDescription()?.Count((char c) => c == '\n') ?? 0;
                    string[] array2 = metamagicSpell.Description.Split('\n', 4 + num3);
                    if (array2.Length >= 4)
                    {
                        metamagicSpell.Description = array2[0] + "\n" + array2[1] + "\n{Blue}" + metamagicSpell.Target.ToDescription() + "{/Blue}\n" + array2[3 + num3];
                    }
                    metamagicSpell.EffectOnChosenTargets += async (action, caster, targets) =>
                    {
                        foreach (var target in targets.ChosenCreatures)
                        {
                            metamagicSpell.Name = "Grasping Spirits Spell";
                            if (CommonSpellEffects.RollSpellSavingThrow(target, metamagicSpell, Defense.Fortitude) <= CheckResult.Failure)
                            {
                                await caster.PullCreature(target);
                            }
                        }
                    };
                    return metamagicSpell;
                    bool IncreaseTargetLine(Target? targetLine)
                    {
                        if (targetLine == null)
                        {
                            return false;
                        }
                        if (targetLine is CreatureTarget creatureTarget2)
                        {
                            return IncreaseTarget(creatureTarget2);
                        }
                        if (targetLine is DependsOnActionsSpentTarget dependsOnActionsSpentTarget)
                        {
                            return IncreaseTargetLine(dependsOnActionsSpentTarget.IfOneAction) | IncreaseTargetLine(dependsOnActionsSpentTarget.IfTwoActions);
                        }
                        if (targetLine is DependsOnSpellVariantTarget dependsOnSpellVariantTarget)
                        {
                            bool flag2 = false;
                            {
                                foreach (Target target in dependsOnSpellVariantTarget.Targets)
                                {
                                    if (target is CreatureTarget creatureTarget4)
                                    {
                                        flag2 |= IncreaseTarget(creatureTarget4);
                                    }
                                }
                                return flag2;
                            }
                        }
                        return false;
                    }
                    bool IncreaseTarget(CreatureTarget creatureTarget)
                    {
                        if (creatureTarget.RangeKind == RangeKind.Melee)
                        {
                            metamagicSpell.Traits = new Traits(metamagicSpell.Traits.Except([Trait.Melee]).Concat([Trait.Ranged]));
                            creatureTarget.RangeKind = RangeKind.Ranged;
                            creatureTarget.CreatureTargetingRequirements.RemoveAll((CreatureTargetingRequirement ctr) => ctr is AdjacencyCreatureTargetingRequirement || ctr is AdjacentOrSelfTargetingRequirement);
                            creatureTarget.CreatureTargetingRequirements.Add(new MaximumRangeCreatureTargetingRequirement(6));
                            creatureTarget.CreatureTargetingRequirements.Add(new UnblockedLineOfEffectCreatureTargetingRequirement());
                            return true;
                        }
                        MaximumRangeCreatureTargetingRequirement? maximumRangeCreatureTargetingRequirement = creatureTarget.CreatureTargetingRequirements.OfType<MaximumRangeCreatureTargetingRequirement>().FirstOrDefault();
                        if (maximumRangeCreatureTargetingRequirement != null)
                        {
                            maximumRangeCreatureTargetingRequirement.Range += 6;
                            return true;
                        }
                        return false;
                    }
                });
            });
        yield return new TrueFeat(AnimistFeat.SpiritualExpansionSpell, 2,
                "Your apparitions manifest to scatter the energy of your spell.",
                "If the next action you use is to Cast a Spell that has an area of a burst, cone, or line and does not have a duration, increase the area of that spell. Add 5 feet to the radius of a burst that normally has a radius of at least 10 feet (a burst with a smaller radius is not affected). Add 5 feet to the length of a cone or line that is normally 15 feet long or smaller, and add 10 feet to the length of a larger cone or line. You can also use this feat to increase the radius of an emanation spell with a duration by 5 feet by dedicating your primary apparition to maintaining the spellshape; dedicating the apparition to the spell prevents you from using the apparition’s vessel spell, apparition skills, or avatar form for the duration of the modified spell.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Concentrate, AnimistTrait.Spellshape])
            .WithActionCost(1)
            .WithPermanentQEffect("You can increase the area of a spell.", qe =>
            {
                qe.MetamagicProvider = new MetamagicProvider("Spiritual Expansion spell", delegate (CombatAction spell)
                {
                    CombatAction metamagicSpell = Spell.DuplicateSpell(spell).CombatActionSpell;
                    if (metamagicSpell.ActionCost == 3 || metamagicSpell.ActionCost == -2)
                    {
                        return null;
                    }
                    if (!IncreaseTargetLine(metamagicSpell.Target))
                    {
                        return null;
                    }
                    metamagicSpell.Name = "Expansive " + metamagicSpell.Name;
                    CommonSpellEffects.IncreaseActionCostByOne(metamagicSpell);
                    int num3 = metamagicSpell.Target.ToDescription()?.Count((char c) => c == '\n') ?? 0;
                    string[] array2 = metamagicSpell.Description.Split('\n', 4 + num3);
                    if (array2.Length >= 4)
                    {
                        metamagicSpell.Description = array2[0] + "\n" + array2[1] + "\n{Blue}" + metamagicSpell.Target.ToDescription() + "{/Blue}\n" + array2[3 + num3];
                    }
                    return metamagicSpell;
                    bool IncreaseTargetLine(Target? targetLine)
                    {
                        if (targetLine == null)
                        {
                            return false;
                        }
                        if (targetLine is BurstAreaTarget burstAreaTarget)
                        {
                            if (burstAreaTarget.Radius >= 2)
                            {
                                burstAreaTarget.Radius += 1;
                                return true;
                            }
                        }
                        if (targetLine is ConeAreaTarget coneAreaTarget)
                        {
                            if (coneAreaTarget.ConeLength < 4)
                            {
                                coneAreaTarget.ConeLength += 1;
                            }
                            else
                            {
                                coneAreaTarget.ConeLength += 2;
                            }
                            return true;
                        }
                        if (targetLine is LineAreaTarget lineAreaTarget)
                        {
                            if (lineAreaTarget.LineLength < 4)
                            {
                                lineAreaTarget.LineLength += 1;
                            }
                            else
                            {
                                lineAreaTarget.LineLength += 2;
                            }
                            return true;
                        }
                        if (targetLine is EmanationTarget emanation)
                        {
                            //TODO: just set Range when it's writeable
                            //emanation.Range += 1;
                            EmanationTarget newEmanation = new EmanationTarget(emanation.Range + 1, emanation.IncludeSelf)
                            {
                                CreatureGoodness = emanation.CreatureGoodness,
                                AdditionalRequirementOnCaster = emanation.AdditionalRequirementOnCaster
                            };
                            emanation = newEmanation;
                            metamagicSpell.EffectOnChosenTargets += async (spell, caster, targets) =>
                            {
                                caster.AddQEffect(new QEffect()
                                {
                                    Id = AnimistQEffects.PrimaryApparitionBusy,
                                    StateCheck = q =>
                                    {
                                        //TODO: make sure this actually works?
                                        if (spell.ReferencedQEffect != null && !q.Owner.HasEffect(spell.ReferencedQEffect))
                                        {
                                            q.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    }
                                });
                            };
                            return true;
                        }
                        if (targetLine is DependsOnActionsSpentTarget dependsOnActionsSpentTarget)
                        {
                            return IncreaseTargetLine(dependsOnActionsSpentTarget.IfOneAction) | IncreaseTargetLine(dependsOnActionsSpentTarget.IfTwoActions);
                        }
                        if (targetLine is DependsOnSpellVariantTarget dependsOnSpellVariantTarget)
                        {
                            bool flag2 = false;
                            {
                                foreach (Target target in dependsOnSpellVariantTarget.Targets)
                                {
                                    IncreaseTargetLine(target);
                                }
                                return flag2;
                            }
                        }
                        return false;
                    }
                });
            });
    }
}

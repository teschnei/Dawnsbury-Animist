using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using Dawnsbury.Modding;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Apparitions;

public static class Extensions
{
    public static QEffect WithSustaining(this QEffect qe, CombatAction spell, Func<QEffect, Task>? onSustain = null, string? additionalText = null)
    {
        qe.ReferencedSpell = spell;
        spell.ReferencedQEffect = qe;
        qe.ProvideContextualAction = (QEffect qf) => (!qe.CannotExpireThisTurn) ? new ActionPossibility(new CombatAction(qf.Owner, spell.Illustration, "Sustain " + spell.Name, new Trait[4]
        {
            Trait.Concentrate,
            Trait.SustainASpell,
            Trait.Basic,
            Trait.DoesNotBreakStealth
        }, "The duration of " + spell.Name + " continues until the end of your next turn." + ((additionalText == null) ? "" : ("\n\n" + additionalText)), Target.Self((Creature self, AI ai) => ai.ShouldSustain(spell))
        .WithAdditionalRestriction(self =>
        {
            if (!self.Spellcasting!.GetSourceByOrigin(AnimistTrait.Apparition)!.FocusSpells.Exists(sp => sp.SpellId == spell.SpellId))
            {
                return "You do not have the primary apparition to sustain this spell.";
            }
            if (self.HasEffect(AnimistQEffects.PrimaryApparitionBusy))
            {
                return "Your primary apparition is currently busy.";
            }
            return null;
        }))
        .WithReferencedQEffect(qf)
        .WithEffectOnSelf(async delegate (CombatAction action, Creature creature)
        {
            qe.CannotExpireThisTurn = true;
            if (onSustain != null)
            {
                await onSustain(qe);
            }
        })).WithPossibilityGroup("Maintain an activity") : null;
        return qe;
    }

    public static QEffect WithZone(this QEffect qe, ZoneAttachment zoneAttachment, Action<QEffect, Zone>? zoneSetup = null)
    {
        var zone = Zone.Spawn(qe, zoneAttachment);
        zoneSetup?.Invoke(qe, zone);
        return qe;
    }
}

public class Apparition : Feat
{
    public List<Skill> Skills { get; set; }
    public List<SpellId> Spells { get; set; }
    public SpellId VesselSpell { get; set; }
    public Feat AttunedFeat;
    public Apparition(FeatName attunedFeatName, FeatName primaryFeatName, List<SpellId> spells, SpellId vesselSpell, string flavorText) : base(primaryFeatName, flavorText, GenerateRulesText(spells, vesselSpell), [AnimistTrait.ApparitionPrimary], null)
    {
        Skills = new List<Skill>();
        Spells = spells;
        VesselSpell = vesselSpell;
        WithOnSheet(sheet =>
        {
            sheet.AddFocusSpellAndFocusPoint(AnimistTrait.Apparition, Ability.Wisdom, VesselSpell);
        });
        AttunedFeat = new Feat(attunedFeatName, flavorText, GenerateRulesText(spells, vesselSpell), [AnimistTrait.ApparitionAttuned], null)
            .WithOnSheet(sheet =>
            {
                for (var i = 0; i <= sheet.MaximumSpellLevel; ++i)
                {
                    SpellId spellID = Spells[i];
                    // All Apparition Spells are signature spells 
                    if (i > 0)
                    {
                        for (var j = i; j <= sheet.MaximumSpellLevel; ++j)
                        {
                            sheet.SpellRepertoires[AnimistTrait.Apparition].SpellsKnown.Add(AllSpells.CreateModernSpellTemplate(spellID, AnimistTrait.Apparition, j));
                        }
                    }
                    else
                    {
                        // Except cantrips, of course
                        sheet.SpellRepertoires[AnimistTrait.Apparition].SpellsKnown.Add(AllSpells.CreateModernSpellTemplate(spellID, AnimistTrait.Apparition, 0));
                    }
                }
            });
        WithPrerequisite(sheet => sheet.HasFeat(AttunedFeat), "You must be attuned to this apparition.");
    }

    private static string Ordinalize(int lvl)
    {
        if (lvl == 0) return "Cantrip";
        return lvl.Ordinalize2();
    }

    private static string GenerateRulesText(List<SpellId> spells, SpellId VesselSpell)
    {
        string text = "\n{b}Apparition Spells{/b} ";
        for (var i = 0; i < spells.Count; ++i)
        {
            if (spells[i] != SpellId.None)
            {
                text += $"{{b}}{Ordinalize(i)}{{/b}} {AllSpells.CreateModernSpellTemplate(spells[i], AnimistTrait.Animist).ToSpellLink()}; ";
            }
        }
        text += $"\n{{b}}Vessel Spell{{/b}} {AllSpells.CreateModernSpellTemplate(VesselSpell, AnimistTrait.Animist).ToSpellLink()}";
        return text;
    }

    private static SpellId GetSpell(string name, SpellId backup)
    {
        if (ModManager.TryParse<SpellId>(name, out var spellId))
        {
            return spellId;
        }
        return backup;
    }

    public static IEnumerable<Apparition> GetApparitions()
    {
        //TODO: Is Crafter in the Vault even usable in DD?
        /*
        yield return new Apparition(AnimistFeat.CrafterInTheVault, AnimistFeat.CrafterInTheVaultPrimary, "", "")
        {
            Spells = new List<SpellId>()
            {
                //TODO
            },
            VesselSpell = ModManager.RegisterNewSpell("TravelingWorkshop", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Heal,
                    "Traveling Workshop",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Manipulate],
                    "",
                    "",
                    Target.Self(),
                    spellLevel,
                    null
                );
            })
        };
        */
        yield return new Apparition(AnimistFeat.CustodianOfGrovesAndGardens, AnimistFeat.CustodianOfGrovesAndGardensPrimary,
            new List<SpellId>()
            {
                GetSpell("TangleVine", SpellId.Tanglefoot),
                GetSpell("ProtectorTree", SpellId.ProtectorTree),
                GetSpell("GentleBreeze", SpellId.Barkskin),
                GetSpell("SafePassage", SpellId.PositiveAttunement),
                GetSpell("PeacefulBubble", SpellId.TortoiseAndTheHare),
            },
            ModManager.RegisterNewSpell("GardenOfHealing", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Heal,
                    "Garden Of Healing",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Emotion, Trait.Focus, Trait.Healing, Trait.Mental],
                    "Spirits of comfort and respite swirl around you, trailing visions of growing grass and blooming blossoms.",
                    @$"When you cast this spell and the first time you sustain it each subsequent round, you generate a pulse of renewing energy that heals each creature within the emanation for {S.HeightenedVariable(spellLevel, 1)}d4 Hit Points.
The calm of this effect lingers; once this spell ends, any creature that has been affected by its healing gains a +1 circumstance bonus to saves against emotion effects but does not receive any healing from additional castings of the spell while the bonus persists.",
                    Target.Emanation(2),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The healing granted by the spell's pulse increases by 1d4 Hit Points.")
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    if (!target.HasEffect(AnimistQEffects.GardenOfHealingImmunity))
                    {
                        await target.HealAsync($"{spellLevel}d4", spell);
                        target.AddQEffect(new QEffect(ExpirationCondition.Never)
                        {
                            Id = AnimistQEffects.GardenOfHealingHealed
                        });
                    }
                })
                .WithEffectOnSelf(async (spell, caster) =>
                {
                    var qe = new QEffect(spell.Name, "A Garden Of Healing restores hit points to allies in range when you Sustain it.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, spell.Illustration)
                    {
                        WhenExpires = (qe) =>
                        {
                            foreach (Creature cr in qe.Owner.Battle.AllCreatures.Where((Creature cr) => cr.HasEffect(AnimistQEffects.GardenOfHealingHealed)))
                            {
                                cr.RemoveAllQEffects(qe => qe.Id == AnimistQEffects.GardenOfHealingHealed);
                                cr.AddQEffect(new QEffect(ExpirationCondition.Never)
                                {
                                    Id = AnimistQEffects.GardenOfHealingImmunity,
                                    BonusToDefenses = (qf, spell, defense) => (spell?.HasTrait(Trait.Emotion) ?? false) ? new Bonus(1, BonusType.Circumstance, "Garden Of Healing") : null
                                });
                            }
                        },
                        CannotExpireThisTurn = true,
                        SpawnsAura = (qe) => caster.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle, 2, Color.Green)
                    }.WithSustaining(spell, async (qe) =>
                    {
                        foreach (Creature cr in qe.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qe.Owner) <= 2 && !cr.HasTrait(Trait.Object) && !cr.HasEffect(AnimistQEffects.GardenOfHealingImmunity)))
                        {
                            await cr.HealAsync($"{spellLevel}d4", spell);
                            cr.AddQEffect(new QEffect(ExpirationCondition.Never)
                            {
                                Id = AnimistQEffects.GardenOfHealingHealed
                            });
                        }
                    }, $"When you Sustain this spell, you generates a pulse of renewing energy that heals each creature within the emanation for {S.HeightenedVariable(spellLevel, 1)}d4 Hit Points.");
                    caster.AddQEffect(qe);
                });
            }), "Custodians of groves and gardens frequent tended greenery and farmlands cared for by loving stewards, and other places of reflection and restoration where green things grow. Some of these apparitions linger in the mortal realms not because they have lost their way, but because they believe they have already found Elysium. Others are the cultivated spiritual essence of the location itself. Custodians of groves and gardens are peaceful, quiet, and averse to conflict.Custodians of groves and gardens frequent tended greenery and farmlands cared for by loving stewards, and other places of reflection and restoration where green things grow. Some of these apparitions linger in the mortal realms not because they have lost their way, but because they believe they have already found Elysium. Others are the cultivated spiritual essence of the location itself. Custodians of groves and gardens are peaceful, quiet, and averse to conflict.");
        yield return new Apparition(AnimistFeat.EchoOfLostMoments, AnimistFeat.EchoOfLostMomentsPrimary,
            new List<SpellId>()
            {
                GetSpell("Figment", SpellId.OpenDoor),
                GetSpell("DejaVu", SpellId.Command),
                GetSpell("DispelMagic", SpellId.LooseTimesArrow),
                GetSpell("CurseOfLostTime", SpellId.CurseOfLostTime),
                GetSpell("VisionOfDeath", SpellId.PhantasmalKiller),
            },
            ModManager.RegisterNewSpell("StoreTime", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.TimeStop,
                    "Store Time",
                    [AnimistTrait.Animist, Trait.Focus],
                    "You store time for later use.",
                    "When you Cast this Spell and the first time you Sustain it each round, you gain a bonus reaction that you can use for any animist or apparition reaction you have.",
                    Target.Self(null),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    var qe = new QEffect(spell.Name, "Storing time for a bonus reaction for any animist or apparition reaction.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                    {
                        CannotExpireThisTurn = true,
                        StartOfYourEveryTurn = async (q, self) =>
                        {
                            q.Tag = null;
                        }
                    }.WithSustaining(spell);
                    caster.AddQEffect(qe);
                });
            }), "Echoes of lost moments are apparitions born from memories that everyone has forgotten, often arising from fragmented pieces of magic and memory left behind by time-altering magic. They may even occur in response to significant temporal tampering, cleaning up fragments of time damaged by irresponsible magic. These apparitions are drawn to animists who are orderly and responsible, and they can give such hosts access to spells that alter a target’s timeline or removes them from the current timeline, reveal visions of past or future events, or even accelerate magical effects to a point in time where they have already ended.");
        yield return new Apparition(AnimistFeat.ImposterInHiddenPlaces, AnimistFeat.ImposterInHiddenPlacesPrimary,
            new List<SpellId>()
            {
                GetSpell("TelekineticHand", SpellId.TelekineticProjectile),
                GetSpell("IllOmen", SpellId.IllOmen),
                GetSpell("Invisibility", SpellId.Invisibility),
                GetSpell("VeilOfPrivacy", SpellId.ImpendingDoom),
                GetSpell("LiminalDoorway", SpellId.DimensionDoor),
            },
            ModManager.RegisterNewSpell("DiscomfitingWhispers", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Bane,
                    "Discomfiting Whispers",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Focus, Trait.Misfortune, Trait.Negative],
                    "You are surrounded by an aura of spiteful murmuring that incite bad luck and punish failure.",
                    $"Each creature that starts their turn within the area of this spell must succeed at a Will save or roll twice on their first attack roll that round and take the lower result. If an attack roll modified in this way results in a failure, the creature that rolled the failed attack takes {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d6 damage.",
                    Target.Emanation(1),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 2, "The void damage dealt on a failure increases by 1d6.")
                .WithEffectOnSelf(async (spell, caster) =>
                {
                    var aura = caster.AnimationData.AddAuraAnimation(IllustrationName.BaneCircle, 1);
                    aura.Color = Color.Purple;
                    var qe = new QEffect(spell.Name, "An aura of spiteful murmurings may cause nearby creatures to reroll their first attack roll.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, spell.Illustration)
                    {
                        WhenExpires = qe => aura.MoveTo(0),
                        CannotExpireThisTurn = true
                    }.WithSustaining(spell, async (qe) =>
                    {
                        ApplyQEffect(spell, qe);
                    });
                    caster.AddQEffect(qe);
                    ApplyQEffect(spell, qe);
                    void ApplyQEffect(CombatAction spell, QEffect whispersQE)
                    {
                        var qe = new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                        {
                            Id = AnimistQEffects.DiscomfitingWhispersStartTurn,
                            StartOfYourPrimaryTurn = async (q, cr) =>
                            {
                                if (cr.DistanceTo(whispersQE.Owner) <= 1)
                                {
                                    CheckResult result = CommonSpellEffects.RollSavingThrow(cr, spell, Defense.Will, spell.SpellcastingSource?.GetSpellSaveDC() ?? 0);
                                    if (result <= CheckResult.Failure)
                                    {
                                        cr.AddQEffect(new QEffect("Discomfiting Whispers", "Discomfiting Whispers has caused you to reroll your first attack roll and take damage if it fails.", ExpirationCondition.ExpiresAtEndOfYourTurn, whispersQE.Owner, IllustrationName.Bane)
                                        {
                                            RerollActiveRoll = async (qe, result, action, target) => action.HasTrait(Trait.Attack) ? RerollDirection.RerollAndKeepWorst : RerollDirection.DoNothing,
                                            AfterYouMakeAttackRoll = (qe, result) =>
                                            {
                                                if (result.CheckResult <= CheckResult.Failure)
                                                {
                                                    CommonSpellEffects.DealDirectDamage(spell, DiceFormula.FromText($"{(spellLevel + 1) / 2}d6"), qe.Owner, CheckResult.Failure, DamageKind.Negative);
                                                }
                                                qe.ExpiresAt = ExpirationCondition.Immediately;
                                            }
                                        });
                                    }
                                }
                            }
                        };
                        foreach (Creature cr in whispersQE.Owner.Battle.AllCreatures.Where((Creature cr) => !cr.HasTrait(Trait.Object)))
                        {
                            cr.AddQEffect(qe);
                        }
                    }
                });
            }), "Impostors in hidden places whisper in quiet corners where mortal voices rarely resound, hoarding secrets and pondering unknowable truths. They often bring misfortune to those who disturb them, though an animist who earns their trust will find that they make effective allies.");
        yield return new Apparition(AnimistFeat.LurkerInDevouringDark, AnimistFeat.LurkerInDevouringDarkPrimary,
            new List<SpellId>()
            {
                GetSpell("CausticBlast", SpellId.AcidSplash),
                GetSpell("GrimTendrils", SpellId.GrimTendrils),
                GetSpell("AcidGrip", SpellId.AcidArrow),
                GetSpell("AqueousOrb", SpellId.SeaOfThought),
                GetSpell("GraspOfTheDeep", SpellId.PhantasmalKiller),
            },
            ModManager.RegisterNewSpell("DevouringDarkForm", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Tentacle,
                    "Devouring Dark Form",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Focus, Trait.Morph],
                    "Your apparition's dark power blends with your physical body, allowing you to take on terrifying characteristics of creatures that lurk in dark places.",
                    $"Your arms and legs transform into twisting tentacles. You gain a tentacle unarmed attack with 10-foot reach that deals 1d8 bludgeoning damage and has the grapple trait. The first time you Sustain this spell each round, you can attempt a single Grapple check with your tentacle against a creature within its reach.",
                    Target.DependsOnSpellVariant(variant =>
                    {
                        SelfTarget selfTarget = Target.Self();
                        if (variant.Id == "Shark")
                        {
                            selfTarget.WithAdditionalRestriction((Creature self) => (!self.HasEffect(QEffectId.AquaticCombat)) ? "You can't transform into a shark above water." : null);
                        }
                        return selfTarget;
                    }),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [2], ["You can choose to take on the shark battle form from {i}animal form{/i} instead of gaining a tentacle unarmed attack, heightened to the same level as this vessel spell. When you do, this spell loses the morph trait and gains the polymorph trait. You can attempt a jaws unarmed Strike against a creature within your reach each time you Sustain this spell."])
                .WithVariants(new SpellVariant[] {
                        new SpellVariant("Tentacle", "Tentacle Form", IllustrationName.Tentacle),
                        new SpellVariant("Shark", "Shark Form", IllustrationName.AnimalFormShark)
                    }.Where(v => v.Id == "Tentacle" || (v.Id == "Shark" && spellLevel >= 2)).ToArray())
                .WithCreateVariantDescription((_, variant) =>
                {
                    if (variant?.Id == "Tentacle")
                    {
                        return "You gain a tentacle unarmed attack with 10-foot reach that deals 1d8 bludgeoning damage and has the grapple trait. The first time you Sustain this spell each round, you can attempt a single Grapple check with your tentacle against a creature within its reach.";
                    }
                    else if (variant?.Id == "Shark")
                    {
                        return "You take on the shark battle form from {i}animal form{/i}, heightened to the same level as this vessel spell. You can attempt a jaws unarmed Strike against a creature within your reach each time you Sustain this spell.";
                    }
                    return "";
                })
                .WithEffectOnEachTarget(async (spell, caster, target, _) =>
                {
                    SpellVariant? variant = spell.ChosenVariant;
                    if (variant?.Id == "Tentacle")
                    {
                        var qe = new QEffect(spell.Name, "Your apparition's dark power has granted you twisting tentacle appendages.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                        {
                            CannotExpireThisTurn = true,
                            //TODO: grapple trait?
                            AdditionalUnarmedStrike = CommonItems.CreateNaturalWeapon(IllustrationName.Tentacle, "Tentacle", "1d8", DamageKind.Bludgeoning, [Trait.Reach])
                        }.WithSustaining(spell, async q =>
                        {
                            Creature? target = await q.Owner.Battle.AskToChooseACreature(q.Owner,
                                    q.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(q.Owner) <= 2 && cr.EnemyOf(q.Owner) && !cr.HasTrait(Trait.Object)),
                    IllustrationName.Tentacle, "Target a creature to attempt to grapple.", "Click to attempt a grapple on this target.", "Skip grapple attempt");
                            if (target != null)
                            {
                                var grapple = new CombatAction(q.Owner, IllustrationName.Grapple, "Grapple", new Trait[4]
                                {
                                    Trait.Attack,
                                    Trait.Basic,
                                    Trait.AttackDoesNotTargetAC,
                                    Trait.Restraining
                                }, "", Target.Reach(q.AdditionalUnarmedStrike!).WithAdditionalConditionOnTargetCreature(new GrappleCreatureTargetingRequirement())).WithItem(q.AdditionalUnarmedStrike!).WithActionId(ActionId.Grapple).WithSoundEffect(SfxName.Grapple)
                                    .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics), TaggedChecks.DefenseDC(Defense.Fortitude)))
                                    .WithEffectOnEachTarget(async delegate (CombatAction action, Creature grappler, Creature target, CheckResult checkResult)
                                    {
                                        await Possibilities.Grapple(grappler, target, checkResult);
                                    });
                                grapple.ChosenTargets = ChosenTargets.CreateSingleTarget(target);
                                await grapple.WithActionCost(0).AllExecute();
                            }
                        }, "When you Sustain this spell, you can attempt a single Grapple check with your tentacle against a creature within its reach.");
                        target.AddQEffect(qe);
                    }
                    else if (variant?.Id == "Shark")
                    {
                        int temporaryHP = spellLevel * 5 - 5;
                        int athletics = ((spellLevel == 5) ? 20 : ((spellLevel == 4) ? 16 : ((spellLevel == 3) ? 14 : 9)));
                        int attackModifier = ((spellLevel == 5) ? 18 : ((spellLevel == 4) ? 16 : ((spellLevel == 3) ? 14 : 9)));
                        int damageBonus = ((spellLevel >= 4) ? 9 : ((spellLevel != 3) ? 1 : 5));
                        int acBase = ((spellLevel >= 4) ? 18 : ((spellLevel == 3) ? 17 : 16));
                        int ac = acBase + caster.ProficiencyLevel;
                        caster.GainTemporaryHP(temporaryHP);
                        QEffect form = CommonSpellEffects.EnterBattleform(caster, IllustrationName.AnimalFormShark, ac, 7, false);
                        QEffect qEffect8 = form;
                        qEffect8.ProvideActionIntoPossibilitySection = null;
                        qEffect8.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                        qEffect8.WithSustaining(spell, async q =>
                        {
                            Creature? target = await q.Owner.Battle.AskToChooseACreature(q.Owner,
                                    q.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(q.Owner) <= 1 && cr.EnemyOf(q.Owner) && !cr.HasTrait(Trait.Object)),
                    IllustrationName.Tentacle, "Target a creature to attempt a jaws strike.", "Click to attempt a Strike on this target.", "Skip jaws Strike");
                            if (target != null)
                            {
                                var strike = q.Owner.CreateStrike(q.Owner.UnarmedStrike);
                                strike.ChosenTargets = ChosenTargets.CreateSingleTarget(target);
                                await strike.WithActionCost(0).AllExecute();
                            }
                        }, "When you Sustain this spell, you can attempt a jaws unarmed Strike against a creature within your reach.");
                        qEffect8.StateCheck = (Action<QEffect>)Delegate.Combine(qEffect8.StateCheck, (Action<QEffect>)delegate (QEffect qfForm)
                        {
                            Item replacementUnarmedStrike = CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "2d8", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                            {
                                wp.FixedDamageBonus = damageBonus;
                            });
                            Creature owner2 = qfForm.Owner;
                            owner2.ReplacementUnarmedStrike = replacementUnarmedStrike;
                        });
                        form.BattleformMinimumStrikeModifier = attackModifier;
                        form.BattleformMinimumAthleticsModifier = athletics;
                        QEffect swimmingEffect = QEffect.Swimming();
                        bool hasTemporaryAquaticTrait = false;
                        if (!form.Owner.HasTrait(Trait.Aquatic))
                        {
                            form.Owner.Traits.Add(Trait.Aquatic);
                            hasTemporaryAquaticTrait = true;
                        }
                        form.WhenExpires = delegate
                        {
                            swimmingEffect.ExpiresAt = ExpirationCondition.Immediately;
                            if (hasTemporaryAquaticTrait)
                            {
                                form.Owner.Traits.Remove(Trait.Aquatic);
                            }
                        };
                        form.Owner.AddQEffect(swimmingEffect);
                    }
                });
            }), "Lurkers in devouring dark are most often near old shipwrecks, deadly icebergs, and other places where ice and deep water are most prevalent.");
        yield return new Apparition(AnimistFeat.MonarchOfTheFeyCourts, AnimistFeat.MonarchOfTheFeyCourtsPrimary,
            new List<SpellId>()
            {
                GetSpell("TangleVine", SpellId.Tanglefoot),
                GetSpell("Charm", SpellId.Command),
                GetSpell("CreateFood", SpellId.Glitterdust),
                GetSpell("Enthrall", SpellId.RoaringApplause),
                GetSpell("Suggestion", SpellId.Sleep),
            },
            ModManager.RegisterNewSpell("NymphsGrace", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.DemonMask,
                    "Nymph's Grace",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Emotion, Trait.Focus, Trait.Incapacitation, Trait.Mental, Trait.Visual],
                    "Your apparition manifests as a mask of unearthly beauty that bewilders your enemies.",
                    "The first time an enemy enters the aura each round, or if they start their turn within the aura, they must succeed at a Will saving throw or become confused for 1 round. While confused by this effect, the creature's confused actions never include harming you.",
                    Target.Emanation(2),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithEffectOnSelf(async (spell, self) =>
                {
                    self.AddQEffect(new QEffect(spell.Name, "Enemies must make a Will saving throw when entering the aura or become confused.", ExpirationCondition.ExpiresAtEndOfYourTurn, self, spell.Illustration)
                    {
                        CannotExpireThisTurn = true,
                        PreventTargetingBy = action =>
                        {
                            if (action.Owner.QEffects.Any(q => q.Id == QEffectId.Confused && q.SourceAction == spell) && action.IsHostileAction)
                            {
                                return "Enthralled by Nymph's Grace";
                            }
                            return null;
                        },
                        SpawnsAura = q => q.Owner.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle, 2, Color.Plum)
                    }
                    .WithSustaining(spell)
                    .WithZone(ZoneAttachment.Aura(2), (qe, zone) =>
                    {
                        zone.TileEffectCreator = (Tile tl) => new TileQEffect(tl).WithOncePerRoundEffectWhenCreatureBeginsTurnOrEnters(zone, async cr =>
                        {
                            if (cr == cr.Battle.ActiveCreature && cr.EnemyOf(qe.Owner))
                            {
                                CheckResult result = CommonSpellEffects.RollSpellSavingThrow(cr.Battle.ActiveCreature, spell, Defense.Will);
                                if (result < CheckResult.Success)
                                {
                                    var confused = QEffect.Confused(false, spell)
                                            .WithExpirationAtStartOfSourcesTurn(qe.Owner, 1)
                                            .WithSourceAction(spell);
                                    confused.Description = "You're flat-footed, you can't use reactions and you can't cast non-cantrip spells.\n\nYou consider everyone to be your enemy except the source and you use all your actions to attack other creatures.\n\nYou still provide flanking to creatures who consider you an ally.";
                                    cr.Battle.ActiveCreature.AddQEffect(confused);
                                }
                                return true;
                            }
                            return false;
                        });
                    }))
                    ;
                });
            }), "Monarchs of the fey courts make their homes near places with strong ties to the First World, or in places where nymphs once held sway. They are drawn to animists who blend an appreciation for art and nature’s beauty with a ruler’s ambition. Monarchs of fey courts are vain, capricious, and do not easily forgive slights or poor manners.");
        yield return new Apparition(AnimistFeat.RevelerInLostGlee, AnimistFeat.RevelerInLostGleePrimary,
            new List<SpellId>()
            {
                GetSpell("Prestidigitation", SpellId.TelekineticProjectile),
                GetSpell("DizzyingColors", SpellId.ColorSpray),
                GetSpell("LaughingFit", SpellId.HideousLaughter),
                GetSpell("Hypnotize", SpellId.ImpendingDoom),
                GetSpell("Confusion", SpellId.Confusion),
            },
            ModManager.RegisterNewSpell("TrickstersMirrors", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.MirrorImage,
                    "Trickster's Mirrors",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Illusion, Trait.Mental, Trait.Visual],
                    "You are surrounded by mirrors that reflect twisted and distorted images of you.",
                    $"You start with 1 mirror and gain an additional mirror each time you Sustain this spell, up to a maximum of 3 mirrors. Any attack that would hit you has a random chance of hitting one of your mirrors instead of you. With one mirror, the chances are 1 in 2 (1–3 on 1d6). With two mirrors, there is a 1 in 3 chance of hitting you (1–2 on 1d6). With three mirrors, there is a 1 in 4 chance of hitting you (1 on 1d4). Once an image is hit, it is destroyed. If an attack roll fails to hit your AC but doesn’t critically fail, it destroys a mirror. If the attacker was within 5 feet, they must succeed at a basic Will save or take {S.HeightenedVariable(spellLevel, 1)}d4 mental damage as they believe themselves cut by a shower of glass shards from the breaking mirror. A damaging effect that affects all targets within your space (such as caustic blast) destroys all of the mirrors.",
                    Target.Self(null),
                    spellLevel,
                    null
                )
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The mental damage dealt by a broken mirror increases by 1d4.")
                .WithActionCost(1)
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    var qe = new QEffect(spell.Name, "If you have a mirror and when somebody attacks you:\n• If it's a miss, it destroys one of the mirrors.\n• If it's a hit, it hits you or one of the mirrors at random. If it hits a mirror, it destroys that mirror but counts as a miss against you.\n• If it's a critical hit, it destroys a mirror and counts as a hit against you.\nIn any case a mirror is destroyed, if the attacker was within 5 feet, they must succeed a Will save or take damage from the illusion shattering.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                    {
                        Id = AnimistQEffects.TrickstersMirrors,
                        CannotExpireThisTurn = true
                    }.WithSustaining(spell, async q =>
                    {
                        if (q.Owner.QEffects.Where(q => q.Id == QEffectId.MirrorImage && q.ReferencedSpell == spell).FirstOrDefault() is { } qe)
                        {
                            qe.Value = Math.Min(qe.Value + 1, 3);
                        }
                        else
                        {
                            q.Owner.AddQEffect(GetMirrorImage());
                        }
                    },
                    "When you Sustain this spell, you gain an additional mirror, up to a maximum of 3 mirrors.");
                    caster.AddQEffect(qe);
                    caster.AddQEffect(GetMirrorImage());
                    QEffect GetMirrorImage()
                    {
                        return new QEffect(ExpirationCondition.Never)
                        {
                            Id = QEffectId.MirrorImage,
                            SpawnsAura = q => new MirrorImageAuraAnimation(),
                            Value = 1,
                            ReferencedSpell = spell,
                            StateCheck = q =>
                            {
                                if (q.Value == 0)
                                {
                                    q.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            },
                            AfterYouAreTargeted = async (qe, action) =>
                            {
                                if (action.HasTrait(Trait.Attack) && action.CheckResult == CheckResult.Failure && action.Owner.DistanceTo(qe.Owner) <= 1)
                                {
                                    CheckResult saveResult = CommonSpellEffects.RollSpellSavingThrow(action.Owner, spell, Defense.Will);
                                    await CommonSpellEffects.DealBasicDamage(spell, qe.Owner, action.Owner, saveResult, $"{spellLevel}d4", DamageKind.Mental);
                                }
                            }
                        };
                    }
                });
            }), "Revelers in lost glee are twisted apparitions that arise in desolate and abandoned places where people once found great joy. They take immense mirth in causing harm or discomfort to others and do not enjoy being attuned to animists who fail to laugh at their antics.");
        yield return new Apparition(AnimistFeat.StalkerInDarkenedBoughs, AnimistFeat.StalkerInDarkenedBoughsPrimary,
            new List<SpellId>()
            {
                GetSpell("GougingClaw", SpellId.PhaseBolt),
                GetSpell("RunicBody", SpellId.MagicFang),
                GetSpell("VomitSwarm", SpellId.BoneSpray),
                GetSpell("WallOfThorns", SpellId.StinkingCloud),
                GetSpell("BestialCurse", SpellId.BestowCurse),
            },
            ModManager.RegisterNewSpell("DarkenedForestForm", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                int AC = spellLevel >= 4 ? 18 : spellLevel >= 3 ? 17 : 16;
                int tempHP = spellLevel >= 4 ? 15 : spellLevel >= 3 ? 10 : 5;
                int attackBonus = spellLevel >= 4 ? 16 : spellLevel >= 3 ? 14 : 9;
                int damageBonus = spellLevel >= 4 ? 9 : spellLevel >= 3 ? 5 : 1;
                SpellVariant[] variants = new SpellVariant[] {
                    new SpellVariant("insect", "Insect", IllustrationName.InsectFormPortrait),
                    new SpellVariant("bear", "Bear", IllustrationName.AnimalFormBear),
                    new SpellVariant("canine", "Canine", IllustrationName.AnimalFormWolf),
                    new SpellVariant("snake", "Snake", IllustrationName.AnimalFormSnake),
                    new SpellVariant("cat", "Cat", IllustrationName.AnimalFormCat),
                    new SpellVariant("shark", "Shark", IllustrationName.AnimalFormShark),
                    new SpellVariant("ape", "Ape", IllustrationName.AnimalFormApe)
                }.Where(v => v.Id == "insect" || spellLevel >= 2).ToArray();
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.WildShape,
                    "Darkened Forest Form",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Polymorph],
                    "Your apparition casts a feral shadow over your form.",
                    "You can polymorph into any form listed in {i}insect form{/i}. When you transform into a form granted by a spell, you gain all the effects of the form you chose from a version of the spell heightened to {i}darkened forest form{/i}’s rank. Each time you Sustain this Spell, you can choose to change to a different shape from those available via any of the associated spells.",
                    Target.Self(null),
                    spellLevel,
                    null
                )
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [2], ["You can also transform into the forms listed in {i}animal form{/i}."])
                .WithActionCost(1)
                .WithVariants(variants)
                .WithCreateVariantDescription((_, variant) => variant!.Id switch
                {
                    "insect" => "You enter a battleform. Until you dismiss the spell:\r\n• Your AC is {Blue} 15+your level.{/Blue}\r\n• Your Speed is 20 feet" + ((spellLevel >= 4) ? "{Blue} and you have flying.{/Blue}" : ".") + "\r\n• You have a sting unarmed attack with a minimum attack modifier of +7, which deals 1d4 piercing damage and applies insect venom (DC 14; {i}Stage 1{/i} 1d6 poison damage; {i}Stage 2{/i} 1d8 poison damage and flat-footed; {i}Stage 3{/i} 1d12 poison damage, clumsy 1, and flat-footed). You can use your own unarmed attack modifier instead of +7 if it's higher.\r\n• You have weakness 5 to bludgeoning, piercing and slashing damage.\r\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "bear" => $"You take on the bear battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 30 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attacks are jaws (2d8+{S.HeightenedVariable(damageBonus, 1)} piercing); claw (agile, 1d8+{S.HeightenedVariable(damageBonus, 1)} slashing).\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "canine" => $"You take on the canine battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 40 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attack is jaws (2d8+{S.HeightenedVariable(damageBonus, 1)} piercing).\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "snake" => $"You take on the snake battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 20 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attack is fangs (2d4+" + S.HeightenedVariable(damageBonus, 1) + " piercing plus 1d6 poison).\n• You have a swim speed equal to your Speed.\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "cat" => $"You take on the cat battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 40 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attacks are jaws (2d6+{S.HeightenedVariable(damageBonus, 1)} piercing); claw (agile, 1d10+{S.HeightenedVariable(damageBonus, 1)} slashing).\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "shark" => $"You take on the shark battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 35 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attack is jaws (2d8+" + S.HeightenedVariable(damageBonus, 1) + " piercing).\n• You're aquatic and have a swim speed.\n• You can't cast spells, use weapons or perform manipulate actions.",
                    "ape" => $"You take on the ape battle form. Until you dismiss the spell:\n• Your AC is {{Blue}}{S.HeightenedVariable(AC, 16)}+your level{{/Blue}}.\n• Your Athletics modifier becomes {S.HeightenedVariable(attackBonus, 9)}, unless yours is already higher.\n• Your speed is 25 feet.\n• Your minimum attack modifier is +{S.HeightenedVariable(attackBonus, 9)}. You can use your own unarmed attack modifier instead if it's higher.\n• Your attack is fist (2d6+" + S.HeightenedVariable(damageBonus, 1) + " bludgeoning).\n• You can't cast spells, use weapons or perform manipulate actions.",
                    _ => ""
                })
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    ApplyDarkenedForestForm(spell, spell.ChosenVariant!, caster);
                });
                void ApplyDarkenedForestForm(CombatAction spell, SpellVariant variant, Creature caster)
                {
                    caster.RemoveAllQEffects(qe => qe.ReferencedSpell == spell);
                    Illustration illustration = variant.Illustration;
                    int speed;
                    switch (variant.Id)
                    {
                        case "bear":
                            speed = 6;
                            break;
                        case "canine":
                        case "cat":
                            speed = 8;
                            break;
                        case "insect":
                        case "snake":
                            speed = 4;
                            break;
                        case "shark":
                            speed = 7;
                            break;
                        case "ape":
                            speed = 5;
                            break;
                        default:
                            throw new ArgumentException("Unknown variant.");
                    }
                    int ac = variant.Id == "insect" ? 15 + caster.ProficiencyLevel : AC + caster.ProficiencyLevel;
                    QEffect form = CommonSpellEffects.EnterBattleform(caster, illustration, ac, speed, false);
                    form.ReferencedSpell = spell;
                    form.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                    form.CannotExpireThisTurn = true;
                    form.WithSustaining(spell, async qe =>
                    {
                        var choice = await qe.Owner.AskForChoiceAmongButtons(spell.Illustration, "Change into a different shape?", [.. from variant in variants select variant.Name, "Stay in current form"]);
                        var new_variant = variants.Where(variant => variant.Name == choice.Caption).First();
                        ApplyDarkenedForestForm(spell, new_variant, caster);
                    }, "When you Sustain this spell, you can choose to change into a different shape from those available via any of the associated spells.");
                    form.StateCheck = (Action<QEffect>)Delegate.Combine(form.StateCheck, (Action<QEffect>)delegate (QEffect qfForm)
                    {
                        if (variant.Id == "insect")
                        {
                            qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Bludgeoning, 5);
                            qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Slashing, 5);
                            qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Piercing, 5);
                            qfForm.Owner.ReplacementUnarmedStrike = CommonItems.CreateNaturalWeapon(IllustrationName.Horn, "sting", "1d4", DamageKind.Piercing, Trait.AddsInjuryPoison, Trait.FixedDamageBonusIs0, Trait.BattleformAttack);
                            qfForm.Owner.AddQEffect(Affliction.CreateInjuryQEffect(Affliction.CreateSnakeVenom("Insect Venom")).WithExpirationEphemeral());
                            if (spellLevel >= 4)
                            {
                                qfForm.Owner.AddQEffect(QEffect.Flying().WithExpirationEphemeral());
                            }
                        }
                        else
                        {
                            Item replacementUnarmedStrike = variant.Id switch
                            {
                                "bear" => CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "2d8", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                "canine" => CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "2d8", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                "snake" => CommonItems.CreateNaturalWeapon(IllustrationName.Fang, "fangs", "2d4", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.WithAdditionalDamage("1d6", DamageKind.Poison);
                                }).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                "cat" => CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "2d6", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                "shark" => CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "2d8", DamageKind.Piercing, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                "ape" => CommonItems.CreateNaturalWeapon(IllustrationName.Fist, "fist", "2d6", DamageKind.Bludgeoning, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                                {
                                    wp.FixedDamageBonus = damageBonus;
                                }),
                                _ => throw new ArgumentOutOfRangeException(),
                            };
                            qfForm.Owner.ReplacementUnarmedStrike = replacementUnarmedStrike;
                        }
                    });
                    form.BattleformMinimumStrikeModifier = attackBonus;
                    form.BattleformMinimumAthleticsModifier = attackBonus;
                    if (variant.Id == "bear")
                    {
                        form.AdditionalUnarmedStrike = CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "claw", "1d8", DamageKind.Slashing, Trait.Agile, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                        {
                            wp.FixedDamageBonus = damageBonus;
                        });
                    }
                    if (variant.Id == "cat")
                    {
                        form.AdditionalUnarmedStrike = CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "claw", "1d10", DamageKind.Slashing, Trait.Agile, Trait.BattleformAttack).WithAdditionalWeaponProperties(delegate (WeaponProperties wp)
                        {
                            wp.FixedDamageBonus = damageBonus;
                        });
                    }
                    if (variant.Id == "snake" || variant.Id == "shark")
                    {
                        QEffect swimmingEffect = QEffect.Swimming();
                        bool hasTemporaryAquaticTrait = false;
                        if (variant.Id == "shark" && !form.Owner.HasTrait(Trait.Aquatic))
                        {
                            form.Owner.Traits.Add(Trait.Aquatic);
                            hasTemporaryAquaticTrait = true;
                        }
                        form.WhenExpires = delegate
                        {
                            swimmingEffect.ExpiresAt = ExpirationCondition.Immediately;
                            if (hasTemporaryAquaticTrait)
                            {
                                form.Owner.Traits.Remove(Trait.Aquatic);
                            }
                        };
                        form.Owner.AddQEffect(swimmingEffect);
                    }
                }
            }), "Stalkers in darkened boughs make their homes in ancient forests and jungles unfriendly to humanoids and others who would exert control or influence over nature’s designs. These apparitions are drawn to animists who harbor violent thoughts or impulses but are more likely to linger with animists who can quell their hatred. Stalkers in darkened boughs are moody, impulsive, and prone to seeing things from the least charitable perspective.");
        yield return new Apparition(AnimistFeat.StewardOfStoneAndFire, AnimistFeat.StewardOfStoneAndFirePrimary,
            new List<SpellId>()
            {
                GetSpell("Ignition", SpellId.ProduceFlame),
                GetSpell("InterposingEarth", SpellId.PummelingRubble),
                GetSpell("ExplodingEarth", SpellId.FlamingSphere),
                GetSpell("Fireball", SpellId.Fireball),
                GetSpell("WallOfFire", SpellId.WallOfFire),
            },
            ModManager.RegisterNewSpell("EarthsBile", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.FireRay,
                    "Earth's Bile",
                    [AnimistTrait.Animist, Trait.Earth, Trait.Fire, Trait.Focus],
                    "Your apparition is the will of lava and magma made manifest, the earth’s molten blood unleashing devastating bursts of liquid stone and unquenchable fire at your command.",
                    $"When you Cast this Spell and the first time you Sustain it each round thereafter, choose an area within range. Each creature in the area takes {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 fire damage, {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 bludgeoning damage, and {S.HeightenedVariable((spellLevel + 1) / 2, 1)} persistent fire damage (the persistent fire damage is negated on a successful save).",
                    Target.Burst(6, 2),
                    spellLevel,
                    SpellSavingThrow.Basic(Defense.Reflex)
                )
                .WithHeighteningNumerical(spellLevel, 1, inCombat, 2, "The fire and bludgeoning damage each increase by 1d4, and the persistent fire damage increases by 1.")
                .WithActionCost(1)
                .WithEffectOnSelf(async (spell, self) =>
                {
                    self.AddQEffect(new QEffect(spell.Name, "Your apparition unleashes stone and fire whenever you Sustain this spell.", ExpirationCondition.ExpiresAtEndOfYourTurn, self, spell.Illustration)
                    {
                        CannotExpireThisTurn = true,
                        ProvideContextualAction = (QEffect qf) => (!qf.CannotExpireThisTurn) ? new ActionPossibility(new CombatAction(qf.Owner, spell.Illustration, "Sustain " + spell.Name,
                        [
                            Trait.Concentrate,
                            Trait.SustainASpell,
                            Trait.Basic,
                            Trait.Spell
                        ],
                        $"The duration of {spell.Name} continues until the end of your next turn.\n\nWhen you Sustain this spell, you choose another area to deal {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 fire damage, {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 bludgeoning damage, and {S.HeightenedVariable((spellLevel + 1) / 2, 1)} persistent fire damage.",
                        Target.Burst(6, 2))
                        {
                            SpellId = spell.SpellId,
                            SpellcastingSource = spell.SpellcastingSource
                        }
                        .WithProjectileCone(spell.Illustration, 15, Core.Animations.ProjectileKind.Cone)
                        .WithReferencedQEffect(qf)
                        .WithSpellSavingThrow(Defense.Reflex)
                        .WithEffectOnSelf(async (action, creature) =>
                        {
                            qf.CannotExpireThisTurn = true;
                        })
                        .WithEffectOnEachTarget(ApplyDamage)
                        ).WithPossibilityGroup("Maintain an activity") : null
                    });
                })
                .WithEffectOnEachTarget(ApplyDamage);
                async Task ApplyDamage(CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, $"{(spellLevel + 1) / 2}d4", DamageKind.Fire);
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, $"{(spellLevel + 1) / 2}d4", DamageKind.Bludgeoning);
                    await CommonSpellEffects.DealBasicPersistentDamage(target, checkResult, $"{(spellLevel + 1) / 2}", DamageKind.Fire);
                }
            }), "Stewards of stone and fire linger near volcanoes and deep places near the heart of the earth, hot springs where the water is too scorchingly hot to allow casual enjoyment, and other places where the barrier between fire and earth is thin or nonexistent, though particularly old rock formations, canyons, and other natural features of earth may also spawn or attract them. Stewards of stone and fire are quick to anger and slow to forget.");
        yield return new Apparition(AnimistFeat.VanguardOfRoaringWaters, AnimistFeat.VanguardOfRoaringWatersPrimary,
            new List<SpellId>()
            {
                GetSpell("RousingSplash", SpellId.RayOfFrost),
                GetSpell("HydraulicPush", SpellId.HydraulicPush),
                GetSpell("Mist", SpellId.ObscuringMist),
                GetSpell("CrashingWave", SpellId.CrashingWave),
                GetSpell("HydraulicTorrent", SpellId.HydraulicTorrent),
            },
            ModManager.RegisterNewSpell("RiverCarvingMountains", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.ShallowWater,
                    "River Carving Mountains",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Water],
                    "Your apparition solidifies around you into roaring water and spraying mist.",
                    "For the duration of this spell, you have lesser cover against ranged attacks and gain a +10-foot status bonus to each Speed you have. When you first cast this spell and each time you Sustain it, you can Stride up to your speed while your apparition fills each square you pass through with the lingering energy of a coursing river. These squares become difficult terrain until the start of your next turn. You can use river carving mountains while Burrowing, Climbing, Flying, or Swimming instead of Striding if you have the corresponding movement type.",
                    Target.Self(null),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    var qe = new QEffect(spell.Name, "Your apparition allows you to Stride and leave difficult terrain in your path when you Sustain this spell.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                    {
                        CannotExpireThisTurn = true,
                        BonusToAllSpeeds = qe => new Bonus(2, BonusType.Status, "River Carving Mountains"),
                        IncreaseCover = (qe, action, cover) => (action.HasTrait(Trait.Ranged) && cover == CoverKind.None) ? CoverKind.Lesser : CoverKind.None,
                    }.WithSustaining(spell, ApplyStride, "When you Sustain this spell, you can Stride up to your speed, while each square you pass through becomes difficult terrain.");
                    caster.AddQEffect(qe);
                    await ApplyStride(qe);
                    async Task ApplyStride(QEffect qe)
                    {
                        Tile before = qe.Owner.Occupies;
                        if (await qe.Owner.StrideAsync("Choose where to Stride with River Carving Mountains", allowPass: true))
                        {
                            var tiles = Pathfinding.ReconstructPathFromEarlierFloodfill(before, qe.Owner.Occupies);
                            tiles.Add(before);
                            foreach (var tile in tiles)
                            {
                                tile.AddQEffect(new TileQEffect()
                                {
                                    TileQEffectId = AnimistQEffects.RiverCarvingMountains,
                                    TransformsTileIntoDifficultTerrain = true,
                                    ExpiresAt = ExpirationCondition.Never,
                                    Illustration = IllustrationName.ShallowWater
                                });
                            }
                            qe.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                            {
                                WhenExpires = qe => tiles.ForEach(tile => tile.RemoveAllQEffects(qe => qe.TileQEffectId == AnimistQEffects.RiverCarvingMountains))
                            });
                        }
                    }
                });
            }), "Vanguards of roaring waters are found where rivers carve their way through mountains, creating fearsome rapids. They can also be found near bays where rivers meet the sea and create turbulent breakers and unpredictable undertows, coastal reefs that tear the bottoms from unwary ships and isolate islands, or anywhere else where water becomes violent and difficult to navigate safely. Vanguards of roaring waters encourage chaos and are easily bored.");
        yield return new Apparition(AnimistFeat.WitnessToAncientBattles, AnimistFeat.WitnessToAncientBattlesPrimary,
            new List<SpellId>()
            {
                GetSpell("Shield", SpellId.Shield),
                GetSpell("SureStrike", SpellId.TrueStrike),
                GetSpell("Enlarge", SpellId.CladInMetal),
                GetSpell("GhostlyWeapon", SpellId.DeflectCriticalHit),
                GetSpell("WeaponStorm", SpellId.Stoneskin),
            },
            ModManager.RegisterNewSpell("EmbodimentOfBattle", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                var bonus = spellLevel >= 4 ? 2 : 1;
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.MagicWeapon,
                    "Embodiment of Battle",
                    [AnimistTrait.Animist, Trait.Focus],
                    "Your apparition guides your attacks and imparts its skill to your movements.",
                    $"For the duration, your proficiency with martial weapons is equal to your proficiency with simple weapons, you gain a +{S.HeightenedVariable(bonus, 1)} status bonus to attack and damage rolls made with weapons or unarmed attacks, and you gain the Attack Of Opportunity reaction (page 37); this reaction gains the apparition trait. The instincts of an apparition of battle run contrary to the use of magic; for the duration of this spell, you take a –2 status penalty to your spell attack modifiers and your spell DCs.",
                    Target.Self(null),
                    spellLevel,
                    null
                )
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [4], ["The status bonus to attack and damage rolls granted by this spell is increased to +2."])
                .WithActionCost(1)
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    var reactiveStrike = QEffect.AttackOfOpportunity();
                    var oldProficiency = target.Proficiencies.Get(Trait.Martial);
                    var qe = new QEffect(spell.Name, "Your apparition guides your attacks, granting you increased martial prowess, but reduced spellcasting prowess.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                    {
                        CannotExpireThisTurn = true,

                        BonusToAttackRolls = (q, action, target) =>
                        {
                            if (action.HasTrait(Trait.Attack) && action.SpellId == SpellId.None)
                            {
                                return new Bonus(bonus, BonusType.Status, "Embodiment of Battle", true);
                            }
                            else if (action.SpellId != SpellId.None)
                            {
                                return new Bonus(-2, BonusType.Status, "Embodiment of Battle");
                            }
                            return null;
                        },
                        BonusToSpellSaveDCs = (q) =>
                        {
                            return new Bonus(-2, BonusType.Status, "Embodiment of Battle");
                        },
                        BonusToDamage = (q, action, target) =>
                        {
                            if (action.HasTrait(Trait.Attack) && action.SpellId == SpellId.None)
                            {
                                return new Bonus(bonus, BonusType.Status, "Embodiment of Battle", true);
                            }
                            return null;
                        },
                        WhenExpires = (q) =>
                        {
                            q.Owner.RemoveAllQEffects(q => q == reactiveStrike);
                            q.Owner.Proficiencies.SetExactly([Trait.Martial], oldProficiency);
                        }
                    }.WithSustaining(spell);
                    target.Proficiencies.Set([Trait.Martial], target.Proficiencies.Get(Trait.Simple));
                    target.AddQEffect(qe);
                    target.AddQEffect(reactiveStrike);
                });
            }), "Witnesses to ancient battles may be the lingering remnants of soldiers who never returned from their last deployment or the restless souls of warriors whose final contest left them unfulfilled. Or the apparitions may be valkyries and other beings from beyond, naturally drawn to sites of death and battle, or even the unquiet entity formed from a battlefield that saw so much death and blood it gained a spiritual essence of its own. Witnesses to ancient battles are often somber and grim.");
    }

    [FeatGenerator(0)]
    public static IEnumerable<Feat> CreateFeats()
    {
        foreach (var apparition in GetApparitions())
        {
            yield return apparition;
            yield return apparition.AttunedFeat;
        }
    }
}

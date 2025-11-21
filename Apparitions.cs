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
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.ReactiveAttacks;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using Dawnsbury.Modding;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Display.Controls.Portraits;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Display.Illustrations;

namespace Dawnsbury.Mods.Classes.Animist.Apparitions;

public static class Extensions
{
    public static QEffect WithSustaining(this QEffect qe, CombatAction spell, QEffectId apparitionDispersed, Func<QEffect, Task>? onSustain = null, string? additionalText = null)
    {
        qe.ReferencedSpell = spell;
        spell.ReferencedQEffect = qe;
        qe.ProvideContextualAction = (QEffect qf) => (!qe.CannotExpireThisTurn) ? new ActionPossibility(new CombatAction(qf.Owner, spell.ChosenVariant?.Illustration ?? spell.Illustration, "Sustain " + spell.Name,
        [
            Trait.Concentrate,
            Trait.SustainASpell,
            Trait.Basic,
            Trait.DoesNotBreakStealth,
            AnimistTrait.Apparition
        ], "The duration of " + spell.Name + " continues until the end of your next turn." + ((additionalText == null) ? "" : ("\n\n" + additionalText)), Target.Self((Creature self, AI ai) => ai.ShouldSustain(spell))
        .WithAdditionalRestriction(self =>
        {
            if (self.HasEffect(apparitionDispersed))
            {
                return $"Your {apparitionDispersed.HumanizeTitleCase2()} is currently dispersed.";
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
    public static List<Apparition> ApparitionLUT = new List<Apparition>();
    public List<Skill> Skills { get; set; }
    public List<SpellId> Spells { get; set; }
    public SpellId VesselSpell { get; set; }
    public Feat AttunedFeat;
    public Feat FamiliarFeat;
    public Feat ArchetypeFeat;
    public QEffectId AttunedQID;
    public QEffectId DispersedQID;
    public Apparition(FeatName attunedFeatName, FeatName primaryFeatName, FeatName archetypeFeatName, QEffectId apparitionQID, QEffectId dispersedQID, List<SpellId> spells, SpellId vesselSpell, string flavorText) :
        base(primaryFeatName, flavorText, GenerateRulesText(spells, vesselSpell), [AnimistTrait.ApparitionPrimary], null)
    {
        Skills = new List<Skill>();
        Spells = spells;
        VesselSpell = vesselSpell;
        AttunedQID = apparitionQID;
        DispersedQID = dispersedQID;
        WithRulesBlockForSpell(vesselSpell);
        WithIllustration(AllSpells.CreateModernSpellTemplate(vesselSpell, AnimistTrait.Apparition).Illustration);
        WithOnSheet(sheet =>
        {
            sheet.AddFocusSpellAndFocusPoint(AnimistTrait.Apparition, Ability.Wisdom, VesselSpell);
        });
        WithOnCreature(cr =>
        {
            cr.FindQEffect(apparitionQID)!.Tag = true;
        });
        AttunedFeat = new Feat(attunedFeatName, flavorText, GenerateRulesText(spells, vesselSpell), [AnimistTrait.ApparitionAttuned], null)
            .WithOnSheet(sheet =>
            {
                var maxLevel = Math.Min(sheet.MaximumSpellLevel + 1, Spells.Count());
                for (var i = 0; i < maxLevel; ++i)
                {
                    SpellId spellID = Spells[i];
                    // All Apparition Spells are signature spells 
                    if (i > 0)
                    {
                        for (var j = i; j < maxLevel; ++j)
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
            })
            .WithRulesBlockForSpell(vesselSpell)
            .WithIllustration(AllSpells.CreateModernSpellTemplate(vesselSpell, AnimistTrait.Apparition).Illustration)
            .WithPermanentQEffect(null, q =>
            {
                q.Id = AttunedQID;
                q.Tag = false;
            });
        /*
    FamiliarFeat = FamiliarFeats.CreateFamiliarFeat(attunedFeatName.HumanizeTitleCase2(), AllSpells.CreateModernSpellTemplate(vesselSpell, AnimistTrait.Apparition).Illustration, [])
        .WithPrerequisite(sheet => sheet.HasFeat(AttunedFeat), "You must be attuned to this apparition.")
        .WithOnCreature(master =>
        {
            if (Familiar.IsFamiliarDead(master))
            {
                master.AddQEffect(Disable(master));
            }
            return new QEffect()
            {
                AfterYouAcquireEffect = async (q, gained) =>
                {
                    if (gained.Id == Familiar.QFamiliarDeployed)
                    {
                        Familiar.GetFamiliar(master)?.AddQEffect(new QEffect()
                        {
                            Id = AnimistQEffects.SpiritFamiliar,
                            WhenMonsterDies = q => master.AddQEffect(Disable(master)),
                            Tag = attunedFeatName
                        });
                        q.ExpiresAt = ExpirationCondition.Immediately;
                    }
                }
            };
        });
    FamiliarFeat.Traits.Remove(FamiliarFeats.TFamiliar);
    */
        FamiliarFeat = new Feat(ModManager.RegisterFeatName(attunedFeatName.ToStringOrTechnical() + "Familiar", attunedFeatName.HumanizeTitleCase2()), "A familiar is a small creature, a magical spirit, or an alchemical creation that dutifully serves you.", "You gain a combat familiar. A combat familiar is not a creature in its own right. It can't be targeted or dealt damage, it always sits in your space, and it participates in combat only by aiding you with familiar abilities.\r\n\r\nDuring morning preparations, you choose two familiar abilities and you gain the benefits of those abilities for that day. Familiar abilities give you passive bonuses or familiar actions. Familiar actions cost {icon:Action}one action, but you can only take one familiar action each turn.", [Trait.Rebalanced], null)
            .WithPrerequisite(sheet => sheet.HasFeat(AttunedFeat), "You must be attuned to this apparition.")
            .WithEquivalent(values => values.Tags.ContainsKey("CombatFamiliar"))
            .WithOnSheet(values =>
            {
                FamiliarTag familiarTag = new FamiliarTag();
                values.Tags["CombatFamiliar"] = familiarTag;
                familiarTag.FamiliarAbilities = 2 + (values.HasFeat(AnimistFeat.EnhancedFamiliar) ? 2 : 0) + (values.HasFeat(AnimistFeat.IncredibleFamiliar) ? 2 : 0);
                values.AddSelectionOptionRightNow(new SingleFeatSelectionOption("FamiliarIllustrationDisplay", "Show familiar", SelectionOption.MORNING_PREPARATIONS_LEVEL, (Feat ft) => ft.HasTrait(Trait.FamiliarIllustrationDisplay)).WithIsOptional());
                values.AddSelectionOptionRightNow(new CompanionIdentitySelectionOption(
                    "FamiliarName",
                    "Familiar identity",
                    SelectionOption.MORNING_PREPARATIONS_LEVEL,
                    $"You can name your familiar.\n\nIf you don't choose a name, it will be called {{b}}{attunedFeatName.HumanizeTitleCase2()}{{/b}}.",
                    attunedFeatName.HumanizeTitleCase2(),
                    IllustrationName.FamiliarFlameFairy,
                    [
                        PortraitCategory.Familiars,
                        PortraitCategory.AnimalCompanions,
                        PortraitCategory.Custom
                    ],
                    (val, txt) =>
                    {
                        if (val.Tags.TryGetValue("CombatFamiliar", out var value) && value is FamiliarTag familiarTag2)
                        {
                            CompanionIdentitySelectionOption.SetFamiliarDataFromSection(familiarTag2, txt);
                        }
                    }
                ).WithIsOptional());
                if (values.Tags.TryGetValue("CombatFamiliar", out var value) && value is FamiliarTag familiarTag2)
                {
                    values.AddSelectionOptionRightNow(new MultipleFeatSelectionOption("FamiliarAbilities", "Familiar abilities", SelectionOption.MORNING_PREPARATIONS_LEVEL, delegate (Feat ft, CalculatedCharacterSheetValues calculatedCharacterSheetValues)
                    {
                        if (ft.HasTrait(Trait.CombatFamiliarAbility))
                        {
                            if (ft.Tag is Trait trait && !calculatedCharacterSheetValues.AdditionalClassTraits.Contains(trait))
                            {
                                ClassSelectionFeat? classSelectionFeat = calculatedCharacterSheetValues.Class;
                                if (classSelectionFeat == null)
                                {
                                    return false;
                                }
                                return classSelectionFeat.ClassTrait == trait;
                            }
                            return true;
                        }
                        return false;
                    }, familiarTag2.FamiliarAbilities)
                    {
                        DoNotApplyEffectsInsteadOfRemovingThem = true
                    });
                }
            });
        ArchetypeFeat = new Feat(archetypeFeatName, flavorText, GenerateRulesText(spells, null), [AnimistTrait.ApparitionArchetype], null)
            .WithIllustration(AllSpells.CreateModernSpellTemplate(vesselSpell, AnimistTrait.Apparition).Illustration)
            .WithOnSheet(sheet =>
            {
                sheet.PreparedSpells[AnimistTrait.Animist].AdditionalPreparableSpells.AddRange(spells);
            });
        WithPrerequisite(sheet => sheet.HasFeat(AttunedFeat), "You must be attuned to this apparition.");
        ApparitionLUT.Add(this);
    }

    public QEffect Disable(Creature source, string? name = null, string? desc = null)
    {
        string name2 = name ?? $"{AttunedFeat.FeatName.HumanizeTitleCase2()} dispersed";
        string desc2 = desc ?? $"Your {AttunedFeat.FeatName.HumanizeTitleCase2()} has been dispersed, forbidding you from using its abilities.";

        bool hasFamiliar = source.HasEffect(QEffectId.FamiliarAbility) && source.HasFeat(FamiliarFeat.FeatName);
        if (hasFamiliar)
        {
            source.RemoveAllQEffects(q => q.Id == QEffectId.FamiliarAbility);
        }

        return new QEffect(name2, desc2, ExpirationCondition.Never, source, AllSpells.CreateModernSpellTemplate(VesselSpell, AnimistTrait.Apparition).Illustration)
        {
            Id = DispersedQID,
            PreventTakingAction = action =>
            {
                if (action.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition)
                {
                    var apparitions = GetAttunedApparitions(action.Owner);
                    if (apparitions?.Count() > 1)
                    {
                        if (Spells.Contains(action.SpellId) || action.SpellId == VesselSpell)
                        {
                            return desc2;
                        }
                    }
                    else
                    {
                        return desc2;
                    }
                }
                else if (action.HasTrait(AnimistTrait.Apparition))
                {
                    var apparitions = GetAttunedApparitions(action.Owner);
                    if (apparitions?.Count() <= 1)
                    {
                        return desc2;
                    }
                    else if (action.ReferencedQEffect?.ReferencedSpell?.SpellId == VesselSpell)
                    {
                        return desc2;
                    }
                }
                return null;
            },
            WhenExpires = hasFamiliar ? q =>
            {
			    if ((q.Owner.PersistentCharacterSheet?.Calculated.Tags.TryGetValue("CombatFamiliar", out var value) ?? false) && value is FamiliarTag familiarTag)
			    {
				    Illustration illustration = familiarTag.IllustrationOrDefault;
				    string name = familiarTag.FamiliarName ?? "Familiar";
                    q.Owner.AddQEffect(new QEffect
					{
						Id = QEffectId.FamiliarAbility,
						ProvideMainAction = (QEffect qfSelf) => qfSelf.UsedThisTurn ? null : new SubmenuPossibility(illustration, name)
						{
							Subsections = 
							{
								new PossibilitySection("Familiar action")
								{
									PossibilitySectionId = PossibilitySectionId.FamiliarAbility
								}
							}
						}.WithPossibilityGroup("Abilities")
					});
				}
            } : null
        };
    }

    private static string Ordinalize(int lvl)
    {
        if (lvl == 0) return "Cantrip";
        return lvl.Ordinalize2();
    }

    private static string GenerateRulesText(List<SpellId> spells, SpellId? vesselSpell)
    {
        string text = "\n{b}Apparition Spells{/b} ";
        for (var i = 0; i < spells.Count; ++i)
        {
            if (spells[i] != SpellId.None)
            {
                text += $"{{b}}{Ordinalize(i)}{{/b}} {AllSpells.CreateModernSpellTemplate(spells[i], AnimistTrait.Animist).ToSpellLink()}; ";
            }
        }
        if (vesselSpell != null)
        {
            text += $"\n{{b}}Vessel Spell{{/b}} {AllSpells.CreateModernSpellTemplate((SpellId)vesselSpell, AnimistTrait.Animist).ToSpellLink()}";
        }
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

    public static IEnumerable<Apparition> GetAttunedApparitions(Creature animist)
    {
        return ApparitionLUT.Where(app => animist.QEffects.Any(q => q.Id == app.AttunedQID));
    }

    public static IEnumerable<Apparition> GetPrimaryApparitions(Creature animist)
    {
        return ApparitionLUT.Where(app => animist.QEffects.Any(q => q.Id == app.AttunedQID && (bool?)q.Tag == true));
    }

    public static IEnumerable<Apparition> GetApparitions()
    {
        yield return new Apparition(AnimistFeat.CustodianOfGrovesAndGardens, AnimistFeat.CustodianOfGrovesAndGardensPrimary, AnimistFeat.CustodianOfGrovesAndGardensArchetype, AnimistQEffects.CustodianOfGrovesAndGardens, AnimistQEffects.CustodianOfGrovesAndGardensDispersed,
            new List<SpellId>()
            {
                GetSpell("TangleVine", SpellId.Tanglefoot),
                GetSpell("ProtectorTree", SpellId.ProtectorTree),
                GetSpell("GentleBreeze", SpellId.Barkskin),
                GetSpell("SafePassage", SpellId.PositiveAttunement),
                GetSpell("PeacefulBubble", SpellId.TortoiseAndTheHare),
                GetSpell("Truespeech", SpellId.TreeStep),
            },
            ModManager.RegisterNewSpell("GardenOfHealing", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Heal,
                    "Garden Of Healing",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Emotion, Trait.Focus, Trait.Healing, Trait.Mental],
                    "Spirits of comfort and respite swirl around you, trailing visions of growing grass and blooming blossoms.",
                    @$"When you cast this spell and the first time you sustain it each subsequent round, you generate a pulse of renewing energy that heals each creature within the emanation for {S.HeightenedVariable(spellLevel, 1)}d4 Hit Points.
                    The calm of this effect lingers; once this spell ends, any creature that has been affected by its healing gains a +1 circumstance bonus to saves against emotion effects but does not receive any healing from additional castings of the spell while the bonus persists.",
                    Target.Emanation(2).WithAdditionalRequirementOnCaster(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? Usability.NotUsable("This effect is already active") : Usability.Usable),
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
                    }.WithSustaining(spell, AnimistQEffects.CustodianOfGrovesAndGardensDispersed, async (qe) =>
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
        yield return new Apparition(AnimistFeat.EchoOfLostMoments, AnimistFeat.EchoOfLostMomentsPrimary, AnimistFeat.EchoOfLostMomentsArchetype, AnimistQEffects.EchoOfLostMoments, AnimistQEffects.EchoOfLostMomentsDispersed,
            new List<SpellId>()
            {
                GetSpell("Figment", SpellId.OpenDoor),
                GetSpell("DejaVu", SpellId.Command),
                GetSpell("DispelMagic", SpellId.LooseTimesArrow),
                GetSpell("CurseOfLostTime", SpellId.CurseOfLostTime),
                GetSpell("VisionOfDeath", SpellId.PhantasmalKiller),
                GetSpell("IllusoryScene", SpellId.StagnateTime),
            },
            ModManager.RegisterNewSpell("StoreTime", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.TimeStop,
                    "Store Time",
                    [AnimistTrait.Animist, Trait.Focus],
                    "You store time for later use.",
                    "When you Cast this Spell and the first time you Sustain it each round, you gain a bonus reaction that you can use for any animist or apparition reaction you have.",
                    Target.Self(null).WithAdditionalRestriction(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : null),
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
                        },
                        OfferExtraReaction = (q, question, traits) => traits.Contains(AnimistTrait.Apparition) ? "StoreTime" : null
                    }.WithSustaining(spell, AnimistQEffects.EchoOfLostMomentsDispersed);
                    caster.AddQEffect(qe);
                });
            }), "Echoes of lost moments are apparitions born from memories that everyone has forgotten, often arising from fragmented pieces of magic and memory left behind by time-altering magic. They may even occur in response to significant temporal tampering, cleaning up fragments of time damaged by irresponsible magic. These apparitions are drawn to animists who are orderly and responsible, and they can give such hosts access to spells that alter a target’s timeline or removes them from the current timeline, reveal visions of past or future events, or even accelerate magical effects to a point in time where they have already ended.");
        yield return new Apparition(AnimistFeat.ImposterInHiddenPlaces, AnimistFeat.ImposterInHiddenPlacesPrimary, AnimistFeat.ImposterInHiddenPlacesArchetype, AnimistQEffects.ImposterInHiddenPlaces, AnimistQEffects.ImposterInHiddenPlacesDispersed,
            new List<SpellId>()
            {
                GetSpell("TelekineticHand", SpellId.TelekineticProjectile),
                GetSpell("IllOmen", SpellId.IllOmen),
                GetSpell("Invisibility", SpellId.Invisibility),
                GetSpell("VeilOfPrivacy", SpellId.ImpendingDoom),
                GetSpell("LiminalDoorway", SpellId.DimensionDoor),
                GetSpell("StrangeGeometry", SpellId.Synesthesia),
            },
            ModManager.RegisterNewSpell("DiscomfitingWhispers", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Bane,
                    "Discomfiting Whispers",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Focus, Trait.Misfortune, Trait.Negative],
                    "You are surrounded by an aura of spiteful murmuring that incite bad luck and punish failure.",
                    $"Each creature that starts their turn within the area of this spell must succeed at a Will save or roll twice on their first attack roll that round and take the lower result. If an attack roll modified in this way results in a failure, the creature that rolled the failed attack takes {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d6 damage.",
                    Target.Emanation(1).WithAdditionalRequirementOnCaster(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? Usability.NotUsable("This effect is already active") : Usability.Usable),
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
                    }.WithSustaining(spell, AnimistQEffects.ImposterInHiddenPlacesDispersed, async (qe) =>
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
        yield return new Apparition(AnimistFeat.LurkerInDevouringDark, AnimistFeat.LurkerInDevouringDarkPrimary, AnimistFeat.LurkerInDevouringDarkArchetype, AnimistQEffects.LurkerInDevouringDark, AnimistQEffects.LurkerInDevouringDarkDispersed,
            new List<SpellId>()
            {
                GetSpell("CausticBlast", SpellId.AcidSplash),
                GetSpell("GrimTendrils", SpellId.GrimTendrils),
                GetSpell("AcidGrip", SpellId.AcidArrow),
                GetSpell("AqueousOrb", SpellId.SeaOfThought),
                GetSpell("GraspOfTheDeep", SpellId.PhantasmalKiller),
                GetSpell("WallOfIce", SpellId.BlackTentacles),
            },
            ModManager.RegisterNewSpell("DevouringDarkForm", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.Tentacle,
                    "Devouring Dark Form",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Morph],
                    "Your apparition's dark power blends with your physical body, allowing you to take on terrifying characteristics of creatures that lurk in dark places.",
                    $"Your arms and legs transform into twisting tentacles. You gain a tentacle unarmed attack with 10-foot reach that deals 1d8 bludgeoning damage and has the grapple trait. The first time you Sustain this spell each round, you can attempt a single Grapple check with your tentacle against a creature within its reach.",
                    Target.DependsOnSpellVariant(variant =>
                    {
                        SelfTarget selfTarget = Target.Self();
                        if (variant.Id == "Shark")
                        {
                            selfTarget.WithAdditionalRestriction(self => (!self.HasEffect(QEffectId.AquaticCombat)) ? "You can't transform into a shark above water." : null);
                        }
                        return selfTarget.WithAdditionalRestriction(self => self.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : selfTarget.AdditionalRestriction?.Invoke(self));
                    }),
                    spellLevel,
                    null
                )
                .WithActionCost(1)
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [2, 5], [
                        "You can choose to take on the shark battle form from {i}animal form{/i} instead of gaining a tentacle unarmed attack, heightened to the same level as this vessel spell. When you do, this spell loses the morph trait and gains the polymorph trait. You can attempt a jaws unarmed Strike against a creature within your reach each time you Sustain this spell.",
                        "You can choose to take on the water elemental battle form from {i}elemental form{/i} instead of gaining a tentacle unarmed attack, heightened to the same level as this vessel spell. When you do, this spell loses the morph trait and gains the polymorph trait. You can attempt an unarmed attack Strike against a creature within your reach each time you Sustain this spell.",
                ])
                .WithVariants(new SpellVariant[] {
                        new SpellVariant("Tentacle", "Tentacle Form", IllustrationName.Tentacle),
                        new SpellVariant("Shark", "Shark Form", IllustrationName.AnimalFormShark),
                        new SpellVariant("WaterElemental", "Water Elemental Form", IllustrationName.ElementalFormWater),
                    }.Where(v => v.Id == "Tentacle" || (v.Id == "Shark" && spellLevel >= 2) || (v.Id == "WaterElemental" && spellLevel >= 5)).ToArray())
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
                    else if (variant?.Id == "WaterElemental")
                    {
                        return "You take on the water elemental battle form from {i}elemental form{/i}, heightened to the same level as this vessel spell. You can attempt an unarmed attack Strike against a creature within your reach each time you Sustain this spell.";
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
                        }.WithSustaining(spell, AnimistQEffects.LurkerInDevouringDarkDispersed, async q =>
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
                                        var grappled = target.QEffects.FirstOrDefault(q => q.Id == QEffectId.Grappled && q.Source == grappler);
                                        if (grappled != null)
                                        {
                                            //Override the grapple state check because the tentacle has Reach
                                            grappled.StateCheck = grapple =>
                                            {
                                                if (!grapple.Source!.Actions.CanTakeActions() || grapple.Source.DistanceTo(grapple.Owner) > 2)
                                                {
                                                    grapple.ExpiresAt = ExpirationCondition.Immediately;
                                                    grapple.Source.HeldItems.RemoveAll((Item hi) => hi.Grapplee == grapple.Owner);
                                                }
                                                else if (checkResult == CheckResult.CriticalSuccess)
                                                {
                                                    grapple.Owner.AddQEffect(QEffect.Restrained(grappler));
                                                }
                                                else
                                                {
                                                    grapple.Owner.AddQEffect(QEffect.Grabbed(grappler));
                                                }
                                            };
                                        }
                                    });
                                grapple.ChosenTargets = ChosenTargets.CreateSingleTarget(target);
                                await grapple.WithActionCost(0).AllExecute();
                            }
                        }, "When you Sustain this spell, you can attempt a single Grapple check with your tentacle against a creature within its reach.");
                        target.AddQEffect(qe);
                    }
                    else if (variant?.Id == "Shark")
                    {
                        CombatAction sharkForm = AllSpells.CreateModernSpell(SpellId.AnimalForm, spell.Owner, spell.SpellLevel, inCombat: true, new SpellInformation
                        {
                            ClassOfOrigin = spell.SpellcastingSource!.ClassOfOrigin,
                            Superspell = spell
                        }).CombatActionSpell;
                        sharkForm.ChosenVariant = sharkForm.Variants?.Where(variant => variant.Id == "SHARK").FirstOrDefault();
                        sharkForm.ChosenTargets = ChosenTargets.CreateSingleTarget(spell.Owner);
                        await sharkForm.WithActionCost(0).AllExecute();
                        var battleform = spell.Owner.QEffects.Where(q => q.Name == "Battleform").FirstOrDefault();
                        if (battleform != null)
                        {
                            battleform.CannotExpireThisTurn = true;
                            battleform.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                            battleform.WithSustaining(spell, AnimistQEffects.LurkerInDevouringDarkDispersed, async q =>
                            {
                                Creature? target = await q.Owner.Battle.AskToChooseACreature(q.Owner,
                                        q.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(q.Owner) <= 1 && cr.EnemyOf(q.Owner) && !cr.HasTrait(Trait.Object)),
                        IllustrationName.Jaws, "Target a creature to attempt a jaws strike.", "Click to attempt a Strike on this target.", "Skip jaws Strike");
                                if (target != null)
                                {
                                    var strike = q.Owner.CreateStrike(q.Owner.UnarmedStrike);
                                    strike.ChosenTargets = ChosenTargets.CreateSingleTarget(target);
                                    await strike.WithActionCost(0).AllExecute();
                                }
                            }, "When you Sustain this spell, you can attempt a jaws unarmed Strike against a creature within your reach.");
                        }
                    }
                    else if (variant?.Id == "WaterElemental")
                    {
                        CombatAction waterElementalForm = AllSpells.CreateModernSpell(SpellId.ElementalForm, spell.Owner, spell.SpellLevel, inCombat: true, new SpellInformation
                        {
                            ClassOfOrigin = spell.SpellcastingSource!.ClassOfOrigin,
                            Superspell = spell
                        }).CombatActionSpell;
                        waterElementalForm.ChosenVariant = waterElementalForm.Variants?.Where(variant => variant.Id == "WATER").FirstOrDefault();
                        waterElementalForm.ChosenTargets = ChosenTargets.CreateSingleTarget(spell.Owner);
                        await waterElementalForm.WithActionCost(0).AllExecute();
                        var battleform = spell.Owner.QEffects.Where(q => q.Name == "Battleform").FirstOrDefault();
                        if (battleform != null)
                        {
                            battleform.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                            battleform.CannotExpireThisTurn = true;
                            battleform.WithSustaining(spell, AnimistQEffects.LurkerInDevouringDarkDispersed, async q =>
                            {
                                Creature? target = await q.Owner.Battle.AskToChooseACreature(q.Owner,
                                        q.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(q.Owner) <= 1 && cr.EnemyOf(q.Owner) && !cr.HasTrait(Trait.Object)),
                        IllustrationName.ElementalBlastWater, "Target a creature to attempt a strike.", "Click to attempt a Strike on this target.", "Skip Strike");
                                if (target != null)
                                {
                                    var strike = q.Owner.CreateStrike(q.Owner.UnarmedStrike);
                                    strike.ChosenTargets = ChosenTargets.CreateSingleTarget(target);
                                    await strike.WithActionCost(0).AllExecute();
                                }
                            }, "When you Sustain this spell, you can attempt an unarmed Strike against a creature within your reach.");
                        }
                    }
                });
            }), "Lurkers in devouring dark are most often near old shipwrecks, deadly icebergs, and other places where ice and deep water are most prevalent.");
        yield return new Apparition(AnimistFeat.MonarchOfTheFeyCourts, AnimistFeat.MonarchOfTheFeyCourtsPrimary, AnimistFeat.MonarchOfTheFeyCourtsArchetype, AnimistQEffects.MonarchOfTheFeyCourts, AnimistQEffects.MonarchOfTheFeyCourtsDispersed,
            new List<SpellId>()
            {
                GetSpell("TangleVine", SpellId.Tanglefoot),
                GetSpell("Charm", SpellId.Command),
                GetSpell("CreateFood", SpellId.Glitterdust),
                GetSpell("Enthrall", SpellId.RoaringApplause),
                GetSpell("Suggestion", SpellId.Sleep),
                GetSpell("Hallucination", SpellId.DeathWard),
            },
            ModManager.RegisterNewSpell("NymphsGrace", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.DemonMask,
                    "Nymph's Grace",
                    [AnimistTrait.Animist, Trait.Aura, Trait.Emotion, Trait.Focus, Trait.Incapacitation, Trait.Mental, Trait.Visual],
                    "Your apparition manifests as a mask of unearthly beauty that bewilders your enemies.",
                    "The first time an enemy enters the aura each round, or if they start their turn within the aura, they must succeed at a Will saving throw or become confused for 1 round. While confused by this effect, the creature's confused actions never include harming you.",
                    Target.Emanation(2).WithAdditionalRequirementOnCaster(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? Usability.NotUsable("This effect is already active") : Usability.Usable),
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
                    .WithSustaining(spell, AnimistQEffects.MonarchOfTheFeyCourtsDispersed)
                    .WithZone(ZoneAttachment.Aura(2), (qe, zone) =>
                    {
                        zone.WithOncePerRoundEffectWhenCreatureBeginsTurnOrEnters(async cr =>
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
        yield return new Apparition(AnimistFeat.RevelerInLostGlee, AnimistFeat.RevelerInLostGleePrimary, AnimistFeat.RevelerInLostGleeArchetype, AnimistQEffects.RevelerInLostGlee, AnimistQEffects.RevelerInLostGleeDispersed,
            new List<SpellId>()
            {
                GetSpell("Prestidigitation", SpellId.TelekineticProjectile),
                GetSpell("DizzyingColors", SpellId.ColorSpray),
                GetSpell("LaughingFit", SpellId.HideousLaughter),
                GetSpell("Hypnotize", SpellId.ImpendingDoom),
                GetSpell("Confusion", SpellId.Confusion),
                GetSpell("IllusoryScene", SpellId.CloakOfColors),
            },
            ModManager.RegisterNewSpell("TrickstersMirrors", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.MirrorImage,
                    "Trickster's Mirrors",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Illusion, Trait.Mental, Trait.Visual],
                    "You are surrounded by mirrors that reflect twisted and distorted images of you.",
                    $"You start with 1 mirror and gain an additional mirror each time you Sustain this spell, up to a maximum of 3 mirrors. Any attack that would hit you has a random chance of hitting one of your mirrors instead of you. With one mirror, the chances are 1 in 2 (1–3 on 1d6). With two mirrors, there is a 1 in 3 chance of hitting you (1–2 on 1d6). With three mirrors, there is a 1 in 4 chance of hitting you (1 on 1d4). Once an image is hit, it is destroyed. If an attack roll fails to hit your AC but doesn’t critically fail, it destroys a mirror. If the attacker was within 5 feet, they must succeed at a basic Will save or take {S.HeightenedVariable(spellLevel, 1)}d4 mental damage as they believe themselves cut by a shower of glass shards from the breaking mirror. A damaging effect that affects all targets within your space (such as caustic blast) destroys all of the mirrors.",
                    Target.Self(null).WithAdditionalRestriction(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : null),
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
                        CannotExpireThisTurn = true,
                        WhenExpires = q => q.Owner.RemoveAllQEffects(q => q.Id == QEffectId.MirrorImage && q.ReferencedSpell == spell)
                    }.WithSustaining(spell, AnimistQEffects.RevelerInLostGleeDispersed, async q =>
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
        yield return new Apparition(AnimistFeat.StalkerInDarkenedBoughs, AnimistFeat.StalkerInDarkenedBoughsPrimary, AnimistFeat.StalkerInDarkenedBoughsArchetype, AnimistQEffects.StalkerInDarkenedBoughs, AnimistQEffects.StalkerInDarkenedBoughsDispersed,
            new List<SpellId>()
            {
                GetSpell("GougingClaw", SpellId.PhaseBolt),
                GetSpell("RunicBody", SpellId.MagicFang),
                GetSpell("VomitSwarm", SpellId.BoneSpray),
                GetSpell("WallOfThorns", SpellId.StinkingCloud),
                GetSpell("BestialCurse", SpellId.BestowCurse),
                GetSpell("MoonFrenzy", SpellId.WyvernSting),
            },
            ModManager.RegisterNewSpell("DarkenedForestForm", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.WildShape,
                    "Darkened Forest Form",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Polymorph],
                    "Your apparition casts a feral shadow over your form.",
                    "You can polymorph into any form listed in {i}insect form{/i}. When you transform into a form granted by a spell, you gain all the effects of the form you chose from a version of the spell heightened to {i}darkened forest form{/i}’s rank. Each time you Sustain this Spell, you can choose to change to a different shape from those available via any of the associated spells.",
                    Target.Self(null).WithAdditionalRestriction(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : null),
                    spellLevel,
                    null
                )
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [2, 5], ["You can also transform into the forms listed in {i}animal form{/i}.", "You can also transform into the forms listed in {i}elemental form{/i}."])
                .WithActionCost(1)
                .WithVariantsCreator(caster =>
                {
                    List<SpellVariant> variants = [new SpellVariant("INSECT", "Insect", IllustrationName.InsectFormPortrait)];
                    if (spellLevel >= 2)
                    {
                        variants.AddRange(AllSpells.CreateModernSpell(SpellId.AnimalForm, caster, spellLevel, inCombat: true, spellInformation).CombatActionSpell.Variants!);
                    }
                    if (spellLevel >= 5)
                    {
                        variants.AddRange(AllSpells.CreateModernSpell(SpellId.ElementalForm, caster, spellLevel, inCombat: true, spellInformation).CombatActionSpell.Variants!);
                    }
                    return variants.ToArray();
                })
                .WithCreateVariantDescription((_, variant) =>
                {
                    if (variant != null)
                    {
                        var (subspell, subspellVariant) = GetSpellAndVariantFromVariant(variant);
                        return subspell.CreateVariantDescription?.Invoke(_, subspellVariant) ?? subspell.Description;
                    }
                    return "";
                })
                .WithEffectOnSelf(async (spell, self) =>
                {
                    ApplyDarkenedForestForm(spell, spell.ChosenVariant!, self);
                });
                void ApplyDarkenedForestForm(CombatAction spell, SpellVariant variant, Creature caster)
                {
                    caster.RemoveAllQEffects(qe => qe.ReferencedSpell == spell);
                    var oldTempHP = caster.TemporaryHP;

                    var (subspell, subspellVariant) = GetSpellAndVariantFromVariant(variant);
                    subspell.ChosenVariant = subspellVariant;
                    subspell.ChosenTargets = ChosenTargets.CreateSingleTarget(caster);
                    subspell.WithActionCost(0).AllExecute();

                    caster.TemporaryHP = oldTempHP;

                    QEffect? form = caster.QEffects.Where(q => q.Name == "Battleform").FirstOrDefault();
                    if (form != null)
                    {
                        form.ReferencedSpell = spell;
                        form.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                        form.CannotExpireThisTurn = true;
                        form.WithSustaining(spell, AnimistQEffects.StalkerInDarkenedBoughsDispersed);
                    }

                    caster.AddQEffect(new QEffect()
                    {
                        ReferencedSpell = spell,
                        StateCheck = q =>
                        {
                            if (!caster.QEffects.Contains(form))
                            {
                                q.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        },
                        ProvideContextualAction = q => q.ReferencedSpell?.ReferencedQEffect?.CannotExpireThisTurn ?? true ? null : new SubmenuPossibility(spell.Illustration, $"Change {spell.Name} Form")
                        {
                            Subsections =
                            [
                                new PossibilitySection($"Change {spell.Name} Form")
                                {
                                    Possibilities = spell.VariantsCreator!(caster).ExceptBy([variant.Id], var => var.Id).Select(variant =>
                                        new ActionPossibility(new CombatAction(caster, variant.Illustration, $"{spell.Name} ({variant.Name})",
                                                    [Trait.Concentrate, Trait.SustainASpell, Trait.Basic, Trait.DoesNotBreakStealth, AnimistTrait.Apparition], spell.CreateVariantDescription!(1, variant), Target.Self()
                                            .WithAdditionalRestriction(self =>
                                            {
                                                if (!self.Spellcasting!.GetSourceByOrigin(AnimistTrait.Apparition)!.FocusSpells.Exists(sp => sp.SpellId == spell.SpellId))
                                                {
                                                    return "You do not have the primary apparition to sustain this spell.";
                                                }
                                                if (self.HasEffect(AnimistQEffects.StalkerInDarkenedBoughsDispersed))
                                                {
                                                    return $"Your {AnimistQEffects.StalkerInDarkenedBoughsDispersed.HumanizeTitleCase2()} is currently dispersed.";
                                                }
                                                return null;
                                            }))
                                            .WithReferencedQEffect(q)
                                            .WithActionCost(1)
                                            .WithEffectOnSelf(async delegate (CombatAction action, Creature creature)
                                            {
                                                ApplyDarkenedForestForm(spell, variant, caster);
                                            }))
                                    ).Cast<Possibility>().ToList()
                                }
                            ]
                        }
                    });
                }
                (CombatAction, SpellVariant?) GetSpellAndVariantFromVariant(SpellVariant variant)
                {
                    if (spellLevel >= 5)
                    {
                        var elementalForm = AllSpells.CreateModernSpell(SpellId.ElementalForm, spellCaster, spellLevel, inCombat: true, spellInformation).CombatActionSpell;
                        var elementalFormVariant = elementalForm.Variants!.FirstOrDefault(v => v.Id == variant.Id);
                        if (elementalFormVariant != null)
                        {
                            return (elementalForm, elementalFormVariant);
                        }
                    }
                    var animalForm = AllSpells.CreateModernSpell(SpellId.AnimalForm, spellCaster, spellLevel, inCombat: true, spellInformation).CombatActionSpell;
                    var animalFormVariant = animalForm.Variants!.FirstOrDefault(v => v.Id == variant.Id);
                    if (animalFormVariant != null)
                    {
                        return (animalForm, animalFormVariant);
                    }
                    return (AllSpells.CreateModernSpell(SpellId.InsectForm, spellCaster, spellLevel, inCombat: true, spellInformation).CombatActionSpell, null);
                }
            }), "Stalkers in darkened boughs make their homes in ancient forests and jungles unfriendly to humanoids and others who would exert control or influence over nature’s designs. These apparitions are drawn to animists who harbor violent thoughts or impulses but are more likely to linger with animists who can quell their hatred. Stalkers in darkened boughs are moody, impulsive, and prone to seeing things from the least charitable perspective.");
        yield return new Apparition(AnimistFeat.StewardOfStoneAndFire, AnimistFeat.StewardOfStoneAndFirePrimary, AnimistFeat.StewardOfStoneAndFireArchetype, AnimistQEffects.StewardOfStoneAndFire, AnimistQEffects.StewardOfStoneAndFireDispersed,
            new List<SpellId>()
            {
                GetSpell("Ignition", SpellId.ProduceFlame),
                GetSpell("InterposingEarth", SpellId.PummelingRubble),
                GetSpell("ExplodingEarth", SpellId.FlamingSphere),
                GetSpell("Fireball", SpellId.Fireball),
                GetSpell("WallOfFire", SpellId.WallOfFire),
                GetSpell("WallOfStone", SpellId.IncendiaryFog),
            },
            ModManager.RegisterNewSpell("EarthsBile", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.FireRay,
                    "Earth's Bile",
                    [AnimistTrait.Animist, Trait.Earth, Trait.Fire, Trait.Focus],
                    "Your apparition is the will of lava and magma made manifest, the earth’s molten blood unleashing devastating bursts of liquid stone and unquenchable fire at your command.",
                    $"When you Cast this Spell and the first time you Sustain it each round thereafter, choose an area within range. Each creature in the area takes {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 fire damage, {S.HeightenedVariable((spellLevel + 1) / 2, 1)}d4 bludgeoning damage, and {S.HeightenedVariable((spellLevel + 1) / 2, 1)} persistent fire damage (the persistent fire damage is negated on a successful save).",
                    Target.Burst(6, 2).WithAdditionalRequirementOnCaster(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? Usability.NotUsable("This effect is already active") : Usability.Usable),
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
                        ReferencedSpell = spell,
                        ProvideContextualAction = (QEffect qf) => (!qf.CannotExpireThisTurn) && !self.HasEffect(AnimistQEffects.StewardOfStoneAndFireDispersed) ? new ActionPossibility(new CombatAction(qf.Owner, spell.Illustration, "Sustain " + spell.Name,
                        [
                            ..spell.Traits.Except([Trait.Focus]),
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
                        .WithSpellInformation(spell.SpellLevel, "", null)
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
                    var diceFormula = DiceFormula.FromText($"{(spellLevel + 1) / 2}d4", spell.Name);
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, new KindedDamage(diceFormula, DamageKind.Fire), new KindedDamage(diceFormula, DamageKind.Bludgeoning));
                    await CommonSpellEffects.DealBasicPersistentDamage(target, checkResult, $"{(spellLevel + 1) / 2}", DamageKind.Fire);
                }
            }), "Stewards of stone and fire linger near volcanoes and deep places near the heart of the earth, hot springs where the water is too scorchingly hot to allow casual enjoyment, and other places where the barrier between fire and earth is thin or nonexistent, though particularly old rock formations, canyons, and other natural features of earth may also spawn or attract them. Stewards of stone and fire are quick to anger and slow to forget.");
        yield return new Apparition(AnimistFeat.VanguardOfRoaringWaters, AnimistFeat.VanguardOfRoaringWatersPrimary, AnimistFeat.VanguardOfRoaringWatersArchetype, AnimistQEffects.VanguardOfRoaringWaters, AnimistQEffects.VanguardOfRoaringWatersDispersed,
            new List<SpellId>()
            {
                GetSpell("RousingSplash", SpellId.RayOfFrost),
                GetSpell("HydraulicPush", SpellId.HydraulicPush),
                GetSpell("Mist", SpellId.ObscuringMist),
                GetSpell("CrashingWave", SpellId.CrashingWave),
                GetSpell("HydraulicTorrent", SpellId.HydraulicTorrent),
                GetSpell("ControlWater", SpellId.Geyser),
            },
            ModManager.RegisterNewSpell("RiverCarvingMountains", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.ShallowWater,
                    "River Carving Mountains",
                    [AnimistTrait.Animist, Trait.Focus, Trait.Water],
                    "Your apparition solidifies around you into roaring water and spraying mist.",
                    "For the duration of this spell, you have lesser cover against ranged attacks and gain a +10-foot status bonus to each Speed you have. When you first cast this spell and each time you Sustain it, you can Stride up to your speed while your apparition fills each square you pass through with the lingering energy of a coursing river. These squares become difficult terrain until the start of your next turn. You can use river carving mountains while Burrowing, Climbing, Flying, or Swimming instead of Striding if you have the corresponding movement type.",
                    Target.Self(null).WithAdditionalRestriction(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : null),
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
                    }.WithSustaining(spell, AnimistQEffects.VanguardOfRoaringWatersDispersed, ApplyStride, "When you Sustain this spell, you can Stride up to your speed, while each square you pass through becomes difficult terrain.");
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
        yield return new Apparition(AnimistFeat.WitnessToAncientBattles, AnimistFeat.WitnessToAncientBattlesPrimary, AnimistFeat.WitnessToAncientBattlesArchetype, AnimistQEffects.WitnessToAncientBattles, AnimistQEffects.WitnessToAncientBattlesDispersed,
            new List<SpellId>()
            {
                GetSpell("Shield", SpellId.Shield),
                GetSpell("SureStrike", SpellId.TrueStrike),
                GetSpell("Enlarge", SpellId.Enlarge),
                GetSpell("GhostlyWeapon", SpellId.GhostlyWeapon),
                GetSpell("WeaponStorm", SpellId.ReboundingBarrier),
                GetSpell("InvokeSpirits", SpellId.BlinkCharge),
            },
            ModManager.RegisterNewSpell("EmbodimentOfBattle", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
            {
                var bonus = spellLevel >= 7 ? 3 : spellLevel >= 4 ? 2 : 1;
                return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.MagicWeapon,
                    "Embodiment of Battle",
                    [AnimistTrait.Animist, Trait.Focus],
                    "Your apparition guides your attacks and imparts its skill to your movements.",
                    $"For the duration, your proficiency with martial weapons is equal to your proficiency with simple weapons, you gain a +{S.HeightenedVariable(bonus, 1)} status bonus to attack and damage rolls made with weapons or unarmed attacks, and you gain the Attack Of Opportunity reaction; this reaction gains the apparition trait. The instincts of an apparition of battle run contrary to the use of magic; for the duration of this spell, you take a –2 status penalty to your spell attack modifiers and your spell DCs.",
                    Target.Self(null).WithAdditionalRestriction(caster => caster.QEffects.Any(q => q.ReferencedSpell?.SpellId == spellId) ? "This effect is already active" : null),
                    spellLevel,
                    null
                )
                .WithHeightenedAtSpecificLevels(spellLevel, inCombat, [4, 7], ["The status bonus to attack and damage rolls granted by this spell is increased to +2.", "The status bonus to attack and damage rolls granted by this spell is increased to +3."])
                .WithActionCost(1)
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    var reactiveStrike = AttackOfOpportunityMechanics.AttackOfOpportunity(new AttackOfOpportunityMechanics()
                    {
                        Name = "Attack of Opportunity",
                        Description = "When a creature leaves a square within your reach, makes a ranged attack or uses a move or manipulate action, you can Strike it for free. On a critical hit, you also disrupt the manipulate action.",
                        RestrictToOnlyAgainstWhom = null,
                        OverheadName = "*attack of opportunity*",
                        StandStill = false,
                        StrikeAndReactionTraits = [
                            Trait.ReactiveAttack,
                            Trait.AttackOfOpportunity,
                            AnimistTrait.Apparition
                        ],
                        NumberOfStrikes = 1
                    });
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
                    }.WithSustaining(spell, AnimistQEffects.WitnessToAncientBattlesDispersed);
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
            yield return apparition.FamiliarFeat;
            yield return apparition.ArchetypeFeat;
        }
    }
}

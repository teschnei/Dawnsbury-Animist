using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
using Dawnsbury.Mods.Classes.Animist.Apparitions;
using Dawnsbury.Mods.Classes.Animist.Practices;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;

namespace Dawnsbury.Mods.Classes.Animist;

public static class Animist
{
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new ClassSelectionFeat(AnimistFeat.AnimistClass,
                "You are the interlocutor between the seen and unseen, the voice that connects the mortal and the spiritual. You bond with spirits, manifesting their distinct magic and allowing their knowledge to flow through you. You may favor apparitions that grant you healing magic, others that grant you spells of destructive power, or pick and choose between different apparitions as your environment and circumstances demand. You may consider your powers part of a sacred trust or see your unique abilities as a sign that you’ve been chosen as a champion of two worlds. Whether you advocate for mortals in the planes beyond or whether you represent the spirits’ interests, you provide the bridge between realms.",
                AnimistTrait.Animist,
                new EnforcedAbilityBoost(Ability.Wisdom),
                8, [Trait.Perception, Trait.Fortitude, Trait.Reflex, Trait.Simple, Trait.Unarmed, Trait.LightArmor, Trait.MediumArmor, Trait.UnarmoredDefense], [Trait.Will], 2,
                @$"{{b}}1. Animistic Practice.{{/b}} At 1st level, you choose an animistic practice that influences the way your power grows and develops, and you gain its first invocation.

{{b}}2. Animist & Apparition Spellcasting.{{/b}} You can cast spells in two distinct ways: you both learn and prepare spells from the divine tradition yourself, and you also channel the knowledge and power of your attuned apparitions, gaining spell slots and a repertoire of spells from them that you can cast spontaneously.
{S.DescribePreparedSpellcasting(Trait.Divine, 1, Ability.Wisdom).Replace("5 cantrips", "2 cantrips").Replace("You can cast spells. ", "")}
You also gain one spell slot that can be used to cast any apparition spell once per day.

{{b}}3. Apparition Attunement.{{/b}} Each day during daily preparations, choose two apparitions to attune to. Of these, choose one to be your primary apparition. Your attuned apparitions each grant you a repertoire of additional spells you can cast using apparition spellcasting, and your primary apparition grants you its unique vessel focus spell. You may change your primary apparition as part of pre-combat preparations.

{{b}}At higher levels:{{/b}}
{{b}}Level 2:{{/b}} Animist feat, additional level 1 spell slot
{{b}}Level 3:{{/b}} General feat, skill increase, expert in Fortitude saving throws, level 2 spells (one prepared spell slot, one apparition spell slot)
{{b}}Level 4:{{/b}} Animist feat, additional level 2 spell slot
{{b}}Level 5:{{/b}} Ability boosts, ancestry feat, skill increase, level 3 spells (one prepared spell slot, one apparition spell slot)
{{b}}Level 6:{{/b}} Animist feat, additional level 3 spell slot
{{b}}Level 7:{{/b}} General feat, skill increase, expert in spellcasting, third apparition {{i}}(when you attune to apparitions during your daily preparations, you may choose three apparitions to attune to){{/i}}, level 4 spells (one prepared spell slot, one apparition spell slot)
{{b}}Level 8:{{/b}} Animist feat, additional level 4 spell slot",
    Practice.GetPractices().ToList()
            )
            .WithOnSheet(sheet =>
            {
                sheet.SpellTraditionsKnown.Add(Trait.Divine);
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);

                sheet.PreparedSpells.Add(AnimistTrait.Animist, new PreparedSpellSlots(Ability.Wisdom, Trait.Divine));
                sheet.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(0, "AnimistSlot0-1"));
                sheet.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(0, "AnimistSlot0-2"));
                sheet.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(1, "AnimistSlot1-1"));
                sheet.SpellRepertoires.Add(AnimistTrait.Apparition, new SpellRepertoire(Ability.Wisdom, Trait.Divine));
                sheet.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[0] = 2;
                sheet.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[1] = 1;

                sheet.AddAtLevel(2, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(1, "AnimistSlot1-2"));
                });

                sheet.AddAtLevel(3, values =>
                {
                    values.SetProficiency(Trait.Fortitude, Proficiency.Expert);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(2, "AnimistSlot2-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[2] = 1;
                });

                sheet.AddAtLevel(4, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(2, "AnimistSlot2-2"));
                });

                sheet.AddAtLevel(5, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(3, "AnimistSlot3-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[3] = 1;
                });

                sheet.AddAtLevel(6, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(3, "AnimistSlot3-2"));
                });

                sheet.AddAtLevel(7, values =>
                {
                    values.SetProficiency(Trait.Spell, Proficiency.Expert);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(4, "AnimistSlot4-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[0] = 3;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[4] = 1;
                });

                sheet.AddAtLevel(8, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(4, "AnimistSlot4-2"));
                });
                // If we unattune a primary apparition, unset the primary apparition
                /*
                sheet.AtEndOfRecalculation += (sheet) =>
                {
                    if (sheet.AllFeats.Any(ft =>
                    {
                        if (ft is Apparition apparition)
                        {
                            return !sheet.HasFeat(apparition.AttunedFeat);
                        }
                        return false;
                    }))
                    {
                        foreach (var selection in sheet.SelectionOptions.Where(option => option.Name == "Primary Apparitions"))
                        {
                            sheet.Sheet.SelectedFeats[selection.Key] = null;
                            sheet.Sheet.SelectedFeats[selection.KeyLegacy] = null;
                        }
                    }
                };
                */
                sheet.AddSelectionOption(new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionAttuned), sheet.CurrentLevel >= 7 ? 3 : 2));
                sheet.AddSelectionOption(new MultipleFeatSelectionOption("AnimistPrimaryApparition", "Primary Apparitions", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, (ft, values) =>
                {
                    if (ft is Apparition apparition)
                    {
                        return values.HasFeat(apparition.AttunedFeat);
                    }
                    return false;
                }, 1));
            })
            .WithOnCreature((sheet, creature) =>
            {
            });
        foreach (var i in Apparition.GetApparitions())
        {
            yield return i;
            yield return i.AttunedFeat;
        }
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
                                   [.. from feat in self.PersistentCharacterSheet?.Calculated.AllFeats where feat.HasTrait(AnimistTrait.ApparitionAttuned) select feat.Name]);
                            Apparition? chosenApparition = AllFeats.All.Where(feat => feat.Name == choice.Caption && feat is Apparition).FirstOrDefault() as Apparition;
                            if (chosenApparition != null)
                            {
                                self.Spellcasting?.GetSourceByOrigin(AnimistTrait.Apparition)?.FocusSpells.RemoveAll(spell => true);
                                self.Spellcasting?.GetSourceByOrigin(AnimistTrait.Apparition)?.WithSpells([chosenApparition.VesselSpell], self.PersistentCharacterSheet?.Calculated.MaximumSpellLevel ?? 0);
                            }
                        })
                    );
                };
            });
        yield return new TrueFeat(AnimistFeat.RelinquishControl, 1,
                "Your apparition takes over and shields you from outside influence.",
                "Until the start of your next turn, you gain a +4 status bonus on saves against spells and effects that give you the controlled condition or attempt to influence your actions (such as charm, command, or a nosoi’s haunting melody). However, the only actions you can take are to Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action that has the apparition trait.\n{b}Special{/b} This feat requires a particularly strong bond with a specific apparition to learn. Choose one apparition you have access to; once you learn this feat, you must always choose that apparition as one of the apparitions you attune to each day.",
                [AnimistTrait.Animist, AnimistTrait.Apparition],
                AllFeats.All.Where(feat => feat.HasTrait(AnimistTrait.ApparitionAttuned)).ToList())
            .WithActionCost(0)
            .WithOnSheet(sheet =>
            {
                sheet.SelectionOptions.RemoveAll(option => option.Name == "Attuned Apparitions");
                sheet.AddSelectionOption(new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionAttuned), sheet.CurrentLevel >= 7 ? 2 : 1));
            })
            .WithPermanentQEffect("You gain a +4 status bonus on saves against controlling effects, but you can only take the Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action with the apparition trait.", q =>
            {
                q.ProvideMainAction = qe =>
                {
                    return new ActionPossibility(new CombatAction(qe.Owner, IllustrationName.CircleOfProtection, "Relinquish Control", [AnimistTrait.Animist, AnimistTrait.Apparition],
                                "Until the start of your next turn, you gain a +4 status bonus on saves against spells and effects that give you the controlled condition or attempt to influence your actions (such as charm, command, or a nosoi’s haunting melody). However, the only actions you can take are to Step, Strike, Cast an apparition Spell, Cast a vessel Spell, Sustain a vessel spell, or use an action that has the apparition trait.\n{b}Special{/b} This feat requires a particularly strong bond with a specific apparition to learn. Choose one apparition you have access to; once you learn this feat, you must always choose that apparition as one of the apparitions you attune to each day.",
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
                                        (action.ReferencedQEffect?.ReferencedSpell?.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition))
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
        yield return new TrueFeat(AnimistFeat.ConcealSpell, 2,
                "You speak with the unheard voice of the spirits.",
                "If the next action you use is to Cast a Spell, the spell gains the subtle trait, hiding the shining runes, sparks of magic, and other manifestations that would usually give away your spellcasting. The trait hides only the spell’s spellcasting actions and manifestations, not its effects, so an observer might still see a ray streak out from you or see you vanish into thin air.",
                [AnimistTrait.Animist, Trait.Concentrate, Trait.Metamagic])
            .WithActionCost(1);
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
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Concentrate, Trait.Metamagic])
            .WithActionCost(1);
        yield return new TrueFeat(AnimistFeat.SpiritualExpansionSpell, 2,
                "Your apparitions manifest to scatter the energy of your spell.",
                "If the next action you use is to Cast a Spell that has an area of a burst, cone, or line and does not have a duration, increase the area of that spell. Add 5 feet to the radius of a burst that normally has a radius of at least 10 feet (a burst with a smaller radius is not affected). Add 5 feet to the length of a cone or line that is normally 15 feet long or smaller, and add 10 feet to the length of a larger cone or line. You can also use this feat to increase the radius of an emanation spell with a duration by 5 feet by dedicating your primary apparition to maintaining the spellshape; dedicating the apparition to the spell prevents you from using the apparition’s vessel spell, apparition skills, or avatar form for the duration of the modified spell.",
                [AnimistTrait.Animist, AnimistTrait.Apparition, Trait.Concentrate, Trait.Metamagic])
            .WithActionCost(1);
    }
}

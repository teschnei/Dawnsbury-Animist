using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Text;
using Dawnsbury.Mods.Classes.Animist.Apparitions;
using Dawnsbury.Mods.Classes.Animist.Practices;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist;

public static class Animist
{
    [FeatGenerator(0)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new Feat(AnimistFeat.ThirdApparition,
                "You've learned to shelter more spirits, gaining access to more magic.",
                "When you attune to apparitions during your daily preparations, you choose three apparitions to attune to, with one of them being your primary apparition. The number of Focus Points in your focus pool increases by 1 (maximum 3).",
                [],
                null)
            .WithOnSheet(sheet =>
            {
                sheet.FocusPointCount++;
                var attunedIndex = sheet.SelectionOptions.FindIndex(option => option.Name == "Attuned Apparitions");
                int count = sheet.AllFeatNames.Contains(AnimistFeat.RelinquishControl) ? 2 : 3;
                sheet.SelectionOptions[attunedIndex] = new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionAttuned), count);
            });
        yield return new Feat(AnimistFeat.FourthApparition,
                "You're truly loved by the spirits, with apparitions flocking to you from far and wide",
                "When you attune to apparitions during your daily preparations, you choose four apparitions to attune to, with one of them being your primary apparition. The number of Focus Points in your focus pool increases by 1 (maximum 3).",
                [],
                null)
            .WithOnSheet(sheet =>
            {
                sheet.FocusPointCount++;
                var attunedIndex = sheet.SelectionOptions.FindIndex(option => option.Name == "Attuned Apparitions");
                int count = sheet.AllFeatNames.Contains(AnimistFeat.RelinquishControl) ? 3 : 4;
                sheet.SelectionOptions[attunedIndex] = new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionAttuned), count);
            });
        yield return new ClassSelectionFeat(AnimistFeat.AnimistClass,
                "You are the interlocutor between the seen and unseen, the voice that connects the mortal and the spiritual. You bond with spirits, manifesting their distinct magic and allowing their knowledge to flow through you. You may favor apparitions that grant you healing magic, others that grant you spells of destructive power, or pick and choose between different apparitions as your environment and circumstances demand. You may consider your powers part of a sacred trust or see your unique abilities as a sign that you’ve been chosen as a champion of two worlds. Whether you advocate for mortals in the planes beyond or whether you represent the spirits’ interests, you provide the bridge between realms.",
                AnimistTrait.Animist,
                new EnforcedAbilityBoost(Ability.Wisdom),
                8, [Trait.Perception, Trait.Fortitude, Trait.Reflex, Trait.Simple, Trait.Unarmed, Trait.LightArmor, Trait.MediumArmor, Trait.UnarmoredDefense], [Trait.Will], 2,
                @$"{{b}}1. Animistic Practice.{{/b}} At 1st level, you choose an animistic practice that influences the way your power grows and develops, and you gain its first invocation.

{{b}}2. Animist & Apparition Spellcasting.{{/b}} You can cast spells in two distinct ways: you both learn and prepare spells from the divine tradition yourself, and you also channel the knowledge and power of your attuned apparitions, gaining spell slots and a repertoire of spells from them that you can cast spontaneously.
{S.DescribePreparedSpellcasting(Trait.Divine, 1, Ability.Wisdom).Replace("5 cantrips", "2 cantrips").Replace("You can cast spells. ", "")}
You also gain one spell slot that can be used to cast any apparition spell once per day.

{{b}}3. Apparition Attunement.{{/b}} Each day during daily preparations, choose two apparitions to attune to. Of these, choose one to be your primary apparition. Your attuned apparitions each grant you a repertoire of additional spells you can cast using apparition spellcasting, and your primary apparition grants you its unique vessel focus spell. You may change your primary apparition as part of pre-combat preparations.",
    Practice.GetPractices().ToList()
            )
            .WithClassFeatures(features =>
            {
                features.AddPreparedSpellcasting("one prepared Animist spell slot, one spontaneous Apparition spell slot");
                features.AddFeature(3, WellKnownClassFeature.ExpertInFortitude);
                features.AddFeature(7, WellKnownClassFeature.ExpertInSpellcasting);
                features.AddFeature(7, "Third Apparition", "When you attune to apparitions during your daily preparations, you choose three apparitions to attune to. The number of Focus Points in your focus pool increases by 1 (maximum 3).");
                features.AddFeature(9, WellKnownClassFeature.ExpertInPerception);
                features.AddFeature(9, "Second Invocation", "You learn a new ability, based on your chosen practice.");
                features.AddFeature(11, "Expert Protections", "Your proficiency for light armor, medium armor, unarmored defense and Reflex saves increases to expert.");
                features.AddFeature(11, "Simple Weapon Expertise");
                features.AddFeature(13, "Master of Mind and Spirit", "Your proficiency rank for Will saves increases to master; when you roll a success on a Will save, you get a critical success instead.");
                features.AddFeature(13, WellKnownClassFeature.WeaponSpecialization);
                features.AddFeature(15, "Fourth Apparition", "When you attune to apparitions during your daily preparations, you choose four apparitions to attune to. The number of Focus Points in your focus pool increases by 1 (maximum 3).");
                features.AddFeature(15, "Master in Spellcasting");
                features.AddFeature(17, "Third Invocation", "You learn a new ability, based on your chosen practice.");
                features.AddFeature(19, "Legendary in Spellcasting");
                features.AddFeature(19, "Supreme Incarnation");
            })
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
                    values.GrantFeat(AnimistFeat.ThirdApparition);
                    values.SetProficiency(Trait.Spell, Proficiency.Expert);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(4, "AnimistSlot4-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[0] = 3;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[4] = 1;
                });

                sheet.AddAtLevel(8, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(4, "AnimistSlot4-2"));
                });

                sheet.AddAtLevel(9, values =>
                {
                    values.SetProficiency(Trait.Perception, Proficiency.Expert);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(5, "AnimistSlot5-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[5] = 1;
                });

                sheet.AddAtLevel(10, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(5, "AnimistSlot5-2"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[1] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[2] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[3] = 2;
                });

                sheet.AddAtLevel(11, values =>
                {
                    values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                    values.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    values.SetProficiency(Trait.Reflex, Proficiency.Expert);
                    values.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(6, "AnimistSlot6-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[4] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[6] = 1;
                });

                sheet.AddAtLevel(12, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(6, "AnimistSlot6-2"));
                });

                sheet.AddAtLevel(13, values =>
                {
                    values.SetProficiency(Trait.Will, Proficiency.Master);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(7, "AnimistSlot7-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[5] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[7] = 1;
                });

                sheet.AddAtLevel(14, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(7, "AnimistSlot7-2"));
                });

                sheet.AddAtLevel(15, values =>
                {
                    values.GrantFeat(AnimistFeat.FourthApparition);
                    values.SetProficiency(Trait.Spell, Proficiency.Master);
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(8, "AnimistSlot8-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[0] = 4;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[6] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[8] = 1;
                });

                sheet.AddAtLevel(16, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(8, "AnimistSlot8-2"));
                });

                sheet.AddAtLevel(17, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(9, "AnimistSlot9-1"));
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[7] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[9] = 1;
                });

                sheet.AddAtLevel(18, values =>
                {
                    values.PreparedSpells[AnimistTrait.Animist].Slots.Add(new FreePreparedSpellSlot(9, "AnimistSlot9-2"));
                });

                sheet.AddAtLevel(19, values =>
                {
                    values.SetProficiency(Trait.Spell, Proficiency.Legendary);
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[8] = 2;
                    values.SpellRepertoires[AnimistTrait.Apparition].SpellSlots[10] = 1;
                });

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
            .WithOnCreature(cr =>
            {
                if (cr.Level >= 7)
                {
                    cr.AddQEffect(new QEffect()
                    {
                        Id = AnimistQEffects.ThirdApparition
                    });
                }
                if (cr.Level >= 13)
                {
                    CommonCharacterFeatures.AddEvasion(cr, "Master of Mind and Spirit", Defense.Will);
                    cr.AddQEffect(QEffect.WeaponSpecialization());
                }
            });
    }
}

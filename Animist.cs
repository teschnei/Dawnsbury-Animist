using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
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
    [FeatGenerator]
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
            .WithOnCreature(cr =>
            {
                if (cr.Level >= 7)
                {
                    cr.AddQEffect(new QEffect()
                    {
                        Id = AnimistQEffects.ThirdApparition
                    });
                }
            });
    }
}

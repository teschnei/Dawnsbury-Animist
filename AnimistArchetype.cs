
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.Multiclass;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist;

public static class AnimistArchetype
{
    class ApparitionPreparedSpellSlot : PreparedSpellSlot
    {
        public override string SlotName { get; }

        public ApparitionPreparedSpellSlot(int spellLevel, string key) : base(spellLevel, key)
        {
            SlotName = "Apparition";
        }

        public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (!Apparitions.Apparition.ApparitionLUT.Where(a => sheet.Calculated.HasFeat(a.ArchetypeFeat)).Any(a => a.Spells.Contains(preparedSpell.SpellId)))
            {
                return preparedSpell.Name + " isn't a spell from your apparition.";
            }
            return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
        }
    }

    [FeatGenerator(1)]
    public static IEnumerable<Feat> CreateFeats()
    {
        yield return ArchetypeFeats.CreateMulticlassDedication(AnimistTrait.Animist, "You have established a relationship with a spiritual entity known as an apparition, unlocking the ability to use animistic magic.",
                "You have formed a bond with an apparition and can cast divine spells. Choose a single apparition from those available to the animist. You become bound to that apparition and can attune to it each day during your daily preparations.\n\n" +
                "You gain the Cast a Spell activity. You can prepare two common cantrips each day from the divine spell list or any other divine cantrips you have access to, including the cantrip listed in your apparition’s apparition spells. You’re trained in the spell attack modifier and spell DC statistics. " +
                "Your key spellcasting attribute for animist archetype spells is Wisdom, and they are divine animist spells.")
            .WithDemandsAbility14(Ability.Wisdom)
            .WithOnSheet(sheet =>
            {
                MulticlassArchetypeFeats.SetupPreparedSpellcasting(sheet, Trait.Divine, Ability.Wisdom, AnimistTrait.Animist);
                sheet.AddSelectionOption(new MultipleFeatSelectionOption("AnimistApparition", "Attuned Apparitions", SelectionOption.MORNING_PREPARATIONS_LEVEL, (ft) => ft.HasTrait(AnimistTrait.ApparitionArchetype), 1));
            });
        var basicSpellCasting = MulticlassArchetypeFeats.CreateBasicSpellcastingBenefitsFeat(AnimistTrait.Animist, Trait.Prepared);
        yield return basicSpellCasting;
        yield return new TrueFeat(AnimistFeat.SpiritualAwakening, 4, null, "You gain a 1st- or 2nd-level Animist feat.", [], null)
            .WithAvailableAsArchetypeFeat(AnimistTrait.Animist)
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("SpiritualAwakeningFeat", "Spiritual Awakening feat", -1, (Feat ft) => ft is TrueFeat trueFeat && ft.HasTrait(AnimistTrait.Animist) && trueFeat.Level <= 2));
            });
        yield return new TrueFeat(AnimistFeat.AnimistsPower, 6, null, "You gain one Animist feat.\r\n\r\nFor the purpose of meeting its prerequisites, your Animist level is equal to half your character level:\r\n• If you take this feat at level 6, you can only take a level 1 or level 2 Animist feat.\r\n• If you take this feat at level 8, you can only take a level 1, level 2 or level 4 Animist feat.", [], null)
            .WithAvailableAsArchetypeFeat(AnimistTrait.Animist)
            .WithMultipleSelection()
            .WithPrerequisite(AnimistFeat.SpiritualAwakening, "Spiritual Awakening")
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("AnimistsPowerFeat", "Animist's Power feat", -1, (ft, val) => ft is TrueFeat trueFeat2 && ft.HasTrait(AnimistTrait.Animist) && trueFeat2.Level <= val.CurrentLevel / 2));
            });
        yield return new TrueFeat(AnimistFeat.ApparitionMagic, 8, "You can cast more divine spells each day.", "You gain 1 additional spell slot from animist archetype feats for each spell rank other than your two highest animist spell slots. These additional slots can only be used to prepare spells from your apparition's apparition spells.", [], null)
            .WithAvailableAsArchetypeFeat(AnimistTrait.Animist)
            .WithPrerequisite(basicSpellCasting.FeatName, "Basic Animist Spellcasting")
            .WithOnSheet(sheet =>
            {
                if (sheet.PreparedSpells.ContainsKey(AnimistTrait.Animist))
                {
                    var newSlots = sheet.PreparedSpells[AnimistTrait.Animist].Slots.Select(slot => slot.SpellLevel).Distinct().Order().SkipLast(2);
                    sheet.PreparedSpells[AnimistTrait.Animist].Slots.AddRange(newSlots.Where(level => level != 0).Select(level => new ApparitionPreparedSpellSlot(level, $"ApparitionMagic{level}")));
                }
            });
    }
}

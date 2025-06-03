using System.Collections.Generic;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using static Dawnsbury.Mods.Classes.Animist.AnimistClassLoader;

namespace Dawnsbury.Mods.Classes.Animist.Apparitions;

public static class Apparition
{
    [FeatGenerator]
    public static IEnumerable<Feat> CreateFeats()
    {
        var spell = ModManager.RegisterNewSpell("EmbodimentOfBattle", 1, (spellId, spellCaster, spellLevel, inCombat, spellInformation) =>
        {
            var bonus = spellLevel >= 4 ? 2 : 1;
            return Core.CharacterBuilder.FeatsDb.Spellbook.Spells.CreateModern(IllustrationName.MagicWeapon,
                "Embodiment of Battle",
                [Trait.Focus],
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
                var oldProficiency = target.Proficiencies.Get(Trait.Martial);
                var qe = new QEffect(spell.Name, "Your apparition guides your attacks, granting you increased martial prowess, but reduced spellcasting prowess.", ExpirationCondition.ExpiresAtEndOfYourTurn, caster, spell.Illustration)
                {
                    WhenExpires = (q) =>
                    {
                        q.Owner.Proficiencies.SetExactly([Trait.Martial], oldProficiency);
                    }
                };
                target.Proficiencies.Set(Trait.Martial, target.Proficiencies.Get(Trait.Simple));
                target.AddQEffect(qe);
            });
        });
        FeatName testFeat = ModManager.RegisterFeatName("TestFeat", "Test Feat");

        yield return new Feat(testFeat, "", "", [Trait.Druid], null).WithOnSheet(sheet => sheet.AddFocusSpellAndFocusPoint(Trait.Druid, Ability.Intelligence, spell));
    }
}

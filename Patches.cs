using System;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Classes.Animist.RegisteredComponents;
using HarmonyLib;

namespace Dawnsbury.Mods.Classes.Animist.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.ProvokeOpportunityAttacks))]
static class AoOPatch
{
    static void Postfix(Creature __instance, CombatAction combatAction)
    {
        if (combatAction.Disrupted == true && __instance.HasEffect(AnimistQEffects.ApparitionStabilization))
        {
            int dc = 15;
            if (__instance.HasEffect(AnimistQEffects.ThirdApparition)) { dc = 13; }
            (CheckResult result, string message) = Checks.RollFlatCheck(dc);
            bool succeeded = result >= CheckResult.Success;
            __instance.Occupies.Battle.Log($"{__instance?.ToString()} {(succeeded ? "{Green}succeeds{/}" : "{Red}fails{/}")} an apparition stabilization flat check vs. DC {dc} to keep the spell ({message})");
            combatAction.Disrupted = !succeeded;
        }
    }
}

[HarmonyPatch(typeof(CombatAction), nameof(CombatAction.WithCastsAsAReaction), [typeof(Action<QEffect, CombatAction, Func<bool>>)])]
static class StoreTimePatch
{
    static bool Prefix(CombatAction __instance, Action<QEffect, CombatAction, Func<bool>> action, ref CombatAction __result)
    {
        __instance.WhenCombatBegins = delegate (Creature creature)
        {
            QEffect qEffect = new QEffect();
            creature.AddQEffect(qEffect);
            action(qEffect, __instance, delegate
            {
                __instance.Owner = creature;
                if (__instance.HasTrait(Trait.Impulse))
                {
                    if (creature.Impulsing != null)
                    {
                        return creature.Impulsing.CanInvokeReactiveImpulse(__instance);
                    }
                    return false;
                }
                if (creature.Spellcasting != null)
                {
                    if (__instance.SpellcastingSource?.ClassOfOrigin == AnimistTrait.Apparition &&
                        creature.HasEffect(AnimistQEffects.StoreTimeReaction) &&
                        !creature.HasEffect(AnimistQEffects.StoreTimeReactionUsed))
                    {
                        creature.QEffects.Where(q => q.Id == AnimistQEffects.StoreTimeReaction).FirstOrDefault()!.Tag = __instance;
                        return true;
                    }
                    return creature.Spellcasting.CanCastReactiveSpell(__instance);
                }
                return false;
            });
        };
        __result = __instance;
        return false;
    }
}

[HarmonyPatch(typeof(Actions), nameof(Actions.CanTakeReaction), [])]
static class StoreTimePatch2
{
    static void Postfix(ref bool __result, Creature ___creature)
    {
        if (__result == false)
        {
            if (___creature.QEffects.Where(q => q.Id == AnimistQEffects.StoreTimeReaction).FirstOrDefault()?.Tag is CombatAction action)
            {
                //Reaction is a spell usable by Store Time
                __result = true;
            }
        }
    }
}

[HarmonyPatch(typeof(Actions), nameof(Actions.UseUpReaction), [])]
static class StoreTimePatch3
{
    static bool Prefix(Creature ___creature)
    {
        if (___creature.QEffects.Where(q => q.Id == AnimistQEffects.StoreTimeReaction).FirstOrDefault()?.Tag is CombatAction action)
        {
            ___creature.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
            {
                Id = AnimistQEffects.StoreTimeReactionUsed
            });
            //Don't use up the reaction since Store Time was used instead
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Spellcasting), nameof(Spellcasting.GeneratePossibilitiesInto))]
static class ApparitionsReflectionPatch
{
    static void Postfix(Spellcasting __instance)
    {
        var animistsReflection = __instance.Self.QEffects.Where(q => q.Id == AnimistQEffects.AnimistsReflectionUnholiness).FirstOrDefault();
        if (animistsReflection != null)
        {
            var spellSlots = __instance.GetSourceByOrigin(AnimistTrait.Apparition)?.SpontaneousSpellSlots;
            var copiedSpellSlots = animistsReflection.Tag as int[];
            if (spellSlots != null && copiedSpellSlots != null)
            {
                for (int i = 0; i < spellSlots.Count(); ++i)
                {
                    spellSlots[i] = copiedSpellSlots[i];
                }
            }
            animistsReflection.Tag = null;
        }
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.EnemyOf))]
static class NymphsGracePatch
{
    static void Postfix(Creature anotherCreature, ref bool __result, Creature __instance)
    {
        if (ModManager.TryParse<SpellId>("NymphsGrace", out var spellid))
        {
            var confused = anotherCreature.QEffects.Where(q => q.Id == QEffectId.Confused).FirstOrDefault();
            if (confused?.SourceAction?.SpellId == spellid && confused?.Source == __instance)
            {
                __result = false;
            }
        }
    }
}

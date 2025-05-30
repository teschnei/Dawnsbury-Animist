using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Animist.RegisteredComponents
{
    public static class AnimistQEffects
    {
        public static readonly QEffectId ThirdApparition = ModManager.RegisterEnumMember<QEffectId>("Third Apparition");

        public static readonly QEffectId GardenOfHealingHealed = ModManager.RegisterEnumMember<QEffectId>("Garden Of Healing Healed");
        public static readonly QEffectId GardenOfHealingImmunity = ModManager.RegisterEnumMember<QEffectId>("Garden Of Healing Immunity");
        public static readonly QEffectId StoreTimeReaction = ModManager.RegisterEnumMember<QEffectId>("Store Time Reaction");
        public static readonly QEffectId StoreTimeReactionUsed = ModManager.RegisterEnumMember<QEffectId>("Store Time Reaction Used");
        public static readonly QEffectId DiscomfitingWhispersStartTurn = ModManager.RegisterEnumMember<QEffectId>("Discomfiting Whispers Start Turn");
        public static readonly QEffectId NymphsGraceAffectedAlready = ModManager.RegisterEnumMember<QEffectId>("Nymphs Grace Affected Already");
        public static readonly QEffectId TrickstersMirrors = ModManager.RegisterEnumMember<QEffectId>("Trickster's Mirrors");

        public static readonly QEffectId ChannelersStance = ModManager.RegisterEnumMember<QEffectId>("Channeler's Stance");
        public static readonly QEffectId ApparitionStabilization = ModManager.RegisterEnumMember<QEffectId>("Apparition Stabilization");
        public static readonly QEffectId BlazingSpiritUsed = ModManager.RegisterEnumMember<QEffectId>("Blazing Spirit Used");
        public static readonly QEffectId AnimistsReflectionUnholiness = ModManager.RegisterEnumMember<QEffectId>("Animist's Reflection Unholiness");
        public static readonly QEffectId AnimistsReflectionUsed = ModManager.RegisterEnumMember<QEffectId>("Animist's Reflection Used");
        public static readonly QEffectId InstinctiveManeuvers = ModManager.RegisterEnumMember<QEffectId>("Instinctive Maneuvers");
        //TODO: probably need one for each apparition
        public static readonly QEffectId PrimaryApparitionBusy = ModManager.RegisterEnumMember<QEffectId>("Apparition Busy");

        public static readonly TileQEffectId RiverCarvingMountains = ModManager.RegisterEnumMember<TileQEffectId>("River Carving Mountains Tile");
    }
}

using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Animist.RegisteredComponents
{
    public static class AnimistQEffects
    {
        public static readonly QEffectId GardenOfHealingHealed = ModManager.RegisterEnumMember<QEffectId>("Garden Of Healing Healed");
        public static readonly QEffectId GardenOfHealingImmunity = ModManager.RegisterEnumMember<QEffectId>("Garden Of Healing Immunity");
        public static readonly QEffectId StoreTimeReaction = ModManager.RegisterEnumMember<QEffectId>("Store Time Reaction");
        public static readonly QEffectId StoreTimeReactionUsed = ModManager.RegisterEnumMember<QEffectId>("Store Time Reaction Used");
        public static readonly QEffectId DiscomfitingWhispersStartTurn = ModManager.RegisterEnumMember<QEffectId>("Discomfiting Whispers Start Turn");
        public static readonly QEffectId NymphsGraceAffectedAlready = ModManager.RegisterEnumMember<QEffectId>("Nymphs Grace Affected Already");
        public static readonly QEffectId TrickstersMirrors = ModManager.RegisterEnumMember<QEffectId>("Trickster's Mirrors");

        public static readonly QEffectId ChannelersStance = ModManager.RegisterEnumMember<QEffectId>("Channeler's Stance");

        public static readonly TileQEffectId RiverCarvingMountains = ModManager.RegisterEnumMember<TileQEffectId>("River Carving Mountains Tile");
    }
}

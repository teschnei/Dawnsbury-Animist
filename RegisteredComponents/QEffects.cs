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
        public static readonly QEffectId SpiritFamiliar = ModManager.RegisterEnumMember<QEffectId>("Spirit Familiar");
        public static readonly QEffectId ApparitionStabilization = ModManager.RegisterEnumMember<QEffectId>("Apparition Stabilization");
        public static readonly QEffectId BlazingSpiritUsed = ModManager.RegisterEnumMember<QEffectId>("Blazing Spirit Used");
        public static readonly QEffectId AnimistsReflectionUnholiness = ModManager.RegisterEnumMember<QEffectId>("Animist's Reflection Unholiness");
        public static readonly QEffectId AnimistsReflectionUsed = ModManager.RegisterEnumMember<QEffectId>("Animist's Reflection Used");
        public static readonly QEffectId InstinctiveManeuvers = ModManager.RegisterEnumMember<QEffectId>("Instinctive Maneuvers");

        public static readonly QEffectId CrafterInTheVaultDispersed = ModManager.RegisterEnumMember<QEffectId>("Crafter In The Vault Dispersed");
        public static readonly QEffectId CustodianOfGrovesAndGardensDispersed = ModManager.RegisterEnumMember<QEffectId>("Custodian Of Groves And Gardens Dispersed");
        public static readonly QEffectId EchoOfLostMomentsDispersed = ModManager.RegisterEnumMember<QEffectId>("Echo Of Lost Moments Dispersed");
        public static readonly QEffectId ImposterInHiddenPlacesDispersed = ModManager.RegisterEnumMember<QEffectId>("Imposter In Hidden Places Dispersed");
        public static readonly QEffectId LurkerInDevouringDarkDispersed = ModManager.RegisterEnumMember<QEffectId>("Lurker In Devouring Dark Dispersed");
        public static readonly QEffectId MonarchOfTheFeyCourtsDispersed = ModManager.RegisterEnumMember<QEffectId>("Monarch Of The Fey Courts DisperseDispersed");
        public static readonly QEffectId RevelerInLostGleeDispersed = ModManager.RegisterEnumMember<QEffectId>("Reveler In Lost Glee Dispersed");
        public static readonly QEffectId StalkerInDarkenedBoughsDispersed = ModManager.RegisterEnumMember<QEffectId>("Stalker In Darkened Boughs Dispersed");
        public static readonly QEffectId StewardOfStoneAndFireDispersed = ModManager.RegisterEnumMember<QEffectId>("Steward Of Stone And Fire Dispersed");
        public static readonly QEffectId VanguardOfRoaringWatersDispersed = ModManager.RegisterEnumMember<QEffectId>("Vanguard Of Roaring Waters Dispersed");
        public static readonly QEffectId WitnessToAncientBattlesDispersed = ModManager.RegisterEnumMember<QEffectId>("Witness To Ancient Battles Dispersed");

        public static readonly TileQEffectId RiverCarvingMountains = ModManager.RegisterEnumMember<TileQEffectId>("River Carving Mountains Tile");
    }
}

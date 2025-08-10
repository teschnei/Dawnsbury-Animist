using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Animist.RegisteredComponents
{
    public static class AnimistFeat
    {
        public static readonly FeatName AnimistClass = ModManager.RegisterFeatName("AnimistClass", "Animist");
        //Practices
        public static readonly FeatName Liturgist = ModManager.RegisterFeatName("Liturgist");
        public static readonly FeatName Medium = ModManager.RegisterFeatName("Medium");
        public static readonly FeatName Seer = ModManager.RegisterFeatName("Seer");
        public static readonly FeatName Shaman = ModManager.RegisterFeatName("Shaman");

        public static readonly FeatName SongOfInvocation = ModManager.RegisterFeatName("SongOfInvocation", "Song of Invocation");
        public static readonly FeatName DancingInvocation = ModManager.RegisterFeatName("DancingInvocation", "Dancing Invocation");
        public static readonly FeatName InvocationOfUnity = ModManager.RegisterFeatName("InvocationOfUnity", "Invocation of Unity");
        public static readonly FeatName DualInvocation = ModManager.RegisterFeatName("DualInvocation", "Dual Invocation");
        public static readonly FeatName InvocationOfSight = ModManager.RegisterFeatName("InvocationOfSight", "Invocation of Sight");
        public static readonly FeatName InvocationOfProtection = ModManager.RegisterFeatName("InvocationOfProtection", "Invocation of Protection");
        public static readonly FeatName InvocationOfEmbodiment = ModManager.RegisterFeatName("InvocationOfEmbodiment", "Invocation of Embodiment");
        public static readonly FeatName InvocationOfGrowth = ModManager.RegisterFeatName("InvocationOfGrowth", "Invocation of Growth");
        //Apparitions
        public static readonly FeatName CrafterInTheVault = ModManager.RegisterFeatName("Crafter In The Vault");
        public static readonly FeatName CustodianOfGrovesAndGardens = ModManager.RegisterFeatName("Custodian Of Groves And Gardens");
        public static readonly FeatName EchoOfLostMoments = ModManager.RegisterFeatName("Echo Of Lost Moments");
        public static readonly FeatName ImposterInHiddenPlaces = ModManager.RegisterFeatName("Imposter In Hidden Places");
        public static readonly FeatName LurkerInDevouringDark = ModManager.RegisterFeatName("Lurker In Devouring Dark");
        public static readonly FeatName MonarchOfTheFeyCourts = ModManager.RegisterFeatName("Monarch Of The Fey Courts");
        public static readonly FeatName RevelerInLostGlee = ModManager.RegisterFeatName("Reveler In Lost Glee");
        public static readonly FeatName StalkerInDarkenedBoughs = ModManager.RegisterFeatName("Stalker In Darkened Boughs");
        public static readonly FeatName StewardOfStoneAndFire = ModManager.RegisterFeatName("Steward Of Stone And Fire");
        public static readonly FeatName VanguardOfRoaringWaters = ModManager.RegisterFeatName("Vanguard Of Roaring Waters");
        public static readonly FeatName WitnessToAncientBattles = ModManager.RegisterFeatName("Witness To Ancient Battles");
        //Primary Apparitions
        public static readonly FeatName CrafterInTheVaultPrimary = ModManager.RegisterFeatName("Crafter In The Vault Primary", "Crafter In The Vault");
        public static readonly FeatName CustodianOfGrovesAndGardensPrimary = ModManager.RegisterFeatName("Custodian Of Groves And Gardens Primary", "Custodian Of Groves And Gardens");
        public static readonly FeatName EchoOfLostMomentsPrimary = ModManager.RegisterFeatName("Echo Of Lost Moments Primary", "Echo Of Lost Moments");
        public static readonly FeatName ImposterInHiddenPlacesPrimary = ModManager.RegisterFeatName("Imposter In Hidden Places Primary", "Imposter In Hidden Places");
        public static readonly FeatName LurkerInDevouringDarkPrimary = ModManager.RegisterFeatName("Lurker In Devouring Dark Primary", "Lurker In Devouring Dark");
        public static readonly FeatName MonarchOfTheFeyCourtsPrimary = ModManager.RegisterFeatName("Monarch Of The Fey Courts Primary", "Monarch Of The Fey Courts");
        public static readonly FeatName RevelerInLostGleePrimary = ModManager.RegisterFeatName("Reveler In Lost Glee Primary", "Reveler In Lost Glee");
        public static readonly FeatName StalkerInDarkenedBoughsPrimary = ModManager.RegisterFeatName("Stalker In Darkened Boughs Primary", "Stalker In Darkened Boughs");
        public static readonly FeatName StewardOfStoneAndFirePrimary = ModManager.RegisterFeatName("Steward Of Stone And Fire Primary", "Steward Of Stone And Fire");
        public static readonly FeatName VanguardOfRoaringWatersPrimary = ModManager.RegisterFeatName("Vanguard Of Roaring Waters Primary", "Vanguard Of Roaring Waters");
        public static readonly FeatName WitnessToAncientBattlesPrimary = ModManager.RegisterFeatName("Witness To Ancient Battles Primary", "Witness To Ancient Battles");
        //Archetype
        public static readonly FeatName CrafterInTheVaultArchetype = ModManager.RegisterFeatName("Crafter In The Vault Archetype", "Crafter In The Vault");
        public static readonly FeatName CustodianOfGrovesAndGardensArchetype = ModManager.RegisterFeatName("Custodian Of Groves And Gardens Archetype", "Custodian Of Groves And Gardens");
        public static readonly FeatName EchoOfLostMomentsArchetype = ModManager.RegisterFeatName("Echo Of Lost Moments Archetype", "Echo Of Lost Moments");
        public static readonly FeatName ImposterInHiddenPlacesArchetype = ModManager.RegisterFeatName("Imposter In Hidden Places Archetype", "Imposter In Hidden Places");
        public static readonly FeatName LurkerInDevouringDarkArchetype = ModManager.RegisterFeatName("Lurker In Devouring Dark Archetype", "Lurker In Devouring Dark");
        public static readonly FeatName MonarchOfTheFeyCourtsArchetype = ModManager.RegisterFeatName("Monarch Of The Fey Courts Archetype", "Monarch Of The Fey Courts");
        public static readonly FeatName RevelerInLostGleeArchetype = ModManager.RegisterFeatName("Reveler In Lost Glee Archetype", "Reveler In Lost Glee");
        public static readonly FeatName StalkerInDarkenedBoughsArchetype = ModManager.RegisterFeatName("Stalker In Darkened Boughs Archetype", "Stalker In Darkened Boughs");
        public static readonly FeatName StewardOfStoneAndFireArchetype = ModManager.RegisterFeatName("Steward Of Stone And Fire Archetype", "Steward Of Stone And Fire");
        public static readonly FeatName VanguardOfRoaringWatersArchetype = ModManager.RegisterFeatName("Vanguard Of Roaring Waters Archetype", "Vanguard Of Roaring Waters");
        public static readonly FeatName WitnessToAncientBattlesArchetype = ModManager.RegisterFeatName("Witness To Ancient Battles Archetype", "Witness To Ancient Battles");
        //Features
        public static readonly FeatName ThirdApparition = ModManager.RegisterFeatName("Third Apparition");
        public static readonly FeatName FourthApparition = ModManager.RegisterFeatName("Fourth Apparition");
        public static readonly FeatName WanderingFeat = ModManager.RegisterFeatName("Wandering Feat");
        //Feats
        // 1st
        public static readonly FeatName ApparitionSense = ModManager.RegisterFeatName("Apparition Sense");
        public static readonly FeatName ChannelersStance = ModManager.RegisterFeatName("Channeler's Stance");
        public static readonly FeatName CircleOfSpirits = ModManager.RegisterFeatName("Circle Of Spirits");
        public static readonly FeatName RelinquishControl = ModManager.RegisterFeatName("Relinquish Control");
        public static readonly FeatName SpiritFamiliar = ModManager.RegisterFeatName("Spirit Familiar");
        // 2nd
        public static readonly FeatName ConcealSpell = ModManager.RegisterFeatName("Conceal Spell");
        public static readonly FeatName EmbodimentOfTheBalance = ModManager.RegisterFeatName("Embodiment Of The Balance");
        public static readonly FeatName EnhancedFamiliar = ModManager.RegisterFeatName("EnhancedFamiliarAnimist", "Enhanced Familiar");
        public static readonly FeatName GraspingSpiritsSpell = ModManager.RegisterFeatName("Grasping Spirits Spell");
        public static readonly FeatName SpiritualExpansionSpell = ModManager.RegisterFeatName("Spiritual Expansion Spell");
        // 4th
        public static readonly FeatName ApparitionsEnhancement = ModManager.RegisterFeatName("Apparitions Enhancement");
        public static readonly FeatName ChanneledProtection = ModManager.RegisterFeatName("Channeled Protection");
        public static readonly FeatName WalkTheWilds = ModManager.RegisterFeatName("Walk The Wilds");
        // 6th
        public static readonly FeatName ApparitionStabilization = ModManager.RegisterFeatName("Apparition Stabilization");
        public static readonly FeatName BlazingSpirit = ModManager.RegisterFeatName("Blazing Spirit");
        public static readonly FeatName GrudgeStrike = ModManager.RegisterFeatName("Grudge Strike");
        public static readonly FeatName MediumsAwareness = ModManager.RegisterFeatName("Medium's Awareness");
        public static readonly FeatName RoaringHeart = ModManager.RegisterFeatName("Roaring Heart");
        // 8th
        public static readonly FeatName ApparitionsReflection = ModManager.RegisterFeatName("Apparition's Reflection");
        public static readonly FeatName InstinctiveManeuvers = ModManager.RegisterFeatName("Instinctive Maneuvers");
        public static readonly FeatName SpiritWalk = ModManager.RegisterFeatName("Spirit Walk");
        public static readonly FeatName WindSeeker = ModManager.RegisterFeatName("Wind Seeker");
        // 10th
        public static readonly FeatName IncredibleFamiliar = ModManager.RegisterFeatName("IncredibleFamiliarAnimist", "Incredible Familiar");
    }
}

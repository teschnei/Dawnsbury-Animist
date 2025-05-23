using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Classes.Animist.RegisteredComponents
{
    public static class AnimistTrait
    {
        public static readonly Trait Animist = ModManager.RegisterTrait("Animist", new TraitProperties("Animist", true) { IsClassTrait = true });
        public static readonly Trait Apparition = ModManager.RegisterTrait("Apparition", new TraitProperties("Apparition", true));
        public static readonly Trait ApparitionAttuned = ModManager.RegisterTrait("ApparitionAttuned", new TraitProperties("Attuned Apparition", false));
        public static readonly Trait ApparitionPrimary = ModManager.RegisterTrait("ApparitionPrimary", new TraitProperties("Primary Apparition", false));
        public static readonly Trait Wandering = ModManager.RegisterTrait("Wandering", new TraitProperties("Wandering", true));
    }
}

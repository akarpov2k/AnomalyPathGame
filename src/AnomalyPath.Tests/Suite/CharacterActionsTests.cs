#nullable enable
namespace AnomalyPath.Tests
{
    /// <summary>Действия персонажа: экипировка, снятие, артефакты, патроны, предметы, разборка и слоты по умолчанию.</summary>
    public sealed partial class CharacterActionsTests : TestBase
    {
        private const string ArmorJson = "{\"kind\":\"items\",\"id\":\"armor_test\",\"nameKey\":\"n\",\"category\":\"armor\",\"footprint\":[2,2],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"artifactSlots\":1,\"material\":\"light\",\"durabilityFactor\":1.0,\"quality\":1}";
        private const string BeltJson = "{\"kind\":\"items\",\"id\":\"belt_test\",\"nameKey\":\"n\",\"category\":\"belt\",\"footprint\":[2,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"artifactSlots\":2}";
        private const string ArtifactJson = "{\"kind\":\"items\",\"id\":\"art_test\",\"nameKey\":\"n\",\"category\":\"artifact\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"artifactType\":\"fire\"}";
        private const string HelmetJson = "{\"kind\":\"items\",\"id\":\"helm_test\",\"nameKey\":\"n\",\"category\":\"helmet\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"material\":\"light\",\"durabilityFactor\":1.0,\"quality\":1}";
        private const string DualWeaponJson = "{\"kind\":\"items\",\"id\":\"dual_test\",\"nameKey\":\"n\",\"category\":\"weapon\",\"footprint\":[2,2],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"equipSlots\":[\"main\",\"secondary\"],\"weapon\":{\"classId\":\"w\",\"damageTypeId\":\"dmg_bullet\",\"damage\":1,\"accuracy\":0.5,\"magazine\":5,\"ammoCaliber\":\"c\",\"reloadTime\":1,\"armorPenetration\":0,\"durabilityMax\":10,\"durabilityPerShot\":0.1,\"defaultSkillId\":\"skill_shot\"}}";
        private const string OtherAmmoJson = "{\"kind\":\"items\",\"id\":\"ammo_other\",\"nameKey\":\"n\",\"category\":\"ammo\",\"footprint\":[1,1],\"maxStack\":60,\"weight\":0.1,\"caliber\":\"other\",\"tier\":1,\"icon\":\"i\"}";
        private const string AmmoApJson = "{\"kind\":\"items\",\"id\":\"ammo_ap_test\",\"nameKey\":\"n\",\"category\":\"ammo\",\"footprint\":[1,1],\"maxStack\":60,\"weight\":0.1,\"caliber\":\"c\",\"penetration\":0.05,\"tier\":1,\"icon\":\"i\"}";
        private const string BackpackJson = "{\"kind\":\"items\",\"id\":\"backpack_test\",\"nameKey\":\"n\",\"category\":\"backpack\",\"footprint\":[2,2],\"maxStack\":1,\"weight\":1.5,\"extraWeight\":10,\"tier\":1,\"icon\":\"i\"}";

        public override void Run()
        {
            RunEquip();
            RunUnequip();
            RunArtifacts();
            RunQuickSlots();
            RunUnload();
            RunLoad();
            RunLoadFrom();
            RunMagazinePreservation();
            RunDisassemble();
            RunDiscard();
            RunMove();
            RunMissingItem();
            RunOverflowEquip();
            RunDefaultSlot();
        }
    }
}

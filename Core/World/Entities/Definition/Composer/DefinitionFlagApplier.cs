using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.World.Entities.Definition.Composer;

public static class DefinitionFlagApplier
{
    public static void Apply(EntityDefinition definition, ActorFlags flags, ActorFlagProperty flagProperties)
    {
        if (flagProperties.ClearFlags ?? false)
            definition.Flags.ClearAll();

        if (flags.Monster ?? false)
        {
            definition.Flags.ActivateMCross = true;
            definition.Flags.CanPass = true;
            definition.Flags.CanPushWalls = true;
            definition.Flags.CanUseWalls = true;
            definition.Flags.CountKill = true;
            definition.Flags.IsMonster = true;
            definition.Flags.Shootable = true;
            definition.Flags.Solid = true;
        }

        if (flags.Projectile ?? false)
        {
            definition.Flags.ActivateImpact = true;
            definition.Flags.ActivatePCross = true;
            definition.Flags.Dropoff = true;
            definition.Flags.Missile = true;
            definition.Flags.NoBlockmap = true;
            definition.Flags.NoGravity = true;
            definition.Flags.NoTeleport = true;
        }

        if (flags.AbsMaskAngle != null)
            definition.Flags.AbsMaskAngle = flags.AbsMaskAngle.Value;
        if (flags.AbsMaskPitch != null)
            definition.Flags.AbsMaskPitch = flags.AbsMaskPitch.Value;
        if (flags.ActivateImpact != null)
            definition.Flags.ActivateImpact = flags.ActivateImpact.Value;
        if (flags.ActivateMCross != null)
            definition.Flags.ActivateMCross = flags.ActivateMCross.Value;
        if (flags.ActivatePCross != null)
            definition.Flags.ActivatePCross = flags.ActivatePCross.Value;
        if (flags.ActLikeBridge != null)
            definition.Flags.ActLikeBridge = flags.ActLikeBridge.Value;
        if (flags.AdditivePoisonDamage != null)
            definition.Flags.AdditivePoisonDamage = flags.AdditivePoisonDamage.Value;
        if (flags.AdditivePoisonDuration != null)
            definition.Flags.AdditivePoisonDuration = flags.AdditivePoisonDuration.Value;
        if (flags.AimReflect != null)
            definition.Flags.AimReflect = flags.AimReflect.Value;
        if (flags.AllowBounceOnActors != null)
            definition.Flags.AllowBounceOnActors = flags.AllowBounceOnActors.Value;
        if (flags.AllowPain != null)
            definition.Flags.AllowPain = flags.AllowPain.Value;
        if (flags.AllowParticles != null)
            definition.Flags.AllowParticles = flags.AllowParticles.Value;
        if (flags.AllowThruFlags != null)
            definition.Flags.AllowThruFlags = flags.AllowThruFlags.Value;
        if (flags.AlwaysFast != null)
            definition.Flags.AlwaysFast = flags.AlwaysFast.Value;
        if (flags.AlwaysPuff != null)
            definition.Flags.AlwaysPuff = flags.AlwaysPuff.Value;
        if (flags.AlwaysRespawn != null)
            definition.Flags.AlwaysRespawn = flags.AlwaysRespawn.Value;
        if (flags.AlwaysTelefrag != null)
            definition.Flags.AlwaysTelefrag = flags.AlwaysTelefrag.Value;
        if (flags.Ambush != null)
            definition.Flags.Ambush = flags.Ambush.Value;
        if (flags.AvoidMelee != null)
            definition.Flags.AvoidMelee = flags.AvoidMelee.Value;
        if (flags.Blasted != null)
            definition.Flags.Blasted = flags.Blasted.Value;
        if (flags.BlockAsPlayer != null)
            definition.Flags.BlockAsPlayer = flags.BlockAsPlayer.Value;
        if (flags.BlockedBySolidActors != null)
            definition.Flags.BlockedBySolidActors = flags.BlockedBySolidActors.Value;
        if (flags.BloodlessImpact != null)
            definition.Flags.BloodlessImpact = flags.BloodlessImpact.Value;
        if (flags.BloodSplatter != null)
            definition.Flags.BloodSplatter = flags.BloodSplatter.Value;
        if (flags.Boss != null)
            definition.Flags.Boss = flags.Boss.Value;
        if (flags.BossDeath != null)
            definition.Flags.BossDeath = flags.BossDeath.Value;
        if (flags.BounceAutoOff != null)
            definition.Flags.BounceAutoOff = flags.BounceAutoOff.Value;
        if (flags.BounceAutoOffFloorOnly != null)
            definition.Flags.BounceAutoOffFloorOnly = flags.BounceAutoOffFloorOnly.Value;
        if (flags.BounceLikeHeretic != null)
            definition.Flags.BounceLikeHeretic = flags.BounceLikeHeretic.Value;
        if (flags.BounceOnActors != null)
            definition.Flags.BounceOnActors = flags.BounceOnActors.Value;
        if (flags.BounceOnCeilings != null)
            definition.Flags.BounceOnCeilings = flags.BounceOnCeilings.Value;
        if (flags.BounceOnFloors != null)
            definition.Flags.BounceOnFloors = flags.BounceOnFloors.Value;
        if (flags.BounceOnUnrippables != null)
            definition.Flags.BounceOnUnrippables = flags.BounceOnUnrippables.Value;
        if (flags.BounceOnWalls != null)
            definition.Flags.BounceOnWalls = flags.BounceOnWalls.Value;
        if (flags.Bright != null)
            definition.Flags.Bright = flags.Bright.Value;
        if (flags.Buddha != null)
            definition.Flags.Buddha = flags.Buddha.Value;
        if (flags.BumpSpecial != null)
            definition.Flags.BumpSpecial = flags.BumpSpecial.Value;
        if (flags.CanBlast != null)
            definition.Flags.CanBlast = flags.CanBlast.Value;
        if (flags.CanBounceWater != null)
            definition.Flags.CanBounceWater = flags.CanBounceWater.Value;
        if (flags.CannotPush != null)
            definition.Flags.CannotPush = flags.CannotPush.Value;
        if (flags.CanPass != null)
            definition.Flags.CanPass = flags.CanPass.Value;
        if (flags.CanPushWalls != null)
            definition.Flags.CanPushWalls = flags.CanPushWalls.Value;
        if (flags.CantLeaveFloorPic != null)
            definition.Flags.CantLeaveFloorPic = flags.CantLeaveFloorPic.Value;
        if (flags.CantSeek != null)
            definition.Flags.CantSeek = flags.CantSeek.Value;
        if (flags.CanUseWalls != null)
            definition.Flags.CanUseWalls = flags.CanUseWalls.Value;
        if (flags.CausePain != null)
            definition.Flags.CausePain = flags.CausePain.Value;
        if (flags.CeilingHugger != null)
            definition.Flags.CeilingHugger = flags.CeilingHugger.Value;
        if (flags.Corpse != null)
            definition.Flags.Corpse = flags.Corpse.Value;
        if (flags.CountItem != null)
            definition.Flags.CountItem = flags.CountItem.Value;
        if (flags.CountKill != null)
            definition.Flags.CountKill = flags.CountKill.Value;
        if (flags.CountSecret != null)
            definition.Flags.CountSecret = flags.CountSecret.Value;
        if (flags.Deflect != null)
            definition.Flags.Deflect = flags.Deflect.Value;
        if (flags.DehExplosion != null)
            definition.Flags.DehExplosion = flags.DehExplosion.Value;
        if (flags.DoHarmSpecies != null)
            definition.Flags.DoHarmSpecies = flags.DoHarmSpecies.Value;
        if (flags.DontBlast != null)
            definition.Flags.DontBlast = flags.DontBlast.Value;
        if (flags.DontBounceOnShootables != null)
            definition.Flags.DontBounceOnShootables = flags.DontBounceOnShootables.Value;
        if (flags.DontBounceOnSky != null)
            definition.Flags.DontBounceOnSky = flags.DontBounceOnSky.Value;
        if (flags.DontCorpse != null)
            definition.Flags.DontCorpse = flags.DontCorpse.Value;
        if (flags.DontDrain != null)
            definition.Flags.DontDrain = flags.DontDrain.Value;
        if (flags.DontFaceTalker != null)
            definition.Flags.DontFaceTalker = flags.DontFaceTalker.Value;
        if (flags.DontFall != null)
            definition.Flags.DontFall = flags.DontFall.Value;
        if (flags.DontGib != null)
            definition.Flags.DontGib = flags.DontGib.Value;
        if (flags.DontHarmClass != null)
            definition.Flags.DontHarmClass = flags.DontHarmClass.Value;
        if (flags.DontHarmSpecies != null)
            definition.Flags.DontHarmSpecies = flags.DontHarmSpecies.Value;
        if (flags.DontHurtSpecies != null)
            definition.Flags.DontHurtSpecies = flags.DontHurtSpecies.Value;
        if (flags.DontInterpolate != null)
            definition.Flags.DontInterpolate = flags.DontInterpolate.Value;
        if (flags.DontMorph != null)
            definition.Flags.DontMorph = flags.DontMorph.Value;
        if (flags.DontOverlap != null)
            definition.Flags.DontOverlap = flags.DontOverlap.Value;
        if (flags.DontReflect != null)
            definition.Flags.DontReflect = flags.DontReflect.Value;
        if (flags.DontRip != null)
            definition.Flags.DontRip = flags.DontRip.Value;
        if (flags.DontSeekInvisible != null)
            definition.Flags.DontSeekInvisible = flags.DontSeekInvisible.Value;
        if (flags.DontSplash != null)
            definition.Flags.DontSplash = flags.DontSplash.Value;
        if (flags.DontSquash != null)
            definition.Flags.DontSquash = flags.DontSquash.Value;
        if (flags.DontThrust != null)
            definition.Flags.DontThrust = flags.DontThrust.Value;
        if (flags.DontTranslate != null)
            definition.Flags.DontTranslate = flags.DontTranslate.Value;
        if (flags.DoomBounce != null)
            definition.Flags.DoomBounce = flags.DoomBounce.Value;
        if (flags.Dormant != null)
            definition.Flags.Dormant = flags.Dormant.Value;
        if (flags.Dropoff != null)
            definition.Flags.Dropoff = flags.Dropoff.Value;
        if (flags.Dropped != null)
            definition.Flags.Dropped = flags.Dropped.Value;
        if (flags.ExploCount != null)
            definition.Flags.ExploCount = flags.ExploCount.Value;
        if (flags.ExplodeOnWater != null)
            definition.Flags.ExplodeOnWater = flags.ExplodeOnWater.Value;
        if (flags.ExtremeDeath != null)
            definition.Flags.ExtremeDeath = flags.ExtremeDeath.Value;
        if (flags.Faster != null)
            definition.Flags.Faster = flags.Faster.Value;
        if (flags.FastMelee != null)
            definition.Flags.FastMelee = flags.FastMelee.Value;
        if (flags.FireDamage != null)
            definition.Flags.FireDamage = flags.FireDamage.Value;
        if (flags.FireResist != null)
            definition.Flags.FireResist = flags.FireResist.Value;
        if (flags.FixMapThingPos != null)
            definition.Flags.FixMapThingPos = flags.FixMapThingPos.Value;
        if (flags.FlatSprite != null)
            definition.Flags.FlatSprite = flags.FlatSprite.Value;
        if (flags.Float != null)
            definition.Flags.Float = flags.Float.Value;
        if (flags.FloatBob != null)
            definition.Flags.FloatBob = flags.FloatBob.Value;
        if (flags.FloorClip != null)
            definition.Flags.FloorClip = flags.FloorClip.Value;
        if (flags.FloorHugger != null)
            definition.Flags.FloorHugger = flags.FloorHugger.Value;
        if (flags.FoilBuddha != null)
            definition.Flags.FoilBuddha = flags.FoilBuddha.Value;
        if (flags.FoilInvul != null)
            definition.Flags.FoilInvul = flags.FoilInvul.Value;
        if (flags.ForceDecal != null)
            definition.Flags.ForceDecal = flags.ForceDecal.Value;
        if (flags.ForceInFighting != null)
            definition.Flags.ForceInFighting = flags.ForceInFighting.Value;
        if (flags.ForcePain != null)
            definition.Flags.ForcePain = flags.ForcePain.Value;
        if (flags.ForceRadiusDmg != null)
            definition.Flags.ForceRadiusDmg = flags.ForceRadiusDmg.Value;
        if (flags.ForceXYBillboard != null)
            definition.Flags.ForceXYBillboard = flags.ForceXYBillboard.Value;
        if (flags.ForceYBillboard != null)
            definition.Flags.ForceYBillboard = flags.ForceYBillboard.Value;
        if (flags.ForceZeroRadiusDmg != null)
            definition.Flags.ForceZeroRadiusDmg = flags.ForceZeroRadiusDmg.Value;
        if (flags.Friendly != null)
            definition.Flags.Friendly = flags.Friendly.Value;
        if (flags.Frightened != null)
            definition.Flags.Frightened = flags.Frightened.Value;
        if (flags.Frightening != null)
            definition.Flags.Frightening = flags.Frightening.Value;
        if (flags.FullVolActive != null)
            definition.Flags.FullVolActive = flags.FullVolActive.Value;
        if (flags.FullVolDeath != null)
            definition.Flags.FullVolDeath = flags.FullVolDeath.Value;
        if (flags.GetOwner != null)
            definition.Flags.GetOwner = flags.GetOwner.Value;
        if (flags.Ghost != null)
            definition.Flags.Ghost = flags.Ghost.Value;
        if (flags.GrenadeTrail != null)
            definition.Flags.GrenadeTrail = flags.GrenadeTrail.Value;
        if (flags.HarmFriends != null)
            definition.Flags.HarmFriends = flags.HarmFriends.Value;
        if (flags.HereticBounce != null)
            definition.Flags.HereticBounce = flags.HereticBounce.Value;
        if (flags.HexenBounce != null)
            definition.Flags.HexenBounce = flags.HexenBounce.Value;
        if (flags.HitMaster != null)
            definition.Flags.HitMaster = flags.HitMaster.Value;
        if (flags.HitOwner != null)
            definition.Flags.HitOwner = flags.HitOwner.Value;
        if (flags.HitTarget != null)
            definition.Flags.HitTarget = flags.HitTarget.Value;
        if (flags.HitTracer != null)
            definition.Flags.HitTracer = flags.HitTracer.Value;
        if (flags.IceCorpse != null)
            definition.Flags.IceCorpse = flags.IceCorpse.Value;
        if (flags.IceDamage != null)
            definition.Flags.IceDamage = flags.IceDamage.Value;
        if (flags.IceShatter != null)
            definition.Flags.IceShatter = flags.IceShatter.Value;
        if (flags.InCombat != null)
            definition.Flags.InCombat = flags.InCombat.Value;
        if (flags.InterpolateAngles != null)
            definition.Flags.InterpolateAngles = flags.InterpolateAngles.Value;
        if (flags.Inventory.AdditiveTime != null)
            definition.Flags.InventoryAdditiveTime = flags.Inventory.AdditiveTime.Value;
        if (flags.Inventory.AlwaysPickup != null)
            definition.Flags.InventoryAlwaysPickup = flags.Inventory.AlwaysPickup.Value;
        if (flags.Inventory.AlwaysRespawn != null)
            definition.Flags.InventoryAlwaysRespawn = flags.Inventory.AlwaysRespawn.Value;
        if (flags.Inventory.AutoActivate != null)
            definition.Flags.InventoryAutoActivate = flags.Inventory.AutoActivate.Value;
        if (flags.Inventory.BigPowerup != null)
            definition.Flags.InventoryBigPowerup = flags.Inventory.BigPowerup.Value;
        if (flags.Inventory.FancyPickupSound != null)
            definition.Flags.InventoryFancyPickupSound = flags.Inventory.FancyPickupSound.Value;
        if (flags.Inventory.HubPower != null)
            definition.Flags.InventoryHubPower = flags.Inventory.HubPower.Value;
        if (flags.Inventory.IgnoreSkill != null)
            definition.Flags.InventoryIgnoreSkill = flags.Inventory.IgnoreSkill.Value;
        if (flags.Inventory.InterHubStrip != null)
            definition.Flags.InventoryInterHubStrip = flags.Inventory.InterHubStrip.Value;
        if (flags.Inventory.Invbar != null)
            definition.Flags.InventoryInvbar = flags.Inventory.Invbar.Value;
        if (flags.Inventory.IsArmor != null)
            definition.Flags.InventoryIsArmor = flags.Inventory.IsArmor.Value;
        if (flags.Inventory.IsHealth != null)
            definition.Flags.InventoryIsHealth = flags.Inventory.IsHealth.Value;
        if (flags.Inventory.KeepDepleted != null)
            definition.Flags.InventoryKeepDepleted = flags.Inventory.KeepDepleted.Value;
        if (flags.Inventory.NeverRespawn != null)
            definition.Flags.InventoryNeverRespawn = flags.Inventory.NeverRespawn.Value;
        if (flags.Inventory.NoAttenPickupSound != null)
            definition.Flags.InventoryNoAttenPickupSound = flags.Inventory.NoAttenPickupSound.Value;
        if (flags.Inventory.NoScreenBlink != null)
            definition.Flags.InventoryNoScreenBlink = flags.Inventory.NoScreenBlink.Value;
        if (flags.Inventory.NoScreenFlash != null)
            definition.Flags.InventoryNoScreenFlash = flags.Inventory.NoScreenFlash.Value;
        if (flags.Inventory.NoTeleportFreeze != null)
            definition.Flags.InventoryNoTeleportFreeze = flags.Inventory.NoTeleportFreeze.Value;
        if (flags.Inventory.PersistentPower != null)
            definition.Flags.InventoryPersistentPower = flags.Inventory.PersistentPower.Value;
        if (flags.Inventory.PickupFlash != null)
            definition.Flags.InventoryPickupFlash = flags.Inventory.PickupFlash.Value;
        if (flags.Inventory.Quiet != null)
            definition.Flags.InventoryQuiet = flags.Inventory.Quiet.Value;
        if (flags.Inventory.RestrictAbsolutely != null)
            definition.Flags.InventoryRestrictAbsolutely = flags.Inventory.RestrictAbsolutely.Value;
        if (flags.Inventory.Tossed != null)
            definition.Flags.InventoryTossed = flags.Inventory.Tossed.Value;
        if (flags.Inventory.Transfer != null)
            definition.Flags.InventoryTransfer = flags.Inventory.Transfer.Value;
        if (flags.Inventory.Unclearable != null)
            definition.Flags.InventoryUnclearable = flags.Inventory.Unclearable.Value;
        if (flags.Inventory.Undroppable != null)
            definition.Flags.InventoryUndroppable = flags.Inventory.Undroppable.Value;
        if (flags.Inventory.Untossable != null)
            definition.Flags.InventoryUntossable = flags.Inventory.Untossable.Value;
        if (flags.Invisible != null)
            definition.Flags.Invisible = flags.Invisible.Value;
        if (flags.Invulnerable != null)
            definition.Flags.Invulnerable = flags.Invulnerable.Value;
        if (flags.IsMonster != null)
            definition.Flags.IsMonster = flags.IsMonster.Value;
        if (flags.IsTeleportSpot != null)
            definition.Flags.IsTeleportSpot = flags.IsTeleportSpot.Value;
        if (flags.JumpDown != null)
            definition.Flags.JumpDown = flags.JumpDown.Value;
        if (flags.JustAttacked != null)
            definition.Flags.JustAttacked = flags.JustAttacked.Value;
        if (flags.JustHit != null)
            definition.Flags.JustHit = flags.JustHit.Value;
        if (flags.LaxTeleFragDmg != null)
            definition.Flags.LaxTeleFragDmg = flags.LaxTeleFragDmg.Value;
        if (flags.LongMeleeRange != null)
            definition.Flags.LongMeleeRange = flags.LongMeleeRange.Value;
        if (flags.LookAllAround != null)
            definition.Flags.LookAllAround = flags.LookAllAround.Value;
        if (flags.LowGravity != null)
            definition.Flags.LowGravity = flags.LowGravity.Value;
        if (flags.MaskRotation != null)
            definition.Flags.MaskRotation = flags.MaskRotation.Value;
        if (flags.MbfBouncer != null)
            definition.Flags.MbfBouncer = flags.MbfBouncer.Value;
        if (flags.MirrorReflect != null)
            definition.Flags.MirrorReflect = flags.MirrorReflect.Value;
        if (flags.Missile != null)
            definition.Flags.Missile = flags.Missile.Value;
        if (flags.MissileEvenMore != null)
            definition.Flags.MissileEvenMore = flags.MissileEvenMore.Value;
        if (flags.MissileMore != null)
            definition.Flags.MissileMore = flags.MissileMore.Value;
        if (flags.Monster != null)
        {
            definition.Flags.Shootable = true;
            definition.Flags.CountKill = true;
            definition.Flags.Solid = true;
            definition.Flags.CanPushWalls = true;
            definition.Flags.CanUseWalls = true;
            definition.Flags.ActivateMCross = true;
            definition.Flags.CanPass = true;
            definition.Flags.IsMonster = true;
        }
        if (flags.MoveWithSector != null)
            definition.Flags.MoveWithSector = flags.MoveWithSector.Value;
        if (flags.MThruSpecies != null)
            definition.Flags.MThruSpecies = flags.MThruSpecies.Value;
        if (flags.NeverFast != null)
            definition.Flags.NeverFast = flags.NeverFast.Value;
        if (flags.NeverRespawn != null)
            definition.Flags.NeverRespawn = flags.NeverRespawn.Value;
        if (flags.NeverTarget != null)
            definition.Flags.NeverTarget = flags.NeverTarget.Value;
        if (flags.NoBlockmap != null)
            definition.Flags.NoBlockmap = flags.NoBlockmap.Value;
        if (flags.NoBlockMonst != null)
            definition.Flags.NoBlockMonst = flags.NoBlockMonst.Value;
        if (flags.NoBlood != null)
            definition.Flags.NoBlood = flags.NoBlood.Value;
        if (flags.NoBloodDecals != null)
            definition.Flags.NoBloodDecals = flags.NoBloodDecals.Value;
        if (flags.NoBossRip != null)
            definition.Flags.NoBossRip = flags.NoBossRip.Value;
        if (flags.NoBounceSound != null)
            definition.Flags.NoBounceSound = flags.NoBounceSound.Value;
        if (flags.NoClip != null)
            definition.Flags.NoClip = flags.NoClip.Value;
        if (flags.NoDamage != null)
            definition.Flags.NoDamage = flags.NoDamage.Value;
        if (flags.NoDamageThrust != null)
            definition.Flags.NoDamageThrust = flags.NoDamageThrust.Value;
        if (flags.NoDecal != null)
            definition.Flags.NoDecal = flags.NoDecal.Value;
        if (flags.NoDropoff != null)
            definition.Flags.NoDropoff = flags.NoDropoff.Value;
        if (flags.NoExplodeFloor != null)
            definition.Flags.NoExplodeFloor = flags.NoExplodeFloor.Value;
        if (flags.NoExtremeDeath != null)
            definition.Flags.NoExtremeDeath = flags.NoExtremeDeath.Value;
        if (flags.NoFear != null)
            definition.Flags.NoFear = flags.NoFear.Value;
        if (flags.NoFriction != null)
            definition.Flags.NoFriction = flags.NoFriction.Value;
        if (flags.NoFrictionBounce != null)
            definition.Flags.NoFrictionBounce = flags.NoFrictionBounce.Value;
        if (flags.NoForwardFall != null)
            definition.Flags.NoForwardFall = flags.NoForwardFall.Value;
        if (flags.NoGravity != null)
            definition.Flags.NoGravity = flags.NoGravity.Value;
        if (flags.NoIceDeath != null)
            definition.Flags.NoIceDeath = flags.NoIceDeath.Value;
        if (flags.NoInfighting != null)
            definition.Flags.NoInfighting = flags.NoInfighting.Value;
        if (flags.NoInfightSpecies != null)
            definition.Flags.NoInfightSpecies = flags.NoInfightSpecies.Value;
        if (flags.NoInteraction != null)
            definition.Flags.NoInteraction = flags.NoInteraction.Value;
        if (flags.NoKillScripts != null)
            definition.Flags.NoKillScripts = flags.NoKillScripts.Value;
        if (flags.NoLiftDrop != null)
            definition.Flags.NoLiftDrop = flags.NoLiftDrop.Value;
        if (flags.NoMenu != null)
            definition.Flags.NoMenu = flags.NoMenu.Value;
        if (flags.NonShootable != null)
            definition.Flags.NonShootable = flags.NonShootable.Value;
        if (flags.NoPain != null)
            definition.Flags.NoPain = flags.NoPain.Value;
        if (flags.NoRadiusDmg != null)
            definition.Flags.NoRadiusDmg = flags.NoRadiusDmg.Value;
        if (flags.NoSector != null)
            definition.Flags.NoSector = flags.NoSector.Value;
        if (flags.NoSkin != null)
            definition.Flags.NoSkin = flags.NoSkin.Value;
        if (flags.NoSplashAlert != null)
            definition.Flags.NoSplashAlert = flags.NoSplashAlert.Value;
        if (flags.NoTarget != null)
            definition.Flags.NoTarget = flags.NoTarget.Value;
        if (flags.NoTargetSwitch != null)
            definition.Flags.NoTargetSwitch = flags.NoTargetSwitch.Value;
        if (flags.NotAutoaimed != null)
            definition.Flags.NotAutoaimed = flags.NotAutoaimed.Value;
        if (flags.NotDMatch != null)
            definition.Flags.NotDMatch = flags.NotDMatch.Value;
        if (flags.NoTelefrag != null)
            definition.Flags.NoTelefrag = flags.NoTelefrag.Value;
        if (flags.NoTeleOther != null)
            definition.Flags.NoTeleOther = flags.NoTeleOther.Value;
        if (flags.NoTeleport != null)
            definition.Flags.NoTeleport = flags.NoTeleport.Value;
        if (flags.NoTelestomp != null)
            definition.Flags.NoTelestomp = flags.NoTelestomp.Value;
        if (flags.NoTimeFreeze != null)
            definition.Flags.NoTimeFreeze = flags.NoTimeFreeze.Value;
        if (flags.NotOnAutomap != null)
            definition.Flags.NotOnAutomap = flags.NotOnAutomap.Value;
        if (flags.NoTrigger != null)
            definition.Flags.NoTrigger = flags.NoTrigger.Value;
        if (flags.NoVerticalMeleeRange != null)
            definition.Flags.NoVerticalMeleeRange = flags.NoVerticalMeleeRange.Value;
        if (flags.NoWallBounceSnd != null)
            definition.Flags.NoWallBounceSnd = flags.NoWallBounceSnd.Value;
        if (flags.OldRadiusDmg != null)
            definition.Flags.OldRadiusDmg = flags.OldRadiusDmg.Value;
        if (flags.Painless != null)
            definition.Flags.Painless = flags.Painless.Value;
        if (flags.Pickup != null)
            definition.Flags.Pickup = flags.Pickup.Value;
        if (flags.PierceArmor != null)
            definition.Flags.PierceArmor = flags.PierceArmor.Value;
        if (flags.PlayerPawn.CanSuperMorph != null)
            definition.Flags.PlayerPawnCanSuperMorph = flags.PlayerPawn.CanSuperMorph.Value;
        if (flags.PlayerPawn.CrouchableMorph != null)
            definition.Flags.PlayerPawnCrouchableMorph = flags.PlayerPawn.CrouchableMorph.Value;
        if (flags.PlayerPawn.NoThrustWhenInvul != null)
            definition.Flags.PlayerPawnNoThrustWhenInvul = flags.PlayerPawn.NoThrustWhenInvul.Value;
        if (flags.PoisonAlways != null)
            definition.Flags.PoisonAlways = flags.PoisonAlways.Value;
        if (flags.Projectile != null)
        {
            definition.Flags.NoBlockmap = true;
            definition.Flags.NoGravity = true;
            definition.Flags.Dropoff = true;
            definition.Flags.Missile = true;
            definition.Flags.ActivateImpact = true;
            definition.Flags.ActivatePCross = true;
            definition.Flags.NoTeleport = true;
        }
        if (flags.PuffGetsOwner != null)
            definition.Flags.PuffGetsOwner = flags.PuffGetsOwner.Value;
        if (flags.PuffOnActors != null)
            definition.Flags.PuffOnActors = flags.PuffOnActors.Value;
        if (flags.Pushable != null)
            definition.Flags.Pushable = flags.Pushable.Value;
        if (flags.QuarterGravity != null)
            definition.Flags.QuarterGravity = flags.QuarterGravity.Value;
        if (flags.QuickToRetaliate != null)
            definition.Flags.QuickToRetaliate = flags.QuickToRetaliate.Value;
        if (flags.Randomize != null)
            definition.Flags.Randomize = flags.Randomize.Value;
        if (flags.Reflective != null)
            definition.Flags.Reflective = flags.Reflective.Value;
        if (flags.RelativeToFloor != null)
            definition.Flags.RelativeToFloor = flags.RelativeToFloor.Value;
        if (flags.Ripper != null)
            definition.Flags.Ripper = flags.Ripper.Value;
        if (flags.RocketTrail != null)
            definition.Flags.RocketTrail = flags.RocketTrail.Value;
        if (flags.RollCenter != null)
            definition.Flags.RollCenter = flags.RollCenter.Value;
        if (flags.RollSprite != null)
            definition.Flags.RollSprite = flags.RollSprite.Value;
        if (flags.ScreenSeeker != null)
            definition.Flags.ScreenSeeker = flags.ScreenSeeker.Value;
        if (flags.SeeInvisible != null)
            definition.Flags.SeeInvisible = flags.SeeInvisible.Value;
        if (flags.SeekerMissile != null)
            definition.Flags.SeekerMissile = flags.SeekerMissile.Value;
        if (flags.SeesDaggers != null)
            definition.Flags.SeesDaggers = flags.SeesDaggers.Value;
        if (flags.Shadow != null)
            definition.Flags.Shadow = flags.Shadow.Value;
        if (flags.ShieldReflect != null)
            definition.Flags.ShieldReflect = flags.ShieldReflect.Value;
        if (flags.Shootable != null)
            definition.Flags.Shootable = flags.Shootable.Value;
        if (flags.ShortMissileRange != null)
            definition.Flags.ShortMissileRange = flags.ShortMissileRange.Value;
        if (flags.Skullfly != null)
            definition.Flags.Skullfly = flags.Skullfly.Value;
        if (flags.SkyExplode != null)
            definition.Flags.SkyExplode = flags.SkyExplode.Value;
        if (flags.SlidesOnWalls != null)
            definition.Flags.SlidesOnWalls = flags.SlidesOnWalls.Value;
        if (flags.Solid != null)
            definition.Flags.Solid = flags.Solid.Value;
        if (flags.SpawnCeiling != null)
            definition.Flags.SpawnCeiling = flags.SpawnCeiling.Value;
        if (flags.SpawnFloat != null)
            definition.Flags.SpawnFloat = flags.SpawnFloat.Value;
        if (flags.SpawnSoundSource != null)
            definition.Flags.SpawnSoundSource = flags.SpawnSoundSource.Value;
        if (flags.Special != null)
            definition.Flags.Special = flags.Special.Value;
        if (flags.SpecialFireDamage != null)
            definition.Flags.SpecialFireDamage = flags.SpecialFireDamage.Value;
        if (flags.SpecialFloorClip != null)
            definition.Flags.SpecialFloorClip = flags.SpecialFloorClip.Value;
        if (flags.Spectral != null)
            definition.Flags.Spectral = flags.Spectral.Value;
        if (flags.SpriteAngle != null)
            definition.Flags.SpriteAngle = flags.SpriteAngle.Value;
        if (flags.SpriteFlip != null)
            definition.Flags.SpriteFlip = flags.SpriteFlip.Value;
        if (flags.StandStill != null)
            definition.Flags.StandStill = flags.StandStill.Value;
        if (flags.StayMorphed != null)
            definition.Flags.StayMorphed = flags.StayMorphed.Value;
        if (flags.Stealth != null)
            definition.Flags.Stealth = flags.Stealth.Value;
        if (flags.StepMissile != null)
            definition.Flags.StepMissile = flags.StepMissile.Value;
        if (flags.StrifeDamage != null)
            definition.Flags.StrifeDamage = flags.StrifeDamage.Value;
        if (flags.SummonedMonster != null)
            definition.Flags.SummonedMonster = flags.SummonedMonster.Value;
        if (flags.Synchronized != null)
            definition.Flags.Synchronized = flags.Synchronized.Value;
        if (flags.Teleport != null)
            definition.Flags.Teleport = flags.Teleport.Value;
        if (flags.Telestomp != null)
            definition.Flags.Telestomp = flags.Telestomp.Value;
        if (flags.ThruActors != null)
            definition.Flags.ThruActors = flags.ThruActors.Value;
        if (flags.ThruGhost != null)
            definition.Flags.ThruGhost = flags.ThruGhost.Value;
        if (flags.ThruReflect != null)
            definition.Flags.ThruReflect = flags.ThruReflect.Value;
        if (flags.ThruSpecies != null)
            definition.Flags.ThruSpecies = flags.ThruSpecies.Value;
        if (flags.Touchy != null)
            definition.Flags.Touchy = flags.Touchy.Value;
        if (flags.UseBounceState != null)
            definition.Flags.UseBounceState = flags.UseBounceState.Value;
        if (flags.UseKillScripts != null)
            definition.Flags.UseKillScripts = flags.UseKillScripts.Value;
        if (flags.UseSpecial != null)
            definition.Flags.UseSpecial = flags.UseSpecial.Value;
        if (flags.VisibilityPulse != null)
            definition.Flags.VisibilityPulse = flags.VisibilityPulse.Value;
        if (flags.Vulnerable != null)
            definition.Flags.Vulnerable = flags.Vulnerable.Value;
        if (flags.WallSprite != null)
            definition.Flags.WallSprite = flags.WallSprite.Value;
        if (flags.Weapon.AltAmmoOptional != null)
            definition.Flags.WeaponAltAmmoOptional = flags.Weapon.AltAmmoOptional.Value;
        if (flags.Weapon.AltUsesBoth != null)
            definition.Flags.WeaponAltUsesBoth = flags.Weapon.AltUsesBoth.Value;
        if (flags.Weapon.AmmoCheckBoth != null)
            definition.Flags.WeaponAmmoCheckBoth = flags.Weapon.AmmoCheckBoth.Value;
        if (flags.Weapon.AmmoOptional != null)
            definition.Flags.WeaponAmmoOptional = flags.Weapon.AmmoOptional.Value;
        if (flags.Weapon.AxeBlood != null)
            definition.Flags.WeaponAxeBlood = flags.Weapon.AxeBlood.Value;
        if (flags.Weapon.Bfg != null)
            definition.Flags.WeaponBfg = flags.Weapon.Bfg.Value;
        if (flags.Weapon.CheatNotWeapon != null)
            definition.Flags.WeaponCheatNotWeapon = flags.Weapon.CheatNotWeapon.Value;
        if (flags.Weapon.DontBob != null)
            definition.Flags.WeaponDontBob = flags.Weapon.DontBob.Value;
        if (flags.Weapon.Explosive != null)
            definition.Flags.WeaponExplosive = flags.Weapon.Explosive.Value;
        if (flags.Weapon.MeleeWeapon != null)
            definition.Flags.WeaponMeleeWeapon = flags.Weapon.MeleeWeapon.Value;
        if (flags.Weapon.NoAlert != null)
            definition.Flags.WeaponNoAlert = flags.Weapon.NoAlert.Value;
        if (flags.Weapon.NoAutoaim != null)
            definition.Flags.WeaponNoAutoaim = flags.Weapon.NoAutoaim.Value;
        if (flags.Weapon.NoAutofire != null)
            definition.Flags.WeaponNoAutofire = flags.Weapon.NoAutofire.Value;
        if (flags.Weapon.NoDeathDeselect != null)
            definition.Flags.WeaponNoDeathDeselect = flags.Weapon.NoDeathDeselect.Value;
        if (flags.Weapon.NoDeathInput != null)
            definition.Flags.WeaponNoDeathInput = flags.Weapon.NoDeathInput.Value;
        if (flags.Weapon.NoAutoSwitch != null)
            definition.Flags.WeaponNoAutoSwitch = flags.Weapon.NoAutoSwitch.Value;
        if (flags.Weapon.PoweredUp != null)
            definition.Flags.WeaponPoweredUp = flags.Weapon.PoweredUp.Value;
        if (flags.Weapon.PrimaryUsesBoth != null)
            definition.Flags.WeaponPrimaryUsesBoth = flags.Weapon.PrimaryUsesBoth.Value;
        if (flags.Weapon.ReadySndHalf != null)
            definition.Flags.WeaponReadySndHalf = flags.Weapon.ReadySndHalf.Value;
        if (flags.Weapon.Staff2Kickback != null)
            definition.Flags.WeaponStaff2Kickback = flags.Weapon.Staff2Kickback.Value;
        if (flags.Weapon.WimpyWeapon != null)
            definition.Flags.WeaponWimpyWeapon = flags.Weapon.WimpyWeapon.Value;
        if (flags.Weapon.Spawn != null)
            definition.Flags.WeaponSpawn = flags.Weapon.Spawn.Value;
        if (flags.WindThrust != null)
            definition.Flags.WindThrust = flags.WindThrust.Value;
        if (flags.ZdoomTrans != null)
            definition.Flags.ZdoomTrans = flags.ZdoomTrans.Value;
        if (flags.E1M8Boss != null)
            definition.Flags.E1M8Boss = flags.E1M8Boss.Value;
        if (flags.E2M8Boss != null)
            definition.Flags.E2M8Boss = flags.E2M8Boss.Value;
        if (flags.E3M8Boss != null)
            definition.Flags.E3M8Boss = flags.E3M8Boss.Value;
        if (flags.E4M6Boss != null)
            definition.Flags.E3M8Boss = flags.E4M6Boss.Value;
        if (flags.E4M8Boss != null)
            definition.Flags.E4M8Boss = flags.E4M8Boss.Value;
        if (flags.FullVolSee != null)
            definition.Flags.FullVolSee = flags.FullVolSee.Value;
    }
}

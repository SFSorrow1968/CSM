using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000007 RID: 7
	public class CreatureBoneBreakingModule : CreatureModule
	{
		// Token: 0x06000037 RID: 55 RVA: 0x00002CCF File Offset: 0x00000ECF
		public override void Enabled()
		{
			this.SetupProfiles();
			base.Enabled();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002CE0 File Offset: 0x00000EE0
		public override void Update()
		{
			if (!ModOptions.boneBreaking || Utils.IsNullOrEmpty(this._profiles))
			{
				return;
			}
			for (int i = 0; i < this._profiles.Length; i++)
			{
				if (!this._profiles[i].isBroken && !this.IsBoneVariantBroken(this._profiles[i]) && this._profiles[i].CanBreak())
				{
					this.Break(this._profiles[i]);
				}
				if (this._profiles[i].isBroken)
				{
					if (this._profiles[i].paralysis || this._profiles[i].partialParalysis)
					{
						this.creature.ragdoll.SetState(1);
						if (this._profiles[i].paralysis)
						{
							float mult = ModOptions.paralysisLimpMultiplier;
							foreach (RagdollPart part in this.creature.ragdoll.parts)
							{
								this.creature.ragdoll.SetPinForceMultiplier(mult, mult, mult, mult, false, false, part.type, null);
							}
						}
					}
					this.SetJointForceForBrokenProfile(this._profiles[i]);
				}
			}
			base.Update();
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002E28 File Offset: 0x00001028
		public override void Restore()
		{
			for (int i = 0; i < this._profiles.Length; i++)
			{
				this._profiles[i].isBroken = false;
				this._profiles[i].bone.ResetCharJointLimit();
				this._profiles[i].bone.ragdoll.ResetPinForce(true, false, this._profiles[i].bone.type);
			}
			this.creature.ragdoll.SetState(3, true);
			base.Restore();
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002EAC File Offset: 0x000010AC
		public void BreakAll()
		{
			for (int i = 0; i < this._profiles.Length; i++)
			{
				BoneProfile profile = this._profiles[i];
				if (!this.IsBoneVariantBroken(profile))
				{
					this.Break(profile);
				}
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002EE8 File Offset: 0x000010E8
		private bool IsBoneVariantBroken(BoneProfile profile)
		{
			for (int i = 0; i < this._profiles.Length; i++)
			{
				if (this._profiles[i].bone == profile.bone && !this._profiles[i].allowMultipleBreaks && this._profiles[i].isBroken)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002F44 File Offset: 0x00001144
		private void Break(BoneProfile profile)
		{
			if (profile.isBroken || profile.bone == null || !ModOptions.boneBreaking)
			{
				return;
			}
			if (!ModOptions.breakableArms && (profile.bone.type == 8 || profile.bone.type == 16))
			{
				return;
			}
			if (!ModOptions.breakableLegs && (profile.bone.type == 128 || profile.bone.type == 256))
			{
				return;
			}
			if (!ModOptions.breakableSpine && profile.bone.type == 4)
			{
				return;
			}
			if (!ModOptions.neckSnapping && (profile.bone.type == 1 || profile.bone.type == 2))
			{
				return;
			}
			profile.isBroken = true;
			if (profile.causesDeath)
			{
				this.creature.Kill();
			}
			Action broken = profile.broken;
			if (broken != null)
			{
				broken();
			}
			this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.BoneBreak", profile.bone.transform.position, profile.bone.transform, false, false, null);
			this.SetJointForceForBrokenProfile(profile);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003064 File Offset: 0x00001264
		private void SetJointForceForBrokenProfile(BoneProfile profile)
		{
			float mult = ModOptions.paralysisLimpMultiplier;
			profile.bone.ragdoll.SetPinForceMultiplier(mult, mult, mult, mult, false, false, profile.bone.type, null);
			if (profile.bone.characterJoint != null)
			{
				SoftJointLimit lowTwistLimit = profile.bone.characterJoint.lowTwistLimit;
				lowTwistLimit.limit = -ModOptions.maxSnapAngle;
				profile.bone.characterJoint.lowTwistLimit = lowTwistLimit;
				lowTwistLimit = profile.bone.characterJoint.highTwistLimit;
				lowTwistLimit.limit = ModOptions.maxSnapAngle;
				profile.bone.characterJoint.highTwistLimit = lowTwistLimit;
				lowTwistLimit = profile.bone.characterJoint.swing1Limit;
				lowTwistLimit.limit = ModOptions.maxSnapAngle;
				profile.bone.characterJoint.swing1Limit = lowTwistLimit;
				lowTwistLimit = profile.bone.characterJoint.swing2Limit;
				lowTwistLimit.limit = ModOptions.maxSnapAngle;
				profile.bone.characterJoint.swing2Limit = lowTwistLimit;
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003168 File Offset: 0x00001368
		private void SetupProfiles()
		{
			BoneProfile[] array = new BoneProfile[8];
			array[0] = new BoneProfile(this.creature.ragdoll.GetPart(16, 1), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, -ModOptions.minimumArmAngle, ModOptions.minimumArmForce, -ModOptions.idealArmAngle, ModOptions.idealArmForce, false, false, false, false, null);
			array[1] = new BoneProfile(this.creature.ragdoll.GetPart(8, 1), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, -ModOptions.minimumArmAngle, ModOptions.minimumArmForce, -ModOptions.idealArmAngle, ModOptions.idealArmForce, false, false, false, false, null);
			array[2] = new BoneProfile(this.creature.ragdoll.GetPart(256, 1), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, -ModOptions.minimumLegsAngle, ModOptions.minimumLegsForce, -ModOptions.idealLegsAngle, ModOptions.idealLegsForce, false, true, false, false, null);
			array[3] = new BoneProfile(this.creature.ragdoll.GetPart(128, 1), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, -ModOptions.minimumLegsAngle, ModOptions.minimumLegsForce, -ModOptions.idealLegsAngle, ModOptions.idealLegsForce, false, true, false, false, null);
			array[4] = new BoneProfile(this.creature.ragdoll.GetPart(4, 3), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, -ModOptions.minimumSpineAngle, ModOptions.minimumSpineForce, -ModOptions.idealSpineAngle, ModOptions.idealSpineForce, true, false, false, false, null);
			array[5] = new BoneProfile(this.creature.ragdoll.GetPart(2, 0), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.y, ModOptions.minimumNeckYAngle, ModOptions.minimumNeckYForce, -ModOptions.idealNeckYAngle, ModOptions.idealNeckYForce, true, false, true, false, delegate()
			{
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().InterpolateEyeColour(Color.red, false);
				if (this.creature.isKilled)
				{
					return;
				}
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.Gurgle", this.creature.ragdoll.GetPart(1).physicBody.transform.position, this.creature.ragdoll.GetPart(1).physicBody.transform, true, true, (Creature c) => c.isKilled);
			});
			array[6] = new BoneProfile(this.creature.ragdoll.GetPart(2, 0), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.x, ModOptions.minimumNeckXAngle, ModOptions.minimumNeckXForce, ModOptions.idealNeckXAngle, ModOptions.idealNeckXForce, false, false, true, false, delegate()
			{
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().InterpolateEyeColour(Color.red, false);
				if (this.creature.isKilled)
				{
					return;
				}
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.Gurgle", this.creature.ragdoll.GetPart(1).physicBody.transform.position, this.creature.ragdoll.GetPart(1).physicBody.transform, true, true, (Creature c) => c.isKilled);
			});
			array[7] = new BoneProfile(this.creature.ragdoll.GetPart(2, 0), (Transform bone) => bone.localEulerAngles.y, (Vector3 vel) => vel.x, -ModOptions.minimumNeckXAngle, ModOptions.minimumNeckXForce, -ModOptions.idealNeckXAngle, ModOptions.idealNeckXForce, false, false, true, false, delegate()
			{
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().InterpolateEyeColour(Color.red, false);
				if (this.creature.isKilled)
				{
					return;
				}
				this.creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.Gurgle", this.creature.ragdoll.GetPart(1).physicBody.transform.position, this.creature.ragdoll.GetPart(1).physicBody.transform, true, true, (Creature c) => c.isKilled);
			});
			this._profiles = array;
		}

		// Token: 0x0400001E RID: 30
		private BoneProfile[] _profiles;
	}
}

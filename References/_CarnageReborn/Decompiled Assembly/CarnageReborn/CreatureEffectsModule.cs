using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CarnageReborn.Gore;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000009 RID: 9
	public class CreatureEffectsModule : CreatureModule
	{
		// Token: 0x0600004D RID: 77 RVA: 0x0000393C File Offset: 0x00001B3C
		public override void HookEvents()
		{
			this._audioSource = this.creature.ragdoll.GetPart(1).physicBody.gameObject.AddComponent<AudioSource>();
			this._audioSource.playOnAwake = false;
			this._audioSource.spatialBlend = 1f;
			this._audioSource.minDistance = 1f;
			this._audioSource.maxDistance = 5f;
			this._audioSource.volume = ModOptions.effectVolume;
			this._audioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(1);
			this._speak = this.creature.brain.instance.GetModule<BrainModuleSpeak>(true);
			this.creature.OnDamageEvent += new Creature.DamageEvent(this.HandleDamage);
			CarnageEventManager.EyeballPopped += this.HandleEyeballPopped;
			CarnageEventManager.MeshExploded += this.HandleMeshExplode;
			base.HookEvents();
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003A28 File Offset: 0x00001C28
		public override void UnHookEvents()
		{
			this.creature.OnDamageEvent -= new Creature.DamageEvent(this.HandleDamage);
			CarnageEventManager.EyeballPopped -= this.HandleEyeballPopped;
			CarnageEventManager.MeshExploded -= this.HandleMeshExplode;
			base.UnHookEvents();
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003A74 File Offset: 0x00001C74
		public override void Update()
		{
			for (int i = 0; i < this._tendants.Count; i++)
			{
				if (this._tendants[i].Item1 == null)
				{
					Object.Destroy(this._tendants[i].Item2);
					this._tendants.RemoveAt(i);
					i--;
				}
				else
				{
					this._tendants[i].Item2.SetPosition(0, this._tendants[i].Item1.transform.position);
					this._tendants[i].Item2.SetPosition(1, this._tendants[i].Item1.connectedBody.transform.position);
					if (Vector3.Distance(this._tendants[i].Item1.transform.position, this._tendants[i].Item1.connectedBody.transform.position) >= ModOptions.tendantSnapDistance)
					{
						Object.Destroy(this._tendants[i].Item1);
						Object.Destroy(this._tendants[i].Item2);
						this._tendants.RemoveAt(i);
						i--;
					}
				}
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003BD0 File Offset: 0x00001DD0
		public void PlaySound(string address, bool speak = false)
		{
			this.PlaySound(address, this.creature.ragdoll.GetPart(1).physicBody.transform.position, this.creature.ragdoll.GetPart(1).physicBody.transform, speak, false, null);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003C24 File Offset: 0x00001E24
		public void PlaySound(string address, Vector3 position, Transform parent = null, bool speak = false, bool loop = false, Func<Creature, bool> playUntil = null)
		{
			CreatureEffectsModule.<>c__DisplayClass10_0 CS$<>8__locals1 = new CreatureEffectsModule.<>c__DisplayClass10_0();
			CS$<>8__locals1.speak = speak;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.loop = loop;
			CS$<>8__locals1.position = position;
			CS$<>8__locals1.parent = parent;
			CS$<>8__locals1.playUntil = playUntil;
			if (!ModOptions.allowCustomSounds || (Player.local != null && Vector3.Distance(CS$<>8__locals1.position, Player.local.transform.position) > 10f))
			{
				return;
			}
			Catalog.LoadAssetAsync<AudioClip>(address, delegate(AudioClip clip)
			{
				CreatureEffectsModule.<>c__DisplayClass10_0.<<PlaySound>b__0>d <<PlaySound>b__0>d;
				<<PlaySound>b__0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
				<<PlaySound>b__0>d.<>4__this = CS$<>8__locals1;
				<<PlaySound>b__0>d.clip = clip;
				<<PlaySound>b__0>d.<>1__state = -1;
				<<PlaySound>b__0>d.<>t__builder.Start<CreatureEffectsModule.<>c__DisplayClass10_0.<<PlaySound>b__0>d>(ref <<PlaySound>b__0>d);
			}, "CreatureEffectsModule->PlaySound");
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003CB4 File Offset: 0x00001EB4
		public void SpawnBone(RagdollPart part, string boneAddress = "SD.Bone")
		{
			Catalog.InstantiateAsync(boneAddress, part.meshBone.position, part.meshBone.rotation, part.meshBone, delegate(GameObject b)
			{
				this._bones.Add(b);
				foreach (Collider collider in part.ragdoll.GetComponentsInChildren<Collider>())
				{
					foreach (Collider collider2 in b.GetComponentsInChildren<Collider>())
					{
						Physics.IgnoreCollision(collider, collider2);
					}
				}
			}, "");
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003D18 File Offset: 0x00001F18
		public void CreateTendant(RagdollPart a, RagdollPart b)
		{
			Catalog.InstantiateAsync("SD.Tendant", a.meshBone.position, a.meshBone.rotation, a.physicBody.rigidBody.transform, delegate(GameObject lr)
			{
				ConfigurableJoint tendant = a.physicBody.rigidBody.gameObject.AddComponent<ConfigurableJoint>();
				tendant.connectedBody = b.physicBody.rigidBody;
				ConfigurableJoint configurableJoint = tendant;
				ConfigurableJoint configurableJoint2 = tendant;
				ConfigurableJoint configurableJoint3 = tendant;
				JointDrive zDrive = default(JointDrive);
				zDrive.positionSpring = 500f;
				zDrive.positionDamper = 50f;
				zDrive.maximumForce = 800f;
				configurableJoint.xDrive = (configurableJoint2.yDrive = (configurableJoint3.zDrive = zDrive));
				tendant.breakForce = 50f;
				this._tendants.Add(new ValueTuple<ConfigurableJoint, LineRenderer>(tendant, lr.GetComponent<LineRenderer>()));
			}, "");
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003D90 File Offset: 0x00001F90
		public void InterpolateEyeColour(Color colour, bool force = false)
		{
			CreatureEffectsModule.<InterpolateEyeColour>d__13 <InterpolateEyeColour>d__;
			<InterpolateEyeColour>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<InterpolateEyeColour>d__.<>4__this = this;
			<InterpolateEyeColour>d__.colour = colour;
			<InterpolateEyeColour>d__.force = force;
			<InterpolateEyeColour>d__.<>1__state = -1;
			<InterpolateEyeColour>d__.<>t__builder.Start<CreatureEffectsModule.<InterpolateEyeColour>d__13>(ref <InterpolateEyeColour>d__);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003DD8 File Offset: 0x00001FD8
		public override void Restore()
		{
			this._interpolatingEyes = false;
			this.InterpolateEyeColour(Color.white, false);
			foreach (GameObject bone in this._bones)
			{
				if (!(bone == null))
				{
					Object.Destroy(bone);
				}
			}
			this._bones.Clear();
			foreach (ValueTuple<ConfigurableJoint, LineRenderer> entry in this._tendants)
			{
				if (!(entry.Item1 == null))
				{
					Object.Destroy(entry.Item1);
					Object.Destroy(entry.Item2);
				}
			}
			this._tendants.Clear();
			foreach (GameObject go in this._effects)
			{
				if (!(go == null))
				{
					Object.Destroy(go);
				}
			}
			this._effects.Clear();
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003F14 File Offset: 0x00002114
		public void PlayParticleEffect(string address, Transform parent, Vector3 position, Quaternion rotation)
		{
			if (!ModOptions.customEffects || (Player.local != null && Vector3.Distance(position, Player.local.transform.position) > 10f))
			{
				return;
			}
			Catalog.InstantiateAsync(address, position, rotation, parent, delegate(GameObject p)
			{
				this._effects.Add(p);
				Object.Destroy(p, 10f);
			}, "CreatureEffectModule->PlayParticleEffect");
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003F6D File Offset: 0x0000216D
		private void HandleEyeballPopped(Eyeball eye)
		{
			this.PlaySound("SD.SFX.EyeballPop", eye.transform.position, null, false, false, null);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003F89 File Offset: 0x00002189
		private void HandleMeshExplode(Explodable mesh)
		{
			this.PlaySound("SD.SFX.HeadSmash", mesh.transform.position, null, false, false, null);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003FA8 File Offset: 0x000021A8
		private void HandleDamage(CollisionInstance collisionInstance, EventTime time)
		{
			if (time != 1)
			{
				return;
			}
			RagdollPart part = collisionInstance.damageStruct.hitRagdollPart;
			if (part != null)
			{
				if (part.type == 2 && part.ragdoll.creature.state == 2)
				{
					if (collisionInstance.damageStruct.damageType == 2)
					{
						this.creature.InvokeThroatSlit();
						this.PlayParticleEffect("SD.VFX.BloodBurst", part.transform, collisionInstance.contactPoint, Quaternion.FromToRotation(Vector3.up, collisionInstance.contactNormal));
						this.PlayParticleEffect("SD.VFX.BloodSpirtPool", part.transform, collisionInstance.contactPoint, Quaternion.FromToRotation(Vector3.up, collisionInstance.contactNormal));
						this.InterpolateEyeColour(Color.red, true);
					}
					return;
				}
				if (ModOptions.stabBloodBursts && collisionInstance.damageStruct.damageType == 1)
				{
					this.PlayParticleEffect("SD.VFX.BloodBurst", part.transform, collisionInstance.contactPoint, Quaternion.FromToRotation(Vector3.up, collisionInstance.contactNormal));
					this.PlayParticleEffect("SD.VFX.BloodSpirtPool", part.transform, collisionInstance.contactPoint, Quaternion.FromToRotation(Vector3.up, collisionInstance.contactNormal));
				}
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000040C4 File Offset: 0x000022C4
		private void LipSyncTo(AudioClip clip)
		{
			this._audioSource.clip = clip;
			this._audioSource.Play();
			this.creature.brain.instance.Stop();
			this.creature.brain.StopAllCoroutines();
			this.creature.StartCoroutine(this.LipSyncCoroutine());
		}

		// Token: 0x0600005B RID: 91 RVA: 0x0000411F File Offset: 0x0000231F
		private IEnumerator LipSyncCoroutine()
		{
			float[] sampleBuffer = new float[1024];
			while (this._audioSource.isPlaying)
			{
				if (this._audioSource.timeSamples + this._speak.audioLipSampleDataLength > this._audioSource.clip.samples * this._audioSource.clip.channels)
				{
					this._speak.jawTargetWeight = 0f;
					yield return Yielders.ForSeconds(this._speak.audioLipSyncUpdateRate);
				}
				else if (this._audioSource.timeSamples >= this._audioSource.clip.samples)
				{
					this._speak.jawTargetWeight = 0f;
					yield return Yielders.ForSeconds(this._speak.audioLipSyncUpdateRate);
				}
				else
				{
					this._audioSource.clip.GetData(sampleBuffer, (this._audioSource.timeSamples > this._audioSource.clip.samples) ? this._audioSource.clip.samples : this._audioSource.timeSamples);
					this._speak.speakLoudness = 0f;
					foreach (float f in sampleBuffer)
					{
						this._speak.speakLoudness += Mathf.Abs(f);
					}
					this._speak.speakLoudness /= (float)this._speak.audioLipSampleDataLength;
					this._speak.jawTargetWeight = Utils.CalculateRatio(this._speak.speakLoudness, 0f, this._speak.audioLipSyncMaxValue, 0f, 1f);
					yield return Yielders.ForSeconds(this._speak.audioLipSyncUpdateRate);
				}
			}
			this.creature.brain.instance.Start();
			yield break;
		}

		// Token: 0x04000021 RID: 33
		private BrainModuleSpeak _speak;

		// Token: 0x04000022 RID: 34
		private AudioSource _audioSource;

		// Token: 0x04000023 RID: 35
		private bool _interpolatingEyes;

		// Token: 0x04000024 RID: 36
		private readonly List<ValueTuple<ConfigurableJoint, LineRenderer>> _tendants = new List<ValueTuple<ConfigurableJoint, LineRenderer>>();

		// Token: 0x04000025 RID: 37
		private readonly List<GameObject> _bones = new List<GameObject>();

		// Token: 0x04000026 RID: 38
		private readonly List<GameObject> _effects = new List<GameObject>();
	}
}

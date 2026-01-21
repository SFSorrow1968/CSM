using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000005 RID: 5
	public class CreatureAnimationModule : CreatureModule
	{
		// Token: 0x06000026 RID: 38 RVA: 0x00002624 File Offset: 0x00000824
		public override void HookEvents()
		{
			this.creature.OnKillEvent += new Creature.KillEvent(this.HandleCreatureDeath);
			this.creature.OnDamageEvent += new Creature.DamageEvent(this.HandleRecieveDamage);
			CarnageEventManager.ThroatSlit += this.HandleThroatSlit;
			base.HookEvents();
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002678 File Offset: 0x00000878
		public override void UnHookEvents()
		{
			this.creature.OnKillEvent -= new Creature.KillEvent(this.HandleCreatureDeath);
			this.creature.OnDamageEvent -= new Creature.DamageEvent(this.HandleRecieveDamage);
			CarnageEventManager.ThroatSlit -= this.HandleThroatSlit;
			base.UnHookEvents();
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000026CC File Offset: 0x000008CC
		public override void Update()
		{
			if (this.creature.isKilled || (!this.isPlayingFinisher && !this.isPlayingPerma) || this.creature.isPlayer)
			{
				return;
			}
			if (this.isPlayingFinisher || this.isPlayingPerma)
			{
				bool canRecover = this.timer != -1f && Time.time > this.timer;
				if (this.creature.brain.instance.isActive)
				{
					this.creature.brain.Stop();
				}
				if (this.isFatalFinisher)
				{
					float force = Mathf.Clamp(this.timer / Time.time, 0.2f, 1f);
					this.creature.ragdoll.SetPinForceMultiplier(force, force, force, force, true, false, 0, null);
					if (this.timer != -1f && Time.time > this.timer)
					{
						this.creature.Kill();
					}
					return;
				}
				if (!this.isFatalFinisher && ModOptions.enemiesCanGetBackUpFromFailedFinisher && (this.creature.ragdoll.state == 1 || canRecover))
				{
					this.Restore();
					if (ModOptions.failedFinisherHealsEnemies)
					{
						this.creature.Heal(this.creature.maxHealth.GetPercentage(ModOptions.failedFinisherHealAmount), this.creature);
					}
				}
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002818 File Offset: 0x00000A18
		public void RefreshState()
		{
			if (this.creature.isKilled)
			{
				return;
			}
			if (!this.isPlayingFinisher && this.creature.ragdoll.state == 3 && this.creature.currentHealth.Normalize(0f, this.creature.maxHealth) * 100f <= ModOptions.minimumFinisherHealth && Random.value * 100f <= ModOptions.finisherChance && Vector3.Distance(this.transform.position, Player.local.transform.position) <= ModOptions.minimumDistanceToPlayerForFinisher)
			{
				this.PlayNonFatalFinisher();
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000028BC File Offset: 0x00000ABC
		public void Play(string id)
		{
			CreatureAnimationModule.<>c__DisplayClass10_0 CS$<>8__locals1 = new CreatureAnimationModule.<>c__DisplayClass10_0();
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.id = id;
			if (Utils.IsNullOrEmptyOrWhitespace(CS$<>8__locals1.id))
			{
				return;
			}
			this.creature.StartCoroutine(Common.WrapSafely(CS$<>8__locals1.<Play>g__InternalPlay|1(), delegate(Exception e)
			{
				Debug.LogError(e);
			}));
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002924 File Offset: 0x00000B24
		public void PlayNonFatalFinisher()
		{
			if (this.isPlayingFinisher || this.creature == null || this.creature.isKilled || this.creature.brain == null || this.creature.brain.instance == null)
			{
				return;
			}
			this.creature.StartCoroutine(Common.WrapSafely(this.<PlayNonFatalFinisher>g__InternalFinisher|11_0(), null));
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002994 File Offset: 0x00000B94
		public void PlaySlitThroatFinisher()
		{
			if (this.isPlayingFinisher || this.creature == null || this.creature.isKilled || this.creature.ragdoll.state == 1 || this.creature.brain == null)
			{
				return;
			}
			this.creature.StartCoroutine(Common.WrapSafely(this.<PlaySlitThroatFinisher>g__InternalSlitFinisher|12_0(), null));
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002A04 File Offset: 0x00000C04
		public override void Restore()
		{
			if (!this.isInjected)
			{
				return;
			}
			this.creature.ragdoll.ResetPinForce(true, false, 0);
			if (this.animator != null)
			{
				this.animator.enabled = false;
			}
			if (!this.creature.isKilled)
			{
				this.creature.brain.instance.Start();
			}
			this.creature.ragdoll.SetState(1);
			this.isPlayingFinisher = false;
			this.isInjected = false;
			this.isFatalFinisher = false;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002A8F File Offset: 0x00000C8F
		private IEnumerator InjectCRAnimator()
		{
			if (this.isInjected)
			{
				yield break;
			}
			RuntimeAnimatorController controller = null;
			Catalog.InstantiateAsync("SD.Anim", Vector3.zero, Quaternion.identity, null, delegate(GameObject ao)
			{
				controller = ao.GetComponent<Animator>().runtimeAnimatorController;
			}, "Entry");
			while (controller == null)
			{
				yield return null;
			}
			if (controller == null)
			{
				Debug.LogError("Unable to get animation controller!");
				yield break;
			}
			Animator animator;
			if (!(this.animator != null))
			{
				Creature creature = this.creature;
				if (creature == null)
				{
					animator = null;
				}
				else
				{
					Animator animator2 = creature.animator;
					if (animator2 == null)
					{
						animator = null;
					}
					else
					{
						Transform transform = animator2.transform;
						if (transform == null)
						{
							animator = null;
						}
						else
						{
							Transform transform2 = transform.Find("Rig");
							if (transform2 == null)
							{
								animator = null;
							}
							else
							{
								GameObject gameObject = transform2.gameObject;
								animator = ((gameObject != null) ? gameObject.AddComponent<Animator>() : null);
							}
						}
					}
				}
			}
			else
			{
				animator = this.animator;
			}
			this.animator = animator;
			if (this.animator == null)
			{
				this.isInjected = false;
				Debug.LogError(string.Format("Unable to inject, no animator found on creature[{0}]!", this.creature));
				yield break;
			}
			this.animator.applyRootMotion = true;
			this.animator.runtimeAnimatorController = controller;
			this.isInjected = true;
			yield break;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002A9E File Offset: 0x00000C9E
		private void HandleCreatureDeath(CollisionInstance collisionInstance, EventTime eventTime)
		{
			this.Restore();
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002AA6 File Offset: 0x00000CA6
		private void HandleRecieveDamage(CollisionInstance collisionInstance, EventTime eventTime)
		{
			this.RefreshState();
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002AB0 File Offset: 0x00000CB0
		private void HandleThroatSlit(Creature creature)
		{
			if (creature != this.creature)
			{
				return;
			}
			if (Random.value * 100f > ModOptions.slitThroatFinisherChance)
			{
				creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.Gurgle", creature.ragdoll.GetPart(1).physicBody.transform.position, creature.ragdoll.GetPart(1).physicBody.transform, true, false, null);
				return;
			}
			creature.GetComponent<CarnageCreature>().GetCreatureModule<CreatureEffectsModule>().PlaySound("SD.SFX.Gurgle", creature.ragdoll.GetPart(1).physicBody.transform.position, creature.ragdoll.GetPart(1).physicBody.transform, true, true, (Creature c) => c.isKilled);
			this.PlaySlitThroatFinisher();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002B9D File Offset: 0x00000D9D
		[CompilerGenerated]
		private IEnumerator <PlayNonFatalFinisher>g__InternalFinisher|11_0()
		{
			this.timer = ((ModOptions.timeTillEnemyRecovers == -1f) ? -1f : (Time.time + ModOptions.timeTillEnemyRecovers));
			this.isPlayingFinisher = true;
			if (!this.isInjected || this.animator == null)
			{
				yield return this.InjectCRAnimator();
			}
			if (this.animator != null)
			{
				this.animator.enabled = true;
			}
			this.creature.brain.instance.Stop();
			Animator animator = this.animator;
			if (animator != null)
			{
				animator.Play(Definitions.nonFatalFinisher);
			}
			yield break;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002BAC File Offset: 0x00000DAC
		[CompilerGenerated]
		private IEnumerator <PlaySlitThroatFinisher>g__InternalSlitFinisher|12_0()
		{
			this.timer = ((ModOptions.timeTillEnemyRecovers == -1f) ? -1f : (Time.time + ModOptions.timeTillEnemyRecovers));
			this.isPlayingFinisher = true;
			this.isFatalFinisher = true;
			if (!this.isInjected || this.animator == null)
			{
				yield return this.InjectCRAnimator();
			}
			if (this.animator != null)
			{
				this.animator.enabled = true;
			}
			this.creature.brain.instance.Stop();
			Animator animator = this.animator;
			if (animator != null)
			{
				animator.Play(Definitions.slitThroatFinisherStart);
			}
			yield break;
		}

		// Token: 0x0400000A RID: 10
		private Animator animator;

		// Token: 0x0400000B RID: 11
		private bool isInjected;

		// Token: 0x0400000C RID: 12
		private bool isPlayingFinisher;

		// Token: 0x0400000D RID: 13
		private bool isFatalFinisher;

		// Token: 0x0400000E RID: 14
		private bool isPlayingPerma;

		// Token: 0x0400000F RID: 15
		private float timer;
	}
}

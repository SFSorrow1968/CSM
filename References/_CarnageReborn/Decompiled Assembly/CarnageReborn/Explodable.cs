using System;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000019 RID: 25
	public class Explodable : MonoBehaviour
	{
		// Token: 0x14000007 RID: 7
		// (add) Token: 0x060000BD RID: 189 RVA: 0x0000550C File Offset: 0x0000370C
		// (remove) Token: 0x060000BE RID: 190 RVA: 0x00005544 File Offset: 0x00003744
		public event Exploded MeshExploded;

		// Token: 0x060000BF RID: 191 RVA: 0x00005579 File Offset: 0x00003779
		private void Awake()
		{
			this.limb = base.GetComponent<RagdollPart>();
			EventManager.onCreatureHit += new EventManager.CreatureHitEvent(this.HandleCreatureHit);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00005598 File Offset: 0x00003798
		private void HandleCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
		{
			if (eventTime != 1 || creature != this.limb.ragdoll.creature)
			{
				return;
			}
			if (this._hasExploded || !this._canExplode(collisionInstance.damageStruct.damageType))
			{
				return;
			}
			if (collisionInstance.impactVelocity.magnitude >= this._explodeVelocity() || (collisionInstance.targetCollider.attachedRigidbody != null && collisionInstance.impactVelocity.magnitude > 3f && collisionInstance.targetCollider.attachedRigidbody.mass >= ModOptions.crushingMassScale))
			{
				this.Explode(false);
			}
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000563E File Offset: 0x0000383E
		private void OnDisable()
		{
			this._hasExploded = false;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00005648 File Offset: 0x00003848
		private void HandleSlice(RagdollPart ragdollPart, EventTime eventTime)
		{
			if (this._hasExploded || eventTime != null || ragdollPart.gameObject != base.gameObject)
			{
				return;
			}
			this._hasExploded = true;
			this.limb.ragdoll.OnSliceEvent -= new Ragdoll.SliceEvent(this.HandleSlice);
			if (this.killsCreature)
			{
				this.limb.ragdoll.creature.Kill();
			}
			foreach (HandleRagdoll handleRagdoll in this.limb.handles)
			{
				handleRagdoll.Release();
			}
			foreach (CollisionInstance instance in this.limb.collisionHandler.collisions)
			{
				if (instance.damageStruct.penetrationJoint != null)
				{
					instance.damageStruct.damager.UnPenetrate(instance, false);
				}
			}
			base.transform.localScale = Vector3.one * 0.05f;
			Catalog.InstantiateAsync(this.explosionPrefabAddress, this.limb.transform.position, Quaternion.identity, null, delegate(GameObject go)
			{
				base.transform.localScale = Vector3.one * 0.05f;
				go.transform.localScale *= ((this.limb.ragdoll.creature.data.gender == 1) ? Definitions.MaleSkullScaleMultiplier : Definitions.FemaleSkullScaleMultiplier);
				Exploded meshExploded = this.MeshExploded;
				if (meshExploded != null)
				{
					meshExploded(this);
				}
				this.InvokeMeshExploded();
				Action<GameObject> action = this.exploded;
				if (action != null)
				{
					action(go);
				}
				Object.Destroy(go, 20f);
			}, "ExplodableMesh->Smash");
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00005790 File Offset: 0x00003990
		public void Set(Func<float> explodeVelocity, string address, bool kills, Func<DamageType, bool> canExplode, Action<GameObject> exploded = null)
		{
			this.explosionPrefabAddress = address;
			this._explodeVelocity = explodeVelocity;
			this.killsCreature = kills;
			this._canExplode = canExplode;
			this.exploded = exploded;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000057B7 File Offset: 0x000039B7
		public void Explode(bool force = false)
		{
			if (this._hasExploded || (!ModOptions.headSmashing && !force))
			{
				return;
			}
			this.limb.ragdoll.OnSliceEvent += new Ragdoll.SliceEvent(this.HandleSlice);
			this.limb.TrySlice();
		}

		// Token: 0x04000051 RID: 81
		public RagdollPart limb;

		// Token: 0x04000052 RID: 82
		public string explosionPrefabAddress;

		// Token: 0x04000053 RID: 83
		public bool killsCreature;

		// Token: 0x04000055 RID: 85
		private Func<DamageType, bool> _canExplode;

		// Token: 0x04000056 RID: 86
		private Func<float> _explodeVelocity;

		// Token: 0x04000057 RID: 87
		private Action<GameObject> exploded;

		// Token: 0x04000058 RID: 88
		private bool _hasExploded;
	}
}

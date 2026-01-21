using System;
using System.Collections.Generic;
using CarnageReborn.Deformation;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x0200000B RID: 11
	[DisallowMultipleComponent]
	public class Deformable : ThunderBehaviour
	{
		// Token: 0x14000004 RID: 4
		// (add) Token: 0x06000063 RID: 99 RVA: 0x000042CC File Offset: 0x000024CC
		// (remove) Token: 0x06000064 RID: 100 RVA: 0x00004304 File Offset: 0x00002504
		public event Deformed Deformed;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x06000065 RID: 101 RVA: 0x0000433C File Offset: 0x0000253C
		// (remove) Token: 0x06000066 RID: 102 RVA: 0x00004374 File Offset: 0x00002574
		public event DeformableRestored Restored;

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000067 RID: 103 RVA: 0x000043A9 File Offset: 0x000025A9
		// (set) Token: 0x06000068 RID: 104 RVA: 0x000043B1 File Offset: 0x000025B1
		public bool HasInitialized { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000069 RID: 105 RVA: 0x000043BA File Offset: 0x000025BA
		public override ManagedLoops EnabledManagedLoops
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x000043C0 File Offset: 0x000025C0
		protected override void ManagedOnEnable()
		{
			if (this.HasInitialized)
			{
				return;
			}
			this.item = base.GetComponent<Item>();
			this._itemClass = ((this.item != null && this.item.data != null && this.item.data.moduleAI != null) ? this.item.data.moduleAI.primaryClass : 0);
			this.HasInitialized = true;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004434 File Offset: 0x00002634
		protected override void ManagedLateUpdate()
		{
			if (this.item == null || !ModOptions.npcThrowDamagedWeapon)
			{
				return;
			}
			if (Time.time > this._lastDamageCheck && this.item.handlers.Count > 0 && !this.item.handlers[0].creature.isPlayer)
			{
				this._lastDamageCheck = Time.time + 10f;
				this.GetDeformationRatio(delegate(float ratio)
				{
					if (ratio <= (float)ModOptions.weaponDamageThrowRatio)
					{
						Debug.Log(string.Format("NPC releasing weapon with health at: {0}%", ratio));
						this.item.handlers[0].TryRelease();
						this.item.data.moduleAI.primaryClass = 0;
					}
				});
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x000044B7 File Offset: 0x000026B7
		internal void InvokeDeformed(Deformer deformer, Mesh mesh, Collision collisionData)
		{
			Deformed deformed = this.Deformed;
			if (deformed == null)
			{
				return;
			}
			deformed(deformer, mesh, collisionData);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000044CC File Offset: 0x000026CC
		public bool TryGetMeshByRenderer(Renderer renderer, out Mesh mesh)
		{
			return this._originalMeshCache.TryGetValue(renderer, out mesh);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000044DC File Offset: 0x000026DC
		public virtual void CacheAllRendererMeshes()
		{
			foreach (MeshFilter filter in base.GetComponentsInChildren<MeshFilter>(true))
			{
				if (!(filter.mesh == null))
				{
					this.SetOriginalMesh(filter.GetComponent<Renderer>(), filter.mesh);
				}
			}
			foreach (SkinnedMeshRenderer skm in base.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (!(skm.sharedMesh == null))
				{
					Mesh mesh = skm.sharedMesh.CloneMesh();
					skm.sharedMesh = mesh;
					this.SetOriginalMesh(skm, Object.Instantiate<Mesh>(mesh));
				}
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004574 File Offset: 0x00002774
		public virtual void RestoreAll()
		{
			foreach (KeyValuePair<Renderer, Mesh> key in this._originalMeshCache)
			{
				this.Restore(key.Key);
			}
			if (this.item != null && this.item.data != null && this.item.data.moduleAI != null)
			{
				this.item.data.moduleAI.primaryClass = this._itemClass;
			}
			DeformableRestored restored = this.Restored;
			if (restored == null)
			{
				return;
			}
			restored();
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00004628 File Offset: 0x00002828
		public bool Restore(Renderer hash)
		{
			if (base.gameObject == null || hash == null)
			{
				return false;
			}
			Mesh cache;
			if (!this.TryGetMeshByRenderer(hash, out cache))
			{
				Debug.LogWarning(string.Format("Unable to restore mesh with hash '{0}' on '{1}'!", hash, base.gameObject.name));
				return false;
			}
			if (!cache.IsMeshEditable())
			{
				return false;
			}
			hash.TrySetMeshToRender(cache.CloneMesh());
			return true;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x0000468D File Offset: 0x0000288D
		public void SetOriginalMesh(Renderer hash, Mesh original)
		{
			if (this._originalMeshCache.ContainsKey(hash))
			{
				this._originalMeshCache[hash] = original;
				return;
			}
			this._originalMeshCache.Add(hash, original);
		}

		// Token: 0x04000028 RID: 40
		public Item item;

		// Token: 0x04000029 RID: 41
		private float _lastDamageCheck;

		// Token: 0x0400002A RID: 42
		private ItemModuleAI.WeaponClass _itemClass;

		// Token: 0x0400002B RID: 43
		protected readonly Dictionary<Renderer, Mesh> _originalMeshCache = new Dictionary<Renderer, Mesh>();
	}
}

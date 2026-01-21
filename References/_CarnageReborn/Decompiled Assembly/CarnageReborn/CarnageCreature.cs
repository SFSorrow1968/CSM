using System;
using System.Collections.Generic;
using CarnageReborn.Deformation;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000002 RID: 2
	public class CarnageCreature : ThunderBehaviour
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override ManagedLoops EnabledManagedLoops
		{
			get
			{
				return 7;
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002054 File Offset: 0x00000254
		private void Awake()
		{
			this.creature = base.GetComponent<Creature>();
			this.creature.ragdoll.OnSliceEvent += new Ragdoll.SliceEvent(this.HandleSliced);
			if (this.headMeshCache == null)
			{
				this.headMeshCache = this.creature.GetCreatureHeadLOD0().sharedMesh.CloneMesh();
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020B2 File Offset: 0x000002B2
		private void Start()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.Start();
			});
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020D9 File Offset: 0x000002D9
		protected override void ManagedOnEnable()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.Enabled();
			});
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002100 File Offset: 0x00000300
		protected override void ManagedOnDisable()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.Disabled();
			});
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002128 File Offset: 0x00000328
		protected override void ManagedUpdate()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.Update();
			});
			if (!this._initialized && this.creature.ragdoll.GetPart(4) != null)
			{
				this.ForEachModule(delegate(CreatureModule m)
				{
					m.HookEvents();
				});
				this._initialized = true;
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000021A7 File Offset: 0x000003A7
		protected override void ManagedLateUpdate()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.LateUpdate();
			});
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000021CE File Offset: 0x000003CE
		protected override void ManagedFixedUpdate()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.FixedUpdate();
			});
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021F5 File Offset: 0x000003F5
		private void OnDestroy()
		{
			this.ForEachModule(delegate(CreatureModule m)
			{
				m.UnHookEvents();
			});
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000221C File Offset: 0x0000041C
		public void LoadModule(Type moduleType)
		{
			if (moduleType == null || !moduleType.IsSubclassOf(typeof(CreatureModule)))
			{
				Debug.LogWarning("Invalid module type provided!");
				return;
			}
			CreatureModule module = (CreatureModule)Activator.CreateInstance(moduleType);
			module.Initialize(this.creature);
			module.Awake();
			Debug.Log(moduleType.Name + " Initialized!");
			this.modules.Add(module);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002290 File Offset: 0x00000490
		public T GetCreatureModule<T>() where T : CreatureModule
		{
			for (int i = 0; i < this.modules.Count; i++)
			{
				if (this.modules[i].GetType() == typeof(T))
				{
					return this.modules[i] as T;
				}
			}
			return default(T);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000022F5 File Offset: 0x000004F5
		public void RemoveAllModulesAndSelf()
		{
			this.modules.Clear();
			Object.Destroy(this);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002308 File Offset: 0x00000508
		public void RestoreAllModules()
		{
			foreach (CreatureModule creatureModule in this.modules)
			{
				creatureModule.Restore();
			}
			Deformable[] componentsInChildren = base.GetComponentsInChildren<Deformable>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].RestoreAll();
			}
			this.creature.SetCreatureHeadLOD0(this.headMeshCache.CloneMesh());
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000238C File Offset: 0x0000058C
		private void ForEachModule(Action<CreatureModule> action)
		{
			for (int i = 0; i < this.modules.Count; i++)
			{
				if (action != null)
				{
					action(this.modules[i]);
				}
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000023C4 File Offset: 0x000005C4
		private void HandleSliced(RagdollPart ragdollPart, EventTime eventTime)
		{
			if (eventTime != 1)
			{
				return;
			}
			if (ModOptions.spawnBones && ragdollPart.type != 2 && ragdollPart.type != 1 && ragdollPart.type != 512 && ragdollPart.type != 1024 && ragdollPart.type != 32 && ragdollPart.type != 64 && ragdollPart.type != 4)
			{
				this.GetCreatureModule<CreatureEffectsModule>().SpawnBone(ragdollPart, "SD.Bone");
			}
			if (ModOptions.spawnSpineBone && ragdollPart.type == 1)
			{
				this.GetCreatureModule<CreatureEffectsModule>().SpawnBone(ragdollPart, "SD.Spine");
			}
			if (ModOptions.createTendants)
			{
				this.GetCreatureModule<CreatureEffectsModule>().CreateTendant(ragdollPart, ragdollPart.parentPart);
			}
		}

		// Token: 0x04000001 RID: 1
		private readonly List<CreatureModule> modules = new List<CreatureModule>();

		// Token: 0x04000002 RID: 2
		public Creature creature;

		// Token: 0x04000003 RID: 3
		public Mesh headMeshCache;

		// Token: 0x04000004 RID: 4
		private bool _initialized;
	}
}

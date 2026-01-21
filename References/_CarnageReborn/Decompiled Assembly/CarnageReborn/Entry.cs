using System;
using CarnageReborn.Utilities;
using IngameDebugConsole;
using ThunderRoad;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarnageReborn
{
	// Token: 0x02000010 RID: 16
	public class Entry : ThunderScript
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00004D4F File Offset: 0x00002F4F
		// (set) Token: 0x0600008D RID: 141 RVA: 0x00004D56 File Offset: 0x00002F56
		public static string Location { get; private set; }

		// Token: 0x0600008E RID: 142 RVA: 0x00004D60 File Offset: 0x00002F60
		public override void ScriptLoaded(ModManager.ModData modData)
		{
			DebugLogConsole.AddCommand("CRTestAll", "", new Action(Commands.TestAll));
			DebugLogConsole.AddCommand("CRTestThroatSlit", "", new Action(Commands.TestThroatFinisher));
			DebugLogConsole.AddCommand("CRTestHeadExplosion", "", new Action(Commands.TestHeadExplosion));
			DebugLogConsole.AddCommand("CRTestDeformation", "", new Action(Commands.TestCreatureDeformation));
			DebugLogConsole.AddCommand("CRTestGuts", "", new Action(Commands.TestGuts));
			DebugLogConsole.AddCommand("CRTestBoneBreaking", "", new Action(Commands.TestBoneBreaking));
			DebugLogConsole.AddCommand("CRTestBoneBreakingThrowing", "", new Action(Commands.TestBoneBreakingThrowing));
			DebugLogConsole.AddCommand("CRTestAnimations", "", new Action(Commands.TestAnimations));
			DebugLogConsole.AddCommand("CRTestAssets", "", new Action(Commands.TestAssets));
			DebugLogConsole.AddCommand("CRTestEyes", "", new Action(Commands.TestEyeballs));
			DebugLogConsole.AddCommand("CRTestSpine", "", new Action(Commands.TestSpine));
			base.ScriptLoaded(modData);
			Entry.Location = modData.fullPath;
			if (!ModOptions.apiOnlyMode)
			{
				EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(this.CreatureSpawned);
				Item.OnItemSpawn = (Action<Item>)Delegate.Combine(Item.OnItemSpawn, new Action<Item>(this.HandleItemSpawn));
				SceneManager.sceneLoaded += delegate(Scene o, LoadSceneMode e)
				{
					GC.Collect();
				};
			}
			else
			{
				Debug.Log("API Mode Only! Pussy.");
			}
			Debug.Log("Carnage Reborn Initialized!");
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004F1A File Offset: 0x0000311A
		public override void ScriptUpdate()
		{
			base.ScriptUpdate();
			if (!ModOptions.apiOnlyMode)
			{
				MainThread.ThreadTick();
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00004F30 File Offset: 0x00003130
		private void HandleItemSpawn(Item item)
		{
			if (ModOptions.selfDeformingWeapons)
			{
				Transform target = item.transform.Find("SELFDEFORMER");
				if (target == null || target.GetComponent<SelfDeformer>() == null)
				{
					target = new GameObject("SELFDEFORMER").transform;
					target.SetParent(item.transform);
					target.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					target.gameObject.AddComponent<SelfDeformer>();
				}
			}
			else
			{
				Transform target2 = item.transform.Find("SELFDEFORMER");
				if (target2 != null)
				{
					Object.Destroy(target2.gameObject);
				}
			}
			if (item.data.damagers.Count > 0 && item.GetComponent<Deformer>() == null)
			{
				Deformable deformable = item.gameObject.AddComponent<Deformable>();
				item.gameObject.AddComponent<Deformer>();
				item.OnDespawnEvent += delegate(EventTime e)
				{
					deformable.RestoreAll();
				};
				deformable.CacheAllRendererMeshes();
			}
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00005028 File Offset: 0x00003228
		private void CreatureSpawned(Creature creature)
		{
			if (creature.isPlayer)
			{
				if (creature.handLeft.gameObject.GetComponent<Deformer>() == null)
				{
					creature.handLeft.gameObject.AddComponent<Deformer>();
				}
				if (creature.handRight.gameObject.GetComponent<Deformer>() == null)
				{
					creature.handRight.gameObject.AddComponent<Deformer>();
				}
				return;
			}
			if (creature.data == null || (!creature.data.id.ToLowerInvariant().Contains("human") && !creature.data.name.ToLowerInvariant().Contains("human")))
			{
				return;
			}
			CarnageCreature cr = creature.GetComponent<CarnageCreature>();
			if (ModOptions.enableHealthMultiplier)
			{
				creature.maxHealth = (float)(100 * ModOptions.npcHealthMultiplier);
				creature.currentHealth = creature.maxHealth;
			}
			if (cr == null)
			{
				this.ConfigureCreatureModules(creature);
				creature.CreateSkull();
				creature.CreateExplodables();
				creature.CreateEyeball(1);
				creature.CreateEyeball(0);
				creature.gameObject.AddComponent<Deformable>().CacheAllRendererMeshes();
				if (ModOptions.selfDeformingEnemies)
				{
					creature.ragdoll.GetPart(1).physicBody.gameObject.AddComponent<SelfDeformer>();
				}
				return;
			}
			cr.RestoreAllModules();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00005160 File Offset: 0x00003360
		private void ConfigureCreatureModules(Creature creature)
		{
			CarnageCreature carnageCreature = creature.gameObject.AddComponent<CarnageCreature>();
			carnageCreature.LoadModule(typeof(CreatureAnimationModule));
			carnageCreature.LoadModule(typeof(CreatureEffectsModule));
			if (ModOptions.sliceableTorso)
			{
				carnageCreature.LoadModule(typeof(CreatureTorsoGibModule));
			}
			carnageCreature.LoadModule(typeof(CreatureConciousnessModule));
			carnageCreature.LoadModule(typeof(CreatureBoneBreakingModule));
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000051D0 File Offset: 0x000033D0
		private static string To_Project_Internals_Developer_Who_Vibe_Codes()
		{
			return "I know what you did.";
		}
	}
}

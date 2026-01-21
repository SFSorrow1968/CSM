using System;
using System.Collections;
using ThunderRoad;
using TMPro;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000011 RID: 17
	public class LevelExecutioner : LevelModule
	{
		// Token: 0x06000095 RID: 149 RVA: 0x000051DF File Offset: 0x000033DF
		public override IEnumerator OnLoadCoroutine()
		{
			this._spawn = this.level.customReferences[0].transforms[0];
			this._executionPoint = this.level.customReferences[1].transforms[0];
			this._waypoints = this.level.customReferences[2].transforms[0].GetComponentsInChildren<Transform>();
			this._npcCanvasGroup = this.level.customReferences[3].transforms[0].GetComponent<CanvasGroup>();
			this._npcName = this.level.customReferences[3].transforms[1].GetComponent<TextMeshProUGUI>();
			this._npcAge = this.level.customReferences[3].transforms[2].GetComponent<TextMeshProUGUI>();
			this._npcCrime = this.level.customReferences[3].transforms[3].GetComponent<TextMeshProUGUI>();
			yield return base.OnLoadCoroutine();
			yield break;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000051F0 File Offset: 0x000033F0
		public override void Update()
		{
			if (this._injected || this._current == null)
			{
				base.Update();
				return;
			}
			if (Vector3.Distance(this._current.transform.position, this._waypoints[3].position) <= 1f)
			{
				this._current.locomotion.forwardSpeed = 0.32f;
			}
			if (Vector3.Distance(this._current.transform.position, this._waypoints[4].position) <= 1f)
			{
				this._current.locomotion.forwardSpeed = 0.16f;
			}
			if (Vector3.Distance(this._current.transform.position, this._executionPoint.position) <= 1f)
			{
				this._current.brain.instance.Stop();
				this._current.transform.position = this._executionPoint.position;
				this._current.transform.rotation = this._executionPoint.rotation;
				this.level.StartCoroutine(this.InjectAnimator(this._current));
				this.level.StartCoroutine(this.PresentNPCCInformation(this._current));
			}
			base.Update();
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000533E File Offset: 0x0000353E
		public override IEnumerator OnPlayerSpawnCoroutine()
		{
			yield return base.OnPlayerSpawnCoroutine();
			yield return this.SpawnNext();
			yield break;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x0000534D File Offset: 0x0000354D
		private IEnumerator SpawnNext()
		{
			CreatureData data = Catalog.GetData<CreatureData>((Random.value <= 0.5f) ? "HumanMale" : "HumanFemale", true);
			data.containerID = "PrisonerDefault";
			data.brainId = "Prisoner";
			data.factionId = Player.local.creature.faction.id;
			data.locomotionForwardSpeed *= 1.1f;
			yield return data.SpawnCoroutine(this._spawn.position, this._spawn.localEulerAngles.y, null, delegate(Creature c)
			{
				c.brain.instance.GetModule<BrainModulePatrol>(true).waypoints = this.level.customReferences[2].transforms[0].GetComponentsInChildren<WayPoint>();
				this._current = c;
				c.OnKillEvent += new Creature.KillEvent(this.HandleCreatureDied);
			}, false, null);
			yield break;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x0000535C File Offset: 0x0000355C
		private IEnumerator PresentNPCCInformation(Creature npc)
		{
			this._npcName.text = GeneralUtilities.GetRandomFirstName(npc.data.gender) + " " + GeneralUtilities.GetRandomLastName();
			this._npcAge.text = string.Format("Age: <u>{0:N0}</u>", Random.Range(18, 50));
			this._npcCrime.text = "Crime: <u>" + GeneralUtilities.GetRandomCrime() + "</u>";
			this._npcCanvasGroup.alpha = 0f;
			for (float i = 0f; i <= 1f; i += 2f * Time.deltaTime)
			{
				this._npcCanvasGroup.alpha = i;
				yield return null;
			}
			this._npcCanvasGroup.alpha = 1f;
			yield return Yielders.ForSeconds(10f);
			this._npcCanvasGroup.alpha = 1f;
			for (float i = 1f; i >= 0f; i -= 2f * Time.deltaTime)
			{
				this._npcCanvasGroup.alpha = i;
				yield return null;
			}
			this._npcCanvasGroup.alpha = 0f;
			yield break;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00005374 File Offset: 0x00003574
		private void HandleCreatureDied(CollisionInstance collisionInstance, EventTime eventTime)
		{
			if (eventTime != 1)
			{
				return;
			}
			this.level.StopAllCoroutines();
			this._npcCanvasGroup.alpha = 0f;
			this._current.OnKillEvent -= new Creature.KillEvent(this.HandleCreatureDied);
			this._current = null;
			this._injected = false;
			this.level.StartCoroutine(this.SpawnNext());
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000053D8 File Offset: 0x000035D8
		private IEnumerator InjectAnimator(Creature creature)
		{
			this._injected = true;
			RuntimeAnimatorController controller = null;
			Catalog.InstantiateAsync("SD.Executioner.Animator", Vector3.zero, Quaternion.identity, null, delegate(GameObject ao)
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
			Animator animator2;
			if (creature == null)
			{
				animator2 = null;
			}
			else
			{
				Animator animator3 = creature.animator;
				if (animator3 == null)
				{
					animator2 = null;
				}
				else
				{
					Transform transform = animator3.transform;
					if (transform == null)
					{
						animator2 = null;
					}
					else
					{
						Transform transform2 = transform.Find("Rig");
						if (transform2 == null)
						{
							animator2 = null;
						}
						else
						{
							GameObject gameObject = transform2.gameObject;
							animator2 = ((gameObject != null) ? gameObject.AddComponent<Animator>() : null);
						}
					}
				}
			}
			Animator animator = animator2;
			if (animator == null)
			{
				this._injected = false;
				Debug.LogError(string.Format("Unable to inject, no animator found on creature[{0}]!", creature));
				yield break;
			}
			animator.applyRootMotion = true;
			animator.runtimeAnimatorController = controller;
			yield break;
		}

		// Token: 0x0400003F RID: 63
		private Transform _spawn;

		// Token: 0x04000040 RID: 64
		private Transform _executionPoint;

		// Token: 0x04000041 RID: 65
		private Transform[] _waypoints;

		// Token: 0x04000042 RID: 66
		private CanvasGroup _npcCanvasGroup;

		// Token: 0x04000043 RID: 67
		private TextMeshProUGUI _npcName;

		// Token: 0x04000044 RID: 68
		private TextMeshProUGUI _npcAge;

		// Token: 0x04000045 RID: 69
		private TextMeshProUGUI _npcCrime;

		// Token: 0x04000046 RID: 70
		private Creature _current;

		// Token: 0x04000047 RID: 71
		private bool _injected;
	}
}

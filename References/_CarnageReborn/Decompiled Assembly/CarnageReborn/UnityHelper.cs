using System;
using System.Linq;
using CarnageReborn.Deformation;
using ThunderRoad;
using ThunderRoad.Manikin;
using TMPro;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000029 RID: 41
	public static class UnityHelper
	{
		// Token: 0x060000FE RID: 254 RVA: 0x00006578 File Offset: 0x00004778
		public static void DisplayMessage(string text, Vector3 position)
		{
			Catalog.LoadAssetAsync<GameObject>("SD.DebugInfo", delegate(GameObject go)
			{
				go = Object.Instantiate<GameObject>(go);
				go.transform.position = position;
				go.transform.LookAt(Camera.main.transform.position);
				go.transform.GetComponentInChildren<TextMeshProUGUI>().text = text;
				Object.Destroy(go, 10f);
			}, string.Empty);
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000065B4 File Offset: 0x000047B4
		public static bool IsWithinDeformationRange(this Transform transform)
		{
			return ModOptions.preventDistantDeformations && Vector3.Distance(transform.position, Player.local.transform.position) <= 10f;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000065E4 File Offset: 0x000047E4
		internal static int GetRendererHashFromTarget(this Component target)
		{
			if (target != null)
			{
				RagdollPart part = target.GetComponentInParent<RagdollPart>();
				if (part != null && !Utils.IsNullOrEmpty(part.skinnedMeshRenderers) && part.type == 1)
				{
					return part.ragdoll.creature.GetCreatureHeadLOD0().transform.GetHashCode();
				}
			}
			Item item = target.GetComponentInParent<Item>();
			if (item != null && item.renderers.Count > 0)
			{
				return item.renderers[0].transform.GetHashCode();
			}
			MeshFilter componentInParent = target.GetComponentInParent<MeshFilter>();
			if (componentInParent != null)
			{
				return componentInParent.transform.GetHashCode();
			}
			SkinnedMeshRenderer componentInParent2 = target.GetComponentInParent<SkinnedMeshRenderer>();
			if (componentInParent2 == null)
			{
				return -1;
			}
			return componentInParent2.transform.GetHashCode();
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00006690 File Offset: 0x00004890
		internal static Renderer TryGetRenderer(this Component component, out Mesh mesh, out bool isSkinnedMesh, out bool isItem)
		{
			Item item = component.GetComponentInParent<Item>();
			MeshFilter filter;
			SkinnedMeshRenderer skm;
			if (item != null)
			{
				filter = item.renderers[0].GetComponent<MeshFilter>();
				skm = item.renderers[0].GetComponent<SkinnedMeshRenderer>();
				mesh = (((filter != null) ? filter.mesh : null) ?? skm.sharedMesh).CloneMesh();
				isSkinnedMesh = (skm != null);
				isItem = true;
				return item.renderers[0];
			}
			RagdollPart part = component.GetComponentInParent<RagdollPart>();
			if (part != null && !Utils.IsNullOrEmpty(part.skinnedMeshRenderers) && part.type == 1)
			{
				skm = part.ragdoll.creature.GetCreatureHeadLOD0();
				isSkinnedMesh = true;
				isItem = false;
				mesh = skm.sharedMesh.CloneMesh();
				return skm;
			}
			filter = component.GetComponentInParent<MeshFilter>();
			skm = component.GetComponentInParent<SkinnedMeshRenderer>();
			mesh = (((filter != null) ? filter.mesh : null) ?? ((skm != null) ? skm.sharedMesh : null)).CloneMesh();
			isSkinnedMesh = (skm != null);
			isItem = false;
			if (!(filter == null))
			{
				return filter.GetComponent<Renderer>();
			}
			return skm;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00006798 File Offset: 0x00004998
		internal static void TrySetMeshToRender(this Renderer renderer, Mesh mesh)
		{
			if (!mesh.IsMeshEditable())
			{
				return;
			}
			SkinnedMeshRenderer skm = renderer as SkinnedMeshRenderer;
			if (skm != null && skm != null)
			{
				skm.sharedMesh = mesh;
				return;
			}
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			if (filter != null)
			{
				filter.mesh = mesh;
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000067DC File Offset: 0x000049DC
		internal static void SetCreatureHeadLOD0(this Creature creature, Mesh mesh)
		{
			foreach (ManikinGroupPart child in creature.GetComponentInChildren<ManikinPartList>().GetComponentsInChildren<ManikinGroupPart>())
			{
				if (child.name.ToLowerInvariant().Contains("head"))
				{
					(child.partLODs[0].renderers[0] as SkinnedMeshRenderer).sharedMesh = mesh;
					return;
				}
			}
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00006844 File Offset: 0x00004A44
		internal static SkinnedMeshRenderer GetCreatureHeadLOD0(this Creature creature)
		{
			foreach (ManikinGroupPart child in creature.GetComponentInChildren<ManikinPartList>().GetComponentsInChildren<ManikinGroupPart>())
			{
				if (child.name.ToLowerInvariant().Contains("head"))
				{
					return child.partLODs[0].renderers[0] as SkinnedMeshRenderer;
				}
			}
			return null;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x000068A4 File Offset: 0x00004AA4
		internal static SkinnedMeshRenderer[] GetCreatureHeadLOD(this Creature creature)
		{
			foreach (ManikinGroupPart child in creature.GetComponentInChildren<ManikinPartList>().GetComponentsInChildren<ManikinGroupPart>())
			{
				if (child.name.ToLowerInvariant().Contains("head"))
				{
					return (from r in child.partLODs[0].renderers
					select r as SkinnedMeshRenderer).ToArray<SkinnedMeshRenderer>();
				}
			}
			return null;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00006924 File Offset: 0x00004B24
		internal static SkinnedMeshRenderer GetCreaturePartLOD0(this RagdollPart part)
		{
			string name = part.type.ToString().ToLowerInvariant();
			foreach (ManikinGroupPart child in part.ragdoll.creature.GetComponentInChildren<ManikinPartList>().GetComponentsInChildren<ManikinGroupPart>())
			{
				if (child.name.ToLowerInvariant().Contains(name))
				{
					return child.partLODs[0].renderers[0] as SkinnedMeshRenderer;
				}
			}
			return null;
		}
	}
}

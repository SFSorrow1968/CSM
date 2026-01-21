using System;
using CarnageReborn.Gore;
using ThunderRoad;

namespace CarnageReborn
{
	// Token: 0x02000003 RID: 3
	public static class CarnageEventManager
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000011 RID: 17 RVA: 0x00002484 File Offset: 0x00000684
		// (remove) Token: 0x06000012 RID: 18 RVA: 0x000024B8 File Offset: 0x000006B8
		public static event EyeballPopped EyeballPopped;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000013 RID: 19 RVA: 0x000024EC File Offset: 0x000006EC
		// (remove) Token: 0x06000014 RID: 20 RVA: 0x00002520 File Offset: 0x00000720
		public static event Exploded MeshExploded;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000015 RID: 21 RVA: 0x00002554 File Offset: 0x00000754
		// (remove) Token: 0x06000016 RID: 22 RVA: 0x00002588 File Offset: 0x00000788
		public static event ThroatSlit ThroatSlit;

		// Token: 0x06000017 RID: 23 RVA: 0x000025BB File Offset: 0x000007BB
		internal static void InvokeEyeballPopped(this Eyeball eyeball)
		{
			EyeballPopped eyeballPopped = CarnageEventManager.EyeballPopped;
			if (eyeballPopped == null)
			{
				return;
			}
			eyeballPopped(eyeball);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000025CD File Offset: 0x000007CD
		internal static void InvokeMeshExploded(this Explodable explodable)
		{
			Exploded meshExploded = CarnageEventManager.MeshExploded;
			if (meshExploded == null)
			{
				return;
			}
			meshExploded(explodable);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000025DF File Offset: 0x000007DF
		internal static void InvokeThroatSlit(this Creature creature)
		{
			ThroatSlit throatSlit = CarnageEventManager.ThroatSlit;
			if (throatSlit == null)
			{
				return;
			}
			throatSlit(creature);
		}
	}
}

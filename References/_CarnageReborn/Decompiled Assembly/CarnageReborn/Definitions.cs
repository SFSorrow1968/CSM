using System;
using UnityEngine;

namespace CarnageReborn
{
	// Token: 0x02000018 RID: 24
	public static class Definitions
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00005465 File Offset: 0x00003665
		public static Vector3 SkullOffset { get; } = new Vector3(-0.03f, 0f, -0.02f);

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x0000546C File Offset: 0x0000366C
		public static Vector3 SkullRotation { get; } = new Vector3(0f, 0f, 90f);

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00005473 File Offset: 0x00003673
		public static float MaleSkullScaleMultiplier { get; } = 0.9f;

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000BB RID: 187 RVA: 0x0000547A File Offset: 0x0000367A
		public static float FemaleSkullScaleMultiplier { get; } = 0.9f;

		// Token: 0x0400004C RID: 76
		public static string skullPrefabAddress = "SD.MaleSkull.prefab";

		// Token: 0x0400004D RID: 77
		public static string eyeballPrefabAddress = "SD.Eyeball.prefab";

		// Token: 0x0400004E RID: 78
		public static string gutsPrefabAddress = "SD.Guts.prefab";

		// Token: 0x0400004F RID: 79
		public static string nonFatalFinisher = "ToKnees";

		// Token: 0x04000050 RID: 80
		public static string slitThroatFinisherStart = "ToKnees-ThroatSlit";
	}
}

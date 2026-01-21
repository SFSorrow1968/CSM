using System;
using System.Runtime.CompilerServices;
using IngameDebugConsole;
using ThunderRoad;
using UnityEngine;

namespace CarnageReborn.Utilities
{
	// Token: 0x0200002A RID: 42
	public static class Commands
	{
		// Token: 0x06000107 RID: 263 RVA: 0x000069A4 File Offset: 0x00004BA4
		public static void TestAll()
		{
			Commands.<TestAll>d__0 <TestAll>d__;
			<TestAll>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<TestAll>d__.<>1__state = -1;
			<TestAll>d__.<>t__builder.Start<Commands.<TestAll>d__0>(ref <TestAll>d__);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x000069D3 File Offset: 0x00004BD3
		public static void TestThroatFinisher()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestThroatFinisher>g__HandleCSpawn|1_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00006A03 File Offset: 0x00004C03
		public static void TestHeadExplosion()
		{
			ModOptions.headSmashing = true;
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestHeadExplosion>g__HandleCSpawn|2_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00006A39 File Offset: 0x00004C39
		public static void TestCreatureDeformation()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestCreatureDeformation>g__HandleCSpawn|3_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00006A69 File Offset: 0x00004C69
		public static void TestSpine()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestSpine>g__HandleCSpawn|4_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00006A99 File Offset: 0x00004C99
		public static void TestGuts()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestGuts>g__HandleCSpawn|5_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00006AC9 File Offset: 0x00004CC9
		public static void TestBoneBreaking()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestBoneBreaking>g__HandleCSpawn|6_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00006AF9 File Offset: 0x00004CF9
		public static void TestBoneBreakingThrowing()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestBoneBreakingThrowing>g__HandleCSpawn|7_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00006B29 File Offset: 0x00004D29
		public static void TestAnimations()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestAnimations>g__HandleCSpawn|8_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00006B59 File Offset: 0x00004D59
		public static void TestEyeballs()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestEyeballs>g__HandleCSpawn|9_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00006B89 File Offset: 0x00004D89
		public static void TestAssets()
		{
			EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(Commands.<TestAssets>g__HandleCSpawn|10_0);
			DebugLogConsole.ExecuteCommand((Random.value < 0.5f) ? "sc HumanFemale" : "sc HumanMale");
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00006BBC File Offset: 0x00004DBC
		[CompilerGenerated]
		internal static void <TestThroatFinisher>g__HandleCSpawn|1_0(Creature creature)
		{
			Commands.<<TestThroatFinisher>g__HandleCSpawn|1_0>d <<TestThroatFinisher>g__HandleCSpawn|1_0>d;
			<<TestThroatFinisher>g__HandleCSpawn|1_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestThroatFinisher>g__HandleCSpawn|1_0>d.creature = creature;
			<<TestThroatFinisher>g__HandleCSpawn|1_0>d.<>1__state = -1;
			<<TestThroatFinisher>g__HandleCSpawn|1_0>d.<>t__builder.Start<Commands.<<TestThroatFinisher>g__HandleCSpawn|1_0>d>(ref <<TestThroatFinisher>g__HandleCSpawn|1_0>d);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00006BF4 File Offset: 0x00004DF4
		[CompilerGenerated]
		internal static void <TestHeadExplosion>g__HandleCSpawn|2_0(Creature creature)
		{
			Commands.<<TestHeadExplosion>g__HandleCSpawn|2_0>d <<TestHeadExplosion>g__HandleCSpawn|2_0>d;
			<<TestHeadExplosion>g__HandleCSpawn|2_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestHeadExplosion>g__HandleCSpawn|2_0>d.creature = creature;
			<<TestHeadExplosion>g__HandleCSpawn|2_0>d.<>1__state = -1;
			<<TestHeadExplosion>g__HandleCSpawn|2_0>d.<>t__builder.Start<Commands.<<TestHeadExplosion>g__HandleCSpawn|2_0>d>(ref <<TestHeadExplosion>g__HandleCSpawn|2_0>d);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00006C2C File Offset: 0x00004E2C
		[CompilerGenerated]
		internal static void <TestCreatureDeformation>g__HandleCSpawn|3_0(Creature creature)
		{
			Commands.<<TestCreatureDeformation>g__HandleCSpawn|3_0>d <<TestCreatureDeformation>g__HandleCSpawn|3_0>d;
			<<TestCreatureDeformation>g__HandleCSpawn|3_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestCreatureDeformation>g__HandleCSpawn|3_0>d.creature = creature;
			<<TestCreatureDeformation>g__HandleCSpawn|3_0>d.<>1__state = -1;
			<<TestCreatureDeformation>g__HandleCSpawn|3_0>d.<>t__builder.Start<Commands.<<TestCreatureDeformation>g__HandleCSpawn|3_0>d>(ref <<TestCreatureDeformation>g__HandleCSpawn|3_0>d);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00006C64 File Offset: 0x00004E64
		[CompilerGenerated]
		internal static void <TestSpine>g__HandleCSpawn|4_0(Creature creature)
		{
			Commands.<<TestSpine>g__HandleCSpawn|4_0>d <<TestSpine>g__HandleCSpawn|4_0>d;
			<<TestSpine>g__HandleCSpawn|4_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestSpine>g__HandleCSpawn|4_0>d.creature = creature;
			<<TestSpine>g__HandleCSpawn|4_0>d.<>1__state = -1;
			<<TestSpine>g__HandleCSpawn|4_0>d.<>t__builder.Start<Commands.<<TestSpine>g__HandleCSpawn|4_0>d>(ref <<TestSpine>g__HandleCSpawn|4_0>d);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00006C9C File Offset: 0x00004E9C
		[CompilerGenerated]
		internal static void <TestGuts>g__HandleCSpawn|5_0(Creature creature)
		{
			Commands.<<TestGuts>g__HandleCSpawn|5_0>d <<TestGuts>g__HandleCSpawn|5_0>d;
			<<TestGuts>g__HandleCSpawn|5_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestGuts>g__HandleCSpawn|5_0>d.creature = creature;
			<<TestGuts>g__HandleCSpawn|5_0>d.<>1__state = -1;
			<<TestGuts>g__HandleCSpawn|5_0>d.<>t__builder.Start<Commands.<<TestGuts>g__HandleCSpawn|5_0>d>(ref <<TestGuts>g__HandleCSpawn|5_0>d);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00006CD4 File Offset: 0x00004ED4
		[CompilerGenerated]
		internal static void <TestBoneBreaking>g__HandleCSpawn|6_0(Creature creature)
		{
			Commands.<<TestBoneBreaking>g__HandleCSpawn|6_0>d <<TestBoneBreaking>g__HandleCSpawn|6_0>d;
			<<TestBoneBreaking>g__HandleCSpawn|6_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestBoneBreaking>g__HandleCSpawn|6_0>d.creature = creature;
			<<TestBoneBreaking>g__HandleCSpawn|6_0>d.<>1__state = -1;
			<<TestBoneBreaking>g__HandleCSpawn|6_0>d.<>t__builder.Start<Commands.<<TestBoneBreaking>g__HandleCSpawn|6_0>d>(ref <<TestBoneBreaking>g__HandleCSpawn|6_0>d);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00006D0C File Offset: 0x00004F0C
		[CompilerGenerated]
		internal static void <TestBoneBreakingThrowing>g__HandleCSpawn|7_0(Creature creature)
		{
			Commands.<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d <<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d;
			<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d.creature = creature;
			<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d.<>1__state = -1;
			<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d.<>t__builder.Start<Commands.<<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d>(ref <<TestBoneBreakingThrowing>g__HandleCSpawn|7_0>d);
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00006D44 File Offset: 0x00004F44
		[CompilerGenerated]
		internal static void <TestAnimations>g__HandleCSpawn|8_0(Creature creature)
		{
			Commands.<<TestAnimations>g__HandleCSpawn|8_0>d <<TestAnimations>g__HandleCSpawn|8_0>d;
			<<TestAnimations>g__HandleCSpawn|8_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestAnimations>g__HandleCSpawn|8_0>d.creature = creature;
			<<TestAnimations>g__HandleCSpawn|8_0>d.<>1__state = -1;
			<<TestAnimations>g__HandleCSpawn|8_0>d.<>t__builder.Start<Commands.<<TestAnimations>g__HandleCSpawn|8_0>d>(ref <<TestAnimations>g__HandleCSpawn|8_0>d);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00006D7C File Offset: 0x00004F7C
		[CompilerGenerated]
		internal static void <TestEyeballs>g__HandleCSpawn|9_0(Creature creature)
		{
			Commands.<<TestEyeballs>g__HandleCSpawn|9_0>d <<TestEyeballs>g__HandleCSpawn|9_0>d;
			<<TestEyeballs>g__HandleCSpawn|9_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestEyeballs>g__HandleCSpawn|9_0>d.creature = creature;
			<<TestEyeballs>g__HandleCSpawn|9_0>d.<>1__state = -1;
			<<TestEyeballs>g__HandleCSpawn|9_0>d.<>t__builder.Start<Commands.<<TestEyeballs>g__HandleCSpawn|9_0>d>(ref <<TestEyeballs>g__HandleCSpawn|9_0>d);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00006DB4 File Offset: 0x00004FB4
		[CompilerGenerated]
		internal static void <TestAssets>g__HandleCSpawn|10_0(Creature creature)
		{
			Commands.<<TestAssets>g__HandleCSpawn|10_0>d <<TestAssets>g__HandleCSpawn|10_0>d;
			<<TestAssets>g__HandleCSpawn|10_0>d.<>t__builder = AsyncVoidMethodBuilder.Create();
			<<TestAssets>g__HandleCSpawn|10_0>d.creature = creature;
			<<TestAssets>g__HandleCSpawn|10_0>d.<>1__state = -1;
			<<TestAssets>g__HandleCSpawn|10_0>d.<>t__builder.Start<Commands.<<TestAssets>g__HandleCSpawn|10_0>d>(ref <<TestAssets>g__HandleCSpawn|10_0>d);
		}
	}
}

using System;
using ThunderRoad;

namespace CarnageReborn
{
	// Token: 0x02000024 RID: 36
	public static class ModOptions
	{
		// Token: 0x060000EB RID: 235 RVA: 0x00005DE4 File Offset: 0x00003FE4
		public static ModOptionInt[] IntProvider()
		{
			ModOptionInt[] floats = new ModOptionInt[100];
			int val = 0;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i] = new ModOptionInt(string.Format("{0:N0}", val), val);
				val++;
			}
			return floats;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00005E28 File Offset: 0x00004028
		public static ModOptionFloat[] FloatProvider()
		{
			ModOptionFloat[] floats = new ModOptionFloat[1000];
			float val = 0f;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i] = new ModOptionFloat(string.Format("{0:0.00}", val), val);
				val += 0.1f;
			}
			return floats;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00005E78 File Offset: 0x00004078
		public static ModOptionFloat[] FloatProvider001()
		{
			ModOptionFloat[] floats = new ModOptionFloat[100];
			float val = 0f;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i] = new ModOptionFloat(string.Format("{0:0.00}", val), val);
				val += 0.01f;
			}
			return floats;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00005EC4 File Offset: 0x000040C4
		public static ModOptionFloat[] FloatProvider01()
		{
			ModOptionFloat[] floats = new ModOptionFloat[10];
			float val = 0f;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i] = new ModOptionFloat(string.Format("{0:0.00}", val), val);
				val += 0.1f;
			}
			return floats;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00005F10 File Offset: 0x00004110
		public static ModOptionFloat[] FloatProviderN1P10()
		{
			ModOptionFloat[] floats = new ModOptionFloat[11];
			float val = -1f;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i] = new ModOptionFloat(string.Format("{0:0.00}", val), val);
				if (i == 1)
				{
					val = 0f;
				}
				val += 0.1f;
			}
			return floats;
		}

		// Token: 0x0400005C RID: 92
		[ModOption(name = "API Mode", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "If enabled CR will take no effect in-game and only be usable via code.")]
		public static bool apiOnlyMode = false;

		// Token: 0x0400005D RID: 93
		[ModOption(name = "Spawn Skull", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Spawn a skull inside humanoids?")]
		public static bool spawnSkull = true;

		// Token: 0x0400005E RID: 94
		[ModOption(name = "Spawn Bones", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Spawn a bone when slicing a ragdoll part?")]
		public static bool spawnBones = true;

		// Token: 0x0400005F RID: 95
		[ModOption(name = "Spawn Spine Bone", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Spawn part of the spine when slicing heads?")]
		public static bool spawnSpineBone = true;

		// Token: 0x04000060 RID: 96
		[ModOption(name = "Spawn Tendant", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "Create a tendant for sliced parts?")]
		public static bool createTendants = false;

		// Token: 0x04000061 RID: 97
		[ModOption(name = "Tendant Snap Distance", category = "Core", defaultValueIndex = 10, categoryOrder = 0, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How far can a tendant stretch before snapping?.")]
		public static float tendantSnapDistance = 1f;

		// Token: 0x04000062 RID: 98
		[ModOption(name = "Survivable Dismemberment", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Use Carnage Reborn's solution for survivable dismemberment?")]
		public static bool surviveDismemberment = true;

		// Token: 0x04000063 RID: 99
		[ModOption(name = "New Removable Eyes", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Use the new removable eye system? (Dangling from the cord)")]
		public static bool newRemovableEyes = true;

		// Token: 0x04000064 RID: 100
		[ModOption(name = "Old Removable Eyes", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "Use the old removable eye system? (Just pop the eyeball itself no cord)")]
		public static bool oldRemovableEyes = false;

		// Token: 0x04000065 RID: 101
		[ModOption(name = "Items Can Deform", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Items can deform enemies/etc?")]
		public static bool itemsCanDeform = true;

		// Token: 0x04000066 RID: 102
		[ModOption(name = "Items Can Be Deformed", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "Items can be deformed by the environment/npcs/etc?")]
		public static bool itemsCanBeDeformed = false;

		// Token: 0x04000067 RID: 103
		[ModOption(name = "NPC Throw Damaged Weapons", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "NPCs will drop deformed weapons?")]
		public static bool npcThrowDamagedWeapon = false;

		// Token: 0x04000068 RID: 104
		[ModOption(name = "NPC Deformable By Environment", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "Can NPCs be deformed by the environment? (e.g: Smashing their face in to a wall deformes it)")]
		public static bool selfDeformingEnemies = true;

		// Token: 0x04000069 RID: 105
		[ModOption(name = "Item Deformable By Environment", category = "Core", defaultValueIndex = 0, categoryOrder = 0, tooltip = "Items can be deformed by the environment? (Hitting a weapon against a wall bends it)")]
		public static bool selfDeformingWeapons = false;

		// Token: 0x0400006A RID: 106
		[ModOption(name = "Weapon Damage Throw Ratio", category = "Core", defaultValueIndex = 97, categoryOrder = 0, valueSourceName = "IntProvider", interactionType = 2, tooltip = "The weapons deformation ratio before an NPC will throw.")]
		public static int weaponDamageThrowRatio = 97;

		// Token: 0x0400006B RID: 107
		[ModOption(name = "Carnage Health Multiplier", category = "Core", defaultValueIndex = 1, categoryOrder = 0, tooltip = "If enabled enemies will have their health multiplied by npcHealthMultiplier.")]
		public static bool enableHealthMultiplier = true;

		// Token: 0x0400006C RID: 108
		[ModOption(name = "NPC Health Multiplier", category = "Core", defaultValueIndex = 1, categoryOrder = 0, valueSourceName = "IntProvider", interactionType = 2, tooltip = "Health multiplier.")]
		public static int npcHealthMultiplier = 1;

		// Token: 0x0400006D RID: 109
		[ModOption(name = "Bone Breaking", category = "Bone Breaking", defaultValueIndex = 0, tooltip = "Enable/disable CR's bone breaking system?")]
		public static bool boneBreaking = false;

		// Token: 0x0400006E RID: 110
		[ModOption(name = "Snapped Max Angle", category = "Bone Breaking", defaultValueIndex = 800, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Maximum angle for snapped bones, lower values mean less twist and bend.")]
		public static float maxSnapAngle = 80f;

		// Token: 0x0400006F RID: 111
		[ModOption(name = "Paralysis Limp Multiplier", category = "Bone Breaking", defaultValueIndex = 2, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Lower values mean more floppy bones when broken.")]
		public static float paralysisLimpMultiplier = 0.2f;

		// Token: 0x04000070 RID: 112
		[ModOption(name = "Breakable Arms", category = "Bone Breaking", defaultValueIndex = 1, tooltip = "Enable/disable broken arms?")]
		public static bool breakableArms = true;

		// Token: 0x04000071 RID: 113
		[ModOption(name = "Arm Minimum Angle", category = "Bone Breaking", defaultValueIndex = 20, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum angle to break arms.")]
		public static float minimumArmAngle = 2f;

		// Token: 0x04000072 RID: 114
		[ModOption(name = "Arm Minimum Force", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum force to break arms.")]
		public static float minimumArmForce = 20f;

		// Token: 0x04000073 RID: 115
		[ModOption(name = "Arm Ideal Angle", category = "Bone Breaking", defaultValueIndex = 100, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal angle to break arms.")]
		public static float idealArmAngle = 10f;

		// Token: 0x04000074 RID: 116
		[ModOption(name = "Arm Ideal Force", category = "Bone Breaking", defaultValueIndex = 300, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal force to break arms.")]
		public static float idealArmForce = 30f;

		// Token: 0x04000075 RID: 117
		[ModOption(name = "Breakable Legs", category = "Bone Breaking", defaultValueIndex = 1, tooltip = "Enable/disable broken legs?")]
		public static bool breakableLegs = true;

		// Token: 0x04000076 RID: 118
		[ModOption(name = "Legs Minimum Angle", category = "Bone Breaking", defaultValueIndex = 10, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum angle to break Legs.")]
		public static float minimumLegsAngle = 1f;

		// Token: 0x04000077 RID: 119
		[ModOption(name = "Legs Minimum Force", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum force to break Legs.")]
		public static float minimumLegsForce = 20f;

		// Token: 0x04000078 RID: 120
		[ModOption(name = "Legs Ideal Angle", category = "Bone Breaking", defaultValueIndex = 50, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal angle to break Legs.")]
		public static float idealLegsAngle = 5f;

		// Token: 0x04000079 RID: 121
		[ModOption(name = "Legs Ideal Force", category = "Bone Breaking", defaultValueIndex = 300, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal force to break Legs.")]
		public static float idealLegsForce = 30f;

		// Token: 0x0400007A RID: 122
		[ModOption(name = "Breakable Spine", category = "Bone Breaking", defaultValueIndex = 1, tooltip = "Enable/disable breakable spine?")]
		public static bool breakableSpine = true;

		// Token: 0x0400007B RID: 123
		[ModOption(name = "Spine Minimum Angle", category = "Bone Breaking", defaultValueIndex = 30, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum angle to break Spine.")]
		public static float minimumSpineAngle = 3f;

		// Token: 0x0400007C RID: 124
		[ModOption(name = "Spine Minimum Force", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum force to break Spine.")]
		public static float minimumSpineForce = 20f;

		// Token: 0x0400007D RID: 125
		[ModOption(name = "Spine Ideal Angle", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal angle to break Spine.")]
		public static float idealSpineAngle = 20f;

		// Token: 0x0400007E RID: 126
		[ModOption(name = "Spine Ideal Force", category = "Bone Breaking", defaultValueIndex = 300, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal force to break Spine.")]
		public static float idealSpineForce = 30f;

		// Token: 0x0400007F RID: 127
		[ModOption(name = "Breakable Neck", category = "Bone Breaking", defaultValueIndex = 1, tooltip = "Enable/disable neck snapping?")]
		public static bool neckSnapping = true;

		// Token: 0x04000080 RID: 128
		[ModOption(name = "Neck Y Minimum Angle", category = "Bone Breaking", defaultValueIndex = 500, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum angle to break neck.")]
		public static float minimumNeckYAngle = 50f;

		// Token: 0x04000081 RID: 129
		[ModOption(name = "Neck Y Minimum Force", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum force to break neck.")]
		public static float minimumNeckYForce = 20f;

		// Token: 0x04000082 RID: 130
		[ModOption(name = "Neck Y Ideal Angle", category = "Bone Breaking", defaultValueIndex = 50, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal angle to break neck.")]
		public static float idealNeckYAngle = 5f;

		// Token: 0x04000083 RID: 131
		[ModOption(name = "Neck Y Ideal Force", category = "Bone Breaking", defaultValueIndex = 300, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal force to break neck.")]
		public static float idealNeckYForce = 30f;

		// Token: 0x04000084 RID: 132
		[ModOption(name = "Neck X Minimum Angle", category = "Bone Breaking", defaultValueIndex = 200, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum angle to break neck.")]
		public static float minimumNeckXAngle = 20f;

		// Token: 0x04000085 RID: 133
		[ModOption(name = "Neck X Minimum Force", category = "Bone Breaking", defaultValueIndex = 150, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum force to break neck.")]
		public static float minimumNeckXForce = 15f;

		// Token: 0x04000086 RID: 134
		[ModOption(name = "Neck X Ideal Angle", category = "Bone Breaking", defaultValueIndex = 350, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal angle to break neck.")]
		public static float idealNeckXAngle = 35f;

		// Token: 0x04000087 RID: 135
		[ModOption(name = "Neck X Ideal Force", category = "Bone Breaking", defaultValueIndex = 300, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The ideal force to break neck.")]
		public static float idealNeckXForce = 30f;

		// Token: 0x04000088 RID: 136
		[ModOption(name = "NPCs Can Recover From Finisher", category = "Finishers", defaultValueIndex = 1, tooltip = "Can NPCs recover from a finisher?")]
		public static bool enemiesCanGetBackUpFromFailedFinisher = true;

		// Token: 0x04000089 RID: 137
		[ModOption(name = "Failed Finisher Heals NPC", category = "Finishers", defaultValueIndex = 1, tooltip = "Will a failed finisher heal an NPC?")]
		public static bool failedFinisherHealsEnemies = true;

		// Token: 0x0400008A RID: 138
		[ModOption(name = "Finisher Chance", category = "Finishers", defaultValueIndex = 250, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How likely is it a finisher will occur?")]
		public static float finisherChance = 25f;

		// Token: 0x0400008B RID: 139
		[ModOption(name = "Minimum Activation Distance", category = "Finishers", defaultValueIndex = 150, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum distance an NPC has to be to you before a finisher can play.")]
		public static float minimumDistanceToPlayerForFinisher = 15f;

		// Token: 0x0400008C RID: 140
		[ModOption(name = "Minimum Activation Health", category = "Finishers", defaultValueIndex = 250, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum health an NPC must be at before a finisher can be considered, this is a % from 0-100 to work with mods.")]
		public static float minimumFinisherHealth = 25f;

		// Token: 0x0400008D RID: 141
		[ModOption(name = "Recovery Time", category = "Finishers", defaultValueIndex = 250, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How long it takes for an NPC to recover or die from a finisher.")]
		public static float timeTillEnemyRecovers = 25f;

		// Token: 0x0400008E RID: 142
		[ModOption(name = "Health Recovery", category = "Finishers", defaultValueIndex = 250, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How much health a failed finisher will restore.")]
		public static float failedFinisherHealAmount = 25f;

		// Token: 0x0400008F RID: 143
		[ModOption(name = "Fatal Finisher Chance", category = "Fatal Finishers", defaultValueIndex = 250, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How likely is a fatal finisher to play?")]
		public static float slitThroatFinisherChance = 25f;

		// Token: 0x04000090 RID: 144
		[ModOption(name = "Smash Enabled", category = "Head Smashing", defaultValueIndex = 0, tooltip = "Use Carnage Reborn's head smashing system?")]
		public static bool headSmashing = false;

		// Token: 0x04000091 RID: 145
		[ModOption(name = "Blunt Only", category = "Head Smashing", defaultValueIndex = 0, tooltip = "Should only blunt damage cause heads to smash?")]
		public static bool bluntOnlySmashing = false;

		// Token: 0x04000092 RID: 146
		[ModOption(name = "Skull Fragments", category = "Head Smashing", defaultValueIndex = 1, tooltip = "Spawn skull fragments when smashing a head?")]
		public static bool spawnSkullFragmentsOnSmash = true;

		// Token: 0x04000093 RID: 147
		[ModOption(name = "Brain", category = "Head Smashing", defaultValueIndex = 1, tooltip = "Spawn a brain when smashing a head?")]
		public static bool spawnBrain = true;

		// Token: 0x04000094 RID: 148
		[ModOption(name = "Brain Spawn Chance", category = "Head Smashing", defaultValueIndex = 800, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "How likely is a brain to spawn when smashing a head?")]
		public static float brainSpawnChance = 80f;

		// Token: 0x04000095 RID: 149
		[ModOption(name = "Head Smash Velocity", category = "Head Smashing", defaultValueIndex = 130, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum velocity required to smash a head. (Higher = harder to smash)")]
		public static float headSmashVelocity = 12f;

		// Token: 0x04000096 RID: 150
		[ModOption(name = "Eyeball Knockout Velocity", category = "Head Smashing", defaultValueIndex = 100, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum velocity required to knockout an eyeball. (Higher = harder to pop)")]
		public static float eyeballKnockoutVelocity = 10f;

		// Token: 0x04000097 RID: 151
		[ModOption(name = "Eyeball Radius", category = "Head Smashing", defaultValueIndex = 9, valueSourceName = "FloatProvider001", interactionType = 2, tooltip = "Radius around the eyes used to calculate when they've been hit.")]
		public static float eyeballRadius = 0.09f;

		// Token: 0x04000098 RID: 152
		[ModOption(name = "Instant Crush Mass Threshold", category = "Head Smashing", defaultValueIndex = 900, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Object mass to instantly crush a head when touched with.")]
		public static float crushingMassScale = 100f;

		// Token: 0x04000099 RID: 153
		[ModOption(name = "Torso Enabled", category = "Sliceable Torso", defaultValueIndex = 1, tooltip = "Allow sliceable torso?")]
		public static bool sliceableTorso = true;

		// Token: 0x0400009A RID: 154
		[ModOption(name = "Guts", category = "Sliceable Torso", defaultValueIndex = 1, tooltip = "Spawn guts when slicing the torso?")]
		public static bool spawnGuts = true;

		// Token: 0x0400009B RID: 155
		[ModOption(name = "Guts Despawn Time", category = "Sliceable Torso", defaultValueIndex = 100, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Seconds till guts despawn.")]
		public static float gutsDespawnTime = 100f;

		// Token: 0x0400009C RID: 156
		[ModOption(name = "Kill On Torso Sliced", category = "Sliceable Torso", defaultValueIndex = 1, tooltip = "Kill the NPC when slicing the torso?")]
		public static bool killOnTorsoSliced = true;

		// Token: 0x0400009D RID: 157
		[ModOption(name = "Slice Threshold", category = "Sliceable Torso", defaultValueIndex = 2, valueSourceName = "FloatProvider01", interactionType = 2, tooltip = "How easy is it to slice the torso? (Higher = easier)")]
		public static float torsoSliceThreshold = 0.25f;

		// Token: 0x0400009E RID: 158
		[ModOption(name = "Max Vertex Velocity", category = "Deformation", defaultValueIndex = 80, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Maximum offset a vertex can have applied when deforming.")]
		public static float maxVertexVelocity = 8f;

		// Token: 0x0400009F RID: 159
		[ModOption(name = "Minimum Deformation Velocity", category = "Deformation", defaultValueIndex = 10, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Minimum offset a vertex can have applied when deforming.")]
		public static float minDeformVelocity = 1f;

		// Token: 0x040000A0 RID: 160
		[ModOption(name = "Weapon Deformation Multiplier", category = "Deformation", defaultValueIndex = 2, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation multiplier to weapons. (higher = easier to deform)")]
		public static float weaponDamageDeformationMultiplier = 0.2f;

		// Token: 0x040000A1 RID: 161
		[ModOption(name = "Prop Deformation Multiplier", category = "Deformation", defaultValueIndex = 2, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation multiplier to props. (higher = easier to deform)")]
		public static float propDamageDeformationMultiplier = 0.2f;

		// Token: 0x040000A2 RID: 162
		[ModOption(name = "Default Deformation Multiplier", category = "Deformation", defaultValueIndex = 2, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation multiplier to objects. (higher = easier to deform)")]
		public static float defaultDamageDeformationMultiplier = 0.2f;

		// Token: 0x040000A3 RID: 163
		[ModOption(name = "Player Hands Deformation Multiplier", category = "Deformation", defaultValueIndex = 1, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation multiplier to your hands. (higher = easier to deform)")]
		public static float playerHandsDamageDeformationMultiplier = 0.1f;

		// Token: 0x040000A4 RID: 164
		[ModOption(name = "Weapon Deformation Damper", category = "Deformation", defaultValueIndex = 15, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation damper to weapons. (higher = harder to deform)")]
		public static float weaponDeformationDamper = 1.5f;

		// Token: 0x040000A5 RID: 165
		[ModOption(name = "Prop Deformation Damper", category = "Deformation", defaultValueIndex = 10, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation damper to props. (higher = harder to deform)")]
		public static float propDeformationDamper = 1f;

		// Token: 0x040000A6 RID: 166
		[ModOption(name = "NPC Deformation Damper", category = "Deformation", defaultValueIndex = 10, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation damper to NPCs. (higher = harder to deform)")]
		public static float enemyDeformationDamper = 1f;

		// Token: 0x040000A7 RID: 167
		[ModOption(name = "Default Deformation Damper", category = "Deformation", defaultValueIndex = 10, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "Applies a deformation damper to objects. (higher = harder to deform)")]
		public static float deformationDamper = 1f;

		// Token: 0x040000A8 RID: 168
		[ModOption(name = "Allow Custom Sounds", category = "Audio", defaultValueIndex = 1, tooltip = "Allow custom CR sounds to play?")]
		public static bool allowCustomSounds = true;

		// Token: 0x040000A9 RID: 169
		[ModOption(name = "Effect Volume", category = "Audio", defaultValueIndex = 9, valueSourceName = "FloatProvider01", interactionType = 2, tooltip = "How louad are CR's custom sounds?")]
		public static float effectVolume = 1f;

		// Token: 0x040000AA RID: 170
		[ModOption(name = "Max Threads", category = "Performance", defaultValueIndex = 30, valueSourceName = "IntProvider", interactionType = 2, tooltip = "How many threads can exist at any given time when deforming?")]
		public static int maxThreads = 30;

		// Token: 0x040000AB RID: 171
		[ModOption(name = "Max Threads Per Instance", category = "Performance", defaultValueIndex = 3, valueSourceName = "IntProvider", interactionType = 2, tooltip = "How many threads can a single object run?")]
		public static int maxThreadsPerInstance = 3;

		// Token: 0x040000AC RID: 172
		[ModOption(name = "Custom Effects", category = "Performance", defaultValueIndex = 1, tooltip = "Play custom effects? (If disabled can save a lot of performance)")]
		public static bool customEffects = true;

		// Token: 0x040000AD RID: 173
		[ModOption(name = "Stab Blood Spurts", category = "Performance", defaultValueIndex = 0, tooltip = "Should stab wounds cause bleeding?")]
		public static bool stabBloodBursts = false;

		// Token: 0x040000AE RID: 174
		[ModOption(name = "Prevent Distanced Deformations", category = "Performance", defaultValueIndex = 1, tooltip = "If a target is too far from you it will not deform to save performance.")]
		public static bool preventDistantDeformations = true;

		// Token: 0x040000AF RID: 175
		[ModOption(name = "Maximum Deformation Distance", category = "Performance", defaultValueIndex = 60, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "The maximum distance deformation will be allowed.")]
		public static float maxDeformDistance = 60f;

		// Token: 0x040000B0 RID: 176
		[ModOption(name = "Deformation Timeout", category = "Performance", defaultValueIndex = 1, valueSourceName = "FloatProvider", interactionType = 2, tooltip = "A small timeout applied when deforming to prevent thousands of requests in a single frame.")]
		public static float deformationTimeout = 0.1f;
	}
}

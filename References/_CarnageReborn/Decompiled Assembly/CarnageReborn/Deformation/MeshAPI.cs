using System;
using System.Collections.Generic;
using System.Threading;
using ThunderRoad;
using Unity.Collections;
using UnityEngine;

namespace CarnageReborn.Deformation
{
	// Token: 0x0200002C RID: 44
	public static class MeshAPI
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000128 RID: 296 RVA: 0x000072A5 File Offset: 0x000054A5
		private static Dictionary<int, List<Thread>> ModifyVertexThreads { get; } = new Dictionary<int, List<Thread>>();

		// Token: 0x06000129 RID: 297 RVA: 0x000072AC File Offset: 0x000054AC
		public static void Deform(this Component target, DeformRequest request, Action<Mesh, Renderer> deformed = null)
		{
			try
			{
				Mesh mesh;
				bool isSkinnedMesh;
				bool isItem;
				Renderer renderer = target.TryGetRenderer(out mesh, out isSkinnedMesh, out isItem);
				if (!(renderer == null))
				{
					Creature creature = renderer.GetComponentInParent<Creature>();
					if ((creature == null || !creature.isPlayer) && (!(target.GetComponentInParent<Item>() != null) || ModOptions.itemsCanBeDeformed))
					{
						if (!mesh.IsMeshEditable())
						{
							Debug.LogWarning("'" + Common.GetPathFromRoot(target.gameObject) + "' can't be deformed, the mesh is not read/write enabled!");
							return;
						}
						MeshAPI.DeformTargetAtPoint(request, renderer.transform, (!isSkinnedMesh) ? null : renderer.GetComponent<SkinnedMeshRenderer>(), mesh, delegate(Mesh m)
						{
							MainThread.Invoke(delegate
							{
								Action<Mesh, Renderer> deformed3 = deformed;
								if (deformed3 == null)
								{
									return;
								}
								deformed3(m, renderer);
							});
						});
						return;
					}
				}
				Debug.LogWarning("'" + Common.GetPathFromRoot(target.gameObject) + "' can't be deformed!");
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Error when deforming: {0}", e));
				Action<Mesh, Renderer> deformed2 = deformed;
				if (deformed2 != null)
				{
					deformed2(null, null);
				}
			}
		}

		// Token: 0x0600012A RID: 298 RVA: 0x000073CC File Offset: 0x000055CC
		public static bool IsMeshEditable(this Mesh mesh)
		{
			return mesh != null && mesh.isReadable;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x000073DF File Offset: 0x000055DF
		public static Mesh CloneMesh(this Mesh mesh)
		{
			if (!(mesh == null))
			{
				return Object.Instantiate<Mesh>(mesh);
			}
			return null;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x000073F4 File Offset: 0x000055F4
		public static Mesh BakeMesh(this SkinnedMeshRenderer skm)
		{
			Mesh mesh = new Mesh();
			List<Matrix4x4> bindPoses = new List<Matrix4x4>();
			Mesh result;
			using (NativeArray<BoneWeight1> boneWeights = skm.sharedMesh.GetAllBoneWeights())
			{
				using (NativeArray<byte> perBoneData = skm.sharedMesh.GetBonesPerVertex())
				{
					skm.sharedMesh.GetBindposes(bindPoses);
					skm.BakeMesh(mesh);
					mesh.bindposes = bindPoses.ToArray();
					mesh.SetBoneWeights(perBoneData, boneWeights);
					result = mesh;
				}
			}
			return result;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000748C File Offset: 0x0000568C
		public static void GetDeformationRatio(this Component target, Action<float> onRatioObtained)
		{
			Deformable deformable = target.GetComponentInParent<Deformable>();
			if (deformable == null)
			{
				Action<float> onRatioObtained2 = onRatioObtained;
				if (onRatioObtained2 == null)
				{
					return;
				}
				onRatioObtained2(100f);
				return;
			}
			else
			{
				Mesh a;
				bool flag;
				bool flag2;
				Renderer renderer = deformable.TryGetRenderer(out a, out flag, out flag2);
				Mesh b;
				if (a == null || !a.IsMeshEditable() || !deformable.TryGetMeshByRenderer(renderer, out b))
				{
					return;
				}
				float areOffset = (float)b.vertexCount;
				new Thread(delegate()
				{
					List<Vector3> aVerts = new List<Vector3>(a.vertexCount);
					a.GetVertices(aVerts);
					List<Vector3> bVerts = new List<Vector3>(b.vertexCount);
					b.GetVertices(bVerts);
					float areOffset;
					for (int i = 0; i < bVerts.Count; i++)
					{
						if ((aVerts[i] - bVerts[i]).sqrMagnitude >= 0.01f)
						{
							areOffset = areOffset;
							areOffset -= 1f;
						}
					}
					MainThread.Invoke(delegate
					{
						Action<float> onRatioObtained3 = onRatioObtained;
						if (onRatioObtained3 == null)
						{
							return;
						}
						onRatioObtained3(areOffset / (float)bVerts.Count * 100f);
					});
				})
				{
					IsBackground = true
				}.Start();
				return;
			}
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00007538 File Offset: 0x00005738
		private static void DeformTargetAtPoint(DeformRequest request, Transform origin, SkinnedMeshRenderer skm, Mesh mesh, Action<Mesh> deformed = null)
		{
			if (origin == null || mesh == null)
			{
				Debug.LogError("Deform Error: No origin or mesh!");
				return;
			}
			Thread vertexModifyThread = null;
			int hash = origin.GetHashCode();
			int index = vertexModifyThread.AddVertexModifyThread(hash);
			if (index == -1)
			{
				Debug.LogError("Deform Error: The pool is full!");
				return;
			}
			NativeArray<Vector3> modifymeshVertices = new NativeArray<Vector3>(mesh.vertices, 4);
			NativeArray<Vector3> originalMeshVertices;
			NativeArray<Vector3> originalMeshNormals;
			if (skm != null)
			{
				mesh = skm.BakeMesh();
				originalMeshVertices = new NativeArray<Vector3>(mesh.vertices, 4);
				originalMeshNormals = new NativeArray<Vector3>(mesh.normals, 4);
				skm.sharedMesh = mesh;
			}
			else
			{
				originalMeshVertices = new NativeArray<Vector3>(mesh.vertices, 4);
				originalMeshNormals = new NativeArray<Vector3>(mesh.normals, 4);
			}
			MeshAPI.ModifyVertexThreads[hash][index] = new Thread(delegate()
			{
				int threadCache_Hash = hash;
				int threadCache_Index = index;
				MeshAPI.ModifyVerticesThreaded(originalMeshVertices.Length, modifymeshVertices, originalMeshVertices, originalMeshNormals, request.worldPoint, origin.localToWorldMatrix, request.velocityOffset, request.radius, mesh, delegate(Mesh modified)
				{
					threadCache_Hash.RemoveVertexModifyThread(threadCache_Index);
					Action<Mesh> deformed2 = deformed;
					if (deformed2 == null)
					{
						return;
					}
					deformed2(modified);
				});
			})
			{
				Name = string.Format("Modify Vertex Data [{0}]", hash),
				IsBackground = true
			};
			hash.RunNextInModifyQueue();
		}

		// Token: 0x0600012F RID: 303 RVA: 0x000076B8 File Offset: 0x000058B8
		private static void ModifyVerticesThreaded(int length, NativeArray<Vector3> modifyMeshVertices, NativeArray<Vector3> originalMeshVertices, NativeArray<Vector3> originalMeshNormals, Vector3 point, Matrix4x4 localToWorldMatrix, Vector3 velocityOffset, float impactRadius, Mesh mesh, Action<Mesh> deformed = null)
		{
			int i = 0;
			while (i < length)
			{
				Vector3 toWorld = localToWorldMatrix.MultiplyPoint3x4(originalMeshVertices[i]);
				if ((point - toWorld).sqrMagnitude > impactRadius * impactRadius)
				{
					goto IL_71;
				}
				if (Vector3.Dot(modifyMeshVertices[i] - originalMeshVertices[i], originalMeshNormals[i]) <= 0f)
				{
					ref NativeArray<Vector3> ptr = ref modifyMeshVertices;
					int num = i;
					ptr[num] -= velocityOffset;
					goto IL_71;
				}
				IL_7D:
				i++;
				continue;
				IL_71:
				if (i % 50 == 0)
				{
					Thread.Yield();
					goto IL_7D;
				}
				goto IL_7D;
			}
			mesh.SetVertices<Vector3>(modifyMeshVertices);
			modifyMeshVertices.Dispose();
			originalMeshVertices.Dispose();
			mesh.RecalculateNormals(1);
			if (deformed != null)
			{
				deformed(mesh);
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00007778 File Offset: 0x00005978
		private static void RunNextInModifyQueue(this int hash)
		{
			if (!MeshAPI.ModifyVertexThreads.ContainsKey(hash) || MeshAPI.ModifyVertexThreads[hash][0].IsAlive)
			{
				return;
			}
			try
			{
				MeshAPI.ModifyVertexThreads[hash][0].Start();
			}
			catch
			{
				hash.RemoveVertexModifyThread(0);
				hash.RunNextInModifyQueue();
			}
		}

		// Token: 0x06000131 RID: 305 RVA: 0x000077E4 File Offset: 0x000059E4
		private static int AddVertexModifyThread(this Thread thread, int hash)
		{
			if (MeshAPI.GetModifyVertexThreadPoolCount() > ModOptions.maxThreads)
			{
				return -1;
			}
			if (!MeshAPI.ModifyVertexThreads.ContainsKey(hash))
			{
				MeshAPI.ModifyVertexThreads.Add(hash, new List<Thread>());
				MeshAPI.ModifyVertexThreads[hash].Add(thread);
				return 0;
			}
			if (MeshAPI.ModifyVertexThreads[hash].Count == 0)
			{
				MeshAPI.ModifyVertexThreads[hash].Add(thread);
				return 0;
			}
			if (MeshAPI.ModifyVertexThreads[hash].Count >= ModOptions.maxThreadsPerInstance)
			{
				return -1;
			}
			MeshAPI.ModifyVertexThreads[hash].Add(thread);
			return MeshAPI.ModifyVertexThreads[hash].Count - 1;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00007890 File Offset: 0x00005A90
		private static void RemoveVertexModifyThread(this int hash, int thread)
		{
			MeshAPI.ModifyVertexThreads[hash].RemoveAt(thread);
			if (MeshAPI.ModifyVertexThreads[hash].Count > 0)
			{
				hash.RunNextInModifyQueue();
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000078BC File Offset: 0x00005ABC
		private static int GetModifyVertexThreadPoolCount()
		{
			int result = 0;
			foreach (KeyValuePair<int, List<Thread>> key in MeshAPI.ModifyVertexThreads)
			{
				result += key.Value.Count;
			}
			return result;
		}

		// Token: 0x040000BA RID: 186
		private static Dictionary<int, List<Thread>> _modifyVertexThreads = new Dictionary<int, List<Thread>>();
	}
}

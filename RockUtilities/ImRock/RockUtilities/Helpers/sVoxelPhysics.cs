// VoxelPhysics
#define UNITY_ASSERTIONS
using BlockTypes;
using Pipliz;
using Pipliz.Helpers;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class sVoxelPhysics
{
    public struct RayHit
    {
        public struct VoxelHit
        {
            public float Distance;

            public Pipliz.Vector3Int VoxelPositionHit;

            public VoxelSide VoxelSideHit;

            public RotatedBounds BoundsHit;

            public Vector3 BoundsCenter;

            public ushort TypeHit;

            public Pipliz.Vector3Int PositionBuild
            {
                get
                {
                    switch (VoxelSideHit)
                    {
                        case VoxelSide.xMin:
                            return VoxelPositionHit.Add(-1, 0, 0);
                        case VoxelSide.xPlus:
                            return VoxelPositionHit.Add(1, 0, 0);
                        case VoxelSide.yPlus:
                            return VoxelPositionHit.Add(0, 1, 0);
                        case VoxelSide.yMin:
                            return VoxelPositionHit.Add(0, -1, 0);
                        case VoxelSide.zPlus:
                            return VoxelPositionHit.Add(0, 0, 1);
                        case VoxelSide.zMin:
                            return VoxelPositionHit.Add(0, 0, -1);
                        default:
                            return VoxelPositionHit;
                    }
                }
            }

            public PlayerClickedData.VoxelHit ToPlayerClick()
            {
                PlayerClickedData.VoxelHit hit = PlayerClickedData.PoolGet<PlayerClickedData.VoxelHit>();
                hit.BlockHit = VoxelPositionHit;
                hit.DistanceToHit = Distance;
                hit.SideHit = VoxelSideHit;
                hit.TypeHit = TypeHit;
                return hit;
            }
        }

        public VoxelHit voxelHit;

        public PlayerClickedData.EHitType hitType;

        public float GetDistance()
        {
            switch (hitType)
            {
                case PlayerClickedData.EHitType.Block:
                    return voxelHit.Distance;
                default:
                    throw new ArgumentException($"Unexpected hit type: {hitType}");
            }
        }

        private struct VoxelRay
        {
            public Pipliz.Vector3Int NextVoxel;

            private VoxelSide LastDirMin;

            private Vector3 tDelta;

            private Vector3 tMax;

            private Vector3 dirNormalized;

            private Pipliz.Vector3Int SourceVoxel;

            private Vector3 source;

            public Vector3 Origin => source;

            public Vector3 DirectionNormalized => dirNormalized;

            public VoxelSide LastSideHit
            {
                get
                {
                    switch (LastDirMin)
                    {
                        default:
                            return (!(dirNormalized.x >= 0f)) ? VoxelSide.xPlus : VoxelSide.xMin;
                        case VoxelSide.yMin:
                            return (dirNormalized.y >= 0f) ? VoxelSide.yMin : VoxelSide.yPlus;
                        case VoxelSide.zMin:
                            return (dirNormalized.z >= 0f) ? VoxelSide.zMin : VoxelSide.zPlus;
                    }
                }
            }

            public float Distance
            {
                get
                {
                    switch (LastDirMin)
                    {
                        default:
                            {
                                int sign = (dirNormalized.x >= 0f) ? 1 : (-1);
                                return ((float)NextVoxel.x - source.x - 0.5f * (float)sign) * (float)sign * tDelta.x;
                            }
                        case VoxelSide.yMin:
                            {
                                int sign = (dirNormalized.y >= 0f) ? 1 : (-1);
                                return ((float)NextVoxel.y - source.y - 0.5f * (float)sign) * (float)sign * tDelta.y;
                            }
                        case VoxelSide.zMin:
                            {
                                int sign = (dirNormalized.z >= 0f) ? 1 : (-1);
                                return ((float)NextVoxel.z - source.z - 0.5f * (float)sign) * (float)sign * tDelta.z;
                            }
                    }
                }
            }

            public VoxelRay(Vector3 source, Vector3 direction)
            {
                this.source = source;
                SourceVoxel = new Pipliz.Vector3Int(source);
                NextVoxel = SourceVoxel;
                dirNormalized = direction;
                tDelta.x = ((dirNormalized.x != 0f) ? Pipliz.Math.Abs(1f / dirNormalized.x) : 1E+07f);
                tDelta.y = ((dirNormalized.y != 0f) ? Pipliz.Math.Abs(1f / dirNormalized.y) : 1E+07f);
                tDelta.z = ((dirNormalized.z != 0f) ? Pipliz.Math.Abs(1f / dirNormalized.z) : 1E+07f);
                tMax.x = tMaxHelper(source.x, dirNormalized.x);
                tMax.y = tMaxHelper(source.y, dirNormalized.y);
                tMax.z = tMaxHelper(source.z, dirNormalized.z);
                LastDirMin = VoxelSide.None;
            }

            public bool Intersects(BoundsPip bounds, out float dist, out VoxelSide hitSide)
            {
                dist = 0f;
                hitSide = VoxelSide.None;
                float txmin;
                float tmin = txmin = (bounds.Min.x - source.x) / dirNormalized.x;
                float txmax;
                float tmax = txmax = (bounds.Max.x - source.x) / dirNormalized.x;
                if (tmin > tmax)
                {
                    Helper.Swap(ref tmin, ref tmax);
                }
                float tymin = (bounds.Min.y - source.y) / dirNormalized.y;
                float tymax = (bounds.Max.y - source.y) / dirNormalized.y;
                if (tymin > tymax)
                {
                    Helper.Swap(ref tymin, ref tymax);
                }
                if (tmin > tymax || tymin > tmax)
                {
                    return false;
                }
                if (tymin > tmin)
                {
                    tmin = tymin;
                }
                if (tymax < tmax)
                {
                    tmax = tymax;
                }
                float tzmin = (bounds.Min.z - source.z) / dirNormalized.z;
                float tzmax = (bounds.Max.z - source.z) / dirNormalized.z;
                if (tzmin > tzmax)
                {
                    Helper.Swap(ref tzmin, ref tzmax);
                }
                if (tmin > tzmax || tzmin > tmax)
                {
                    return false;
                }
                if (tzmin > tmin)
                {
                    tmin = tzmin;
                }
                if (tzmax < tmax)
                {
                }
                if (tmin < 0f)
                {
                    return false;
                }
                dist = tmin;
                if (tmin == txmin || tmin == txmax)
                {
                    hitSide = ((dirNormalized.x < 0f) ? VoxelSide.xPlus : VoxelSide.xMin);
                }
                else if (tmin == tymin || tmin == tymax)
                {
                    hitSide = ((dirNormalized.y < 0f) ? VoxelSide.yPlus : VoxelSide.yMin);
                }
                else
                {
                    hitSide = ((dirNormalized.z < 0f) ? VoxelSide.zPlus : VoxelSide.zMin);
                }
                return true;
            }

            public bool WalkNextVoxel(float maxDistance)
            {
                float min = Pipliz.Math.Min(tMax.x, tMax.y, tMax.z);
                if (tMax.x == min)
                {
                    int sign = (dirNormalized.x >= 0f) ? 1 : (-1);
                    if (((float)NextVoxel.x - source.x - 0.5f * (float)sign) * (float)sign * tDelta.x > maxDistance)
                    {
                        return false;
                    }
                    NextVoxel.x += sign;
                    tMax.x += tDelta.x;
                    LastDirMin = VoxelSide.xMin;
                }
                else if (tMax.y == min)
                {
                    int sign2 = (dirNormalized.y >= 0f) ? 1 : (-1);
                    if (((float)NextVoxel.y - source.y - 0.5f * (float)sign2) * (float)sign2 * tDelta.y > maxDistance)
                    {
                        return false;
                    }
                    NextVoxel.y += sign2;
                    tMax.y += tDelta.y;
                    LastDirMin = VoxelSide.yMin;
                }
                else
                {
                    int sign3 = (dirNormalized.z >= 0f) ? 1 : (-1);
                    if (((float)NextVoxel.z - source.z - 0.5f * (float)sign3) * (float)sign3 * tDelta.z > maxDistance)
                    {
                        return false;
                    }
                    NextVoxel.z += sign3;
                    tMax.z += tDelta.z;
                    LastDirMin = VoxelSide.zMin;
                }
                return true;
            }

            private static float tMaxHelper(float s, float ds)
            {
                return (((ds >= 0f) ? 0.5f : (-0.5f)) - s + (float)Mathf.RoundToInt(s)) / ds;
            }
        }

        public struct VoxelRayCastHitAligned
        {
            public Pipliz.Vector3Int voxelHit;

            public float distanceToHit;
        }

        [Flags]
        public enum RayCastType : byte
        {
            Invalid = 0x0,
            Voxels = 0x1,
            NPCs = 0x2,
            HitNonSolidAsSolid = 0x4,
            HitBoxesSelection = 0x20,
            HitControlledMeshes = 0x40,
            HitAll = 0x67
        }

        public static bool RayCast(Vector3 source, Vector3 direction, float maxDistance, RayCastType type, out RayHit returnableHit)
        {
            returnableHit = default(RayHit);
            if (direction == default(Vector3))
            {
                return false;
            }
            if ((type & RayCastType.Voxels) != 0)
            {
                ThreadManager.BeginSampleMainThread("VoxelRayCast");
                if (RayCastVoxel(source, direction, maxDistance, type, out returnableHit.voxelHit))
                {
                    returnableHit.hitType = PlayerClickedData.EHitType.Block;
                    maxDistance = returnableHit.voxelHit.Distance;
                }
                ThreadManager.EndSampleMainThread();
            }
            return returnableHit.hitType != PlayerClickedData.EHitType.Missed;
        }

        public static bool RayCastVoxel(Vector3 center, Vector3 direction, float maxDistance, RayCastType type, out RayHit.VoxelHit hit)
        {
            hit = default(RayHit.VoxelHit);
            VoxelRay ray = new VoxelRay(center, direction);
            hit.Distance = float.MaxValue;
            while (ray.Distance - 2f < Pipliz.Math.Min(hit.Distance, maxDistance) && !HasCollisionHelper(ref ray, ray.NextVoxel, type, ref hit) && ray.WalkNextVoxel(maxDistance))
            {
            }
            return hit.Distance < float.MaxValue;
        }

        private static bool HasCollisionHelper(ref VoxelRay centerRay, Pipliz.Vector3Int pos, RayCastType type, ref RayHit.VoxelHit hit)
        {
            ushort hitType = default(ushort);
            if (!World.TryGetTypeAt(pos, out hitType))
            {
                return true;
            }
            if (hitType == 0)
            {
                return false;
            }
            if (hitType == BuiltinBlocks.Indices.water)
            {
                return false;
            }
            if ((type & RayCastType.HitBoxesSelection) != 0)
            {
                ItemTypes.ItemType itemType = ItemTypes.GetType(hitType);
                if (itemType.BoxColliders != null && itemType.CollideSelection)
                {
                    Vector3 vector = pos.Vector;
                    List<BoundsPip> boxes = itemType.BoxColliders;
                    for (int i = 0; i < boxes.Count; i++)
                    {
                        BoundsPip bounds2 = boxes[i];
                        bounds2.Shift(pos.Vector);
                        if (centerRay.Intersects(bounds2, out float hitDist2, out VoxelSide hitSides2) && hitDist2 < hit.Distance)
                        {
                            hit.TypeHit = hitType;
                            hit.Distance = hitDist2;
                            hit.VoxelPositionHit = pos;
                            hit.VoxelSideHit = hitSides2;
                            hit.BoundsHit = new RotatedBounds(boxes[i], Quaternion.identity);
                            hit.BoundsCenter = pos.Vector;
                        }
                    }
                    return false;
                }
            }
            if ((type & RayCastType.HitNonSolidAsSolid) == 0 && !ItemTypes.Solids[hitType])
            {
                return false;
            }
            BoundsPip bounds = default(BoundsPip);
            bounds.SetCenterSize(pos.Vector, Vector3.one);
            if (centerRay.Intersects(bounds, out float hitDist, out VoxelSide hitSides) && hitDist < hit.Distance)
            {
                hit.TypeHit = hitType;
                hit.Distance = hitDist;
                hit.VoxelPositionHit = pos;
                hit.VoxelSideHit = hitSides;
                hit.BoundsHit = new RotatedBounds(Vector3.zero, Vector3.one, Quaternion.identity);
                hit.BoundsCenter = pos.Vector;
            }
            return false;
        }
    }
}
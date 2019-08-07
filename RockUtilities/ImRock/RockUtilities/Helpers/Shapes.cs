using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Pipliz;

using Math = System.Math;
using Vector3Int = Pipliz.Vector3Int;

namespace RockUtils.Shapes
{
    public class Shapes
    {
        public static double LengthSq(double x, double y, double z)
        {
            return (x * x) + (y * y) + (z * z);
        }
        public static double LengthSq(double x, double z)
        {
            return (x * x) + (z * z);
        }

        public static List<Vector3Int> MakeLine(Vector3Int pos1, Vector3Int pos2)
        {
            List<Vector3Int> affected = new List<Vector3Int>();
            bool notdrawn = true;

            int x1 = pos1.x;
            int y1 = pos1.x;
            int z1 = pos1.x;
            int x2 = pos2.x;
            int y2 = pos2.y;
            int z2 = pos2.z;
            int tipx = x1;
            int tipy = y1;
            int tipz = z1;
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int dz = Math.Abs(z2 - z1);

            if (dx + dy + dz == 0)
            {
                affected.Add(pos2 + new Vector3Int(tipx, tipy, tipz));
                notdrawn = false;
            }

            if (Math.Max(Math.Max(dx, dy), dz) == dx && notdrawn)
            {
                for (int domstep = 0; domstep <= dx; domstep++)
                {
                    tipx = x1 + domstep * (x2 - x1 > 0 ? 1 : -1);
                    tipy = (int)Math.Round(y1 + domstep * (((double)dy) / ((double)dx)) * (y2 - y1 > 0 ? 1 : -1));
                    tipz = (int)Math.Round(z1 + domstep * (((double)dz) / ((double)dx)) * (z2 - z1 > 0 ? 1 : -1));
                    affected.Add(new Vector3Int(tipx, tipy, tipz));
                }
                notdrawn = false;
            }

            if (Math.Max(Math.Max(dx, dy), dz) == dy && notdrawn)
            {
                for (int domstep = 0; domstep <= dx; domstep++)
                {
                    tipx = x1 + domstep * (x2 - x1 > 0 ? 1 : -1);
                    tipy = (int)Math.Round(y1 + domstep * ((double)dy) / ((double)dx) * (y2 - y1 > 0 ? 1 : -1));
                    tipz = (int)Math.Round(z1 + domstep * ((double)dz) / ((double)dx) * (z2 - z1 > 0 ? 1 : -1));
                    affected.Add(new Vector3Int(tipx, tipy, tipz));
                }
                notdrawn = false;
            }

            if (Math.Max(Math.Max(dx, dy), dz) == dz && notdrawn)
            {
                for (int domstep = 0; domstep <= dz; domstep++)
                {
                    tipz = z1 + domstep * (z2 - z1 > 0 ? 1 : -1);
                    tipy = (int)Math.Round(y1 + domstep * ((double)dy) / ((double)dz) * (y2 - y1 > 0 ? 1 : -1));
                    tipx = (int)Math.Round(x1 + domstep * ((double)dx) / ((double)dz) * (x2 - x1 > 0 ? 1 : -1));
                    affected.Add(pos2 + new Vector3Int(tipx, tipy, tipz));
                }
            }
            return affected;
        }
        public static List<Vector3Int> MakeCuboid(WorldEdit.AreaSelection area, int border = 1, bool hollow = false)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            int minX = Math.Min(area.posA.x, area.posB.x);
            int maxX = Math.Max(area.posA.x, area.posB.x);
            int minY = Math.Min(area.posA.y, area.posB.y);
            int maxY = Math.Max(area.posA.y, area.posB.y);
            int minZ = Math.Min(area.posA.z, area.posB.z);
            int maxZ = Math.Max(area.posA.z, area.posB.z);

            Vector3Int relative;

            for (int x = area.cornerB.x; x >= area.cornerA.x; x--)
                for (int y = area.cornerB.y; y >= area.cornerA.y; y--)
                    for (int z = area.cornerB.z; z >= area.cornerA.z; z--)
                    {
                        relative = new Vector3Int(x, y, z);
                        if (hollow)
                        {
                            if (!(relative.y > (maxY - border) || relative.y < (minY + border) || relative.x > (maxX - border) || x < (minX + border) || relative.z > (maxZ - border) || relative.z < (minZ + border)))
                                affected.Add(relative);
                        }
                        else
                        {
                            if (relative.y > (maxY - border) || relative.y < (minY + border) || relative.x > (maxX - border) || x < (minX + border) || relative.z > (maxZ - border) || relative.z < (minZ + border))
                                affected.Add(relative);
                        }
                    }

            return affected;
        }
        public static List<Vector3Int> MakeOutline(WorldEdit.AreaSelection area, int border = 1)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            int minX = Math.Min(area.posA.x, area.posB.x);
            int maxX = Math.Max(area.posA.x, area.posB.x);
            int minY = Math.Min(area.posA.y, area.posB.y);
            int maxY = Math.Max(area.posA.y, area.posB.y);
            int minZ = Math.Min(area.posA.z, area.posB.z);
            int maxZ = Math.Max(area.posA.z, area.posB.z);

            Vector3Int relative;
            List<bool> truth;

            for (int x = area.cornerB.x; x >= area.cornerA.x; x--)
                for (int y = area.cornerB.y; y >= area.cornerA.y; y--)
                    for (int z = area.cornerB.z; z >= area.cornerA.z; z--)
                    {
                        relative = new Vector3Int(x, y, z);
                        {
                            truth = new List<bool>
                            {
                                relative.y >= (maxY - border),
                                relative.y <= (minY + border),
                                relative.x >= (maxX - border),
                                relative.x <= (minX + border),
                                relative.z >= (maxZ - border),
                                relative.z <= (minZ + border)
                            };
                            Log.Write($"{truth.FindAll(i => i == true).ToList().Count}");
                            if (truth.Where(i => i == true).ToList().Count >= 2)
                                affected.Add(relative);
                        }
                    }

            return affected;
        }
        public static List<Vector3Int> MakeWalls(WorldEdit.AreaSelection area, int border = 1)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            int minX = Math.Min(area.posA.x, area.posB.x);
            int maxX = Math.Max(area.posA.x, area.posB.x);
            int minZ = Math.Min(area.posA.z, area.posB.z);
            int maxZ = Math.Max(area.posA.z, area.posB.z);

            Vector3Int relative;

            for (int x = area.cornerB.x; x >= area.cornerA.x; x--)
                for (int y = area.cornerB.y; y >= area.cornerA.y; y--)
                    for (int z = area.cornerB.z; z >= area.cornerA.z; z--)
                    {
                        relative = new Vector3Int(x, y, z);
                        if (relative.x > (maxX - border) || relative.x < (minX + border) || relative.z > (maxZ - border) || relative.z < (minZ + border))
                        {
                            affected.Add(relative);
                        }
                    }

            return affected;
        }
        public static List<Vector3Int> MakeFloors(WorldEdit.AreaSelection area, int border = 1)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            int minY = area.posA.y;
            int maxY = area.posB.y;

            Vector3Int relative;

            for (int x = area.cornerB.x; x >= area.cornerA.x; x--)
                for (int y = area.cornerB.y; y >= area.cornerA.y; y--)
                    for (int z = area.cornerB.z; z >= area.cornerA.z; z--)
                    {
                        relative = new Vector3Int(x, y, z);
                        if (relative.y > (maxY - border) || relative.y < (minY + border))
                        {
                            affected.Add(new Vector3Int(x, y, z) + area.posA);
                        }
                    }

            return affected;
        }
        public static List<Vector3Int> MakePropSphere(Vector3Int center, double radius, bool filled)
        {
            return MakeSphere(center, radius, radius, radius, filled);
        }
        public static List<Vector3Int> MakeSphere(Vector3Int pos, double radiusX, double radiusY, double radiusZ, bool filled)
        {

            List<Vector3Int> affected = new List<Vector3Int>();
            radiusX += 0.5;
            radiusY += 0.5;
            radiusZ += 0.5;

            double invRadiusX = 1 / radiusX;
            double invRadiusY = 1 / radiusY;
            double invRadiusZ = 1 / radiusZ;

            int ceilRadiusX = (int)Math.Ceiling(radiusX);
            int ceilRadiusY = (int)Math.Ceiling(radiusY);
            int ceilRadiusZ = (int)Math.Ceiling(radiusZ);

            double nextXn = 0;

            for (int x = 0; x <= ceilRadiusX; x++)
            {
                double xn = nextXn;
                nextXn = (x + 1) * invRadiusX;
                double nextYn = 0;

                for (int y = 0; y <= ceilRadiusY; y++)
                {
                    double yn = nextYn;
                    nextYn = (y + 1) * invRadiusY;
                    double nextZn = 0;

                    for (int z = 0; z <= ceilRadiusZ; z++)
                    {
                        double zn = nextZn;
                        nextZn = (z + 1) * invRadiusZ;

                        double distanceSq = LengthSq(xn, yn, zn);
                        if (distanceSq > 1)
                        {
                            if (z == 0)
                            {
                                if (y == 0)
                                {
                                    goto forX;
                                }
                                goto forY;
                            }
                            goto forZ;
                        }

                        if (!filled)
                        {
                            if (LengthSq(nextXn, yn, zn) <= 1 && LengthSq(xn, nextYn, zn) <= 1 && LengthSq(xn, yn, nextZn) <= 1)
                            {
                                continue;
                            }
                        }

                        affected.Add(new Vector3Int(x, y, z) + pos);
                        affected.Add(new Vector3Int(-x, y, z) + pos);
                        affected.Add(new Vector3Int(x, -y, z) + pos);
                        affected.Add(new Vector3Int(x, y, -z) + pos);
                        affected.Add(new Vector3Int(-x, -y, z) + pos);
                        affected.Add(new Vector3Int(x, -y, -z) + pos);
                        affected.Add(new Vector3Int(-x, y, -z) + pos);
                        affected.Add(new Vector3Int(-x, -y, -z) + pos);
                    }
                    forZ:
                        { }
                }
                forY:
                    { }
            }
            forX:
                { }

            return affected;
        }
        public static List<Vector3Int> MakeCylinder(Vector3Int pos, double radius, int height, bool filled)
        {
            return MakeCylinder(pos, radius, radius, height, filled);
        }
        public static List<Vector3Int> MakeCylinder(Vector3Int pos, double radiusX, double radiusZ, int height, bool filled)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            radiusX += 0.5;
            radiusZ += 0.5;

            if (height == 0)
            {
                return affected;
            }
            else if (height < 0)
            {
                height = -height;
                pos = pos - new Vector3Int(0, height, 0);
            }

            double invRadiusX = 1 / radiusX;
            double invRadiusZ = 1 / radiusZ;

            int ceilRadiusX = (int)Math.Ceiling(radiusX);
            int ceilRadiusZ = (int)Math.Ceiling(radiusZ);

            double nextXn = 0;
            for (int x = 0; x <= ceilRadiusX; x++)
            {
                double xn = nextXn;
                nextXn = (x + 1) * invRadiusX;
                double nextZn = 0;
                for (int z = 0; z <= ceilRadiusZ; z++)
                {
                    double zn = nextZn;
                    nextZn = (z + 1) * invRadiusZ;

                    double distanceSq = LengthSq(xn, zn);
                    if (distanceSq > 1)
                    {
                        if (z == 0)
                        {
                            goto forX;
                        }
                        goto forZ;
                    }

                    if (!filled)
                    {
                        if (LengthSq(nextXn, zn) <= 1 && LengthSq(xn, nextZn) <= 1)
                        {
                            continue;
                        }
                    }

                    for (int y = 0; y < height; y++)
                    {
                        affected.Add(new Vector3Int(x, y, z) + pos);
                        affected.Add(new Vector3Int(-x, y, z) + pos);
                        affected.Add(new Vector3Int(x, y, -z) + pos);
                        affected.Add(new Vector3Int(-x, y, -z) + pos);
                    }
                }
                forZ:
                    { }
            }
            forX:
                { }

            return affected;
        }
        public static List<Vector3Int> MakePyramid(Vector3Int pos, int size, bool filled)
        {
            List<Vector3Int> affected = new List<Vector3Int>();

            int height = size;

            for (int y = 0; y <= height; y++)
            {
                size--;
                for (int x = 0; x <= size; x++)
                {
                    for (int z = 0; z <= size; z++)
                    {
                        if ((filled && z <= size && x <= size) || z == size || x == size)
                        {
                            affected.Add(new Vector3Int(x, y, z) + pos);
                            affected.Add(new Vector3Int(-x, y, z) + pos);
                            affected.Add(new Vector3Int(x, y, -z) + pos);
                            affected.Add(new Vector3Int(-x, y, -z) + pos);
                        }
                    }
                }
            }

            return affected;
        }
    }
}

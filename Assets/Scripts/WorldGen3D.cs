using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGen3D : MonoBehaviour {
    public Octree octree;

    private void Start() {
        World3D.voxels.Clear();

        int size = (int)Mathf.Pow(2, 6);
        for (int x = 0; x <= size; x++) {
            for (int y = 0; y <= size; y++) {
                for (int z = 0; z <= size; z++) {
                    Vector3Int position = new Vector3Int(x, y, z);
                    World3D.voxels[position] = new Voxel(position);
                }
            }
        }

        octree = new Octree(new Vector3Int(0,0, 0), size);
        octree.Subdivide(6);

        Debug.Log(World3D.surface.Count);
    }

    private void OnDrawGizmos() {
        if (octree != null) {
            octree.DrawWire();
        }

        // foreach (Octree surface in World3D.surface) {
        //     Gizmos.DrawWireCube(surface.position + new Vector3(0.5f, 0.5f, 0.5f) * surface.size, Vector3.one * surface.size);
        // }
    }
}

public static class World3D {
    public static Dictionary<Vector3Int, Voxel> voxels = new Dictionary<Vector3Int, Voxel>();
    public static Dictionary<Vector3Int, Octree> chunks = new Dictionary<Vector3Int, Octree>();
    public static List<Octree> surface = new List<Octree>();
    public static Noise noise = new Noise();

    public static Vector3Int GetCorner(int index) {
        return new Vector3Int(
            (index & 4) >> 2,
            (index & 2) >> 1,
            index & 1
            );
    }
}

public class Octree {
    public Vector3Int position;
    public int size;

    public Voxel[] corners = new Voxel[8];
    public Octree[] octrees;

    public Octree(Vector3Int position, int size) {
        this.position = position;
        this.size = size;
        for (int i = 0; i < 8; i++) {
            corners[i] = World3D.voxels[position + World3D.GetCorner(i)];
        }
    }

    public void Subdivide(int depth = 1) {
        if ((size / 2f) % 1 == 0) {
            int halfSize = size / 2;
            octrees = new Octree[8];   
            depth--;
            for (int i = 0; i < 8; i++) {
                octrees[i] = new Octree(position + World3D.GetCorner(i) * halfSize, halfSize);

                if (octrees[i].HasSurface()) {
                    if (depth > 0) {
                        octrees[i].Subdivide(depth);
                    } else {
                        World3D.surface.Add(octrees[i]);
                    }
                }
            }
        } else {
            Debug.Log("Cannot subdivide further.");
        }
    }

    public bool HasSurface() {
        bool inTerrain = corners[0].value > 0.5f;
        bool hasSurface = false;
        for (int x = position.x; x <= position.x + size; x++) {
            for (int y = position.y; y <= position.y + size; y++) {
                for (int z = position.z; z <= position.z + size; z++) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (World3D.voxels.ContainsKey(pos)) {
                        if ((World3D.voxels[pos].value >= 0.5f) != inTerrain) {
                            hasSurface = true;
                        }
                    }   
                }
            }
        }
        return hasSurface;
    }

    public void DrawWire() {
        Gizmos.DrawWireCube(position + new Vector3(0.5f, 0.5f, 0.5f) * size, Vector3.one * size);
        if (octrees != null) {
            for (int i = 0; i < 8; i++) {
                octrees[i].DrawWire();
            }
        }
    }
}

public class Voxel {
    public float value;

    public Voxel(Vector3Int position) {
        value = World3D.noise.Evaluate((Vector3)position * 0.03f);
    }
}

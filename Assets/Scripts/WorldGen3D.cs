using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

public class WorldGen3D : MonoBehaviour {
    public Chunk chunk;
    public List<Vector3Int> chunksToLoad = new List<Vector3Int>();
    public Queue<Chunk> chunksToUnload = new Queue<Chunk>();

    [Header("Settings")]
    [Range(0, 4)]
    public int chunkSize = 4;
    [Range(0, 4)]
    public int lod = 0;
    [Range(0, 8)]
    public int renderDistance = 1;

    [Header("Debug")]
    public bool drawChunk;
    [Range(0, 4)]
    public int drawSubdivisions;
    public bool drawSurface;
    public bool drawVoxels;

    private int size;
    private Vector3Int chunkPos;

    private void Start() {
        World3D.voxels.Clear();
    }

    private void Update() {
        Vector3Int newChunkPos = Vector3Int.FloorToInt(Camera.main.transform.position / (1 << chunkSize));

        if (newChunkPos != chunkPos) {
            chunkPos = newChunkPos;

            for (int x = chunkPos.x - renderDistance; x <= chunkPos.x + renderDistance; x++) {
                for (int y = chunkPos.y - renderDistance; y <= chunkPos.y + renderDistance; y++) {
                    for (int z = chunkPos.z - renderDistance; z <= chunkPos.z + renderDistance; z++) {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        if (Vector3Int.Distance(pos, chunkPos) < renderDistance) {
                            if (!World3D.chunks.ContainsKey(pos) && !chunksToLoad.Contains(pos)) {
                                chunksToLoad.Add(pos);
                            }
                        }
                    }
                }
            }       
            chunksToLoad.Sort(SortByDistance);

            foreach(KeyValuePair<Vector3Int, Chunk> chunk in World3D.chunks) {
                if (Vector3Int.Distance(chunk.Key, chunkPos) > renderDistance) {
                    chunksToUnload.Enqueue(chunk.Value);
                }
            }
        }

        for (int i = 0; i < 10 && chunksToLoad.Count > 0; i++) {
            float distance = Vector3Int.Distance(chunksToLoad[0], chunkPos);
            if (distance < renderDistance) {
                new Chunk(chunksToLoad[0], chunkSize, (int)Math.Floor(distance * (chunkSize / (float)renderDistance)));
                chunksToLoad.RemoveAt(0);
            }
        }

        for (int i = 0; i < chunksToUnload.Count; i++) {
            World3D.chunks.Remove(chunksToUnload.Dequeue().chunkPosition);
        }
    }

    private int SortByDistance(Vector3Int v1, Vector3Int v2) {
        float a = Vector3Int.Distance(v1, chunkPos);
        float b = Vector3Int.Distance(v2, chunkPos);
        return a.CompareTo(b);
    }

    private void OnDrawGizmos() {
        if (drawChunk) {
            foreach(KeyValuePair<Vector3Int, Chunk> chunk in World3D.chunks) {
                chunk.Value.octree.DrawWire(drawSubdivisions);     
                if (drawSurface) {
                    foreach (Octree surface in chunk.Value.surface) {
                        Gizmos.DrawWireCube(surface.position + new Vector3(0.5f, 0.5f, 0.5f) * surface.size, Vector3.one * surface.size);
                    }
                }
            }
        }

        if (drawVoxels) {
            foreach(KeyValuePair<Vector3Int, Voxel> voxel in World3D.voxels) {
                Gizmos.color = Color.Lerp(Color.black, Color.white, voxel.Value.value);
                Gizmos.DrawCube(voxel.Key, Vector3.one * 0.2f);
            }
        }
    }

    private void OnValidate() {
        chunkSize = Mathf.Clamp(chunkSize, 1, 4);
        size = 1 << chunkSize;
        lod = Mathf.Clamp(lod, 0, chunkSize);
        drawSubdivisions = Mathf.Clamp(drawSubdivisions, 0, chunkSize - lod);
        if (World3D.voxels.Count > 10000 && drawVoxels) {
            drawVoxels = false;
            Debug.Log("Too many voxel to draw!");
        }
    }
}

public static class World3D {
    public static Dictionary<Vector3Int, Voxel> voxels = new Dictionary<Vector3Int, Voxel>();
    public static Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    public static Noise noise = new Noise();

    public static Vector3Int GetCorner(int index) {
        return new Vector3Int(
            (index & 4) >> 2,
            (index & 2) >> 1,
            index & 1
            );
    }
}

public class Chunk {
    public Vector3Int chunkPosition;
    public Vector3Int position;
    public int size;
    public int maxDepth;
    public int lod;
    public Octree octree;
    public List<Octree> surface = new List<Octree>();

    public Chunk(Vector3Int chunkPosition, int chunkSize, int lod) {
        World3D.chunks[chunkPosition] = this;
        this.chunkPosition = chunkPosition;
        size = 1 << chunkSize;
        this.position = chunkPosition * size;
        this.lod = lod;
        maxDepth = chunkSize - lod;

        GenerateVoxelData();

        octree = new Octree(position, size, this);
        octree.Subdivide(maxDepth);
    }

    public void GenerateVoxelData() {
        for (int x = position.x; x <= position.x + size; x += 1 << lod) {
            for (int y = position.y; y <= position.y + size; y += 1 << lod) {
                for (int z = position.z; z <= position.z + size; z += 1 << lod) {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!World3D.voxels.ContainsKey(pos)) {
                        new Voxel(pos);
                    }
                }
            }
        }
    }
}

public class Octree {
    public Vector3Int position;
    public int size;
    public Chunk chunk;

    public Voxel[] corners = new Voxel[8];
    public Octree[] octrees;

    public Octree(Vector3Int position, int size, Chunk chunk) {
        this.position = position;
        this.size = size;
        this.chunk = chunk;

        for (int i = 0; i < 8; i++) {
            Vector3Int pos = position + World3D.GetCorner(i) * size;
            if (!World3D.voxels.ContainsKey(pos)) {
                new Voxel(pos);
                Debug.Log("No voxel data!");
            }
            corners[i] = World3D.voxels[pos];
        }
    }

    public void Subdivide(int depth = 1) {
        if (depth > 0) {
            depth--;
            if ((size / 2f) % 1 == 0) {
                int halfSize = size / 2;
                octrees = new Octree[8];
                for (int i = 0; i < 8; i++) {
                    octrees[i] = new Octree(position + World3D.GetCorner(i) * halfSize, halfSize, chunk);
                    if (octrees[i].HasSurface()) {
                        octrees[i].Subdivide(depth);
                    }
                }
            } else {
                Debug.Log("Cannot subdivide further!");
            }
        } else {
            if (HasSurface()) {
                chunk.surface.Add(this);
            }
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

    public void DrawWire(int depth = 0) {
        Gizmos.DrawWireCube(position + new Vector3(0.5f, 0.5f, 0.5f) * size, Vector3.one * size);
        depth--;
        if (depth > 0  && octrees != null) {
            for (int i = 0; i < 8; i++) {
                octrees[i].DrawWire(depth);
            }
        }
    }
}

public class Voxel {
    public float value;

    public Voxel(Vector3Int position) {
        World3D.voxels[position] = this;
        value = World3D.noise.Evaluate((Vector3)position * 0.005f);
    }
}

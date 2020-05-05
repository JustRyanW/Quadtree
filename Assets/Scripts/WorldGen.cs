using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGen : MonoBehaviour {
    public Quadtree quad;

    private void Start() {
        World.pixels.Clear();

        int size = (int)Mathf.Pow(2, 5);
        for (int x = 0; x <= size; x++) {
            for (int y = 0; y <= size; y++) {
                Vector2Int position = new Vector2Int(x, y);
                World.pixels[position] = new Pixel(position);
            }
        }

        quad = new Quadtree(new Vector2Int(0,0), size);
        quad.Subdivide(5);

        Debug.Log(World.pixels.Count);
    }

    private void OnDrawGizmos() {
        foreach (KeyValuePair<Vector2Int, Pixel> pixel in World.pixels)
        {
            // Gizmos.color = Color.Lerp(Color.black, Color.white, pixel.Value.value);
            Gizmos.color = (pixel.Value.value >= 0.5f) ? Color.white : Color.black;
            Gizmos.DrawCube((Vector2)pixel.Key, Vector3.one * 1f);
        }

        if (quad != null) {
            Gizmos.color = Color.white;
            quad.DrawWire();
        }
    }
}

public static class World {
    public static Dictionary<Vector2Int, Pixel> pixels = new Dictionary<Vector2Int, Pixel>();

}

public class Pixel {
    public float value;

    public Pixel(Vector2Int position) {
        this.value = Mathf.PerlinNoise(position.x * 0.021f, position.y * 0.021f);
    }
}

public class Quadtree {
    public Vector2Int position;
    public int size;

    public Pixel[] corners = new Pixel[4];
    public Quadtree[] quadtrees;

    public Quadtree(Vector2Int position, int size) {
        this.position = position;
        this.size = size;

        for (int i = 0; i < 4; i++) {
            Vector2Int pos = position + GetCorner(i) * size;
            if (!World.pixels.ContainsKey(pos)) {
                World.pixels[pos] = new Pixel(pos);
            }
            corners[i] = World.pixels[pos];
        }
    }

    public void Subdivide(int depth = 1) {
        if ((size / 2f) % 1 == 0) {
            int halfSize = size / 2;
            quadtrees = new Quadtree[4];   
            depth--;
            for (int i = 0; i < 4; i++) {
                quadtrees[i] = new Quadtree(position + GetCorner(i) * halfSize, halfSize);

                if (depth > 0) {
                    if (quadtrees[i].HasSurface()) {
                        quadtrees[i].Subdivide(depth);
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
                Vector2Int pos = new Vector2Int(x, y);
                if (World.pixels.ContainsKey(pos)) {
                    if ((World.pixels[pos].value >= 0.5f) != inTerrain) {
                        hasSurface = true;
                    }
                }
            }
        }

        return hasSurface;
    }

    public static Vector2Int GetCorner(int index) {
        return new Vector2Int((index & 2) >> 1, index & 1);
    }

    public void DrawWire() {
        Gizmos.DrawWireCube(position + new Vector2(0.5f, 0.5f) * size, Vector2.one * size);
        if (quadtrees != null) {
            for (int i = 0; i < 4; i++) {
                quadtrees[i].DrawWire();
            }
        }
    }
}

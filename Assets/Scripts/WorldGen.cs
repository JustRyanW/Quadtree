using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGen : MonoBehaviour {
    Quadtree quad;

    private void Start() {
        World.pixels.Clear();
        quad = new Quadtree(new Vector2Int(0,0), (int)Mathf.Pow(2, 5));
        quad.Subdivide(5);

        Debug.Log(World.pixels.Count);
    }

    private void OnDrawGizmos() {
        if (quad != null) {
            quad.DrawWire();
        }

        foreach (KeyValuePair<Vector2Int, Pixel> pixel in World.pixels)
        {
            Gizmos.color = Color.Lerp(Color.black, Color.white, pixel.Value.value);
            Gizmos.DrawCube((Vector2)pixel.Key, Vector3.one * 0.4f);
        }
    }
}

public static class World {
    public static Dictionary<Vector2Int, Pixel> pixels = new Dictionary<Vector2Int, Pixel>();


}

public class Pixel {
    public float value;

    public Pixel(float value) {
        this.value = value;
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
                World.pixels[pos] = new Pixel(Mathf.PerlinNoise(pos.x * 0.05f, pos.y * 0.05f));
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
                    if (quadtrees[i].IsSurface()) {
                        quadtrees[i].Subdivide(depth);
                    }
                }
            }
        } else {
            Debug.Log("Cannot subdivide further.");
        }
    }

    public bool IsSurface() {
        bool inTerrain = corners[0].value > 0.5f;
        bool isSurface = false;
        for (int i = 1; i < 4; i++) {
            if ((corners[i].value > 0.5f) != inTerrain) {
               isSurface = true;
            }
        }
        return isSurface;
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

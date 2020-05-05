using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGen : MonoBehaviour {
    Quadtree quad;

    private void Start() {
        quad = new Quadtree(new Vector2Int(0,0), 8);
        quad.Subdivide();

        Debug.Log(World.pixels.Count);
    }

    private void OnDrawGizmos() {
        if (quad != null) {
            quad.DrawWire();
        }

        foreach (KeyValuePair<Vector2Int, Pixel> pixel in World.pixels)
        {
            Gizmos.color = Color.Lerp(Color.black, Color.white, pixel.Value.value);
            Gizmos.DrawCube((Vector2)pixel.Key, Vector3.one * 0.2f);
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
                World.pixels[pos] = new Pixel(Random.Range(0f, 1f));
            }
            corners[i] = World.pixels[pos];
        }
    }

    public void Subdivide() {
        if (size / 2 % 1 == 0) {
            int halfSize = size / 2;
            quadtrees = new Quadtree[4];
            for (int i = 0; i < 4; i++) {
                quadtrees[i] = new Quadtree(position + GetCorner(i) * halfSize, halfSize);
            }
        }
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

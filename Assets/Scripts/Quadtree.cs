using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class World {
    public static Dictionary<Vector2Int, Pixel> pixels = new Dictionary<Vector2Int, Pixel>();
}

public class Pixel {
    Vector2Int position;
    float value;
}

public class Quadtree {
    public Pixel[] corners;
}

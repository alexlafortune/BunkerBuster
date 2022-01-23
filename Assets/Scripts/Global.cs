using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Utils
{
    private static System.Random random = new System.Random();

    public static float ColorDistance(this Color color, Color other)
    {
        return Mathf.Abs(color.r - other.r) + Mathf.Abs(color.g - other.g) + Mathf.Abs(color.b - other.b);
    }

    public static float Distance(this Vector2 v, Vector2 other)
    {
        return (v - other).magnitude;
    }

    public static GameObject FindChildObject(GameObject parentObject, string objectName)
    {
        foreach (Transform t in parentObject.GetComponentsInChildren<Transform>())
            if (t.name == objectName)
                return t.gameObject;

        return null;
    }

    public static T FindChildComponent<T>(GameObject parentObject, string objectName)
    {
        foreach (Transform t in parentObject.GetComponentsInChildren<Transform>())
        {
            if (t.name == objectName)
            {
                T component = t.gameObject.GetComponent<T>();

                if (component != null)
                    return component;
            }
        }

        return default(T);
    }

    public static int Random(int maxValue)
    {
        return random.Next(maxValue);
    }

    public static int Random(int minValue, int maxValue)
    {
        return random.Next(minValue, maxValue);
    }

    public static float RandomFloat()
    {
        return (float)random.NextDouble();
    }

    public static Vector2Int Round(this Vector2 v)
    {
        return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }

    public static Vector2Int RoundDown(this Vector2 v)
    {
        return new Vector2Int((int)v.x - (v.x < 0 ? 1 : 0), (int)v.y - (v.y < 0 ? 1 : 0));
    }
}
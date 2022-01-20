using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Utils
{
    private static System.Random random = new System.Random();

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

    public static float DistanceTo(this Color color, Color other)
    {
        return Mathf.Abs(color.r - other.r) + Mathf.Abs(color.g - other.g) + Mathf.Abs(color.b - other.b);
    }
}
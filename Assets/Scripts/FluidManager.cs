using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FluidManager : MonoBehaviour
{
    private FluidField ff;
    private Texture2D texture;
    private float timer;

    private void Start()
    {
        Texture2D initTexture = Resources.Load<Texture2D>("sprites/ff_init_small");
        ff = new FluidField(initTexture);

        texture = new Texture2D(initTexture.width, initTexture.height);
        texture.filterMode = FilterMode.Point;

        GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        timer = 0;
    }

    private void Update()
    {
        if (timer > 0f)
        {
            ff.Step();
            ff.WriteTexture(texture);
            timer = 0;
        }

        timer += Time.deltaTime;
    }
}
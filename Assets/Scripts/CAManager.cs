using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CAManager : MonoBehaviour
{
    private CAField ff;
    private Texture2D texture;
    private float timer;
    public Text text;
    private int step;
    private int downsampling;

    private void Start()
    {
        CAType.InitTypes();

        //Texture2D initTexture = Resources.Load<Texture2D>("sprites/ff_init_small");
        //ff = new CAField(initTexture);

        downsampling = 5;

        ff = new CAField(TerrainGenerator.Generate(
            Screen.width / downsampling, Screen.height / downsampling,
            Screen.height / downsampling / 2, 100 / downsampling, new NoiseProfile(0.01f * downsampling, 1, 0, 0)));

        texture = new Texture2D(ff.Width, ff.Height);
        texture.filterMode = FilterMode.Point;

        GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        //GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        timer = 0;
        step = 0;
    }

    private void Update()
    {
        Rain();

        if (Input.GetMouseButton(0))
        {
            int brushSize = 2;
            Vector2Int v = MouseToCAField();

            for (int x = -brushSize; x <= brushSize; ++x)
                for (int y = -brushSize; y <= brushSize; ++y)
                    ff.RemoveMaterial(v.x + x, v.y + y);
        }

        ff.Step();
        ff.WriteTexture(texture);
        timer = 0;
        ++step;
        text.text = step.ToString();

        timer += Time.deltaTime;
    }

    private Vector2Int MouseToCAField()
    {
        Vector2 mousePos = Input.mousePosition;
        return (mousePos / downsampling).RoundDown();
    }

    private void Rain()
    {
        for (int x = 0; x < ff.Width; ++x)
            if (Utils.RandomFloat() < 0.02f)
                ff.AddMaterial(x, ff.Height - 1, CAType.Water, 0.2f);
    }
}
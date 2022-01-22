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

    private void Start()
    {
        Texture2D initTexture = Resources.Load<Texture2D>("sprites/ff_init_small");
        ff = new CAField(initTexture);

        texture = new Texture2D(initTexture.width, initTexture.height);
        texture.filterMode = FilterMode.Point;

        GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        timer = 0;
        step = 0;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.S))
        //if (true)
        if (timer > 0.1f)
        {
            ff.Step();
            ff.WriteTexture(texture);
            timer = 0;
            ++step;
            text.text = step.ToString();

            /*if (step > 10)
                UnityEditor.EditorApplication.isPlaying = false;*/
        }

        timer += Time.deltaTime;
    }
}
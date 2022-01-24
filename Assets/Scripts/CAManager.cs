using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CAManager : MonoBehaviour
{
    private CAField ff;
    private Texture2D texture;
    public Text text;
    private int step;
    private int downsampling;
    public ComputeShader computeShader;
    private float timer;

    private void Start()
    {
        CAType.InitTypes();
        downsampling = 5;

        ff = new CAField(TerrainGenerator.Generate(
            Screen.width / downsampling, Screen.height / downsampling,
            Screen.height / downsampling / 2, 100 / downsampling, new NoiseProfile(0.01f * downsampling, 1, 0, 0)));

        ff = new CAField(Resources.Load<Sprite>("sprites/ff_init_shader").texture);

        texture = new Texture2D(ff.Width, ff.Height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        step = 0;

        computeShader.SetInt("width", ff.Width);
        computeShader.SetInt("height", ff.Height);
    }

    private void Update()
    {
        if (timer > 0.2f)
        //if (Input.GetKeyDown(KeyCode.S))
        {
            StepShader();

            /*Rain();

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
            ++step;
            text.text = step.ToString();*/

            timer = 0;
        }

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
                ff.AddMaterial(x, ff.Height - 2, CAType.Water, 0.2f);
    }

    private void StepShader()
    {
        CAShaderNode[] nodes = ff.ToShaderNodeArray();
        int dataSize = sizeof(int) + sizeof(float) + sizeof(float);

        ComputeBuffer computeBuffer = new ComputeBuffer(nodes.Length, dataSize);
        ComputeBuffer resultBuffer = new ComputeBuffer(nodes.Length, dataSize);
        computeBuffer.SetData(nodes);
        computeShader.SetBuffer(0, "nodes", computeBuffer);
        computeShader.SetBuffer(0, "nodesResult", resultBuffer);
        computeShader.Dispatch(0, nodes.Length / 8, 1, 1);
        resultBuffer.GetData(nodes);

        ff.ReadShaderNodeArray(nodes);
        ff.WriteTexture(texture);
        computeBuffer.Dispose();
        resultBuffer.Dispose();
    }
}
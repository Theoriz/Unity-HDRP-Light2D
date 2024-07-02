using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Light2DManager : MonoBehaviour
{
    [Header("Input")]
    public RenderTexture inputTexture;

    [Header("Output")]
    public Material outputMaterial;
    public string propertyName = "_MainTex";

    [Header("Raytracing")]
    [Range(1, 1024)] public int samples = 64;
    public float maxBrightness = 2.6f;
    public Color ambientColor = Color.clear;

    private Shader uvShader;
    private Shader jfShader;
    private Shader sdfShader;

    private Material uvMaterial;
    private Material jfMaterial;
    private Material rayMaterial;

    private RenderTexture jfTextureA;
    private RenderTexture jfTextureB;
    private RenderTexture outputTexture;

    private bool _initialized = false;

    private void OnEnable()
    {
        if(!_initialized)
            Initialize();
    }

    private void Update()
    {
        if (!_initialized)
            Initialize();

        if (!inputTexture)
        {
            Debug.LogError("[Light2DManager] No input texture set for "+name+".");
            return;
        }

        if (rayMaterial == null || uvMaterial == null || jfMaterial == null)
        {
            Debug.LogError("[Light2DManager] Could not find internal shaders.");
            return;
        }

        if (!jfTextureA || !jfTextureB || jfTextureA.width != inputTexture.width || jfTextureA.height != inputTexture.height)
            InitTextures();

        Update2DLighting();
        UpdateOutput();
    }

    private void OnDisable()
    {
        DestroyTextures();
    }

    private void Initialize()
    {
        uvShader = Shader.Find("Hidden/Custom/UV Encoding");
        jfShader = Shader.Find("Hidden/Custom/Jump Flood");
        sdfShader = Shader.Find("Hidden/Custom/Raytracer");

        uvMaterial = new Material(uvShader);
        jfMaterial = new Material(jfShader);
        rayMaterial = new Material(sdfShader);

        InitTextures();

        _initialized = true;
    }

    void CleanUp()
    {
        InitTextures();
    }

    void InitTextures()
    {
        DestroyTextures();

        if (!inputTexture)
            return;

        jfTextureA = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
        jfTextureB = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);

        jfTextureA.filterMode = FilterMode.Point;
        jfTextureB.filterMode = FilterMode.Point;
    }

    void DestroyTextures()
    {
        if (jfTextureA)
        {
            jfTextureA.Release();
            jfTextureA = null;
        }

        if (jfTextureB)
        {
            jfTextureB.Release();
            jfTextureB = null;
        }

        if (outputTexture)
        {
            outputTexture.Release();
            outputTexture = null;
        }
    }

    void Update2DLighting()
    {
        int iterations = Mathf.CeilToInt(Mathf.Log(inputTexture.width * inputTexture.height));

        Vector2 stepSize = new Vector2(inputTexture.width, inputTexture.height);

        Graphics.Blit(inputTexture, jfTextureA, uvMaterial, 0, 0);

        jfMaterial.SetVector("_ScreenSize", new Vector2(inputTexture.width, inputTexture.height));

        for (int i = 0; i < iterations; i++)
        {
            stepSize /= 2;

            jfMaterial.SetVector("_StepSize", stepSize);
            if (i % 2 == 0)
            {
                Graphics.Blit(jfTextureA, jfTextureB, jfMaterial, 0, 0);
            }
            else
            {
                Graphics.Blit(jfTextureB, jfTextureA, jfMaterial, 0, 0);
            }
        }

        rayMaterial.SetVector("_ScreenSize", new Vector2(inputTexture.width, inputTexture.height));
        rayMaterial.SetTexture("_ColorTex", inputTexture);
        rayMaterial.SetFloat("samples", samples);
        rayMaterial.SetFloat("sdfBorder", 0);
        rayMaterial.SetFloat("maxBrightness", maxBrightness);
        rayMaterial.SetColor("ambient", ambientColor);

        if (iterations % 2 != 0)
        {
            Graphics.Blit(jfTextureA, outputTexture, rayMaterial, 0, 0);

        }
        else
        {
            Graphics.Blit(jfTextureB, outputTexture, rayMaterial, 0, 0);
        }
    }

    void UpdateOutput()
    {
        if (outputMaterial)
        {
            outputMaterial.SetTexture(propertyName, outputTexture);
        }
    }
}

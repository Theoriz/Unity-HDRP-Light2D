using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Light2DPP")]
public sealed class Light2DPP : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);
    public FloatParameter sdfBorder = new FloatParameter(0f);

    public IntParameter samples = new IntParameter(128);
    public FloatParameter maxBrightness = new FloatParameter(2.6f);

    public ColorParameter ambientColor = new ColorParameter(new Vector4(0,0,0,0));

#if UNITY_EDITOR
    public BoolParameter raycast = new BoolParameter(true);
#endif

    private Shader uvShader;
    private Shader jfShader;
    private Shader sdfShader;

    private Material uvMaterial;
    private Material jfMaterial;
    private Material rayMaterial;

    private RenderTexture colorTexture;
    private RenderTexture jfTextureA;
    private RenderTexture jfTextureB;

    public bool IsActive() => rayMaterial != null && uvMaterial != null && jfMaterial != null && enable.value;
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;
    public override void Setup()
    {
        uvShader = Shader.Find("Hidden/Custom/UV Encoding");
        jfShader = Shader.Find("Hidden/Custom/Jump Flood");
        sdfShader = Shader.Find("Hidden/Custom/Raytracer");

        uvMaterial = new Material(uvShader);
        jfMaterial = new Material(jfShader);
        rayMaterial = new Material(sdfShader);

        InitTextures();

    }
    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (rayMaterial == null || uvMaterial == null || jfMaterial == null)
            return;
        else
        {
            //Check texture size
            if (colorTexture == null || colorTexture.width != Screen.width || colorTexture.height != Screen.height)
                InitTextures();

            int iterations = Mathf.CeilToInt(Mathf.Log(Screen.width * Screen.height));

            Vector2 stepSize = new Vector2(Screen.width, Screen.height);

            Graphics.Blit(source, colorTexture, 0, 0);
            Graphics.Blit(colorTexture, jfTextureA, uvMaterial, 0, 0);

            jfMaterial.SetVector("_ScreenSize", new Vector2(Screen.width, Screen.height));

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

            rayMaterial.SetVector("_ScreenSize", new Vector2(Screen.width, Screen.height));
            rayMaterial.SetTexture("_ColorTex", colorTexture);
            rayMaterial.SetFloat("samples", samples.value);
            rayMaterial.SetFloat("sdfBorder", sdfBorder.value);
            rayMaterial.SetFloat("maxBrightness", maxBrightness.value);
            rayMaterial.SetColor("ambient", ambientColor.value);
            
#if UNITY_EDITOR
            if (!raycast.value)
            {
                if (iterations % 2 != 0)
                {
                    Graphics.Blit(jfTextureA, destination, 0, 0);

                }
                else
                {
                    Graphics.Blit(jfTextureB, destination, 0, 0);

                }
                return;
            }
#endif
            if (iterations % 2 != 0)
            {
                Graphics.Blit(jfTextureA, destination, rayMaterial, 0, 0);

            }
            else
            {
                Graphics.Blit(jfTextureB, destination, rayMaterial, 0, 0);
            }
        }
    }
    private void InitTextures()
    {
        DestroyTextures();

        colorTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        jfTextureA = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        jfTextureB = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);

        colorTexture.filterMode = FilterMode.Point;
        jfTextureA.filterMode = FilterMode.Point;
        jfTextureB.filterMode = FilterMode.Point;

        //Debug.Log("Creating textures of size "+Screen.width+"x"+Screen.height);
    }
    public override void Cleanup()
    {
        CoreUtils.Destroy(rayMaterial);
        CoreUtils.Destroy(uvMaterial);
        CoreUtils.Destroy(jfMaterial);

        DestroyTextures();
    }
    private void DestroyTextures()
    {
        if (colorTexture)
        {
            colorTexture.Release();
            //CoreUtils.Destroy(colorTexture);
            colorTexture = null;
        }

        if (jfTextureA)
        {
            jfTextureA.Release();
            //CoreUtils.Destroy(jfTextureA);
            jfTextureA = null;
        }

        if (jfTextureB)
        {
            jfTextureB.Release();
            //CoreUtils.Destroy(jfTextureB);
            jfTextureB = null;
        }
    }
}
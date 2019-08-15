using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class TestSrp:RenderPipelineAsset
{
    public Color ClearColor = Color.clear;

    [MenuItem("Tool/CreatNewAsset")]
    public static void CreatSRPAsset()
    {
        TestSrp testSrp = ScriptableObject.CreateInstance<TestSrp>();

        AssetDatabase.CreateAsset(testSrp, Application.absoluteURL + "Assets/SRPAsset/NewSRPAsset.asset");
    }

    protected override IRenderPipeline InternalCreatePipeline()
    {
        MyCustomSRPAsset asset = new MyCustomSRPAsset();
        // set properity
        asset.SetClearColor(ClearColor);
        //

        return asset;
    }



}

public class MyCustomSRPAsset:RenderPipeline
{
    private Color clearColor;


    private CommandBuffer commandBuffer = new CommandBuffer() { name = "render camera"};


    public void SetClearColor(Color _c)
    {
        clearColor = _c;
    }

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);
        ScriptableCullingParameters cullingParams;
        CullResults cull = new CullResults();


        foreach (var item in cameras)
        {

            renderContext.SetupCameraProperties(item);

            // Culling
            if (!CullResults.GetCullingParameters(item, out cullingParams))
                continue;


            // use cull parameters to cull unvisible object, return what is visible
            CullResults.Cull(ref cullingParams, renderContext,ref cull);


            CameraClearFlags clearFlags = item.clearFlags;
            //clear z stencil
            commandBuffer.ClearRenderTarget(
                (clearFlags & CameraClearFlags.Depth) != 0,
                (clearFlags & CameraClearFlags.Color) != 0,
                clearColor
                );

            renderContext.ExecuteCommandBuffer(commandBuffer);

            commandBuffer.Dispose();

            //use default pass name,which is include in build-in shader
            var settings = new DrawRendererSettings(
                                item, new ShaderPassName("BaseShader")
                                );

            // Draw opaque objects using BasicPass shader pass
            // var settings = new DrawRendererSettings(item, new ShaderPassName("BasicPass"));
            //sort flag definition a sort regulation of render order
            settings.sorting.flags = SortFlags.CommonOpaque;

            //draw opaque first
            var filterSettings = new FilterRenderersSettings(true) { renderQueueRange = RenderQueueRange.opaque };

            //Why FilterRenderersSettings and not FilterRendererSettings?
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

            renderContext.DrawSkybox(item);

            //after opaque objects are drawed, command line start darw transparent objects
            //the render order of visible objets is sorted by unity render command.
            //On the surface, it seems that the order is sorted by distance between camera and objects,
            //Is a black box,check the sort model in the source code 
            filterSettings = new FilterRenderersSettings(true){ renderQueueRange = RenderQueueRange.transparent };
            //filterSettings.renderQueueRange = RenderQueueRange.transparent;

            settings.sorting.flags = SortFlags.CommonTransparent;

            //Why FilterRenderersSettings and not FilterRendererSettings?
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);


            renderContext.Submit();

        }
    }
}

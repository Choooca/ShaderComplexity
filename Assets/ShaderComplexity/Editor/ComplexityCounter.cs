using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.ShaderData;

public class ComplexityCounter
{
    static readonly string[] COLOR_LIGHT_MODE =
    {
        "UniversalForward",
        "UniversalForwardOnly",
        "SRPDefaultUnlit",
        "UniversalGBuffer"
    };
    private List<Material> _cached_materials = new List<Material>();

    static ShaderTagId LIGHT_MODE_SHADER_KEY = new ShaderTagId("LightMode");

    [MenuItem("Tools/Shader Complexity/Calculate In Scene")]
    static void CalculateFromMenu()
    {
        new ComplexityCounter().CalculateComplexityInScene();
    }

    void CalculateComplexityInScene()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Dictionary<Renderer, int> costPerRenderer = new Dictionary<Renderer, int>();

        foreach(var renderer in renderers)
        {
            if (renderer == null)
                return;

            renderer.GetSharedMaterials(_cached_materials);
            int material_count = _cached_materials.Count;
            if (material_count < 1)
                continue;

            int cost = 0;
            for(int i = 0; i < material_count; i++) {
                cost += GetCost(_cached_materials[i]);
            }

            costPerRenderer.Add(renderer, cost);
        }
    }

    private int GetCost(Material material)
    {
        if(material == null || material.shader == null) return 0;

        var shader = material.shader;
        var shader_data = ShaderUtil.GetShaderData(shader);
        var sub_shader = shader_data.ActiveSubshader;

        int lighting_pass_index = -1;
        for(int i = 0; i < sub_shader.PassCount; ++i)
        {
            Pass pass = sub_shader.GetPass(i);
            var lightMode = pass.FindTagValue(LIGHT_MODE_SHADER_KEY).name;
            if(COLOR_LIGHT_MODE.Any(s => s.Equals(lightMode)))
            {
                lighting_pass_index = i;
            }
        }

        if(lighting_pass_index  == -1)
            return 0;

        Pass color_pass = sub_shader.GetPass(lighting_pass_index);
        var keywords = material.shaderKeywords;

        int cost = 0;
        cost += GetShaderTypeCost(color_pass, ShaderType.Fragment, keywords);
        cost += GetShaderTypeCost(color_pass, ShaderType.Vertex, keywords);


        return cost;
    }

    private int GetShaderTypeCost(Pass pass, ShaderType type, string[] keywords)
    {
        if (!pass.HasShaderStage(ShaderType.Fragment))
            return 0;

        var compile = pass.CompileVariant(type, keywords, UnityEditor.Rendering.ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);

        if (!compile.Success || compile.ShaderData.Length == 0)
            return 0;

        return CountInstructionToken(compile.ShaderData);
    }

    private int CountInstructionToken(byte[] shader_data)
    {
        if(!TryGetShaderContainer(shader_data, out uint shader_container_pos))
            throw new Exception("Failed to find DXBC token");

        byte[] dxbc_data = ExtractDXBCContainer(shader_data, (int)shader_container_pos);

        if (!TryGetShaderCodeChunk(dxbc_data, out byte[] shdr_data))
            return 0;

        return 0;
    }

    private bool TryGetShaderCodeChunk(byte[] dxbc_data, out byte[] shdr_data)
    {
        shdr_data = null;

        uint chunk_count = BitConverter.ToUInt32(dxbc_data, 28);
        List<string> chunk_names = new List<string>();
        for(int i = 0; i < chunk_count; ++i)
        {
            uint chunk_offset = BitConverter.ToUInt32(dxbc_data, 32 + i * 4);
            uint chunk_id = BitConverter.ToUInt32(dxbc_data, (int)chunk_offset);
            chunk_names.Add(chunk_id.FourCCToString());

            if (chunk_id == "SHEX".ToFourCC() || chunk_id == "SHDR".ToFourCC())
            {
                shdr_data = ExtractChunkData(dxbc_data, (int)chunk_offset);
                return true;
            }
        }

        Debug.LogWarning($"SHEX/SHDR chunk not found. Chunks present: {string.Join(", ", chunk_names)}");
        return false;
    }

    private bool TryGetShaderContainer(byte[] shader_data, out uint shader_container_pos)
    {
        shader_container_pos = 0;

        for (int i = 0; i <= shader_data.Length - 4; i++)
        {
            uint four_cc = BitConverter.ToUInt32(shader_data, i);
            if (four_cc == "DXBC".ToFourCC())
            {
                shader_container_pos = (uint)i;
                return true;
            }
        }

        return false;
    }

    private byte[] ExtractChunkData(byte[] dxbc_data, int chunk_offset)
    {
        // Header de chunk : fourcc (4) + taille des données (4), suivi des données
        const int CHUNK_HEADER_SIZE = 8;
        int data_pos = chunk_offset + CHUNK_HEADER_SIZE;
        if (data_pos > dxbc_data.Length)
            throw new Exception("Chunk header is truncated");

        uint chunk_size = BitConverter.ToUInt32(dxbc_data, chunk_offset + 4);
        if (chunk_size > dxbc_data.Length - data_pos)
            throw new Exception($"Invalid chunk size {chunk_size} (available {dxbc_data.Length - data_pos})");

        byte[] chunk_data = new byte[chunk_size];
        Buffer.BlockCopy(dxbc_data, data_pos, chunk_data, 0, (int)chunk_size);
        return chunk_data;
    }

    private byte[] ExtractDXBCContainer(byte[] shader_data, int container_pos)
    {
        const int HEADER_SIZE = 32;
        int available = shader_data.Length - container_pos;
        if (available < HEADER_SIZE)
            throw new Exception("DXBC container is truncated");

        uint total_size = BitConverter.ToUInt32(shader_data, container_pos + 24);
        if (total_size < HEADER_SIZE || total_size > available)
            throw new Exception($"Invalid DXBC total size {total_size} (available {available})");

        byte[] dxbc_data = new byte[total_size];
        Buffer.BlockCopy(shader_data, container_pos, dxbc_data, 0, (int)total_size);
        return dxbc_data;
    }
}

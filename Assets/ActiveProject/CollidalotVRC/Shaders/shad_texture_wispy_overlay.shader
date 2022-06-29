// Shader created with Shader Forge v1.37 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.37;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:3,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:True,hqlp:False,rprd:True,enco:False,rmgx:True,imps:False,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:1,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:True,fnfb:True,fsmp:False;n:type:ShaderForge.SFN_Final,id:2865,x:33432,y:33549,varname:node_2865,prsc:2|emission-869-OUT,alpha-1117-OUT,voffset-9842-OUT;n:type:ShaderForge.SFN_Tex2d,id:6246,x:32043,y:33574,ptovrint:False,ptlb:noise_map,ptin:_noise_map,varname:node_6246,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:b6d3749570cd86e47880aba865259820,ntxv:0,isnm:False|UVIN-9522-UVOUT;n:type:ShaderForge.SFN_Tex2d,id:5510,x:32043,y:33378,ptovrint:False,ptlb:tex,ptin:_tex,varname:node_5510,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:9b05d96c81bb79a45aa4276b6e07db9f,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Panner,id:9522,x:31863,y:33574,varname:node_9522,prsc:2,spu:1,spv:1|UVIN-1618-OUT,DIST-6534-OUT;n:type:ShaderForge.SFN_Time,id:3581,x:31430,y:33531,varname:node_3581,prsc:2;n:type:ShaderForge.SFN_Multiply,id:6534,x:31661,y:33584,varname:node_6534,prsc:2|A-3581-TSL,B-8598-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8598,x:31430,y:33678,ptovrint:False,ptlb:wisp_speed,ptin:_wisp_speed,varname:node_8598,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ScreenPos,id:5661,x:31430,y:33283,varname:node_5661,prsc:2,sctp:0;n:type:ShaderForge.SFN_Multiply,id:7736,x:32406,y:33345,varname:node_7736,prsc:2|A-5510-A,B-6246-R;n:type:ShaderForge.SFN_Multiply,id:1062,x:32416,y:33609,varname:node_1062,prsc:2|A-6246-RGB,B-3306-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3306,x:32416,y:33768,ptovrint:False,ptlb:emissive_amt,ptin:_emissive_amt,varname:node_3306,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:1618,x:31661,y:33387,varname:node_1618,prsc:2|A-5661-UVOUT,B-9927-OUT;n:type:ShaderForge.SFN_ValueProperty,id:9927,x:31430,y:33456,ptovrint:False,ptlb:noise_map_size,ptin:_noise_map_size,varname:node_9927,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Tex2d,id:7771,x:32079,y:34044,ptovrint:False,ptlb:vertex_offest_map,ptin:_vertex_offest_map,varname:node_7771,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:c9ddd2876f4c3c14a987516a06bf0324,ntxv:3,isnm:True|UVIN-8038-UVOUT;n:type:ShaderForge.SFN_Panner,id:8038,x:31814,y:34101,varname:node_8038,prsc:2,spu:1,spv:1|UVIN-7927-OUT,DIST-2908-OUT;n:type:ShaderForge.SFN_Time,id:7886,x:31363,y:34142,varname:node_7886,prsc:2;n:type:ShaderForge.SFN_Multiply,id:2908,x:31594,y:34195,varname:node_2908,prsc:2|A-7886-TSL,B-5162-OUT;n:type:ShaderForge.SFN_Multiply,id:7927,x:31594,y:33998,varname:node_7927,prsc:2|A-1328-UVOUT,B-3138-OUT;n:type:ShaderForge.SFN_TexCoord,id:1328,x:31354,y:33859,varname:node_1328,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_ValueProperty,id:5162,x:31249,y:34299,ptovrint:False,ptlb:vertex_speed,ptin:_vertex_speed,varname:node_5162,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ValueProperty,id:3138,x:31354,y:34032,ptovrint:False,ptlb:vertex_map_size,ptin:_vertex_map_size,varname:node_3138,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0.5;n:type:ShaderForge.SFN_ValueProperty,id:1167,x:32079,y:34423,ptovrint:False,ptlb:offest_amt,ptin:_offest_amt,varname:node_1167,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:2;n:type:ShaderForge.SFN_Multiply,id:4722,x:32663,y:34215,varname:node_4722,prsc:2|A-7771-RGB,B-1167-OUT;n:type:ShaderForge.SFN_Multiply,id:9842,x:33128,y:34096,varname:node_9842,prsc:2|A-1328-UVOUT,B-4722-OUT;n:type:ShaderForge.SFN_SwitchProperty,id:7454,x:32963,y:33403,ptovrint:False,ptlb:texture_as_color,ptin:_texture_as_color,varname:node_7454,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:True|A-1062-OUT,B-5510-RGB;n:type:ShaderForge.SFN_Clamp01,id:869,x:33143,y:33403,varname:node_869,prsc:2|IN-7454-OUT;n:type:ShaderForge.SFN_Multiply,id:1117,x:32948,y:33703,varname:node_1117,prsc:2|A-7736-OUT,B-5863-OUT;n:type:ShaderForge.SFN_Slider,id:5863,x:32869,y:33866,ptovrint:False,ptlb:opacity_amt,ptin:_opacity_amt,varname:node_5863,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;proporder:5510-7454-5863-9927-6246-8598-3306-7771-5162-3138-1167;pass:END;sub:END;*/

Shader "Collidalot/shad_texture_wispy_overlay" {
    Properties {
        _tex ("tex", 2D) = "white" {}
        [MaterialToggle] _texture_as_color ("texture_as_color", Float ) = 0
        _opacity_amt ("opacity_amt", Range(0, 1)) = 1
        _noise_map_size ("noise_map_size", Float ) = 1
        _noise_map ("noise_map", 2D) = "white" {}
        _wisp_speed ("wisp_speed", Float ) = 1
        _emissive_amt ("emissive_amt", Float ) = 1
        _vertex_offest_map ("vertex_offest_map", 2D) = "bump" {}
        _vertex_speed ("vertex_speed", Float ) = 1
        _vertex_map_size ("vertex_map_size", Float ) = 0.5
        _offest_amt ("offest_amt", Float ) = 2
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent+1"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _noise_map; uniform float4 _noise_map_ST;
            uniform sampler2D _tex; uniform float4 _tex_ST;
            uniform float _wisp_speed;
            uniform float _emissive_amt;
            uniform float _noise_map_size;
            uniform sampler2D _vertex_offest_map; uniform float4 _vertex_offest_map_ST;
            uniform float _vertex_speed;
            uniform float _vertex_map_size;
            uniform float _offest_amt;
            uniform fixed _texture_as_color;
            uniform float _opacity_amt;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float4 node_7886 = _Time + _TimeEditor;
                float2 node_8038 = ((o.uv0*_vertex_map_size)+(node_7886.r*_vertex_speed)*float2(1,1));
                float3 _vertex_offest_map_var = UnpackNormal(tex2Dlod(_vertex_offest_map,float4(TRANSFORM_TEX(node_8038, _vertex_offest_map),0.0,0)));
                v.vertex.xyz += (float3(o.uv0,0.0)*(_vertex_offest_map_var.rgb*_offest_amt));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
////// Lighting:
////// Emissive:
                float4 node_3581 = _Time + _TimeEditor;
                float2 node_9522 = ((i.screenPos.rg*_noise_map_size)+(node_3581.r*_wisp_speed)*float2(1,1));
                float4 _noise_map_var = tex2D(_noise_map,TRANSFORM_TEX(node_9522, _noise_map));
                float4 _tex_var = tex2D(_tex,TRANSFORM_TEX(i.uv0, _tex));
                float3 emissive = saturate(lerp( (_noise_map_var.rgb*_emissive_amt), _tex_var.rgb, _texture_as_color ));
                float3 finalColor = emissive;
                return fixed4(finalColor,((_tex_var.a*_noise_map_var.r)*_opacity_amt));
            }
            ENDCG
        }
    }
    CustomEditor "ShaderForgeMaterialInspector"
}

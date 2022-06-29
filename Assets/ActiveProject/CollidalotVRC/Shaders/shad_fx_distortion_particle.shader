// Shader created with Shader Forge v1.37 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.37;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:1,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:7,dpts:5,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:False,igpj:False,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:True,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:True,fnfb:True,fsmp:False;n:type:ShaderForge.SFN_Final,id:4795,x:33366,y:32570,varname:node_4795,prsc:2|alpha-1247-OUT,clip-1248-OUT,refract-3477-OUT;n:type:ShaderForge.SFN_Tex2d,id:6074,x:32464,y:32781,ptovrint:False,ptlb:distort_shape,ptin:_distort_shape,varname:_MainTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:a9d81228b5216db48809f28a4fa740f2,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:9950,x:32538,y:32179,ptovrint:False,ptlb:norm,ptin:_norm,varname:node_9950,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:c9ddd2876f4c3c14a987516a06bf0324,ntxv:3,isnm:True|UVIN-6602-UVOUT;n:type:ShaderForge.SFN_Multiply,id:1041,x:32986,y:32179,varname:node_1041,prsc:2|A-6431-OUT,B-9420-OUT;n:type:ShaderForge.SFN_ValueProperty,id:9420,x:32986,y:32326,ptovrint:False,ptlb:refrac_intesnity,ptin:_refrac_intesnity,varname:node_9420,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ComponentMask,id:6431,x:32754,y:32179,varname:node_6431,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-9950-RGB;n:type:ShaderForge.SFN_Multiply,id:1492,x:32807,y:32777,varname:node_1492,prsc:2|A-1041-OUT,B-6074-A;n:type:ShaderForge.SFN_ComponentMask,id:1247,x:33018,y:32777,varname:node_1247,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-1492-OUT;n:type:ShaderForge.SFN_Multiply,id:390,x:32828,y:32582,varname:node_390,prsc:2|A-1041-OUT,B-6074-A;n:type:ShaderForge.SFN_Clamp01,id:3477,x:33018,y:32582,varname:node_3477,prsc:2|IN-390-OUT;n:type:ShaderForge.SFN_TexCoord,id:5154,x:32278,y:33007,varname:node_5154,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Panner,id:6602,x:32333,y:32179,varname:node_6602,prsc:2,spu:1,spv:1|UVIN-8725-UVOUT,DIST-1844-OUT;n:type:ShaderForge.SFN_Time,id:785,x:31885,y:32179,varname:node_785,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:4277,x:31885,y:32331,ptovrint:False,ptlb:time_speed,ptin:_time_speed,varname:node_4277,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:1844,x:32111,y:32252,varname:node_1844,prsc:2|A-785-TSL,B-4277-OUT;n:type:ShaderForge.SFN_ScreenPos,id:8725,x:32110,y:32545,varname:node_8725,prsc:2,sctp:0;n:type:ShaderForge.SFN_RemapRange,id:4349,x:32460,y:33007,varname:node_4349,prsc:2,frmn:0,frmx:1,tomn:-1,tomx:1|IN-5154-UVOUT;n:type:ShaderForge.SFN_Floor,id:4791,x:32820,y:33007,varname:node_4791,prsc:2|IN-3035-OUT;n:type:ShaderForge.SFN_Length,id:3035,x:32637,y:33007,varname:node_3035,prsc:2|IN-4349-OUT;n:type:ShaderForge.SFN_OneMinus,id:1248,x:33000,y:33007,varname:node_1248,prsc:2|IN-4791-OUT;proporder:6074-9950-9420-4277;pass:END;sub:END;*/

Shader "Collidalot/shad_fx_distortion_particle" {
    Properties {
        _distort_shape ("distort_shape", 2D) = "white" {}
        _norm ("norm", 2D) = "bump" {}
        _refrac_intesnity ("refrac_intesnity", Float ) = 1
        _time_speed ("time_speed", Float ) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        GrabPass{ }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One OneMinusSrcAlpha
            ZTest NotEqual
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma target 3.0
            uniform sampler2D _GrabTexture;
            uniform float4 _TimeEditor;
            uniform sampler2D _distort_shape; uniform float4 _distort_shape_ST;
            uniform sampler2D _norm; uniform float4 _norm_ST;
            uniform float _refrac_intesnity;
            uniform float _time_speed;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                #if UNITY_UV_STARTS_AT_TOP
                    float grabSign = -_ProjectionParams.x;
                #else
                    float grabSign = _ProjectionParams.x;
                #endif
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                float4 node_785 = _Time + _TimeEditor;
                float2 node_6602 = (i.screenPos.rg+(node_785.r*_time_speed)*float2(1,1));
                float3 _norm_var = UnpackNormal(tex2D(_norm,TRANSFORM_TEX(node_6602, _norm)));
                float2 node_1041 = (_norm_var.rgb.rg*_refrac_intesnity);
                float4 _distort_shape_var = tex2D(_distort_shape,TRANSFORM_TEX(i.uv0, _distort_shape));
                float2 sceneUVs = float2(1,grabSign)*i.screenPos.xy*0.5+0.5 + saturate((node_1041*_distort_shape_var.a));
                float4 sceneColor = tex2D(_GrabTexture, sceneUVs);
                clip((1.0 - floor(length((i.uv0*2.0+-1.0)))) - 0.5);
////// Lighting:
                float3 finalColor = 0;
                fixed4 finalRGBA = fixed4(lerp(sceneColor.rgb, finalColor,(node_1041*_distort_shape_var.a).r),1);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, finalRGBA, fixed4(0.5,0.5,0.5,1));
                return finalRGBA;
            }
            ENDCG
        }
    }
    CustomEditor "ShaderForgeMaterialInspector"
}

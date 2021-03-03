// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Forceling/World Grid"
{
	Properties
	{
		_GridScaleFactor("Grid Scale Factor", Float) = 1
		[Toggle]_Use10m("Use 10m", Float) = 1
		[Toggle]_Use100m("Use 100m", Float) = 1
		[Toggle]_Use1km("Use 1km", Float) = 1
		_1mGrid("1m Grid", 2D) = "gray" {}
		_10mGrid("10m Grid", 2D) = "white" {}
		_100mGrid("100m Grid", 2D) = "white" {}
		_1kmGrid("1km Grid", 2D) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Off
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#define ASE_TEXTURE_PARAMS(textureName) textureName

		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _1mGrid;
		uniform float _GridScaleFactor;
		uniform float _Use10m;
		uniform sampler2D _10mGrid;
		uniform float _Use100m;
		uniform sampler2D _100mGrid;
		uniform float _Use1km;
		uniform sampler2D _1kmGrid;


		inline float4 TriplanarSamplingSF( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.zy * float2( nsign.x, 1.0 ) ) );
			yNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.xz * float2( nsign.y, 1.0 ) ) );
			zNorm = ( tex2D( ASE_TEXTURE_PARAMS( topTexMap ), tiling * worldPos.xy * float2( -nsign.z, 1.0 ) ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar10 = TriplanarSamplingSF( _1mGrid, ase_worldPos, ase_worldNormal, 1.0, ( 0.5 / _GridScaleFactor ), 1.0, 0 );
			float4 triplanar13 = TriplanarSamplingSF( _10mGrid, ase_worldPos, ase_worldNormal, 1.0, ( 0.05 / _GridScaleFactor ), 1.0, 0 );
			float4 blendOpSrc17 = triplanar10;
			float4 blendOpDest17 = triplanar13;
			float temp_output_36_0 = distance( ase_worldPos , _WorldSpaceCameraPos );
			float4 lerpResult33 = lerp( triplanar10 , lerp(triplanar10,( saturate( (( blendOpDest17 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest17 ) * ( 1.0 - blendOpSrc17 ) ) : ( 2.0 * blendOpDest17 * blendOpSrc17 ) ) )),_Use10m) , saturate( pow( ( temp_output_36_0 / 13.0 ) , 15.0 ) ));
			float4 triplanar19 = TriplanarSamplingSF( _100mGrid, ase_worldPos, ase_worldNormal, 1.0, ( 0.005 / _GridScaleFactor ), 1.0, 0 );
			float4 blendOpSrc31 = triplanar10;
			float4 blendOpDest31 = triplanar19;
			float4 lerpResult42 = lerp( lerpResult33 , lerp(lerp(triplanar10,( saturate( (( blendOpDest17 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest17 ) * ( 1.0 - blendOpSrc17 ) ) : ( 2.0 * blendOpDest17 * blendOpSrc17 ) ) )),_Use10m),( saturate( (( blendOpDest31 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest31 ) * ( 1.0 - blendOpSrc31 ) ) : ( 2.0 * blendOpDest31 * blendOpSrc31 ) ) )),_Use100m) , saturate( pow( ( temp_output_36_0 / 80.0 ) , 10.0 ) ));
			float4 triplanar46 = TriplanarSamplingSF( _1kmGrid, ase_worldPos, ase_worldNormal, 1.0, ( 0.0005 / _GridScaleFactor ), 1.0, 0 );
			float4 blendOpSrc43 = triplanar10;
			float4 blendOpDest43 = triplanar46;
			float4 lerpResult47 = lerp( lerpResult42 , lerp(lerp(triplanar10,( saturate( (( blendOpDest17 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest17 ) * ( 1.0 - blendOpSrc17 ) ) : ( 2.0 * blendOpDest17 * blendOpSrc17 ) ) )),_Use10m),( saturate( (( blendOpDest43 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest43 ) * ( 1.0 - blendOpSrc43 ) ) : ( 2.0 * blendOpDest43 * blendOpSrc43 ) ) )),_Use1km) , saturate( pow( ( temp_output_36_0 / 800.0 ) , 10.0 ) ));
			o.Albedo = lerpResult47.xyz;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17101
2560;263;1906;993;3365.256;803.7629;1;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;21;-2202.927,31.89882;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;12;-2216.453,-316.6314;Float;False;Constant;_Tiling1m;Tiling1m;2;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-2196.078,-751.2676;Float;False;Constant;_Tiling10;Tiling10;3;0;Create;True;0;0;False;0;0.05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;-2864.651,-266.7361;Inherit;False;Property;_GridScaleFactor;Grid Scale Factor;0;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;22;-2249.624,491.8359;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;18;-2198.97,-1070.516;Float;False;Constant;_Tiling100;Tiling100;3;0;Create;True;0;0;False;0;0.005;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;57;-2053.107,-211.415;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-1512.87,335.9038;Float;False;Constant;_10mTransDist;10mTransDist;6;0;Create;True;0;0;False;0;13;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;36;-1783.917,353.7185;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;58;-2051.107,-676.415;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;59;-2056.659,-990.6055;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-2186.15,-1506.098;Float;False;Constant;_Tiling1000;Tiling1000;3;0;Create;True;0;0;False;0;0.0005;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;24;-1260.518,94.91224;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-1549.683,1000.116;Float;False;Constant;_100mTransDist;100mTransDist;6;0;Create;True;0;0;False;0;80;80;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-1157.092,337.386;Float;False;Constant;_10mTransFO;10mTransFO;5;0;Create;True;0;0;False;0;15;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;10;-1714.006,-316.0603;Inherit;True;Spherical;World;False;1m Grid;_1mGrid;gray;4;None;Mid Texture 1;_MidTexture1;white;-1;None;Bot Texture 1;_BotTexture1;white;-1;None;Triplanar Sampler;False;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TriplanarNode;13;-1728.071,-730.139;Inherit;True;Spherical;World;False;10m Grid;_10mGrid;white;5;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;Triplanar Sampler;False;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;17;-1277.131,-578.5079;Inherit;False;Overlay;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;26;-904.0915,97.38581;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;60;-2038.659,-1426.605;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;19;-1712.963,-1050.387;Inherit;True;Spherical;World;False;100m Grid;_100mGrid;white;6;None;Mid Texture 2;_MidTexture2;white;-1;None;Bot Texture 2;_BotTexture2;white;-1;None;Triplanar Sampler;False;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;37;-1297.33,759.1246;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-1193.905,1001.598;Float;False;Constant;_100mTransFO;100mTransFO;5;0;Create;True;0;0;False;0;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-1676.036,1396.681;Float;False;Constant;_1000mTransDist;1000mTransDist;6;0;Create;True;0;0;False;0;800;80;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;31;-1267.105,-922.969;Inherit;False;Overlay;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;38;-940.9037,761.5981;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;29;-729.5278,-307.1148;Float;False;Property;_Use10m;Use 10m;1;0;Create;True;0;0;False;0;1;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TriplanarNode;46;-1653.143,-1477.969;Inherit;True;Spherical;World;False;1km Grid;_1kmGrid;white;7;None;Mid Texture 3;_MidTexture3;white;-1;None;Bot Texture 3;_BotTexture3;white;-1;None;Triplanar Sampler;False;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;28;-621.4614,93.54749;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-1320.258,1398.163;Float;False;Constant;_1000mTransFO;1000mTransFO;5;0;Create;True;0;0;False;0;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;49;-1423.683,1155.69;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;30;-457.804,-937.7617;Float;False;Property;_Use100m;Use 100m;2;0;Create;True;0;0;False;0;1;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;39;-604.6263,755.2051;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;-259.283,5.106014;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;43;-1207.285,-1350.551;Inherit;False;Overlay;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;51;-1067.256,1158.163;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;44;-446.0226,-1358.481;Float;False;Property;_Use1km;Use 1km;3;0;Create;True;0;0;False;0;1;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;52;-730.979,1151.77;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;42;233.6958,24.19209;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;47;553.227,-7.494629;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;5;919.0499,-239.6935;Float;False;True;2;ASEMaterialInspector;0;0;Standard;Forceling/World Grid;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;57;0;12;0
WireConnection;57;1;54;0
WireConnection;36;0;21;0
WireConnection;36;1;22;0
WireConnection;58;0;15;0
WireConnection;58;1;54;0
WireConnection;59;0;18;0
WireConnection;59;1;54;0
WireConnection;24;0;36;0
WireConnection;24;1;25;0
WireConnection;10;3;57;0
WireConnection;13;3;58;0
WireConnection;17;0;10;0
WireConnection;17;1;13;0
WireConnection;26;0;24;0
WireConnection;26;1;27;0
WireConnection;60;0;45;0
WireConnection;60;1;54;0
WireConnection;19;3;59;0
WireConnection;37;0;36;0
WireConnection;37;1;40;0
WireConnection;31;0;10;0
WireConnection;31;1;19;0
WireConnection;38;0;37;0
WireConnection;38;1;41;0
WireConnection;29;0;10;0
WireConnection;29;1;17;0
WireConnection;46;3;60;0
WireConnection;28;0;26;0
WireConnection;49;0;36;0
WireConnection;49;1;48;0
WireConnection;30;0;29;0
WireConnection;30;1;31;0
WireConnection;39;0;38;0
WireConnection;33;0;10;0
WireConnection;33;1;29;0
WireConnection;33;2;28;0
WireConnection;43;0;10;0
WireConnection;43;1;46;0
WireConnection;51;0;49;0
WireConnection;51;1;50;0
WireConnection;44;0;29;0
WireConnection;44;1;43;0
WireConnection;52;0;51;0
WireConnection;42;0;33;0
WireConnection;42;1;30;0
WireConnection;42;2;39;0
WireConnection;47;0;42;0
WireConnection;47;1;44;0
WireConnection;47;2;52;0
WireConnection;5;0;47;0
ASEEND*/
//CHKSM=F844A756573A3A132B6E646D8ACCF160384AA5D9
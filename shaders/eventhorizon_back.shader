
HEADER
{
	Description = "";
}

FEATURES
{
	#include "vr_common_features.fxc"
	Feature( F_ADDITIVE_BLEND, 0..1, "Blending" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 1
	#endif
	
	#include "common/shared.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		o.vPositionOs = i.vPositionOs.xyz;
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/pixel.hlsl"
	#include "procedural.hlsl"
	#include "blendmodes.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( Color, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tColor < Channel( RGBA, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( True ); >;
	float g_flIllumbrightness < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 1, 10 ); >;
	bool g_bGrayscale < UiGroup( ",0/,0/0" ); Default( 0 ); >;
	float g_flInMin < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flInMax < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	float g_flOutMin < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flOutMax < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	float4 g_vSaturation < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m;
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = TransformNormal( i, float3( 0, 0, 1 ) );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float local0 = g_flIllumbrightness;
		float local1 = local0 * 1.5;
		float2 local2 = i.vTextureCoords.xy * float2( 1, 1 );
		float local3 = g_flTime * 1;
		float local4 = VoronoiNoise( i.vTextureCoords.xy, local3, 15 );
		float local5 = ( saturate( ( (local4) - (0) ) / ( (1) - (0) ) ) * ((0.525) - (0.5)) ) + (0.5);
		float2 local6 = local2 + float2( local5, local5 );
		float2 local7 = local6 + float2( 0.49, 0.49 );
		float4 local8 = Tex2DS( g_tColor, g_sSampler0, local7 );
		float local9 = local8.x;
		float local10 = local9 * 0.299;
		float local11 = local8.y;
		float local12 = local11 * 0.587;
		float local13 = local10 + local12;
		float local14 = local8.z;
		float local15 = local14 * 0.114;
		float local16 = local13 + local15;
		float4 local17 = float4( local16, local16, local16, 0 );
		float4 local18 = g_bGrayscale ? local17 : local8;
		float local19 = local18.x;
		float local20 = local19 * 0.299;
		float local21 = local18.y;
		float local22 = local21 * 0.587;
		float local23 = local20 + local22;
		float local24 = local18.z;
		float local25 = local24 * 0.114;
		float local26 = local23 + local25;
		float4 local27 = float4( local26, local26, local26, 0 );
		float local28 = g_flInMin;
		float local29 = g_flInMax;
		float local30 = g_flOutMin;
		float local31 = g_flOutMax;
		float4 local32 = ( saturate( ( (local27) - (float4( local28, local28, local28, local28 )) ) / ( (float4( local29, local29, local29, local29 )) - (float4( local28, local28, local28, local28 )) ) ) * ((float4( local31, local31, local31, local31 )) - (float4( local30, local30, local30, local30 ))) ) + (float4( local30, local30, local30, local30 ));
		float local33 = local32.x;
		float local34 = local33 * 1;
		float local35 = local32.y;
		float local36 = local35 * 1;
		float local37 = local34 + local36;
		float local38 = local32.z;
		float local39 = local38 * 4.6;
		float local40 = local37 + local39;
		float4 local41 = float4( local40, local40, local40, 0 );
		float4 local42 = local18 * local41;
		float local43 = local42.x;
		float4 local44 = g_vSaturation;
		float local45 = local43 * local44.r;
		float local46 = local42.y;
		float local47 = local46 * local44.g;
		float local48 = local42.z;
		float local49 = local48 * local44.b;
		float4 local50 = float4( local45, local47, local49, 0 );
		float local51 = local0 * 10;
		float4 local52 = local50 * float4( local51, local51, local51, local51 );
		float4 local53 = ( saturate( ( (local52) - (float4( 0, 0, 0, 0 )) ) / ( (float4( 10, 10, 10, 10 )) - (float4( 0, 0, 0, 0 )) ) ) * ((float4( 1, 1, 1, 1 )) - (float4( 0, 0, 0, 0 ))) ) + (float4( 0, 0, 0, 0 ));
		float4 local54 = float4( local1, local1, local1, local1 ) * local53;
		float local55 = ( saturate( ( (local0) - (1.5) ) / ( (10) - (1.5) ) ) * ((10) - (0)) ) + (0);
		float4 local56 = local52 * float4( local55, local55, local55, local55 );
		float4 local57 = local54 + local56;
		
		m.Albedo = local57.xyz;
		m.Emission = local57.xyz;
		m.Opacity = 1;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		return ShadingModelStandard::Shade( i, m );
	}
}

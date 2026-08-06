#version 140
#define HAS_MOD
#define HAS_DFDX
#define HAS_FLOAT_TEXTURES
#define HAS_SRGB
#define HAS_UNIFORM_BUFFERS
#define FRAGMENT_SHADER

// -- Utilities Start --

// It's literally just called the Z-Library for alphabetical ordering reasons.
//  - 20kdc

// -- varying/attribute/texture2D --

#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#ifdef VERTEX_SHADER
#define varying out
#define attribute in
#else
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif
#endif

#ifndef NO_ARRAY_PRECISION
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#else
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#endif

// -- shadow depth --

// If float textures are supported, puts the values in the R/G fields.
// This assumes RG32F format.
// If float textures are NOT supported.
// This assumes RGBA8 format.
// Operational range is "whatever works for FOV depth"
highp vec4 zClydeShadowDepthPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

// Inverts the previous function.
highp vec2 zClydeShadowDepthUnpack(highp vec4 val) {
#ifdef HAS_FLOAT_TEXTURES
    return val.xy;
#else
    return (val.xy * 255.0) + val.zw;
#endif
}

// -- srgb/linear conversion core --

highp vec4 zFromSrgb(highp vec4 sRGB)
{
    highp vec3 higher = pow((sRGB.rgb + 0.055) / 1.055, vec3(2.4));
    highp vec3 lower = sRGB.rgb / 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.04045));
    return vec4(mix(lower, higher, s), sRGB.a);
}

highp vec4 zToSrgb(highp vec4 sRGB)
{
    highp vec3 higher = (pow(sRGB.rgb, vec3(0.41666666666667)) * 1.055) - 0.055;
    highp vec3 lower = sRGB.rgb * 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.0031308));
    return vec4(mix(lower, higher, s), sRGB.a);
}

// -- uniforms --

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform projectionViewMatrices
{
    highp mat3 projectionMatrix;
    highp mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    highp vec2 SCREEN_PIXEL_SIZE;
    highp float TIME;
};
#else
uniform highp mat3 projectionMatrix;
uniform highp mat3 viewMatrix;
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform sampler2D TEXTURE;
uniform highp vec2 TEXTURE_PIXEL_SIZE;

// -- srgb emulation --

#ifdef HAS_SRGB

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    return texture2D(tex, uv);
}

highp vec4 zAdjustResult(highp vec4 col)
{
    return col;
}
#else
uniform lowp vec2 SRGB_EMU_CONFIG;

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    highp vec4 col = texture2D(tex, uv);
    if (SRGB_EMU_CONFIG.x > 0.5)
    {
        return zFromSrgb(col);
    }
    return col;
}

highp vec4 zAdjustResult(highp vec4 col)
{
    if (SRGB_EMU_CONFIG.y > 0.5)
    {
        return zToSrgb(col);
    }
    return col;
}
#endif

highp vec4 zTexture(highp vec2 uv)
{
    return zTextureSpec(TEXTURE, uv);
}

// -- color --

// Grayscale function for the ITU's Rec BT-709. Primarily intended for HDTVs, but standard sRGB monitors are coincidentally extremely close.
highp float zGrayscale_BT709(highp vec3 col) {
    return dot(col, vec3(0.2126, 0.7152, 0.0722));
}

// Grayscale function for the ITU's Rec BT-601, primarily intended for SDTV, but amazing for a handful of niche use-cases.
highp float zGrayscale_BT601(highp vec3 col) {
    return dot(col, vec3(0.299, 0.587, 0.114));
}

// If you don't have any reason to be specifically using the above grayscale functions, then you should default to this.
highp float zGrayscale(highp vec3 col) {
    return zGrayscale_BT709(col);
}

// -- noise --

//zRandom, zNoise, and zFBM are derived from https://godotshaders.com/snippet/2d-noise/ and https://godotshaders.com/snippet/fractal-brownian-motion-fbm/
highp vec2 zRandom(highp vec2 uv){
    uv = vec2( dot(uv, vec2(127.1,311.7) ),
               dot(uv, vec2(269.5,183.3) ) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

highp float zNoise(highp vec2 uv) {
    highp vec2 uv_index = floor(uv);
    highp vec2 uv_fract = fract(uv);

    highp vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( zRandom(uv_index + vec2(0.0,0.0) ), uv_fract - vec2(0.0,0.0) ),
                     dot( zRandom(uv_index + vec2(1.0,0.0) ), uv_fract - vec2(1.0,0.0) ), blur.x),
                mix( dot( zRandom(uv_index + vec2(0.0,1.0) ), uv_fract - vec2(0.0,1.0) ),
                     dot( zRandom(uv_index + vec2(1.0,1.0) ), uv_fract - vec2(1.0,1.0) ), blur.x), blur.y) * 0.5 + 0.5;
}

highp float zFBM(highp vec2 uv) {
    const int octaves = 6;
    highp float amplitude = 0.5;
    highp float frequency = 3.0;
    highp float value = 0.0;

    for(int i = 0; i < octaves; i++) {
        value += amplitude * zNoise(frequency * uv);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}


// -- generative --

// Function that creates a circular gradient. Screenspace shader bread n butter.
highp float zCircleGradient(highp vec2 ps, highp vec2 coord, highp float maxi, highp float radius, highp float dist, highp float power) {
    highp float rad = (radius * ps.y) * 0.001;
    highp float aspectratio = ps.x / ps.y;
    highp vec2 totaldistance = ((ps * 0.5) - coord) / (rad * ps);
    totaldistance.x *= aspectratio;
    highp float length = (length(totaldistance) * ps.y) - dist;
    return pow(clamp(length, 0.0, maxi), power);
}

// -- Utilities End --

// UV coordinates in texture-space. I.e., (0,0) is the corner of the texture currently being used to draw.
// When drawing a sprite from a texture atlas, (0,0) is the corner of the atlas, not the specific sprite being drawn.
varying highp vec2 UV;

// UV coordinates in quad-space. I.e., when drawing a sprite from a texture atlas (0,0) is the corner of the sprite
// currently being drawn.
varying highp vec2 UV2;

// TBH I'm not sure what this is for. I think it is scree  UV coordiantes, i.e., FRAGCOORD.xy * SCREEN_PIXEL_SIZE ?
// TODO CLYDE Is this still needed?
varying highp vec2 Pos;

// Vertex colour modulation. Note that negative values imply that the LIGHTMAP should be ignored. This is used to avoid
// having to set the texture to a white/blank texture for sprites that have no light shading applied.
varying highp vec4 VtxModulate;

// The current light map. Unless disabled, this is automatically sampled to create the LIGHT vector, which is then used
// to modulate the output colour.
// TODO CLYDE consistent shader variable naming
uniform sampler2D lightMap;

const ARRAY_HIGHP vec3 cCold =  vec3 ( 0.02 , 0.02 , 0.06 );
const ARRAY_HIGHP vec3 cCool =  vec3 ( 0.08 , 0.05 , 0.28 );
const ARRAY_HIGHP vec3 cMid =  vec3 ( 0.45 , 0.08 , 0.35 );
const ARRAY_HIGHP vec3 cWarm =  vec3 ( 0.85 , 0.28 , 0.08 );
const ARRAY_HIGHP vec3 cHot =  vec3 ( 0.95 , 0.65 , 0.12 );
const ARRAY_HIGHP vec3 cPeak =  vec3 ( 1.0 , 0.95 , 0.85 );
uniform sampler2D SCREEN_TEXTURE;
uniform ARRAY_HIGHP float Gain;
uniform ARRAY_HIGHP float TotalLighting;
uniform ARRAY_HIGHP float WhiteoutThreshold;
uniform ARRAY_HIGHP float WhiteoutIntensity;
uniform ARRAY_HIGHP float BloomRadius;
uniform ARRAY_HIGHP float BloomStrength;
uniform ARRAY_HIGHP float EnvCrush;
uniform ARRAY_HIGHP float ColdFloor;
uniform ARRAY_HIGHP float NoiseAmplitude;
uniform ARRAY_HIGHP float FlickerStrength;
uniform ARRAY_HIGHP float GrainSize;
uniform ARRAY_HIGHP float DefectIntensity;
uniform ARRAY_HIGHP float ScanlineDark;


ARRAY_HIGHP float sampleLum( ARRAY_HIGHP vec2 uv) {
 return dot ( zTextureSpec ( SCREEN_TEXTURE , uv ) . rgb , vec3 ( 0.2126 , 0.7152 , 0.0722 ) ) ;

}
ARRAY_HIGHP float brightExcess( ARRAY_HIGHP vec2 uv,  ARRAY_HIGHP float ampScale,  ARRAY_HIGHP float thresh) {
 return max ( 0.0 , sampleLum ( uv ) * ampScale - thresh ) ;

}
ARRAY_HIGHP float blurBrightLight( ARRAY_HIGHP vec2 uv,  ARRAY_HIGHP float ampScale,  ARRAY_HIGHP float thresh,  ARRAY_HIGHP float radiusPx) {
 highp float stepPx = clamp ( radiusPx * 0.12 , 1.0 , 4.0 ) ;
 highp vec2 s = SCREEN_PIXEL_SIZE * stepPx ;
 highp float sum = 0.0 ;
 highp float wsum = 0.0 ;
 sum += brightExcess ( uv , ampScale , thresh ) * 4.0 ;
 wsum += 4.0 ;
 sum += brightExcess ( uv + vec2 ( s . x , 0.0 ) , ampScale , thresh ) * 2.0 ;
 sum += brightExcess ( uv - vec2 ( s . x , 0.0 ) , ampScale , thresh ) * 2.0 ;
 sum += brightExcess ( uv + vec2 ( 0.0 , s . y ) , ampScale , thresh ) * 2.0 ;
 sum += brightExcess ( uv - vec2 ( 0.0 , s . y ) , ampScale , thresh ) * 2.0 ;
 wsum += 8.0 ;
 sum += brightExcess ( uv + vec2 ( s . x , s . y ) , ampScale , thresh ) * 1.0 ;
 sum += brightExcess ( uv + vec2 ( s . x , - s . y ) , ampScale , thresh ) * 1.0 ;
 sum += brightExcess ( uv + vec2 ( - s . x , s . y ) , ampScale , thresh ) * 1.0 ;
 sum += brightExcess ( uv + vec2 ( - s . x , - s . y ) , ampScale , thresh ) * 1.0 ;
 wsum += 4.0 ;
 sum += brightExcess ( uv + vec2 ( s . x * 2.0 , 0.0 ) , ampScale , thresh ) * 0.5 ;
 sum += brightExcess ( uv - vec2 ( s . x * 2.0 , 0.0 ) , ampScale , thresh ) * 0.5 ;
 sum += brightExcess ( uv + vec2 ( 0.0 , s . y * 2.0 ) , ampScale , thresh ) * 0.5 ;
 sum += brightExcess ( uv - vec2 ( 0.0 , s . y * 2.0 ) , ampScale , thresh ) * 0.5 ;
 wsum += 2.0 ;
 return sum / max ( wsum , 0.0001 ) ;

}
ARRAY_HIGHP vec3 infraredPalette( ARRAY_HIGHP float t) {
 t = clamp ( t , 0.0 , 1.0 ) ;
 if ( t < 0.38 ) return mix ( cCold , cCool , t / 0.38 ) ;
 if ( t < 0.62 ) return mix ( cCool , cMid , ( t - 0.38 ) / 0.24 ) ;
 if ( t < 0.78 ) return mix ( cMid , cWarm , ( t - 0.62 ) / 0.16 ) ;
 if ( t < 0.90 ) return mix ( cWarm , cHot , ( t - 0.78 ) / 0.12 ) ;
 return mix ( cHot , cPeak , ( t - 0.90 ) / 0.10 ) ;

}


void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    // The output colour. This should get set by the shader code block.
    // This will get modified by the LIGHT and MODULATE vectors.
    lowp vec4 COLOR;

    // The light colour, usually sampled from the LIGHTMAP
    lowp vec4 LIGHT;

    // Colour modulation vector.
    highp vec4 MODULATE;

    // Sample the texture outside of the branch / with uniform control flow.
    LIGHT = texture2D(lightMap, Pos);

    if (VtxModulate.x < 0.0)
    {
        // Negative VtxModulate implies unshaded/no lighting.
        MODULATE = -1.0 - VtxModulate;
        LIGHT = vec4(1.0);
    }
    else
    {
        MODULATE = VtxModulate;
    }

    // TODO CLYDE consistent shader variable naming
    // Requires breaking changes.
    lowp vec3 lightSample = LIGHT.xyz;

     COLOR = zTextureSpec ( SCREEN_TEXTURE , Pos ) ;
 highp float Y = dot ( COLOR . rgb , vec3 ( 0.2126 , 0.7152 , 0.0722 ) ) ;
 highp float YComp = Y / ( 1.0 + Y * 1.7 ) ;
 highp float ampScale = Gain * ( 1.0 + TotalLighting * 5.0 ) ;
 highp float YAmp = YComp * ampScale ;
 highp float bloomThresh = WhiteoutThreshold * 0.7 ;
 highp float blurredLight = blurBrightLight ( Pos , ampScale * 0.57 , bloomThresh , BloomRadius ) ;
 YAmp += blurredLight * BloomStrength ;
 highp float g = YAmp / ( 1.0 + max ( YAmp , 0.0 ) * 0.42 ) ;
 highp float floor = max ( ColdFloor , 0.0 ) ;
 highp float heat = smoothstep ( floor , 1.0 , g ) ;
 heat = pow ( clamp ( heat , 0.0 , 1.0 ) , max ( EnvCrush , 0.5 ) ) ;
 highp float denom = max ( 1.0 - WhiteoutThreshold , 0.0001 ) ;
 highp float whiteout = clamp ( ( heat - WhiteoutThreshold ) / denom , 0.0 , 1.0 ) ;
 whiteout = pow ( clamp ( whiteout * WhiteoutIntensity , 0.0 , 1.0 ) , 1.3 ) ;
 whiteout = max ( whiteout , smoothstep ( 0.72 , 0.92 , heat ) * 0.55 ) ;
 whiteout *= 0.75 ;
 highp vec3 ir = infraredPalette ( heat ) ;
 highp vec3 white = vec3 ( 1.0 ) ;
 highp vec3 finalColor = mix ( ir , white , whiteout ) ;
 highp float line = 1.0 - step ( 0.5 , mod ( floor ( FRAGCOORD . y ) , 4.0 ) ) ;
 finalColor *= mix ( 1.0 , mix ( ScanlineDark , 1.0 , whiteout ) , line ) ;
 COLOR . rgb = finalColor ;


    LIGHT.xyz = lightSample;

    gl_FragColor = zAdjustResult(COLOR * MODULATE * LIGHT);
}

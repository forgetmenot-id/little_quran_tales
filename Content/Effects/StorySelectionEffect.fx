// StorySelectionEffect.fx — HLSL SpriteEffect for MonoGame
// Combines: wobble/wind distortion, golden hover glow, grayscale desaturation

float2 HalfPixel;
float Time;
float GlowIntensity;
float Saturation;

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

VSOutput SpriteVertexShader(float4 pos : POSITION0, float4 color : COLOR0, float2 texCoord : TEXCOORD0)
{
    VSOutput o;
    o.Position = pos;
    o.TexCoord = texCoord;
    o.Color = color;
    return o;
}

float4 StorySelectionPS(VSOutput input) : COLOR0
{
    float2 uv = input.TexCoord;

    // ── 1. Wobble / wind distortion ──────────────────────────────────
    float wobbleX = sin(uv.y * 28.0 + Time * 1.8) * 0.0025;
    float wobbleY = sin(uv.x * 24.0 + Time * 1.4) * 0.0015;
    uv = uv + float2(wobbleX, wobbleY);

    float4 color = tex2D(SpriteTextureSampler, saturate(uv)) * input.Color;

    // ── 2. Grayscale / desaturation ──────────────────────────────────
    float lum = dot(color.rgb, float3(0.299, 0.587, 0.114));
    color.rgb = lerp(lum.xxx, color.rgb, Saturation);

    // ── 3. Golden hover edge glow ────────────────────────────────────
    if (GlowIntensity > 0.001)
    {
        float2 hp = HalfPixel;
        float aC  = tex2D(SpriteTextureSampler, input.TexCoord).a;
        float aR  = tex2D(SpriteTextureSampler, input.TexCoord + float2(hp.x * 3, 0)).a;
        float aL  = tex2D(SpriteTextureSampler, input.TexCoord - float2(hp.x * 3, 0)).a;
        float aU  = tex2D(SpriteTextureSampler, input.TexCoord + float2(0, hp.y * 3)).a;
        float aD  = tex2D(SpriteTextureSampler, input.TexCoord - float2(0, hp.y * 3)).a;

        // Edge strength from alpha gradient
        float edge = length(float2(aR - aL, aU - aD));
        edge = saturate(edge * 2.0);

        // Only glow on edges that have content (alpha > 0)
        float mask = saturate(aC * 4.0);
        edge *= mask;

        float3 gold = float3(0.832, 0.686, 0.216); // #D4AF37
        color.rgb += gold * edge * GlowIntensity * 0.75;
        color.a = saturate(color.a);
    }

    return color;
}

technique SpriteEffect
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 StorySelectionPS();
    }
}

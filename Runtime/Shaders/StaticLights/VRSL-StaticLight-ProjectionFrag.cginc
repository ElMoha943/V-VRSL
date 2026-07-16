// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#include "../Shared/VRSL-RenderHelpers.cginc"
#include "../Shared/VRSL-ProjectionHelpers.cginc"

#define IF(a, b, c) lerp(b, c, step((fixed) (a), 0));

        fixed4 ProjectionFrag(v2f i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            UNITY_SETUP_INSTANCE_ID(i);
            float4 emissionTint = i.emissionColor;
            #ifdef VRSL_DMX
                float gi = i.globalFinalIntensity.x;
                float fi = i.globalFinalIntensity.y;
                #ifdef FIVECH
                    if(((all(i.rgbColor <= float4(0.01,0.01,0.01,1)) || i.intensityStrobe.x <= 0.01) && isDMX() == 1) || gi <= 0.005 || fi <= 0.005 || all(emissionTint <= float4(0.005, 0.005, 0.005, 1.0)))
                    {
                        return float4(0,0,0,0);
                    }
                #else
                    if(((all(i.rgbColor <= float4(0.05,0.05,0.05,1)) || i.intensityStrobe.x <= 0.05) && isDMX() == 1) || gi <= 0.005 || fi <= 0.005 || all(emissionTint <= float4(0.005, 0.005, 0.005, 1.0)))
                    {
                        return float4(0,0,0,0);
                    }
                #endif
            #endif
            #ifdef VRSL_AUDIOLINK
                float audioReaction = i.audioGlobalFinalIntensity.x;
                float gi = i.audioGlobalFinalIntensity.y;
                float fi = i.audioGlobalFinalIntensity.z;
                if(audioReaction <= 0.005 || gi <= 0.005 || fi <= 0.005 || all(emissionTint <= float4(0.005, 0.005, 0.005, 1.0)))
                {
                    return half4(0,0,0,0);
                }
            #endif

            #if _ALPHATEST_ON && !SHADER_API_GLES3
                float ditherThreshold = VRSL_GetDitherThreshold(i.screenPos);
            #endif
            
            if(i.color.g != 0)
            {
                
                i.ray = i.ray * (_ProjectionParams.z / i.ray.z);

                float2 screenposUV = i.screenPos.xy / i.screenPos.w;

                //CREDIT TO DJ LUKIS FOR MIRROR DEPTH CORRECTION
                float perspectiveDivide = 1.0f / i.pos.w;
                float4 direction = i.worldDirection * perspectiveDivide;
                float sceneZ = VRSL_SampleProjectionDepth(screenposUV);

                #if UNITY_REVERSED_Z
                    if (sceneZ == 0)
                #else
                    sceneZ = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneZ);
                    if (sceneZ == 1)
                #endif
                        return float4(0,0,0,1);
                float depth = VRSL_ProjectionLinear01Depth(sceneZ, direction.w);
                float3 objectOrigin = mul(unity_ObjectToWorld, float4(0.0,0.0,0.0,1.0) ).xyz;
                //get object origin in world space.
                //float3 fragViewPos = float4(i.ray * depth, 1);

                float3 wpos = VRSL_ProjectionWorldPosition(depth, i.ray);
                float3 projPos = VRSL_ProjectionObjectPosition(wpos);
                float distanceFromOrigin = length(objectOrigin - wpos);
                float f = _Fade;
                #if _ALPHATEST_ON && !SHADER_API_GLES3
                    f += 1.0;
                #endif
                float UVscale = VRSL_ProjectionReciprocalFalloff(distanceFromOrigin, _ProjectionDistanceFallOff, _ProjectionUVMod, _FeatherOffset);

                //float3 calculatedWorldNormal = getCalculatedWorldNormal(projPos);

                float2 uvCoords = (((float2((projPos.x), projPos.y) * UVscale)));
                //uvCoords = mul(uvCoords, projPos.z);
                //Get coordinate plane in object space

                uvCoords.x += _XOffset;
                uvCoords.y += _YOffset;
                uvCoords.x *= _ModX;
                uvCoords.y *= _ModY;
                //uvCoords = normalize(mul(float4(uvCoords, 0.0, 0.0), unity_ObjectToWorld)).xy;

                clip(uvCoords);
                //Discard any pixels that are outside of the traditional 0-1 UV bounds.

                float4 tex = tex2D(_ProjectionMainTex, uvCoords); 
                //tex = float4(tex.x, tex.y, tex.z, pow(tex.w * distanceFromOrigin, -1));
                //tex = pow(tex * distanceFromOrigin, 1);
                //calculatedWorldNormal = UnpackNormal(tex2D(_SceneNormals, oldUVcoords));
                // Create create xy coordinate plane based on object space, make sure it scales based on the 
                // distance from the intersection

                clip(1.0 - uvCoords);
                float4 col = tex;
                 //float4 col = tex * float4(n,1);

                //clip(projPos.z);
                #ifdef VRSL_AUDIOLINK
                    float strobe = 1.0;
                    col *= audioReaction;
                #endif
                #ifdef VRSL_DMX
                    float strobe = IF(isStrobe() == 1, i.intensityStrobe.y, 1);    
                    float4 DMXcol = col;
                    DMXcol *= i.rgbColor;
                    col = IF(isDMX() == 1, DMXcol, col);
                #endif


                
                float4 result = ((col * UVscale  * _ProjectionMaxIntensity) * emissionTint) * strobe;
                col = result * VRSL_ProjectionDistanceFadeMultiplier(distanceFromOrigin, f) * gi * fi * _UniversalIntensity;
                
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
                    col *= _AlphaProjectionIntensity;
                    clip(col.a - ditherThreshold);
                    clip((((col.r + col.g + col.b)/3) * (_ClippingThreshold)) - ditherThreshold);
                    return col;
                #else
                    return col;
                #endif
            }
            else
            {
                clip(i.pos);
                discard;
                return float4(0,0,0,0);
            }
                
        }

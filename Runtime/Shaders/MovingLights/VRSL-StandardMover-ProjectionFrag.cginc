#include "../Shared/VRSL-RenderHelpers.cginc"
#include "../Shared/VRSL-ProjectionHelpers.cginc"

//Huge, huge thanks and shoutout to Uncomfy on the VRC Shader Discord for helping me figure this out <3
        half4 CalculateProjectionRotationSinCos(half panValue, half tiltValue)
        {
            half sX, cX, sY, cY;

            #ifdef VRSL_DMX
                half angleY = radians(getOffsetY() + (panValue));
            #endif
            #ifdef VRSL_AUDIOLINK
                half angleY = radians(0);
            #endif

            sincos(angleY, sY, cY);
            sY = checkPanInvertY() == 1 ? -sY : sY;

            half tiltOffset = 90.0;
            tiltOffset = checkTiltInvertZ() == 1 ? -tiltOffset : tiltOffset;

            #ifdef VRSL_DMX
                half angleX = radians(getOffsetX() + (tiltValue + tiltOffset));
            #endif
            #ifdef VRSL_AUDIOLINK
                half angleX = radians(0 + (tiltOffset));
            #endif
            sincos(angleX, sX, cX);
            sX = checkTiltInvertZ() == 1 ? -sX : sX;

            return half4(sY, cY, sX, cX);
        }

        half2 CalculateProjectionUVRotationSinCos(half angle)
        {
            half sinAngle;
            half cosAngle;
            sincos(radians(angle), sinAngle, cosAngle);
            return half2(sinAngle, cosAngle);
        }


        half4 ChooseProjection(half2 uv, half projChooser)
        {
            half2 addition = half2(0.0, 0.0);
            uv*= half2(0.25, 0.5);

            #ifdef WASH
                addition = half2(0.0, 0.5);
            #else
                if(projChooser == 1.0) addition = half2(0.0, 0.5);
                else if(projChooser == 2.0) addition = half2(0.25, 0.5);
                else if(projChooser == 3.0) addition = half2(0.5, 0.5);
                else if(projChooser == 4.0) addition = half2(0.75, 0.5);
                else if(projChooser == 5.0) addition = half2(0.0, 0.0);
                else if(projChooser == 6.0) addition = half2(0.25, 0.0);
                else if(projChooser == 7.0) addition = half2(0.5, 0.0);
                else if(projChooser == 8.0) addition = half2(0.75, 0.0);
            #endif
            uv.x += addition.x;
            uv.y += addition.y;
            return tex2D(_ProjectionMainTex, uv);
        }
        half ChooseProjectionScalar(half coneWidth, half projChooser)
        {
            half result = _ProjectionUVMod;
            if(projChooser == 1.0) result = _ProjectionUVMod * _MinimumBeamRadius;
            #if !defined(WASH)
            else if(projChooser == 2.0) result = _ProjectionUVMod2 * _MinimumBeamRadius;
            else if(projChooser == 3.0) result = _ProjectionUVMod3 * _MinimumBeamRadius;
            else if(projChooser == 4.0) result = _ProjectionUVMod4 * _MinimumBeamRadius;
            else if(projChooser == 5.0) result = _ProjectionUVMod5 * _MinimumBeamRadius;
            else if(projChooser == 6.0) result = _ProjectionUVMod6 * _MinimumBeamRadius;
            else if(projChooser == 7.0) result = _ProjectionUVMod7 * _MinimumBeamRadius;
            else if(projChooser == 8.0) result = _ProjectionUVMod8 * _MinimumBeamRadius;
            #endif


            // half a = 1.8;
            // #ifdef WASH
            //     a = 3.0;
            // #endif
            // return result * (clamp(coneWidth, -2.0, 4) + a);
            half conewidthControl = coneWidth/4.25;
            #ifndef WASH
                return result * lerp(0.325, 1, (conewidthControl));
            #else
                return result * lerp(0.4, 1, (conewidthControl));
            #endif
        }  





        fixed4 ProjectionFrag(v2f i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            UNITY_SETUP_INSTANCE_ID(i);
            if(i.color.g > 0.5)
            {

                half4 emissionTint = i.emissionColor;

                #ifdef VRSL_DMX
                    half gi = getGlobalIntensity();
                    half fi = getFinalIntensity();
                    half coneWidth = i.intensityStrobeWidth.z;
                    if(((all(i.rgbColor <= half4(0.01,0.01,0.01,1)) || i.intensityStrobeWidth.x <= 0.01) && isDMX() == 1) || gi <= 0.005 || fi <= 0.005 || all(emissionTint <= half4(0.005, 0.005, 0.005, 1)))
                    {
                        return half4(0,0,0,0);
                    }
                #endif
                #ifdef VRSL_AUDIOLINK
                    half audioReaction = i.audioGlobalFinalConeIntensity.x;
                    half gi = i.audioGlobalFinalConeIntensity.y;
                    half fi = i.audioGlobalFinalConeIntensity.z;
                    half coneWidth = i.audioGlobalFinalConeIntensity.w;
                    // if((all(i.rgbColor <= half4(0.01,0.01,0.01,1)) || i.intensityStrobeWidth.x <= 0.01) && isOSC() == 1)
                    // {
                    //     return (0,0,0,0);
                    // }
                    if(audioReaction <= 0.005 || gi <= 0.005 || fi <= 0.005 || all(emissionTint<= half4(0.005, 0.005, 0.005, 1.0)))
                    {
                        return half4(0,0,0,0);
                    }
                #endif


				#if _ALPHATEST_ON && !SHADER_API_GLES3
					half ditherThreshold = (half)VRSL_GetDitherThreshold(i.screenPos);
		        #endif


                #ifdef VRSL_DMX
                    uint selection = round(i.intensityStrobeWidth.w);
                #endif

                 //Calculating projection
                i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
                float2 screenposUV = i.screenPos.xy / i.screenPos.w;


                //CREDIT TO DJ LUKIS FOR MIRROR DEPTH CORRECTION
                float perspectiveDivide = 1.0f / i.pos.w;
                float4 depthdirect = i.worldDirection * perspectiveDivide;
                //float2 altScreenPos = i.screenPos.xy * perspectiveDivide;


                float sceneZ = VRSL_SampleProjectionDepth(screenposUV);

                #if UNITY_REVERSED_Z
                    if (sceneZ == 0)
                #else
                    sceneZ = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneZ);
                    if (sceneZ == 1)
                #endif
                        return float4(0,0,0,1);

                
                float depth = VRSL_ProjectionLinear01Depth(sceneZ, depthdirect.w);

                 
                //lienarize the depth

                float3 objectOrigin = mul(unity_ObjectToWorld, half4(0.0,0.0,0.0,1.0) ).xyz;
                //get object origin in world space.

                float3 wpos = VRSL_ProjectionWorldPosition(depth, i.ray);
                //convert view space coordinate to world space coordinate. 
                //Wpos is now coordinates for intersection.

                //get the projection in object space
                float3 oPos = VRSL_ProjectionObjectPosition(wpos);
                float3 fixtureDelta = oPos - _FixtureRotationOrigin.xyz;
                if((dot(fixtureDelta, fixtureDelta) < (_ProjectionCutoff * _ProjectionCutoff)) ||
                    (dot(oPos, oPos) < (_ProjectionOriginCutoff * _ProjectionOriginCutoff)))
                {
                    //check the distance of rotation origin to the set cutoff value.
                    //if distance is less that the set value, discard the pixel.
                    //this is used to prevent the projection from bleeding on to the source fixture mesh.
                    discard;
                }

                float distanceFromOrigin = distance(objectOrigin, wpos);


                #ifdef VRSL_DMX
                    float projChooser = isDMX() == 1 ? selection : instancedGOBOSelection();
                #endif
                #ifdef VRSL_AUDIOLINK
                    float projChooser = round(instancedGOBOSelection());
                #endif


                //Get distance of intersection from the origin in world space
                #ifdef VRSL_DMX
                    float projectionScalar = ChooseProjectionScalar(coneWidth, projChooser);
                    float UVscale = VRSL_ProjectionReciprocalFalloff(distanceFromOrigin, 0.0, projectionScalar, 0.0);
                    distanceFromOrigin = lerp(distanceFromOrigin*0.6 +0.65,distanceFromOrigin, saturate(coneWidth));
                #endif
                #ifdef VRSL_AUDIOLINK
                    distanceFromOrigin = lerp(distanceFromOrigin*0.6 +0.65,distanceFromOrigin, saturate(coneWidth));
                    float projectionScalar = ChooseProjectionScalar(coneWidth, projChooser);
                    float UVscale = VRSL_ProjectionReciprocalFalloff(distanceFromOrigin, 0.0, projectionScalar, 0.0);
                #endif
                // inverse that distance so that it gets smaller as it gets closer, 
                // multiply it by modifier parameter incase things get wonky.
                float3 projPos = oPos;


                //position of the intersection fragment in the cone's object space
                projPos = VRSL_RotateProjectionPosition(projPos, _FixtureRotationOrigin.xyz, i.projectionRotationSinCos);



                float2 uvCoords = projPos.xy * UVscale;

                uvCoords.x += 0.5;
                uvCoords.y += 0.5;
                //Get coordinate plane

                half2 uvOrigin = half2(0.5, 0.5);
                
                uvCoords = VRSL_RotateProjectionUV(uvCoords, i.projectionUVRotationSinCos);
                
                clip(uvCoords);

                //Discard any pixels that are outside of the traditional 0-1 UV bounds.
                float4 tex = ChooseProjection(uvCoords, projChooser);
                float distFromUVOrigin = distance(uvCoords, uvOrigin);
                // Create create xy coordinate plane based on object space, make sure it scales based on the 
                // distance from the intersection


                //Discard any pixels that are outside of the traditional 0-1 UV bounds in the negative range.
                clip(1.0 - uvCoords);
                half4 col = tex;

                clip(projPos.z);
                //Projection Fade
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
                    col *= VRSL_ProjectionFadeMultiplier(distFromUVOrigin, _ProjectionFade - 1.0, _ProjectionFadeCurve);
                #else
                    col *= VRSL_ProjectionFadeMultiplier(distFromUVOrigin, _ProjectionFade, _ProjectionFadeCurve);
                #endif


                #ifdef VRSL_DMX
                    half strobe = isStrobe() == 1 ? i.intensityStrobeWidth.y : 1.0;
                    if(isDMX() == 1 && _EnableStaticEmissionColor == 0)
                    {
                        col *= i.rgbColor;
                    }
                #endif
                #ifdef VRSL_AUDIOLINK
                    half strobe = 1.0;
                #endif

                
                // project plane on to the world normals in object space in the z direction of the object origin.
                half projectionIntensity = _ProjectionIntensity;
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
                    projectionIntensity += 4.0;
                #endif
                col *= emissionTint * UVscale * projectionIntensity * strobe;
                col *= VRSL_ProjectionReciprocalFalloff(distanceFromOrigin, 0.0, 0.0, _ProjectionDistanceFallOff);
                #ifdef VRSL_AUDIOLINK
                     col = col * audioReaction;
                #endif
                col.rgb *= gi * fi;
                if(_EnableStaticEmissionColor == 1)
                {
                    col.rgb *= half3(_RedMultiplier, _GreenMultiplier, _BlueMultiplier);
                }
                col *= _UniversalIntensity;
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
					clip(col.a - ditherThreshold);
					clip((((col.r + col.g + col.b)/3) * (_ClippingThreshold)) - ditherThreshold);
                    return col;
                #else
                    return col;
                #endif
            }
            else
            {
                return half4(0,0,0,0);
            }
                
        }

#include "../Shared/VRSL-RenderHelpers.cginc"
#include "../Shared/VRSL-ProjectionHelpers.cginc"

#define IF(a, b, c) lerp(b, c, step((fixed) (a), 0));

//Huge, huge thanks and shoutout to Uncomfy on the VRC Shader Discord for helping me figure this out <3
        half4 InvertRotations (half4 input, half panValue, half tiltValue)
        {
            half sX, cX, sY, cY;

            #ifdef VRSL_DMX
                half angleY = radians(getOffsetY() + (panValue));
            #endif
            #ifdef VRSL_AUDIOLINK
                half angleY = radians(0);
            #endif

            sincos(angleY, sY, cY);
            half4x4 rotateYMatrix = half4x4(cY, sY, 0, 0,
                -sY, cY, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            half4 BaseAndFixturePos = input;

            	//INVERSION CHECK
            rotateYMatrix = IF(checkPanInvertY() == 1, transpose(rotateYMatrix), rotateYMatrix);

            //half4 localRotY = mul(rotateYMatrix, BaseAndFixturePos);
            //LOCALROTY IS NEW ROTATION


            half tiltOffset = 90.0;
            tiltOffset = IF(checkTiltInvertZ() == 1, -tiltOffset, tiltOffset);
            //set new origin to do transform
            half4 newOrigin = input.w * _FixtureRotationOrigin;
            input.xyz -= newOrigin;

            #ifdef VRSL_DMX
                half angleX = radians(getOffsetX() + (tiltValue + tiltOffset));
            #endif
            #ifdef VRSL_AUDIOLINK
                half angleX = radians(0 + (tiltOffset));
            #endif
            sincos(angleX, sX, cX);

            half4x4 rotateXMatrix = half4x4(1, 0, 0, 0,
                0, cX, sX, 0,
                0, -sX, cX, 0,
                0, 0, 0, 1);

            //half4 fixtureVertexPos = input;

            	//INVERSION CHECK
            rotateXMatrix = IF(checkTiltInvertZ() == 1, transpose(rotateXMatrix), rotateXMatrix);
            //half4 localRotX = mul(rotateXMatrix, fixtureVertexPos);

            half4x4 rotateXYMatrix = mul(rotateXMatrix, rotateYMatrix);
            half4 localRotXY = mul(rotateXYMatrix, input);

            input.xyz = localRotXY;
            input.xyz += newOrigin;
            return input;
        }

        half2 RotateUV(half2 input, half angle)
        {
            half2 newOrigin = half2(0.5, 0.5);
            input -= newOrigin;
            half sinAngle;
            half cosAngle;
            sincos(radians(angle), sinAngle, cosAngle);
            half2x2 rotationMatrix = half2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
            input = mul(input, rotationMatrix);
            input += newOrigin;
            return input;

        }


        half4 ChooseProjection(half2 uv, half projChooser)
        {
            half2 addition = half2(0.0, 0.0);
            uv*= half2(0.25, 0.5);

            #ifdef WASH
            projChooser = 1.0;
            #endif
            
            addition = IF(projChooser == 1.0, half2(0.0, 0.5) , addition);
            #if !defined(WASH)
                addition = IF(projChooser == 2.0, half2(0.25, 0.5), addition);
                addition = IF(projChooser == 3.0, half2(0.5, 0.5), addition);
                addition = IF(projChooser == 4.0, half2(0.75, 0.5), addition);
                addition = IF(projChooser == 5.0, half2(0.0, 0.0) , addition);
                addition = IF(projChooser == 6.0, half2(0.25, 0.0), addition);
                addition = IF(projChooser == 7.0, half2(0.5, 0.0), addition);
                addition = IF(projChooser == 8.0, half2(0.75, 0.0), addition);
            #endif
            uv.x += addition.x;
            uv.y += addition.y;
            return tex2D(_ProjectionMainTex, uv);
        }
        half ChooseProjectionScalar(half coneWidth, half projChooser)
        {
            //half chooser = IF(isDMX() == 1, selection, instancedGOBOSelection());
            half result = _ProjectionUVMod;
            result = IF((projChooser) == 1.0, (_ProjectionUVMod * _MinimumBeamRadius), result);
            #if !defined(WASH)
            result = IF((projChooser) == 2.0, _ProjectionUVMod2 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 3.0, _ProjectionUVMod3 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 4.0, _ProjectionUVMod4 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 5.0, _ProjectionUVMod5 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 6.0, _ProjectionUVMod6 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 7.0, _ProjectionUVMod7 * _MinimumBeamRadius, result);
            result = IF((projChooser) == 8.0, _ProjectionUVMod8 * _MinimumBeamRadius, result);
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
                    half panValue = i.goboPlusSpinPanTilt.z;
                    half tiltValue = i.goboPlusSpinPanTilt.w;
                    uint selection = round(i.goboPlusSpinPanTilt.x);
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
                    float projChooser = IF(isDMX() == 1, selection, instancedGOBOSelection());
                #endif
                #ifdef VRSL_AUDIOLINK
                    float projChooser = round(instancedGOBOSelection());
                #endif


                //Get distance of intersection from the origin in world space
                #ifdef VRSL_DMX
                    float UVscale = rcp(distanceFromOrigin * ChooseProjectionScalar(coneWidth, projChooser));
                    distanceFromOrigin = lerp(distanceFromOrigin*0.6 +0.65,distanceFromOrigin, saturate(coneWidth));
                #endif
                #ifdef VRSL_AUDIOLINK
                    distanceFromOrigin = lerp(distanceFromOrigin*0.6 +0.65,distanceFromOrigin, saturate(coneWidth));
                    float UVscale = rcp(distanceFromOrigin * ChooseProjectionScalar(coneWidth, projChooser));
                #endif
                // inverse that distance so that it gets smaller as it gets closer, 
                // multiply it by modifier parameter incase things get wonky.
                float3 projPos = oPos;


                //position of the intersection fragment in the cone's object space
                #ifdef VRSL_DMX
                    projPos = InvertRotations(float4(projPos,1.0), panValue, tiltValue);
                #endif
                #ifdef VRSL_AUDIOLINK
                    projPos = InvertRotations(float4(projPos,1.0), 0, 0);
                #endif



                float2 uvCoords = (((float2((projPos.x), projPos.y) * UVscale)));

                uvCoords.x += 0.5;
                uvCoords.y += 0.5;
                //Get coordinate plane

                half2 uvOrigin = half2(0.5, 0.5);
                
                #ifdef VRSL_DMX
                    _SpinSpeed = IF(checkPanInvertY() == 1, -_SpinSpeed, _SpinSpeed);
                    _SpinSpeed = IF(isDMX() == 1, _SpinSpeed, _SpinSpeed);
                    uvCoords = IF(isGOBOSpin() == 1 && projChooser > 1.0, RotateUV(uvCoords,  degrees(i.goboPlusSpinPanTilt.y)), RotateUV(uvCoords, _ProjectionRotation));
                #endif
                #ifdef VRSL_AUDIOLINK
                    half goboSpinSpeed = IF(checkPanInvertY() == 1, -getGoboSpinSpeed(), getGoboSpinSpeed());
                    uvCoords = IF(isGOBOSpin() == 1 && projChooser > 1.0, RotateUV(uvCoords, _Time.w * ( 10* goboSpinSpeed)), RotateUV(uvCoords, _ProjectionRotation));
                #endif

               // uvCoords = IF(isGOBOSpin() == 1 && projChooser > 1.0, RotateUV(uvCoords, _Time.w * ( 10* _SpinSpeed)), RotateUV(uvCoords, _ProjectionRotation));
                
                clip(uvCoords);

                //Discard any pixels that are outside of the traditional 0-1 UV bounds.
                float4 tex = ChooseProjection(uvCoords, projChooser);
                float distFromUVOrigin = (abs(distance(uvCoords, uvOrigin)));
                // Create create xy coordinate plane based on object space, make sure it scales based on the 
                // distance from the intersection


                //Discard any pixels that are outside of the traditional 0-1 UV bounds in the negative range.
                clip(1.0 - uvCoords);
                half4 col = tex;

                clip(projPos.z);
                //Projection Fade
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
                    col = lerp(col, half4(0,0,0,0), clamp(pow(distFromUVOrigin * (_ProjectionFade-1.0),_ProjectionFadeCurve),0.0,1.0));
                #else
                    col = lerp(col, half4(0,0,0,0), clamp(pow(distFromUVOrigin * _ProjectionFade,_ProjectionFadeCurve),0.0,1.0));
                #endif


                #ifdef VRSL_DMX
                    half strobe = IF(isStrobe() == 1, i.intensityStrobeWidth.y, 1);
                    col = IF(isDMX() == 1 & _EnableStaticEmissionColor == 0, col * i.rgbColor, col);
                #endif
                #ifdef VRSL_AUDIOLINK
                    half strobe = 1.0;
                #endif

                
                //col = IF(_EnableStaticEmissionColor == 1, col * half4(_StaticEmission.r * _RedMultiplier,_StaticEmission.g * _GreenMultiplier,_StaticEmission.b * _BlueMultiplier,_StaticEmission.a), col);
                
                  
                

                // project plane on to the world normals in object space in the z direction of the object origin.
                half projectionIntesnity = _ProjectionIntensity;
                #if defined(_ALPHATEST_ON) && !SHADER_API_GLES3
                    projectionIntesnity +=4.0;
                #endif
                col = ((col * emissionTint * UVscale * projectionIntesnity)) * strobe; 
                col *= rcp(_ProjectionDistanceFallOff * distanceFromOrigin * distanceFromOrigin);
                #ifdef VRSL_AUDIOLINK
                     col = col * audioReaction;
                #endif
                col.rgb *= gi * fi;
                //half saturation = saturate(RGB2HSV(col)).y;  
                //col = IF(_EnableStaticEmissionColor == 1, lerp(half4(0,0,0,0), col, saturation), col);
                col = IF( _EnableStaticEmissionColor == 1, half4(col.r * _RedMultiplier, col.g * _GreenMultiplier, col.b * _BlueMultiplier, col.a), col);
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

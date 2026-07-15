Shader "VRSL/DMX CRTs/Interpolation"
{
    Properties
    {
        [Toggle] _EnableDMX ("Enable Stream DMX/DMX Control", Int) = 0
        [Toggle] _NineUniverseMode ("Extended Universe Mode", Int) = 0
        [Toggle(_SIGNAL_DETECTION)] _SignalDetectionSystem ("Enable Signal Detection System To Prevent Unnecesary Strobing", Int) = 0
        _SignalDetectionSensativity ("Signal Detection Sensativity", Range(0.0001,0.5)) = 0.025
        [Toggle(_OLD_SCHOOL_SMOOTHING)] _UseOldSchoolSmoothing ("Use Old School Smoothing", Int) = 0
        [NoScaleOffset]_DMXTexture("DMX Grid Render Texture (To Control Lights)", 2D) = "white" {}
        _SmoothValue ("Smoothness Level (0 to 1, 0 = max)", Range(0,1)) = 0.5
        _MinimumSmoothnessDMX ("Minimum Smoothness Value for DMX", Float) = 0
        _MaximumSmoothnessDMX ("Maximum Smoothness Value for OSc", Float) = 0
        
     }

     SubShader
     {
        Lighting Off
        Blend One Zero
        Name "Interpolation Pass"
        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #include "UnityCG.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            #define VRSL_DMX

            half _SmoothValue, _MinimumSmoothnessDMX, _MaximumSmoothnessDMX, _SignalDetectionSensativity;
            sampler2D   _Tex;
            sampler2D _DMXTexture;
            SamplerState sampler_point_repeat;
            int _IsEven, _EnableDMX;
            uint _NineUniverseMode;
            half oscSmoothnessRAW;
            half3 rgbSmoothnessRaw;
            #define IF(a, b, c) lerp(b, c, step((fixed) (a), 0));

            #pragma multi_compile_local _ _OLD_SCHOOL_SMOOTHING
            #pragma multi_compile_local _ _SIGNAL_DETECTION



            half getValueAtUV(half2 uv)
            {
                half4 c = tex2D(_DMXTexture, uv);
                half3 cRGB = half3(c.r, c.g, c.b);
                half value = LinearRgbToLuminance(cRGB);

                return value;
            }

            half3 getValueAtUVRGB(half2 uv)
            {
                half4 c = tex2D(_DMXTexture, uv);
                return c.rgb;
            }

            half2 getCh13coords(half2 uv)
            {
                return half2(0.911, uv.y);
            }

            half getSmoothnessValuePreSet()
            {
                return _SmoothValue;
            }

            half3 getSmoothnessValueRGBPreSet()
            {
                return half3(_SmoothValue, _SmoothValue, _SmoothValue);
            }

            half getSmoothnessValue(half2 uv, out half dmxSmoothness)
            {
                dmxSmoothness = getValueAtUV(half2(0.960, uv.y));
                half oscSmoothness = lerp(saturate(_MaximumSmoothnessDMX), saturate(_MinimumSmoothnessDMX), dmxSmoothness);
                return oscSmoothness;
               // return IF(_EnableDMX == 1, oscSmoothness, _SmoothValue);  
            }


            half3 getSmoothnessValueRGB(half2 uv, out half3 dmxSmoothness)
            {
                dmxSmoothness = getValueAtUVRGB(half2(0.960, uv.y));

                half3 rgbSmoothness = half3(lerp( saturate(_MaximumSmoothnessDMX), saturate(_MinimumSmoothnessDMX), dmxSmoothness.r), 
                lerp(saturate(_MaximumSmoothnessDMX), saturate(_MinimumSmoothnessDMX), dmxSmoothness.g), 
                lerp(saturate(_MaximumSmoothnessDMX), saturate(_MinimumSmoothnessDMX), dmxSmoothness.b));
                return half3(0.001,0.001,0.001); // Happyrobot smoothness fix
                return rgbSmoothness;
               // return IF(_EnableDMX == 1, rgbSmoothness, half3(_SmoothValue, _SmoothValue, _SmoothValue));  
            }

            half SmoothDamp (half current, half target, inout half currentVelocity, half smoothTime, half maxSpeed, half deltaTime)
            {
                //smoothTime = 1-pow(smoothTime, deltaTime);

                half multiplier = target < current ? 0.4 : 1.0;
                smoothTime *= multiplier;
                smoothTime = max(0.0001, smoothTime);
                half num = 2.0 / smoothTime;
                half num2 = num * deltaTime;
                half num3 = 1.0 / (1.0 + num2 + 0.48 * num2 * num2 + 0.235 * num2 * num2 * num2);
                half num4 = current - target;
                half num5 = target;
                half num6 = maxSpeed * smoothTime;
                num4 = clamp(num4, -num6, num6);
                target = current - num4;
                half num7 = (currentVelocity + num * num4) * deltaTime;
                currentVelocity = (currentVelocity - num * num7) * num3;
                half num8 = target + (num4 + num7) * num3;
                if (num5 - current > 0.0 == num8 > num5)
                {
                    num8 = num5;
                    currentVelocity = (num8 - num5) / deltaTime;
                }
                return num8;
            }


            // half invLerp(half from, half to, half value){
            // return (value - from) / (to - from);
            // }

            // half remap(half origFrom, half origTo, half targetFrom, half targetTo, half value){
            // half rel = invLerp(origFrom, origTo, value);
            // return lerp(targetFrom, targetTo, rel);
            // }



            half DampComplex(half source, half target, half smoothing, half dt, half dmxMod)
            {
                // half multiplier = target < source ? 0.02-(0.01 * dmxMod) : 1.0;
                // smoothing *= multiplier;
                return lerp(source, target, 1 - pow(saturate(smoothing), dt));
            }

            half Damp(half source, half target, half smoothing, half dt)
            {
                return lerp(source, target, 1 - pow(saturate(smoothing), dt));
            }
            // half3 Damp(half3 source, half3 target, half smoothing, half dt)
            // {
            //     return lerp(source, target, 1 - pow(smoothing, dt))
            // }


            half4 frag(v2f_customrendertexture IN) : COLOR
            {
                if (_Time.y > 3.0)
                {
                    half4 previousFrame = tex2D(_SelfTexture2D, IN.localTexcoord.xy);
                    half4 currentFrame = tex2D(_DMXTexture, IN.localTexcoord.xy);

                    #if _SIGNAL_DETECTION
                        half2 textureSize = half2(_CustomRenderTextureWidth, _CustomRenderTextureHeight);
                       // half2 target1 = (half2(69,644)+0.5) * textureSize;
                        //half2 target2 = (half2(69,324)+0.5) * textureSize;
                      //  half2 target3 = (half2(69,3)+0.5) * textureSize;
                        half2 target = half2(0.658904,0.32868);
                        half2 target2 = half2(0.658904,0.662112);
                        half2 target3 = half2(0.658904,0.996333);
                        if( (all(tex2D(_DMXTexture, target).rgb >= half3(_SignalDetectionSensativity,_SignalDetectionSensativity,_SignalDetectionSensativity))) || 
                        (all(tex2D(_DMXTexture, target2).rgb >= half3(_SignalDetectionSensativity,_SignalDetectionSensativity,_SignalDetectionSensativity))) || 
                        (all(tex2D(_DMXTexture, target3).rgb >= half3(_SignalDetectionSensativity,_SignalDetectionSensativity,_SignalDetectionSensativity))))
                        {
                            return half4(0,0,0,0);
                        }
                    #endif
                    if(_NineUniverseMode && _EnableDMX)
                    {

                        half3 dmxSmoothness = half3(1.0, 1.0, 1.0);
                        #if _OLD_SCHOOL_SMOOTHING
                            half3 s = getSmoothnessValueRGB(IN.localTexcoord.xy, dmxSmoothness);
                            s = lerp(clamp(lerp(previousFrame.rgb, currentFrame.rgb,smoothstep(0.0, 1.0, clamp(unity_DeltaTime.z,0.0,100.0))) , 0.0, 100.0), currentFrame.rgb, s);
                        #else
                            
                            half3 smoothing = _EnableDMX == 1 ? getSmoothnessValueRGB(IN.localTexcoord.xy, dmxSmoothness) : getSmoothnessValueRGBPreSet() ;




                            half3 s = half3(
                                lerp(DampComplex(previousFrame.r, currentFrame.r, smoothing.r, unity_DeltaTime.x, dmxSmoothness.r), currentFrame.r, dmxSmoothness.r * 0.1),
                                lerp(DampComplex(previousFrame.g, currentFrame.g, smoothing.g, unity_DeltaTime.x, dmxSmoothness.g), currentFrame.g, dmxSmoothness.g * 0.1),
                                lerp(DampComplex(previousFrame.b, currentFrame.b, smoothing.b, unity_DeltaTime.x, dmxSmoothness.b), currentFrame.b, dmxSmoothness.b * 0.1)
                                                    );

                        #endif
                        
                        return half4(s, currentFrame.a); 
                    }
                    else
                    {
                        half dmxSmoothness = 1.0;
                        half smoothing = 0.0;
                        #if _OLD_SCHOOL_SMOOTHING
                         half smoothness = _EnableDMX == 1 ? getSmoothnessValue(IN.localTexcoord.xy, dmxSmoothness) : getSmoothnessValuePreSet();         
                            return lerp(clamp(lerp(previousFrame, currentFrame,smoothstep(0.0, 1.0, clamp(unity_DeltaTime.x,0.0,100.0))) , 0.0, 100.0), currentFrame, smoothness);
                        #else
                            //half currentVelocity = previousFrame.a;
                            if(_EnableDMX == 1)
                            {
                                smoothing = getSmoothnessValue(IN.localTexcoord.xy, dmxSmoothness);
                            }
                            else
                            {
                                smoothing = getSmoothnessValuePreSet();
                            }
                            return lerp(DampComplex(previousFrame.r, currentFrame.r, smoothing, unity_DeltaTime.x, dmxSmoothness), currentFrame.r , dmxSmoothness * 0.1);
                            //half output = SmoothDamp(previousFrame.r, currentFrame.r, currentVelocity, smoothing,100000.0,unity_DeltaTime.x);
                           // return (half4(output, output, output, currentVelocity));
                        #endif
                    }
                }
                else
                {
                    return tex2D(_DMXTexture, IN.localTexcoord.xy);
                }
            }
            ENDCG
         }
    }
    CustomEditor "VRSLInspector"
}

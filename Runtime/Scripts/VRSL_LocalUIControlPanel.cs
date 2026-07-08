using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
#if UDONSHARP
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCShader;
#else
using static UnityEngine.Shader;
using UnityEngine.Rendering;
#endif

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;

#if UDONSHARP
using UdonSharpEditor;
#endif
#endif

using TMPro;

namespace VRSL
{
    public enum VolumetricQualityModes
    {
        High,
        Medium,
        Low
    }
    public enum DefaultQualityModes
    {
        High,
        Low
    }

#if UDONSHARP
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRSL_LocalUIControlPanel : UdonSharpBehaviour
#else
    public class VRSL_LocalUIControlPanel : MonoBehaviour
#endif
    {
        [SerializeField, HideInInspector]
        private VRStageLighting_AudioLink_Laser[] audioLinkLasers;
        [SerializeField, HideInInspector]
        private VRStageLighting_AudioLink_Static[] audiolinkLights;
        [SerializeField, HideInInspector]
        private VRStageLighting_DMX_Static[] dmxLights;
        [Header("Quality Modes")]
        public VolumetricQualityModes volumetricQuality;
        public bool lockVolumetricQualityMode;
        [Space(5.0f)]
        public DefaultQualityModes blinderProjectionQuality;
        public bool lockBlinderProjectionQualityMode;
        [Space(5.0f)]

        public DefaultQualityModes parProjectionQuality;
        public bool lockParProjectionQualityMode;
        [Space(5.0f)]
        public DefaultQualityModes otherProjectionQuality;
        public bool lockOtherProjectionQualityMode;
        [Space(5.0f)]
        public DefaultQualityModes discoballQuality;
        public bool lockDiscoballQualityMode;
        [Space(5.0f)]
        public DefaultQualityModes lensFlareQuality;
        public bool lockLensFlareQualityMode;
        // [Space(20.0f)]
        [Header("Video Sampling")]
        public Texture videoSampleTargetTexture;

        [Header("Materials")]
        public Material[] fixtureMaterials;
        public Material[] volumetricMaterials;
        public Material[] projectionMaterials;
        public Material[] discoBallMaterials;
        public Material[] laserMaterials;
        [Space(5)]

        [Header("Post Processing")]
        public PostProcessVolume bloomPP;

        [Space(5)]
        [Header("UI Sliders")]
        public UnityEngine.UI.Slider masterSlider;
        public UnityEngine.UI.Slider fixtureSlider;
        public UnityEngine.UI.Slider volumetricSlider;
        public UnityEngine.UI.Slider projectionSlider;
        public UnityEngine.UI.Slider discoBallSlider;
        public UnityEngine.UI.Slider laserSlider;
        public UnityEngine.UI.Slider bloomSlider;
        public TMPro.TextMeshProUGUI masterSliderText, fixtureSliderText, volumetricSliderText, projectionSliderText, discoBallSliderText, laserSliderText, bloomSliderText;
        public float fixtureIntensityMax = 1.0f, volumetricIntensityMax = 1.0f, projectionIntensityMax = 1.0f, discoballIntensityMax = 1.0f, laserIntensityMax = 1.0f;

        public UnityEngine.UI.Toggle volumetricNoiseToggle;
        public UnityEngine.UI.Button volumetricHighButton, volumetricMedButton, volumetricLowButton;
        public TMPro.TextMeshProUGUI volumetricHighText, volumetricMedText, volumetricLowText;
        public UnityEngine.UI.Button blinderProjectionHighButton, blinderProjectionLowButton;
        public TMPro.TextMeshProUGUI blinderProjectionHighText, blinderProjectionLowText;

        public UnityEngine.UI.Button parProjectionHighButton, parProjectionLowButton;
        public TMPro.TextMeshProUGUI parProjectionHighText, parProjectionLowText;
        public UnityEngine.UI.Button otherProjectionHighButton, otherProjectionLowButton;
        public TMPro.TextMeshProUGUI otherProjectionHighText, otherProjectionLowText;

        public UnityEngine.UI.Button discoballHighButton, discoballLowButton;
        public TMPro.TextMeshProUGUI discoballHighText, discoballLowText;
        public UnityEngine.UI.Button lensFlareHighButton, lensFlareLowButton;
        public TMPro.TextMeshProUGUI lensFlareHighText, lensFlareLowText;

        public UnityEngine.UI.Button globalStrobeToggleButton;
        public TMPro.TextMeshProUGUI globalStrobeLabel;
        UnityEngine.UI.ColorBlock defaultColorBlock;
        UnityEngine.UI.ColorBlock cbOn;
        public bool isUsingDMX = true;
        public bool isUsingAudioLink = true;
        [Space(10)]
        [Header("0 = Horizontal Mode  1 = Vertical Mode  2 = Legacy Mode")]

        [Range(0, 2)]
        public int DMXMode;

        const int HORIZONTAL_MODE = 0;
        const int VERTICAL_MODE = 1;
        const int LEGACY_MODE = 2;
        [Space(20)]
        public bool delayStrobeForGI = true;
        [Space(5)]
        public CustomRenderTexture[] DMX_CRTS_Horizontal;
        public CustomRenderTexture[] DMX_CRTS_Vertical;
        public CustomRenderTexture[] DMX_CRTS_Legacy;
        public CustomRenderTexture[] AudioLink_CRTs;

        [HideInInspector]
        public int fixtureGizmos;

        [HideInInspector]
        public float panRangeTarget = 180f;
        [HideInInspector]
        public float tiltRangeTarget = -180f;

        [HideInInspector]
        public bool useLegacyStaticLights = false;
        public bool useExtendedUniverses = false;
        // public bool adjustInGameInterpolation;
        public bool sperateInGameInterpolationSpeed = true;
        public float inGameInterpolationModifier = 1.55f;
        public bool outputDebugLogs;

        [HideInInspector]
        public int volumetricMeshQuality;

        [HideInInspector]
        public string fixtureDefGUID = "4d88361aa1276d64d8a60009bfb590ed";

        [HideInInspector]
        public string fixtureSaveFile = "NONE";

        [HideInInspector]
        public bool useDMXGI = false;

        [SerializeField, FieldChangeCallback(nameof(VolumetricNoise))]
        private bool _volumetricNoise = true;
        int _Udon_DMXGridRenderTexture, _Udon_DMXGridRenderTextureMovement, _Udon_DMXGridSpinTimer, _Udon_DMXGridStrobeTimer, _Udon_DMXGridStrobeOutput;
        int _UseDepthLightID, _PotatoModeID, _DisableStrobeID, _NineUniverseModeID, _SamplingTextureID, _UniversalIntensityID;
        int _MaximumSmoothnessDMXID, _MinimumSmoothnessDMXID;
        bool _shaderIDsInitialized;
        CustomRenderTexture[] _strobeCRTs;
        CustomRenderTexture[] _interpolatedCRTsHorizontal;
        CustomRenderTexture[] _interpolatedCRTsVertical;
        CustomRenderTexture[] _interpolatedCRTsLegacy;
        Material[] _blinderProjectionMaterials;
        Material[] _parProjectionMaterials;
        Material[] _otherProjectionMaterials;
        Material[] _lensFlareMaterials;
        bool _materialCategoryCacheValid;
        const string KW_USE_DEPTH_LIGHT = "_USE_DEPTH_LIGHT";
        const string KW_POTATO_MODE_ON = "_POTATO_MODE_ON";
        const string KW_MAGIC_NOISE_ON_MED = "_MAGIC_NOISE_ON_MED";
        const string KW_MAGIC_NOISE_ON_HIGH = "_MAGIC_NOISE_ON_HIGH";
        const string KW_HQ_MODE = "_HQ_MODE";
        const string KW_ALPHATEST_ON = "_ALPHATEST_ON";

        public bool VolumetricNoise
        {
#if (UNITY_ANDROID || UNITY_IOS) && UDONSHARP
            set {
                _volumetricNoise = false;
                _ApplyVolumetricFogStatus();
            }
            get => false;
#else
            set
            {
                _volumetricNoise = value;
                _ApplyVolumetricFogStatus();
            }
            get => _volumetricNoise;
#endif
        }

        [SerializeField, FieldChangeCallback(nameof(RequireDepthLight))]
        private bool _requireDepthLight = true;

        public bool RequireDepthLight
        {
#if UNITY_ANDROID || UNITY_IOS
            set {
                _requireDepthLight = false;
                _ApplyDepthLightStatus();
                _DepthLightStatusReport();
            }
            get => false;
#else
            set
            {
                _requireDepthLight = value;
                _ApplyDepthLightStatus();
                _DepthLightStatusReport();
            }
            get => _requireDepthLight;
#endif
        }

        [SerializeField, FieldChangeCallback(nameof(GlobalDisableStrobe))]
        private bool _globalDisableStrobe = false;

        public bool GlobalDisableStrobe
        {
            set
            {
                _globalDisableStrobe = value;
                SetStrobeTextureStatus();
            }
            get => _globalDisableStrobe;
        }

        public float _targetCRTUpdateRate = 0.0f;

        public void _ToggleGlobalStrobe()
        {
            GlobalDisableStrobe = !GlobalDisableStrobe;
        }

        void SetGlobalStrobeUI()
        {

            if (globalStrobeToggleButton)
            {
                globalStrobeToggleButton.colors = GlobalDisableStrobe ? cbOn : defaultColorBlock;
                globalStrobeToggleButton.gameObject.SetActive(isUsingDMX);
            }

        }

        void SetStrobeTextureStatus()
        {
            _EnsureShaderIDs();
            if (_strobeCRTs == null)
            {
                _RebuildCRTCaches();
            }
            if (_strobeCRTs != null)
            {
                foreach (CustomRenderTexture rt in _strobeCRTs)
                {
                    if (rt == null || rt.material == null) { continue; }
                    if (rt.material.HasProperty(_DisableStrobeID))
                    {
                        rt.material.SetFloat(_DisableStrobeID, GlobalDisableStrobe ? 1f : 0f);
                    }
                }
            }
            SetGlobalStrobeUI();
        }


        void _SetTextureIDS()
        {
            _Udon_DMXGridRenderTexture = PropertyToID("_Udon_DMXGridRenderTexture");
            _Udon_DMXGridRenderTextureMovement = PropertyToID("_Udon_DMXGridRenderTextureMovement");
            _Udon_DMXGridSpinTimer = PropertyToID("_Udon_DMXGridSpinTimer");
            _Udon_DMXGridStrobeTimer = PropertyToID("_Udon_DMXGridStrobeTimer");
            _Udon_DMXGridStrobeOutput = PropertyToID("_Udon_DMXGridStrobeOutput");
            _UseDepthLightID = PropertyToID("_UseDepthLight");
            _PotatoModeID = PropertyToID("_PotatoMode");
            _DisableStrobeID = PropertyToID("_DisableStrobe");
            _NineUniverseModeID = PropertyToID("_NineUniverseMode");
            _SamplingTextureID = PropertyToID("_SamplingTexture");
            _UniversalIntensityID = PropertyToID("_UniversalIntensity");
            _MaximumSmoothnessDMXID = PropertyToID("_MaximumSmoothnessDMX");
            _MinimumSmoothnessDMXID = PropertyToID("_MinimumSmoothnessDMX");
            _shaderIDsInitialized = true;
        }

        void _EnsureShaderIDs()
        {
            if (!_shaderIDsInitialized)
            {
                _SetTextureIDS();
            }
        }

        void _ApplySamplingTextureToMaterials(Material[] mats)
        {
            if (mats == null) { return; }
            _EnsureShaderIDs();
            foreach (Material m in mats)
            {
                if (m != null && m.HasProperty(_SamplingTextureID))
                {
                    m.SetTexture(_SamplingTextureID, videoSampleTargetTexture);
                }
            }
        }

        void _ApplyIntensityToMaterials(Material[] mats, float maxIntensity, float sliderValue)
        {
            if (mats == null) { return; }
            _EnsureShaderIDs();
            float intensity = Mathf.Lerp(0.0f, maxIntensity, sliderValue);
            foreach (Material mat in mats)
            {
                if (mat != null)
                {
                    mat.SetFloat(_UniversalIntensityID, intensity);
                }
            }
        }

        void _ApplyDepthToMaterials(Material[] mats, int depth, bool syncKeyword)
        {
            if (mats == null) { return; }
            _EnsureShaderIDs();
            foreach (Material mat in mats)
            {
                if (mat == null) { continue; }
                mat.SetInt(_UseDepthLightID, depth);
                if (syncKeyword && mat.HasProperty(_UseDepthLightID))
                {
                    SetKeyword(mat, KW_USE_DEPTH_LIGHT, depth == 1);
                }
            }
        }

        CustomRenderTexture[] _CollectNamedCRTs(CustomRenderTexture[] source, string nameFragment)
        {
            if (source == null) { return new CustomRenderTexture[0]; }
            int count = 0;
            foreach (CustomRenderTexture rt in source)
            {
                if (rt != null && rt.name.Contains(nameFragment))
                {
                    count++;
                }
            }
            CustomRenderTexture[] result = new CustomRenderTexture[count];
            int idx = 0;
            foreach (CustomRenderTexture rt in source)
            {
                if (rt != null && rt.name.Contains(nameFragment))
                {
                    result[idx] = rt;
                    idx++;
                }
            }
            return result;
        }

        CustomRenderTexture[] _MergeCRTArrays(CustomRenderTexture[] a, CustomRenderTexture[] b, CustomRenderTexture[] c)
        {
            int aLen = a != null ? a.Length : 0;
            int bLen = b != null ? b.Length : 0;
            int cLen = c != null ? c.Length : 0;
            CustomRenderTexture[] merged = new CustomRenderTexture[aLen + bLen + cLen];
            int idx = 0;
            if (a != null)
            {
                foreach (CustomRenderTexture item in a)
                {
                    merged[idx] = item;
                    idx++;
                }
            }
            if (b != null)
            {
                foreach (CustomRenderTexture item in b)
                {
                    merged[idx] = item;
                    idx++;
                }
            }
            if (c != null)
            {
                foreach (CustomRenderTexture item in c)
                {
                    merged[idx] = item;
                    idx++;
                }
            }
            return merged;
        }

        void _RebuildCRTCaches()
        {
            _strobeCRTs = _MergeCRTArrays(
                _CollectNamedCRTs(DMX_CRTS_Legacy, "Strobe"),
                _CollectNamedCRTs(DMX_CRTS_Horizontal, "Strobe"),
                _CollectNamedCRTs(DMX_CRTS_Vertical, "Strobe")
            );
            _interpolatedCRTsHorizontal = _CollectNamedCRTs(DMX_CRTS_Horizontal, "Interpolated");
            _interpolatedCRTsVertical = _CollectNamedCRTs(DMX_CRTS_Vertical, "Interpolated");
            _interpolatedCRTsLegacy = _CollectNamedCRTs(DMX_CRTS_Legacy, "Interpolated");
        }

        Material[] _CollectNamedMaterials(Material[] source, string nameFragment, bool include)
        {
            if (source == null) { return new Material[0]; }
            int count = 0;
            foreach (Material mat in source)
            {
                if (mat == null) { continue; }
                bool has = mat.name.Contains(nameFragment);
                if ((include && has) || (!include && !has))
                {
                    count++;
                }
            }
            Material[] result = new Material[count];
            int idx = 0;
            foreach (Material mat in source)
            {
                if (mat == null) { continue; }
                bool has = mat.name.Contains(nameFragment);
                if ((include && has) || (!include && !has))
                {
                    result[idx] = mat;
                    idx++;
                }
            }
            return result;
        }

        void _RebuildMaterialCategoryCaches()
        {
            _blinderProjectionMaterials = _CollectNamedMaterials(projectionMaterials, "Blinder", true);
            _parProjectionMaterials = _CollectNamedMaterials(projectionMaterials, "Par", true);
            if (projectionMaterials == null)
            {
                _otherProjectionMaterials = new Material[0];
            }
            else
            {
                int count = 0;
                foreach (Material mat in projectionMaterials)
                {
                    if (mat == null) { continue; }
                    if (!mat.name.Contains("Par") && !mat.name.Contains("Blinder")) { count++; }
                }
                _otherProjectionMaterials = new Material[count];
                int idx = 0;
                foreach (Material mat in projectionMaterials)
                {
                    if (mat == null) { continue; }
                    if (!mat.name.Contains("Par") && !mat.name.Contains("Blinder"))
                    {
                        _otherProjectionMaterials[idx] = mat;
                        idx++;
                    }
                }
            }
            _lensFlareMaterials = _CollectNamedMaterials(fixtureMaterials, "Flare", true);
            _materialCategoryCacheValid = true;
        }

        void _EnsureMaterialCategoryCaches()
        {
            if (!_materialCategoryCacheValid)
            {
                _RebuildMaterialCategoryCaches();
            }
        }

        void _SetCommonMaterialQuality(Material target, bool highQuality, int transparentQueue, int opaqueQueue)
        {
            target.SetOverrideTag("RenderType", highQuality ? "Transparent" : "Opaque");
            SetKeyword(target, KW_ALPHATEST_ON, !highQuality);
            target.SetInt("_BlendDst", highQuality ? 1 : 0);
            target.SetInt("_ZWrite", highQuality ? 0 : 1);
            target.SetInt("_AlphaToCoverage", highQuality ? 0 : 1);
            target.SetInt("_RenderMode", highQuality ? 1 : 2);
            target.renderQueue = highQuality ? transparentQueue : opaqueQueue;
        }

        void _SyncVolumetricKeywords(Material target)
        {
            if (target.HasProperty(KW_MAGIC_NOISE_ON_MED))
            {
                SetKeyword(target, KW_MAGIC_NOISE_ON_MED, (Mathf.FloorToInt(target.GetInt(KW_MAGIC_NOISE_ON_MED))) == 1);
            }
            if (target.HasProperty(KW_MAGIC_NOISE_ON_HIGH))
            {
                SetKeyword(target, KW_MAGIC_NOISE_ON_HIGH, (Mathf.FloorToInt(target.GetInt(KW_MAGIC_NOISE_ON_HIGH))) == 1);
            }
            if (target.HasProperty(_UseDepthLightID))
            {
                SetKeyword(target, KW_USE_DEPTH_LIGHT, (Mathf.FloorToInt(target.GetInt(_UseDepthLightID))) == 1);
            }
            if (target.HasProperty(_PotatoModeID))
            {
                SetKeyword(target, KW_POTATO_MODE_ON, (Mathf.FloorToInt(target.GetInt(_PotatoModeID))) == 1);
            }
            if (target.HasProperty("_HQMode"))
            {
                SetKeyword(target, KW_HQ_MODE, (Mathf.FloorToInt(target.GetInt("_HQMode"))) == 1);
            }
        }

        void ReduceInGameInterpolation()
        {
            if (sperateInGameInterpolationSpeed)
            {
                _EnsureShaderIDs();
                if (_interpolatedCRTsHorizontal == null || _interpolatedCRTsVertical == null || _interpolatedCRTsLegacy == null)
                {
                    _RebuildCRTCaches();
                }
                if (_interpolatedCRTsHorizontal != null)
                {
                    foreach (CustomRenderTexture rend in _interpolatedCRTsHorizontal)
                    {
                        if (rend != null && rend.material != null)
                        {
                            float max = Mathf.Clamp01(rend.material.GetFloat(_MaximumSmoothnessDMXID));
                            float min = Mathf.Clamp01(rend.material.GetFloat(_MinimumSmoothnessDMXID));
                            rend.material.SetFloat(_MaximumSmoothnessDMXID, Mathf.Clamp01(max / inGameInterpolationModifier));
                            rend.material.SetFloat(_MinimumSmoothnessDMXID, Mathf.Clamp01(min / inGameInterpolationModifier));
                        }
                    }
                }
                if (_interpolatedCRTsVertical != null)
                {
                    foreach (CustomRenderTexture rend in _interpolatedCRTsVertical)
                    {
                        if (rend != null && rend.material != null)
                        {
                            float max = Mathf.Clamp01(rend.material.GetFloat(_MaximumSmoothnessDMXID));
                            float min = Mathf.Clamp01(rend.material.GetFloat(_MinimumSmoothnessDMXID));
                            rend.material.SetFloat(_MaximumSmoothnessDMXID, Mathf.Clamp01(max / inGameInterpolationModifier));
                            rend.material.SetFloat(_MinimumSmoothnessDMXID, Mathf.Clamp01(min / inGameInterpolationModifier));
                        }
                    }
                }
                if (_interpolatedCRTsLegacy != null)
                {
                    foreach (CustomRenderTexture rend in _interpolatedCRTsLegacy)
                    {
                        if (rend != null && rend.material != null)
                        {
                            float max = Mathf.Clamp01(rend.material.GetFloat(_MaximumSmoothnessDMXID));
                            float min = Mathf.Clamp01(rend.material.GetFloat(_MinimumSmoothnessDMXID));
                            rend.material.SetFloat(_MaximumSmoothnessDMXID, Mathf.Clamp01(max / inGameInterpolationModifier));
                            rend.material.SetFloat(_MinimumSmoothnessDMXID, Mathf.Clamp01(min / inGameInterpolationModifier));
                        }
                    }
                }
            }
        }


        public void OnEnable()
        {
            _CheckDepthLightStatus();
        }
        void Start()
        {
            if (volumetricHighButton)
            {
                defaultColorBlock = volumetricHighButton.colors;
                cbOn = defaultColorBlock;
                cbOn.normalColor = new Color(cbOn.normalColor.r + 0.35f, cbOn.normalColor.r + 0.35f, cbOn.normalColor.g + 0.35f, 1.0f);
            }
            _SetTextureIDS();
            _RebuildCRTCaches();
            _RebuildMaterialCategoryCaches();
            _CheckDepthLightStatus();
            _SetFinalIntensity();
            _SetFixtureIntensity();
            _SetVolumetricIntensity();
            _SetProjectionIntensity();
            _SetDiscoBallIntensity();
            _SetBloomIntensity();
            _CheckDMX();
            _CheckAudioLink();
            _CheckkExtendedUniverses();
            _ForceUpdateVideoSampleTexture();
            /*  _SetVolumetricQualityMode();
             _SetBlinderProjectionQualityMode();
             _SetParProjectionQualityMode();
             _SetOtherProjectionQualityMode();
             _SetDiscoBallQualityMode();
             _SetLensFlareQualtiyMode(); */
            _CheckButtonLockStatus();

#if !UNITY_EDITOR
                ReduceInGameInterpolation();
#endif
            SetStrobeTextureStatus();
        }

        void _CheckButtonLockStatus()
        {
            Color disableColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
            Color disableButEnabledColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
            Color disabledTextColor = new Color(1.0f, 1.0f, 1.0f, 0.045f);
            if (lockVolumetricQualityMode)
            {
                if (volumetricHighButton)
                {
                    volumetricHighButton.image.color = volumetricQuality == VolumetricQualityModes.High ? disableButEnabledColor : disableColor;
                    volumetricHighButton.interactable = false;
                }
                if (volumetricMedButton)
                {
                    volumetricMedButton.image.color = volumetricQuality == VolumetricQualityModes.Medium ? disableButEnabledColor : disableColor;
                    volumetricMedButton.interactable = false;
                }
                if (volumetricLowButton)
                {
                    volumetricLowButton.image.color = volumetricQuality == VolumetricQualityModes.Low ? disableButEnabledColor : disableColor;
                    volumetricLowButton.interactable = false;
                }
                if (volumetricHighText) { volumetricHighText.color = disabledTextColor; }
                if (volumetricMedText) { volumetricMedText.color = disabledTextColor; }
                if (volumetricLowText) { volumetricLowText.color = disabledTextColor; }
            }
            if (lockBlinderProjectionQualityMode)
            {
                if (blinderProjectionHighButton)
                {
                    blinderProjectionHighButton.image.color = blinderProjectionQuality == DefaultQualityModes.High ? disableButEnabledColor : disableColor;
                    blinderProjectionHighButton.interactable = false;
                }
                if (blinderProjectionLowButton)
                {
                    blinderProjectionLowButton.image.color = blinderProjectionQuality == DefaultQualityModes.Low ? disableButEnabledColor : disableColor;
                    blinderProjectionLowButton.interactable = false;
                }
                if (blinderProjectionHighText) { blinderProjectionHighText.color = disabledTextColor; }
                if (blinderProjectionLowText) { blinderProjectionLowText.color = disabledTextColor; }
            }
            if (lockLensFlareQualityMode)
            {
                if (lensFlareHighButton)
                {
                    lensFlareHighButton.image.color = lensFlareQuality == DefaultQualityModes.High ? disableButEnabledColor : disableColor;
                    lensFlareHighButton.interactable = false;
                }
                if (lensFlareLowButton)
                {
                    lensFlareLowButton.image.color = lensFlareQuality == DefaultQualityModes.Low ? disableButEnabledColor : disableColor;
                    lensFlareLowButton.interactable = false;
                }
                if (lensFlareHighText) { lensFlareHighText.color = disabledTextColor; }
                if (lensFlareLowText) { lensFlareLowText.color = disabledTextColor; }
            }
            if (lockParProjectionQualityMode)
            {
                if (parProjectionHighButton)
                {
                    parProjectionHighButton.image.color = parProjectionQuality == DefaultQualityModes.High ? disableButEnabledColor : disableColor;
                    parProjectionHighButton.interactable = false;
                }
                if (parProjectionLowButton)
                {
                    parProjectionLowButton.image.color = parProjectionQuality == DefaultQualityModes.Low ? disableButEnabledColor : disableColor;
                    parProjectionLowButton.interactable = false;
                }
                if (parProjectionHighText) { parProjectionHighText.color = disabledTextColor; }
                if (parProjectionLowText) { parProjectionLowText.color = disabledTextColor; }
            }
            if (lockOtherProjectionQualityMode)
            {
                if (otherProjectionHighButton)
                {
                    otherProjectionHighButton.image.color = otherProjectionQuality == DefaultQualityModes.High ? disableButEnabledColor : disableColor;
                    otherProjectionHighButton.interactable = false;
                }
                if (otherProjectionLowButton)
                {
                    otherProjectionLowButton.image.color = otherProjectionQuality == DefaultQualityModes.Low ? disableButEnabledColor : disableColor;
                    otherProjectionLowButton.interactable = false;
                }
                if (otherProjectionHighText) { otherProjectionHighText.color = disabledTextColor; }
                if (otherProjectionLowText) { otherProjectionLowText.color = disabledTextColor; }
            }
            if (lockDiscoballQualityMode)
            {
                if (discoballHighButton)
                {
                    discoballHighButton.image.color = discoballQuality == DefaultQualityModes.High ? disableButEnabledColor : disableColor;
                    discoballHighButton.interactable = false;
                }
                if (discoballLowButton)
                {
                    discoballLowButton.image.color = discoballQuality == DefaultQualityModes.Low ? disableButEnabledColor : disableColor;
                    discoballLowButton.interactable = false;
                }
                if (discoballHighText) { discoballHighText.color = disabledTextColor; }
                if (discoballLowText) { discoballLowText.color = disabledTextColor; }
            }
        }

        public void _Test()
        {
            if (outputDebugLogs)
                Debug.Log("This is a test");
        }


        public void _SetVolumetricHigh()
        {
            if (lockVolumetricQualityMode) { return; }
            volumetricQuality = VolumetricQualityModes.High;
            _SetVolumetricQualityMode();
        }
        public void _SetVolumetricMed()
        {
            if (lockVolumetricQualityMode) { return; }
            volumetricQuality = VolumetricQualityModes.Medium;
            _SetVolumetricQualityMode();
        }
        public void _SetVolumetricLow()
        {
            if (lockVolumetricQualityMode) { return; }
            volumetricQuality = VolumetricQualityModes.Low;
            _SetVolumetricQualityMode();
        }
        public void _SetProjectionBlindersHigh()
        {
            if (lockBlinderProjectionQualityMode) { return; }
            blinderProjectionQuality = DefaultQualityModes.High;
            _SetBlinderProjectionQualityMode();
        }
        public void _SetProjectionBlindersLow()
        {
            if (lockBlinderProjectionQualityMode) { return; }
            blinderProjectionQuality = DefaultQualityModes.Low;
            _SetBlinderProjectionQualityMode();
        }
        public void _SetProjectionParsHigh()
        {
            if (lockParProjectionQualityMode) { return; }
            parProjectionQuality = DefaultQualityModes.High;
            _SetParProjectionQualityMode();
        }
        public void _SetProjectionParsLow()
        {
            if (lockParProjectionQualityMode) { return; }
            parProjectionQuality = DefaultQualityModes.Low;
            _SetParProjectionQualityMode();
        }
        public void _SetProjectionOtherHigh()
        {
            if (lockOtherProjectionQualityMode) { return; }
            otherProjectionQuality = DefaultQualityModes.High;
            _SetOtherProjectionQualityMode();
        }
        public void _SetProjectionOtherLow()
        {
            if (lockOtherProjectionQualityMode) { return; }
            otherProjectionQuality = DefaultQualityModes.Low;
            _SetOtherProjectionQualityMode();
        }
        public void _SetDiscoballHigh()
        {
            if (lockDiscoballQualityMode) { return; }
            discoballQuality = DefaultQualityModes.High;
            _SetDiscoBallQualityMode();
        }
        public void _SetDiscoballLow()
        {
            if (lockDiscoballQualityMode) { return; }
            discoballQuality = DefaultQualityModes.Low;
            _SetDiscoBallQualityMode();
        }
        public void _SetLensFlareHigh()
        {
            if (lockLensFlareQualityMode) { return; }
            lensFlareQuality = DefaultQualityModes.High;
            _SetLensFlareQualtiyMode();
        }
        public void _SetLensFlareLow()
        {
            if (lockLensFlareQualityMode) { return; }
            lensFlareQuality = DefaultQualityModes.Low;
            _SetLensFlareQualtiyMode();
        }
        public void _UpdateAllQualityModes()
        {
            _SetDiscoBallQualityMode();
            _SetVolumetricQualityMode();
            _SetParProjectionQualityMode();
            _SetOtherProjectionQualityMode();
            _SetBlinderProjectionQualityMode();
            _SetLensFlareQualtiyMode();
        }
        public void _SetVolumetricQualityMode()
        {
            switch (volumetricQuality)
            {
                case VolumetricQualityModes.High:
                    if (volumetricHighButton) { volumetricHighButton.colors = cbOn; }
                    if (volumetricMedButton) { volumetricMedButton.colors = defaultColorBlock; }
                    if (volumetricLowButton) { volumetricLowButton.colors = defaultColorBlock; }
                    break;
                case VolumetricQualityModes.Medium:
                    if (volumetricHighButton) { volumetricHighButton.colors = defaultColorBlock; }
                    if (volumetricMedButton) { volumetricMedButton.colors = cbOn; }
                    if (volumetricLowButton) { volumetricLowButton.colors = defaultColorBlock; }
                    break;
                case VolumetricQualityModes.Low:
                    if (volumetricHighButton) { volumetricHighButton.colors = defaultColorBlock; }
                    if (volumetricMedButton) { volumetricMedButton.colors = defaultColorBlock; }
                    if (volumetricLowButton) { volumetricLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetVolumetricQuality();
        }
        public void _SetBlinderProjectionQualityMode()
        {

            switch (blinderProjectionQuality)
            {
                case DefaultQualityModes.High:
                    if (blinderProjectionHighButton) { blinderProjectionHighButton.colors = cbOn; }
                    if (blinderProjectionLowButton) { blinderProjectionLowButton.colors = defaultColorBlock; }
                    break;
                case DefaultQualityModes.Low:
                    if (blinderProjectionHighButton) { blinderProjectionHighButton.colors = defaultColorBlock; }
                    if (blinderProjectionLowButton) { blinderProjectionLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetBlinderProjectionQuality();
        }

        public void _SetParProjectionQualityMode()
        {

            switch (parProjectionQuality)
            {
                case DefaultQualityModes.High:
                    if (parProjectionHighButton) { parProjectionHighButton.colors = cbOn; }
                    if (parProjectionLowButton) { parProjectionLowButton.colors = defaultColorBlock; }
                    break;
                case DefaultQualityModes.Low:
                    if (parProjectionHighButton) { parProjectionHighButton.colors = defaultColorBlock; }
                    if (parProjectionLowButton) { parProjectionLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetParProjectionQuality();
        }
        public void _SetOtherProjectionQualityMode()
        {

            switch (otherProjectionQuality)
            {
                case DefaultQualityModes.High:
                    if (otherProjectionHighButton) { otherProjectionHighButton.colors = cbOn; }
                    if (otherProjectionLowButton) { otherProjectionLowButton.colors = defaultColorBlock; }
                    break;
                case DefaultQualityModes.Low:
                    if (otherProjectionHighButton) { otherProjectionHighButton.colors = defaultColorBlock; }
                    if (otherProjectionLowButton) { otherProjectionLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetOtherProjectionQuality();
        }
        public void _SetDiscoBallQualityMode()
        {

            switch (discoballQuality)
            {
                case DefaultQualityModes.High:
                    if (discoballHighButton) { discoballHighButton.colors = cbOn; }
                    if (discoballLowButton) { discoballLowButton.colors = defaultColorBlock; }
                    break;
                case DefaultQualityModes.Low:
                    if (discoballHighButton) { discoballHighButton.colors = defaultColorBlock; }
                    if (discoballLowButton) { discoballLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetDiscoballQuality();
        }
        public void _SetLensFlareQualtiyMode()
        {
            switch (lensFlareQuality)
            {
                case DefaultQualityModes.High:
                    if (lensFlareHighButton) { lensFlareHighButton.colors = cbOn; }
                    if (lensFlareLowButton) { lensFlareLowButton.colors = defaultColorBlock; }
                    break;
                case DefaultQualityModes.Low:
                    if (lensFlareHighButton) { lensFlareHighButton.colors = defaultColorBlock; }
                    if (lensFlareLowButton) { lensFlareLowButton.colors = cbOn; }
                    break;
                default:
                    break;
            }
            SetLensFlareQuality();
        }
        public void _CheckDepthLightStatus()
        {
            _ApplyDepthLightStatus();
            //_ApplyVolumetricFogStatus();
        }

        public void _ApplyDepthLightStatus()
        {
            int depth = RequireDepthLight ? 1 : 0;
            _ApplyDepthToMaterials(volumetricMaterials, depth, true);
            _ApplyDepthToMaterials(projectionMaterials, depth, false);
            _ApplyDepthToMaterials(fixtureMaterials, depth, true);
        }

        public void _ApplyVolumetricFogStatus()
        {
            int potato = VolumetricNoise ? 0 : 1;
            _EnsureShaderIDs();
            if (volumetricMaterials != null)
            {
                foreach (Material mat in volumetricMaterials)
                {
                    if (mat == null) { continue; }
                    mat.SetInt(_PotatoModeID, potato);
                    if (mat.HasProperty(KW_MAGIC_NOISE_ON_MED))
                    {
                        SetKeyword(mat, KW_MAGIC_NOISE_ON_MED, (Mathf.FloorToInt(mat.GetInt(KW_MAGIC_NOISE_ON_MED))) == 1);
                    }
                    if (mat.HasProperty(KW_MAGIC_NOISE_ON_HIGH))
                    {
                        SetKeyword(mat, KW_MAGIC_NOISE_ON_HIGH, (Mathf.FloorToInt(mat.GetInt(KW_MAGIC_NOISE_ON_HIGH))) == 1);
                    }
                    if (mat.HasProperty(_PotatoModeID))
                    {
                        SetKeyword(mat, KW_POTATO_MODE_ON, potato == 1);
                    }
                }
            }
        }
        void _DepthLightStatusReport()
        {
            // if(_requireDepthLight)
            // {
            //     Debug.Log("VRSL Control Panel: Enabling Depth Light Requirement");
            // }
            // else
            // {
            //     Debug.Log("VRSL Control Panel: Disabling Depth Light Requirement");
            // }
        }

        void EnableCRTS(CustomRenderTexture[] rtArray)
        {
            if (rtArray == null) { return; }
            foreach (CustomRenderTexture rt in rtArray)
            {
                if (rt == null) { continue; }
                rt.updateMode = CustomRenderTextureUpdateMode.Realtime;
#if UNITY_2022
                rt.updatePeriod = _targetCRTUpdateRate;
#endif
                if (rt.name.Contains("Color"))
                {
                    if (outputDebugLogs)
                    {
                        Debug.Log("DMX Color: " + rt.name);
                    }
#if UDONSHARP
                    VRCShader.SetGlobalTexture(_Udon_DMXGridRenderTexture, rt);
#else
                    Shader.SetGlobalTexture(_Udon_DMXGridRenderTexture, rt, RenderTextureSubElement.Default);
#endif
                }
                else if (rt.name.Contains("Movement"))
                {
                    if (outputDebugLogs)
                    {
                        Debug.Log("DMX Movement: " + rt.name);
                    }
#if UDONSHARP
                    VRCShader.SetGlobalTexture(_Udon_DMXGridRenderTextureMovement, rt);
#else
                    Shader.SetGlobalTexture(_Udon_DMXGridRenderTextureMovement, rt, RenderTextureSubElement.Default);
#endif
                }
                else if (rt.name.Contains("Spin"))
                {
                    if (outputDebugLogs)
                    {
                        Debug.Log("DMX Spin Timings: " + rt.name);
                    }
#if UDONSHARP
                    VRCShader.SetGlobalTexture(_Udon_DMXGridSpinTimer, rt);
#else
                    Shader.SetGlobalTexture(_Udon_DMXGridSpinTimer, rt, RenderTextureSubElement.Default);
#endif
                }
                else if (rt.name.Contains("Strobe"))
                {
                    if (rt.name.Contains("Timings"))
                    {
                        if (outputDebugLogs)
                        {
                            Debug.Log("DMX Strobe Timings: " + rt.name);
                        }
#if UDONSHARP
                        VRCShader.SetGlobalTexture(_Udon_DMXGridStrobeTimer, rt);
#else
                        Shader.SetGlobalTexture(_Udon_DMXGridStrobeTimer, rt, RenderTextureSubElement.Default);
#endif
                    }
                    else
                    {
                        //Debug.Log("Setting Strobe Output");
                        if (delayStrobeForGI)
                        {
                            if (rt.name.Contains("Delay-Final") && DMXMode != LEGACY_MODE)
                            {
#if UDONSHARP
                                if (outputDebugLogs)
                                {
                                    Debug.Log("DMX Strobe Output: " + rt.name);
                                }
                                VRCShader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt);
#else
                                Shader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt, RenderTextureSubElement.Default);
#endif
                            }
                            else if (DMXMode == LEGACY_MODE)
                            {
#if UDONSHARP
                                if (outputDebugLogs)
                                {
                                    Debug.Log("DMX Strobe Output: " + rt.name);
                                }
                                VRCShader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt);
#else
                                Shader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt, RenderTextureSubElement.Default);
#endif
                            }
                        }
                        else
                        {
                            if (rt.name.Contains("Delay") == false)
                            {
#if UDONSHARP
                                if (outputDebugLogs)
                                {
                                    Debug.Log("Strobe Output: " + rt.name);
                                }
                                VRCShader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt);
#else
                                Shader.SetGlobalTexture(_Udon_DMXGridStrobeOutput, rt, RenderTextureSubElement.Default);
#endif
                            }
                        }
                    }
                }
            }
        }
        void DisableCRTS(CustomRenderTexture[] rtArray)
        {
            if (rtArray == null) { return; }
            foreach (CustomRenderTexture rt in rtArray)
            {
                if (rt == null) { continue; }
                rt.updateMode = CustomRenderTextureUpdateMode.OnDemand;
            }
        }
        public void _CheckDMX()
        {
            if (isUsingDMX)
            {
                switch (DMXMode)
                {
                    case HORIZONTAL_MODE:
                        EnableCRTS(DMX_CRTS_Horizontal);
                        DisableCRTS(DMX_CRTS_Vertical);
                        DisableCRTS(DMX_CRTS_Legacy);
                        break;
                    case VERTICAL_MODE:
                        DisableCRTS(DMX_CRTS_Horizontal);
                        EnableCRTS(DMX_CRTS_Vertical);
                        DisableCRTS(DMX_CRTS_Legacy);
                        break;
                    case LEGACY_MODE:
                        DisableCRTS(DMX_CRTS_Horizontal);
                        DisableCRTS(DMX_CRTS_Vertical);
                        EnableCRTS(DMX_CRTS_Legacy);
                        break;
                    default:
                        DisableCRTS(DMX_CRTS_Horizontal);
                        DisableCRTS(DMX_CRTS_Vertical);
                        DisableCRTS(DMX_CRTS_Legacy);
                        break;
                }
            }
            else
            {
                DisableCRTS(DMX_CRTS_Horizontal);
                DisableCRTS(DMX_CRTS_Vertical);
                DisableCRTS(DMX_CRTS_Legacy);
            }
        }

        public void _SetDMXHorizontal()
        {
            if (isUsingDMX)
            {
                DMXMode = HORIZONTAL_MODE;
                _CheckDMX();
            }
        }
        public void _SetDMXVertical()
        {
            if (isUsingDMX)
            {
                DMXMode = VERTICAL_MODE;
                _CheckDMX();
            }
        }
        public void _SetDMXLegacy()
        {
            if (isUsingDMX)
            {
                DMXMode = LEGACY_MODE;
                _CheckDMX();
            }
        }


        public void _CheckAudioLink()
        {
            if (isUsingAudioLink)
            {
                EnableCRTS(AudioLink_CRTs);
            }
            else
            {
                DisableCRTS(AudioLink_CRTs);
            }
        }

        public void _CheckkExtendedUniverses()
        {
            _EnsureShaderIDs();
            if (DMX_CRTS_Horizontal != null)
            {
                foreach (CustomRenderTexture crt in DMX_CRTS_Horizontal)
                {
                    if (crt == null || crt.material == null || !crt.material.HasProperty(_NineUniverseModeID)) { continue; }
                    crt.material.SetInt(_NineUniverseModeID, useExtendedUniverses ? 1 : 0);
                }
            }
            if (DMX_CRTS_Vertical != null)
            {
                foreach (CustomRenderTexture crt in DMX_CRTS_Vertical)
                {
                    if (crt == null || crt.material == null || !crt.material.HasProperty(_NineUniverseModeID)) { continue; }
                    crt.material.SetInt(_NineUniverseModeID, useExtendedUniverses ? 1 : 0);
                }
            }
        }

        public void _ForceUpdateVideoSampleTexture()
        {
            if (videoSampleTargetTexture == null)
            {
                return;
            }
            _ApplySamplingTextureToMaterials(laserMaterials);
            _ApplySamplingTextureToMaterials(fixtureMaterials);
            _ApplySamplingTextureToMaterials(discoBallMaterials);
            _ApplySamplingTextureToMaterials(projectionMaterials);
            _ApplySamplingTextureToMaterials(volumetricMaterials);
        }

        public void _SetFinalIntensity()
        {
            float masterValue = masterSlider != null ? masterSlider.value : 1.0f;
            fixtureIntensityMax = masterValue;
            volumetricIntensityMax = masterValue;
            projectionIntensityMax = masterValue;
            discoballIntensityMax = masterValue;
            laserIntensityMax = masterValue;
            _SetFixtureIntensity();
            _SetVolumetricIntensity();
            _SetProjectionIntensity();
            _SetDiscoBallIntensity();
            _SetLaserIntensity();
            if (masterSliderText != null)
            {
                masterSliderText.text = Mathf.Round(masterValue * 100.0f).ToString();
            }
        }

        public void _SetFixtureIntensity()
        {
            float sliderValue = fixtureSlider != null ? fixtureSlider.value : 1.0f;
            _ApplyIntensityToMaterials(fixtureMaterials, fixtureIntensityMax, sliderValue);
            if (fixtureSliderText != null)
            {
                fixtureSliderText.text = Mathf.Round(sliderValue * 100.0f).ToString();
            }
        }

        public void _SetVolumetricIntensity()
        {
            float sliderValue = volumetricSlider != null ? volumetricSlider.value : 1.0f;
            _ApplyIntensityToMaterials(volumetricMaterials, volumetricIntensityMax, sliderValue);
            if (volumetricSliderText != null)
            {
                volumetricSliderText.text = Mathf.Round(sliderValue * 100.0f).ToString();
            }
        }

        public void _SetProjectionIntensity()
        {
            float sliderValue = projectionSlider != null ? projectionSlider.value : 1.0f;
            _ApplyIntensityToMaterials(projectionMaterials, projectionIntensityMax, sliderValue);
            if (projectionSliderText != null)
            {
                projectionSliderText.text = Mathf.Round(sliderValue * 100.0f).ToString();
            }
        }

        public void _SetDiscoBallIntensity()
        {
            float sliderValue = discoBallSlider != null ? discoBallSlider.value : 1.0f;
            _ApplyIntensityToMaterials(discoBallMaterials, discoballIntensityMax, sliderValue);
            if (discoBallSliderText != null)
            {
                discoBallSliderText.text = Mathf.Round(sliderValue * 100.0f).ToString();
            }
        }

        public void _SetLaserIntensity()
        {
            float sliderValue = laserSlider != null ? laserSlider.value : 1.0f;
            _ApplyIntensityToMaterials(laserMaterials, laserIntensityMax, sliderValue);
            if (laserSliderText != null)
            {
                laserSliderText.text = Mathf.Round(sliderValue * 100.0f).ToString();
            }
        }
        public void _SetBloomIntensity()
        {
            if (bloomPP != null && bloomSlider != null)
            {
                bloomPP.weight = bloomSlider.value;
                if (bloomSliderText != null){
                    bloomSliderText.text = Mathf.Round(bloomSlider.value * 100.0f).ToString();
                }
            }
            else
            {
                if (bloomSlider != null)
                {
                    bloomSlider.gameObject.SetActive(false);
                }
            }
        }

        void SetKeyword(Material mat, string keyword, bool status)
        {
            if (mat == null) { return; }
            if (status)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }

        void SetVolumetricQuality()
        {
            if (volumetricMaterials == null) { return; }
            _EnsureShaderIDs();
            foreach (Material target in volumetricMaterials)
            {
                if (target == null) { continue; }
                if (volumetricQuality == VolumetricQualityModes.High)
                {
                    target.SetOverrideTag("RenderType", "Transparent");
                    target.DisableKeyword(KW_ALPHATEST_ON);
                    //target.SetInt("_BlendSrc", 1);
                    target.SetInt("_BlendDst", 1);
                    target.SetInt("_ZWrite", 0);
                    target.SetInt("_AlphaToCoverage", 0);
                    target.SetInt("_HQMode", 1);
                    target.SetInt("_RenderMode", 0);
                    _SyncVolumetricKeywords(target);
                    //SetKeyword(target, "_2D_NOISE_ON", (Mathf.FloorToInt(target.GetInt("_2D_NOISE_ON"))) == 1 ? true : false);
                    target.renderQueue = 3002;
                }
                else if (volumetricQuality == VolumetricQualityModes.Medium)
                {
                    target.SetOverrideTag("RenderType", "Transparent");
                    target.DisableKeyword(KW_ALPHATEST_ON);
                    //target.SetInt("_BlendSrc", 1);
                    target.SetInt("_BlendDst", 1);
                    target.SetInt("_ZWrite", 0);
                    target.SetInt("_AlphaToCoverage", 0);
                    target.SetInt("_HQMode", 0);
                    target.SetInt("_RenderMode", 1);
                    _SyncVolumetricKeywords(target);
                    //SetKeyword(target, "_2D_NOISE_ON", (Mathf.FloorToInt(target.GetInt("_2D_NOISE_ON"))) == 1 ? true : false);
                    target.renderQueue = 3002;
                }
                else
                {
                    target.SetOverrideTag("RenderType", "Opaque");
                    target.EnableKeyword(KW_ALPHATEST_ON);
                    //target.SetInt("_BlendSrc", 0);
                    target.SetInt("_BlendDst", 0);
                    target.SetInt("_ZWrite", 1);
                    target.SetInt("_AlphaToCoverage", 1);
                    target.SetInt("_HQMode", 0);
                    target.SetInt("_RenderMode", 2);
                    _SyncVolumetricKeywords(target);
                    //SetKeyword(target, "_2D_NOISE_ON", (Mathf.FloorToInt(target.GetInt("_2D_NOISE_ON"))) == 1 ? true : false);
                    target.renderQueue = 2452;
                }
            }
        }
        void SetBlinderProjectionQuality()
        {
            _EnsureMaterialCategoryCaches();
            foreach (Material target in _blinderProjectionMaterials)
            {
                if (target == null) { continue; }
                if (blinderProjectionQuality == DefaultQualityModes.High)
                {
                    _SetCommonMaterialQuality(target, true, 3001, 2451);
                }
                else
                {
                    _SetCommonMaterialQuality(target, false, 3001, 2451);
                }
            }
        }
        void SetParProjectionQuality()
        {
            _EnsureMaterialCategoryCaches();
            foreach (Material target in _parProjectionMaterials)
            {
                if (target == null) { continue; }
                if (parProjectionQuality == DefaultQualityModes.High)
                {
                    _SetCommonMaterialQuality(target, true, 3001, 2451);
                }
                else
                {
                    _SetCommonMaterialQuality(target, false, 3001, 2451);
                }
            }
        }
        void SetOtherProjectionQuality()
        {
            _EnsureMaterialCategoryCaches();
            foreach (Material target in _otherProjectionMaterials)
            {
                if (target == null) { continue; }
                if (otherProjectionQuality == DefaultQualityModes.High)
                {
                    _SetCommonMaterialQuality(target, true, 3001, 2451);
                }
                else
                {
                    _SetCommonMaterialQuality(target, false, 3001, 2451);
                }
            }
        }

        void SetDiscoballQuality()
        {
            if (discoBallMaterials == null) { return; }
            foreach (Material target in discoBallMaterials)
            {
                if (target == null) { continue; }
                if (discoballQuality == DefaultQualityModes.High)
                {
                    target.SetOverrideTag("RenderType", "Transparent");
                    target.DisableKeyword("_ALPHATEST_ON");
                    //target.SetInt("_BlendSrc", 1);
                    target.SetInt("_BlendDst", 1);
                    target.SetInt("_ZWrite", 0);
                    target.SetInt("_AlphaToCoverage", 0);
                    target.SetInt("_RenderMode", 1);
                    target.renderQueue = 3001;
                }
                else
                {
                    target.SetOverrideTag("RenderType", "Opaque");
                    target.EnableKeyword("_ALPHATEST_ON");
                    //target.SetInt("_BlendSrc", 0);
                    target.SetInt("_BlendDst", 0);
                    target.SetInt("_ZWrite", 1);
                    target.SetInt("_AlphaToCoverage", 1);
                    target.SetInt("_RenderMode", 2);
                    target.renderQueue = 2451;
                }
            }
        }
        void SetLensFlareQuality()
        {
            _EnsureMaterialCategoryCaches();
            foreach (Material target in _lensFlareMaterials)
            {
                if (target == null) { continue; }
                if (lensFlareQuality == DefaultQualityModes.High)
                {
                    _SetCommonMaterialQuality(target, true, 3001, 2451);
                }
                else
                {
                    _SetCommonMaterialQuality(target, false, 3001, 2451);
                }
            }
        }


#if !COMPILER_UDONSHARP && UNITY_EDITOR

        static List<GameObject> GetAllObjectsOnlyInScene()
        {
            List<GameObject> objectsInScene = new List<GameObject>();

            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                    objectsInScene.Add(go);
            }

            return objectsInScene;
        }

        public void _GetNewMaterials()
        {
            List<GameObject> sceneObjects = GetAllObjectsOnlyInScene();
            List<Material> freshFixtureMats = new List<Material>();
            if (fixtureMaterials != null) { freshFixtureMats.AddRange(fixtureMaterials); }
            List<Material> freshVolumetricMats = new List<Material>();
            if (volumetricMaterials != null) { freshVolumetricMats.AddRange(volumetricMaterials); }
            List<Material> freshProjectionMats = new List<Material>();
            if (projectionMaterials != null) { freshProjectionMats.AddRange(projectionMaterials); }
            foreach (GameObject go in sceneObjects)
            {
                MeshRenderer rend = go.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    if (rend.sharedMaterial == null || rend.sharedMaterial.shader == null) { continue; }
                    if (go.name.Contains("Fixture") && (go.name.Contains("Lamp") || go.name.Contains("Mesh")))
                    {
                        if (!freshFixtureMats.Contains(rend.sharedMaterial) && ((rend.sharedMaterial.shader.FindPropertyIndex("_Band") != -1) || rend.sharedMaterial.shader.FindPropertyIndex("_DMXChannel") != -1))
                        {
                            freshFixtureMats.Add(rend.sharedMaterial);
                        }
                    }
                    else if (go.name.Contains("Volumetric") && ((rend.sharedMaterial.shader.FindPropertyIndex("_Band") != -1) || rend.sharedMaterial.shader.FindPropertyIndex("_DMXChannel") != -1))
                    {
                        if (!freshVolumetricMats.Contains(rend.sharedMaterial))
                        {
                            freshVolumetricMats.Add(rend.sharedMaterial);
                        }
                    }
                    else if (go.name.Contains("Projection") && ((rend.sharedMaterial.shader.FindPropertyIndex("_Band") != -1) || rend.sharedMaterial.shader.FindPropertyIndex("_DMXChannel") != -1))
                    {
                        if (!freshProjectionMats.Contains(rend.sharedMaterial))
                        {
                            freshProjectionMats.Add(rend.sharedMaterial);
                        }
                    }
                    else { continue; }
                }
            }

            fixtureMaterials = freshFixtureMats.ToArray();
            volumetricMaterials = freshVolumetricMats.ToArray();
            projectionMaterials = freshProjectionMats.ToArray();
            _materialCategoryCacheValid = false;
            _RebuildMaterialCategoryCaches();
            if (PrefabUtility.IsPartOfAnyPrefab(this))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VRSL_LocalUIControlPanel))]
    public class VRSL_LocalUIControlPanel_Editor : Editor
    {
        public static Texture logo;
        //public static string ver = "VR Stage Lighting ver:" + " <b><color=#6a15ce> 2.1</color></b>";
        SerializedProperty audioLinkLasers, audiolinkLights, dmxLights, isUsingDMX, isUsingAudioLink, fixtureDefGUID, volumetricMeshQuality;

        static string GetVersion()
        {
            string path = Application.dataPath;
            path = path.Replace("Assets", "");
            path += "Packages" + "\\" + "com.acchosen.vr-stage-lighting" + "\\";
            path += "Runtime" + "\\" + "VERSION.txt";

            StreamReader reader = new StreamReader(path);
            string versionNum = reader.ReadToEnd();
            string ver = "VR Stage Lighting ver:" + " <b><color=#b33cff>" + versionNum + "</color></b>";
            return ver;
        }

        public void OnEnable()
        {
            logo = Resources.Load("VRStageLighting-Logo") as Texture;
            audioLinkLasers = serializedObject.FindProperty("audioLinkLasers");
            audiolinkLights = serializedObject.FindProperty("audiolinkLights");
            dmxLights = serializedObject.FindProperty("dmxLights");
            isUsingDMX = serializedObject.FindProperty("isUsingDMX");
            isUsingAudioLink = serializedObject.FindProperty("isUsingAudioLink");
            fixtureDefGUID = serializedObject.FindProperty("fixtureDefGUID");
            volumetricMeshQuality = serializedObject.FindProperty("volumetricMeshQuality");
        }
        public void _RemoveEmptyMaterials()
        {
            VRSL_LocalUIControlPanel controlPanel = (VRSL_LocalUIControlPanel)target;
            if (controlPanel.fixtureMaterials == null)
            {
                controlPanel.fixtureMaterials = new Material[0];
                return;
            }
            int count = 0;
            for (int i = 0; i < controlPanel.fixtureMaterials.Length; i++)
            {
                if (controlPanel.fixtureMaterials[i] == null)
                {
                    count++;
                }
            }
            Material[] newArray = new Material[controlPanel.fixtureMaterials.Length - count];
            int otherCount = 0;
            for (int i = 0; i < controlPanel.fixtureMaterials.Length; i++)
            {
                if (controlPanel.fixtureMaterials[i] != null)
                {
                    newArray[otherCount] = controlPanel.fixtureMaterials[i];
                    otherCount++;
                }
            }
            controlPanel.fixtureMaterials = newArray;

        }
        public static void DrawLogo()
        {
            ///GUILayout.BeginArea(new Rect(0,0, Screen.width, Screen.height));
            // GUILayout.FlexibleSpace();
            //GUI.DrawTexture(pos,logo,ScaleMode.ScaleToFit);
            //EditorGUI.DrawPreviewTexture(new Rect(0,0,400,150), logo);
            Vector2 contentOffset = new Vector2(0f, -2f);
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.fixedHeight = 150;
            //style.fixedWidth = 300;
            style.contentOffset = contentOffset;
            style.alignment = TextAnchor.MiddleCenter;
            var rect = GUILayoutUtility.GetRect(300f, 140f, style);
            //GUILayout.Label(logo,style, GUILayout.MaxWidth(500), GUILayout.MaxHeight(200));
            GUI.Box(rect, logo, style);
            //GUILayout.Label(logo);
            // GUILayout.FlexibleSpace();
            //GUILayout.EndArea();
        }
        private static Rect DrawShurikenCenteredTitle(string title, Vector2 contentOffset, int HeaderHeight)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.boldLabel).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fontSize = 14;
            style.fixedHeight = HeaderHeight;
            style.contentOffset = contentOffset;
            style.alignment = TextAnchor.MiddleCenter;
            var rect = GUILayoutUtility.GetRect(16f, HeaderHeight, style);

            GUI.Box(rect, title, style);
            return rect;
        }
        public static void ShurikenHeaderCentered(string title)
        {
            DrawShurikenCenteredTitle(title, new Vector2(0f, -2f), 22);
        }
        GUIContent Label(string label)
        {
            GUIContent content = new GUIContent();
            content.text = label;
            return content;
        }
        public override void OnInspectorGUI()
        {
#if UDONSHARP
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
#endif
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            DrawLogo();
            ShurikenHeaderCentered(GetVersion());
            EditorGUILayout.Space();
            VRSL_LocalUIControlPanel controlPanel = (VRSL_LocalUIControlPanel)target;
            if (GUILayout.Button(new GUIContent("Force Update Target AudioLink Sample Texture", "Updates all AudioLink VRSL Fixtures to sample from the selected target texture when texture sampling is enabled on the fixture."))) { controlPanel._ForceUpdateVideoSampleTexture(); }
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Apply Quality Modes to All Materials", "Applies currently set quality modes to all materials."))) { controlPanel._UpdateAllQualityModes(); }
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Search For VRSL Materials", "Adds VRSL Compatible Materials in scene to materials lists"))) { controlPanel._GetNewMaterials(); }
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Remove Empty Materials", "Removes all Empty Material slots from material lists."))) { _RemoveEmptyMaterials(); }
            EditorGUILayout.Space();
            if (isUsingDMX.boolValue)
            {
                // EditorGUILayout.PropertyField(dmxLights,true);
                for (int i = 0; i < dmxLights.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(dmxLights.GetArrayElementAtIndex(i));
                }
            }
            if (isUsingAudioLink.boolValue)
            {
                for (int i = 0; i < dmxLights.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(audiolinkLights.GetArrayElementAtIndex(i));
                    EditorGUILayout.PropertyField(audioLinkLasers.GetArrayElementAtIndex(i));
                    // EditorGUILayout.PropertyField(audiolinkLights, true);
                    // EditorGUILayout.PropertyField(audioLinkLasers,true);
                }
            }
            EditorGUILayout.LabelField("Fixture Definition GUID: " + fixtureDefGUID.stringValue);
            EditorGUILayout.LabelField("Volumetric Mesh Quality State: " + volumetricMeshQuality.intValue);
            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                //Debug.Log("Found changes");
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }
        }
    }
#endif
}

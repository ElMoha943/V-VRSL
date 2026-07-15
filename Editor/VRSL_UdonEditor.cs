using UnityEngine;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using UdonSharpEditor;
#endif

namespace VRSL.EditorScripts
{
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CanEditMultipleObjects]
    public class VRSL_UdonEditor : Editor
    {
        public static Texture logo;

        public void OnEnable() 
        {
            logo = Resources.Load("VRStageLighting-Logo") as Texture;
        }

        protected void RegisterFixtureEditor(Action hierarchyChanged)
        {
            OnEnable();
            EditorApplication.hierarchyChanged += hierarchyChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        protected void UnregisterFixtureEditor(Action hierarchyChanged)
        {
            EditorApplication.hierarchyChanged -= hierarchyChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        protected virtual void OnSceneGUI(SceneView sceneView)
        {
        }

        protected void SafeUpdateFixture<TFixture>(TFixture fixture, Action<TFixture> updateFixture)
            where TFixture : UnityEngine.Object
        {
            try
            {
                if(fixture != null)
                {
                    updateFixture(fixture);
                }
            }
            catch(NullReferenceException e)
            {
                e.ToString();
            }
        }

        protected void ApplyModifiedPropertiesAndUpdateTargets<TFixture>(Action<TFixture> updateFixture)
            where TFixture : UnityEngine.Object
        {
            serializedObject.ApplyModifiedProperties();
            foreach(UnityEngine.Object obj in targets)
            {
                TFixture fixture = obj as TFixture;
                if(fixture != null)
                {
                    updateFixture(fixture);
                }
            }
        }

        protected static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color textColor)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.normal.textColor = textColor;
            return style;
        }

        public static string GetVersion()
        {
            return "3.0.0-Alpha";
        }

        public static void DrawLogo()
        {
            Vector2 contentOffset = new Vector2(0f, -2f);
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.fixedHeight = 150;
            //style.fixedWidth = 300;
            style.contentOffset = contentOffset;
            style.alignment = TextAnchor.MiddleCenter;
            var rect = GUILayoutUtility.GetRect(300f, 140f, style);
            GUI.Box(rect, logo,style);
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
    }
    #endif

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CanEditMultipleObjects]
    public abstract class VRSL_FixtureUdonEditor<TFixture> : VRSL_UdonEditor
        where TFixture : UnityEngine.Object
    {
        public new void OnEnable()
        {
            RegisterFixtureEditor(HierarchyChanged);
            OnFixtureEditorEnabled();
        }

        void OnDisable()
        {
            UnregisterFixtureEditor(HierarchyChanged);
        }

        protected virtual void OnFixtureEditorEnabled()
        {
        }

        void HierarchyChanged()
        {
            UpdateSettings(target as TFixture);
        }

        protected void UpdateSettings(TFixture fixture)
        {
            SafeUpdateFixture(fixture, UpdateFixtureProperties);
        }

        protected abstract void UpdateFixtureProperties(TFixture fixture);

        protected void ApplyChangedPropertiesToTargets()
        {
            if(EditorGUI.EndChangeCheck())
            {
                ApplyModifiedPropertiesAndUpdateTargets<TFixture>(UpdateSettings);
            }
        }
    }
    #endif


    #if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VRStageLighting_DMX_Static))]
    [CanEditMultipleObjects]
    public class VRStageLighting_DMX_Static_Editor : VRSL_FixtureUdonEditor<VRStageLighting_DMX_Static>
    {
        GUIStyle l, I;
        GUIContent colorLabel;
        VRSL_LocalUIControlPanel panel;
        VRSL_FixtureDefinitions fixtureDefinitions;
        string[] fixDefinitionNames = new string[1];
    //  SerializedProperty _globalIntensity;
        public static GUIStyle InfoLabel()
        {
            return CreateLabelStyle(13, FontStyle.Italic, Color.white);
        }

        public static GUIStyle SectionLabel()
        {
            return CreateLabelStyle(15, FontStyle.Bold, Color.white);
        }

        protected override void OnFixtureEditorEnabled()
        {
            l = SectionLabel();
            I = InfoLabel();
            colorLabel = new GUIContent();
            colorLabel.text = "Emission Color";
        //  _globalIntensity = serializedObject.FindProperty("globalIntensity");
        GetPanel();
        }

        string[] GetFixtureOptions(string fixtureDefGUID)
        {
          fixtureDefinitions = (VRSL_FixtureDefinitions) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(fixtureDefGUID), typeof(VRSL_FixtureDefinitions));
          return fixtureDefinitions == null ? new string[1] : fixtureDefinitions.GetNames();
        }

        void DrawDMXChannelRangeWarning()
        {
            SerializedProperty enabledProperty = serializedObject.FindProperty("enableDMXChannels");
            SerializedProperty fixtureDefinitionProperty = serializedObject.FindProperty("fixtureDefintion");
            if(targets.Length != 1 || !enabledProperty.boolValue || enabledProperty.hasMultipleDifferentValues ||
                fixtureDefinitionProperty.hasMultipleDifferentValues || fixtureDefinitions == null)
            {
                return;
            }

            int fixtureDefinition = fixtureDefinitionProperty.intValue;
            if(fixtureDefinition < 0 || fixtureDefinition >= fixtureDefinitions.DefinitionsArraySize)
            {
                return;
            }

            string[] channelDefinition = fixtureDefinitions.GetChannelDefinition(fixtureDefinition);
            if(channelDefinition == null || channelDefinition.Length == 0)
            {
                return;
            }

            bool useLegacySectorMode = serializedObject.FindProperty("useLegacySectorMode").boolValue;
            bool singleChannelMode = useLegacySectorMode && serializedObject.FindProperty("singleChannelMode").boolValue;
            int channelCount = singleChannelMode ? 1 : channelDefinition.Length;
            int startChannel;
            int universe;

            if(useLegacySectorMode)
            {
                int sector = Mathf.Max(0, serializedObject.FindProperty("sector").intValue);
                startChannel = ((sector % 40) * 13) + 1;
                universe = (sector / 40) + 1;
                if(singleChannelMode)
                {
                    startChannel += serializedObject.FindProperty("Channel").intValue;
                }
            }
            else
            {
                startChannel = serializedObject.FindProperty("dmxChannel").intValue;
                universe = serializedObject.FindProperty("dmxUniverse").intValue;
            }

            int endChannel = startChannel + channelCount - 1;
            if(endChannel <= 512)
            {
                return;
            }

            int availableChannels = Mathf.Max(0, 513 - startChannel);
            string fixtureName = fixtureDefinitions.definitions[fixtureDefinition].name;
            EditorGUILayout.HelpBox(
                fixtureName + " uses " + channelCount + " channel" + (channelCount == 1 ? "" : "s") +
                " and starts at channel " + startChannel + " in universe " + universe +
                ", but only " + availableChannels + " channel" + (availableChannels == 1 ? " remains." : "s remain.") +
                " Move it to channel " + (513 - channelCount) + " or earlier to keep the fixture within one universe.",
                MessageType.Warning);
        }

        public void GetPanel()
        {
            List<GameObject> sceneObjects = GetAllObjectsOnlyInScene();
            foreach(GameObject go in sceneObjects)
            {
                #pragma warning disable 0618 //suppressing obsoletion warnings
                panel = go.GetUdonSharpComponent<VRSL_LocalUIControlPanel>();
                #pragma warning restore 0618
                if(panel != null)
                {
                    fixDefinitionNames = GetFixtureOptions(panel.fixtureDefGUID);
                    break;
                }
            }
        }

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
    
        protected override void UpdateFixtureProperties(VRStageLighting_DMX_Static fixture)
        {
            fixture._SetProps();
            fixture._UpdateInstancedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawLogo();
            ShurikenHeaderCentered(GetVersion());
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            VRStageLighting_DMX_Static fixture = (VRStageLighting_DMX_Static)target;
            //EditorGUIUtility.LookLikeInspector();
            EditorGUI.BeginChangeCheck();
            //base.OnInspectorGUI();

            //DMX SETTINGS SECTION
            GUILayout.Label("DMX Settings", l);
            serializedObject.FindProperty("enableDMXChannels").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable DMX", 
            "The industry standard DMX Channel this fixture begins on. Most standard VRSL fixtures are 13 channels"), fixture.enableDMXChannels);
            if(serializedObject.FindProperty("enableDMXChannels").boolValue && panel != null)
            {
                EditorGUI.indentLevel++;
                serializedObject.FindProperty("fixtureDefintion").intValue = EditorGUILayout.Popup("Fixture Type",serializedObject.FindProperty("fixtureDefintion").intValue, fixDefinitionNames);
                EditorGUI.indentLevel--;
            }

            serializedObject.FindProperty("nineUniverseMode").boolValue = EditorGUILayout.Toggle(new GUIContent("Extended Universe Mode", 
            "Enables 9-Universe mode for this fixture. The grid will be split up by RGB channels with each section and color representing a universe." + 
            " Only availble on the Vertical and Horizontal Grid nodes."), fixture.nineUniverseMode);

            serializedObject.FindProperty("enableFineChannels").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Fine Channels (For Pan/Tilt)",
            "Enables the computation of fine channels for pan and tilt. This allows for smoother movement of movers when using DMX control if your stream is stable enough to support it"), fixture.enableFineChannels);
            
            serializedObject.FindProperty("fixtureID").intValue = EditorGUILayout.IntField(new GUIContent("Fixture ID", 
            "The ID number for this fixture. This is mostly for organizational purposes and is entirely optional. Most DMX software have an ID attached to each fixture to run the fixtures through commands more easily, and it is recommended to have those IDs lined up here as well for the sake simplicity. This ID is public and can also be used for Udon scripting as well."),fixture.fixtureID);

            serializedObject.FindProperty("useLegacySectorMode").boolValue  = EditorGUILayout.Toggle(new GUIContent("Enable Legacy Sector Mode", 
            "Enables the legacy 'Sector' based method of assigning DMX Channels. Keep this unchecked to use industry standard DMX Channels."), fixture.useLegacySectorMode);
            if(fixture.useLegacySectorMode)
            {
                serializedObject.FindProperty("sector").intValue = EditorGUILayout.IntField(new GUIContent("Sector", 
                "Chooses the DMX Address to start this fixture at. A Sector in this context is every 13 Channels. I.E Sector 0 is channels 1-13, Sector 1 is channels 14-26, etc."),fixture.sector);
                serializedObject.FindProperty("singleChannelMode").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Single Channel Mode",
                "Enables single channel DMX mode for this fixture. This is for single channeled fixtures instead of the standard 13-channeled ones. Currently, the 'Flasher' fixture is the only single-channeled fixture at the moment"), fixture.singleChannelMode);
                if(fixture.singleChannelMode)
                {
                    serializedObject.FindProperty("Channel").intValue = EditorGUILayout.IntSlider(new GUIContent("Single Channel CH",
                    "Chooses the which of the 13 Channels of the current sector to sample from when single channel mode is enabled. Do not worry about this value if you are not using a single-channeled fixture."),fixture.Channel, 0, 12);
                    
                }
            }
            else
            {
                serializedObject.FindProperty("dmxChannel").intValue = EditorGUILayout.IntSlider(new GUIContent("DMX Channel", 
                "The industry standard DMX Channel this fixture begins on. Most standard VRSL fixtures are 13 channels"),fixture.dmxChannel, 1, 512);
                serializedObject.FindProperty("dmxUniverse").intValue = EditorGUILayout.IntSlider(new GUIContent("Universe", 
                "The industry standard Artnet Universe. Use this to choose which universe to read the DMX Channel from."),fixture.dmxUniverse, 1, 9);
            }
            DrawDMXChannelRangeWarning();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label(fixture._DMXChannelToString(), I);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            serializedObject.FindProperty("legacyGoboRange").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Legacy Gobo Range", 
            "Use Only the first 6 gobos instead of all. This is for legacy content where only 6 gobos were originally supported and the channel range was different."), fixture.legacyGoboRange);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //GENERAL SETTINGS
            GUILayout.Label("General Settings", l);
            serializedObject.FindProperty("globalIntensity").floatValue = EditorGUILayout.Slider(new GUIContent("Global Intensity",
            "Sets the overall intensity of the shader. Good for animating or scripting effects related to intensity. Its max value is controlled by Final Intensity."), fixture.globalIntensity, 0.0f, 1.0f);
            EditorGUILayout.PropertyField( serializedObject.FindProperty("finalIntensityComponentMode"), new GUIContent("Control Component Intensities"));
            EditorGUI.indentLevel++;
            if(serializedObject.FindProperty("finalIntensityComponentMode").boolValue){

                serializedObject.FindProperty("finalIntensityVolumetric").floatValue  = EditorGUILayout.Slider(new GUIContent("Volumetric Intensity",
                "Sets the maximum brightness value of Global Intensity for volumetric meshes only. Good for personalized settings of the max brightness of the shader by other users via UI."), fixture.finalIntensityVolumetric, 0.0f, 1.0f);
                
                serializedObject.FindProperty("finalIntensityProjection").floatValue  = EditorGUILayout.Slider(new GUIContent("Projection Intensity",
                "Sets the maximum brightness value of Global Intensity for projection meshes only. Good for personalized settings of the max brightness of the shader by other users via UI."), fixture.finalIntensityProjection, 0.0f, 1.0f);

                serializedObject.FindProperty("finalIntensityFixture").floatValue  = EditorGUILayout.Slider(new GUIContent("Fixture/Other Intensity",
                "Sets the maximum brightness value of Global Intensity for everything else. Good for personalized settings of the max brightness of the shader by other users via UI."), fixture.finalIntensityFixture, 0.0f, 1.0f);
            }
            else{
                serializedObject.FindProperty("finalIntensity").floatValue  = EditorGUILayout.Slider(new GUIContent("Final Intensity",
                "Sets the maximum brightness value of Global Intensity. Good for personalized settings of the max brightness of the shader by other users via UI."), fixture.finalIntensity, 0.0f, 1.0f);
            }
            EditorGUI.indentLevel--;
            serializedObject.FindProperty("lightColorTint").colorValue = EditorGUILayout.ColorField(colorLabel, fixture.lightColorTint, true, true, true);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //MOVEMENT SETTINGS
            GUILayout.Label("Movement Settings", l);
            serializedObject.FindProperty("invertPan").boolValue = EditorGUILayout.Toggle(new GUIContent("Invert Pan", 
            "Invert the tilt values (Up/Down Movement) for movers."), fixture.invertPan);
            serializedObject.FindProperty("invertTilt").boolValue = EditorGUILayout.Toggle(new GUIContent("Invert Tilt", 
            "Enable this if the mover is hanging upside down."), fixture.invertTilt);
            serializedObject.FindProperty("isUpsideDown").boolValue = EditorGUILayout.Toggle(new GUIContent("Is Upside Down?",
            "Enable projection spinning (Udon Override Only)."), fixture.isUpsideDown);
            serializedObject.FindProperty("maxMinPan").floatValue = EditorGUILayout.FloatField(new GUIContent("Max/Min Pan Range",
            "Control the range of rotation for the pan channel of the fixture"), fixture.maxMinPan);
            serializedObject.FindProperty("maxMinTilt").floatValue = EditorGUILayout.FloatField(new GUIContent("Max/Min Tilt Range",
            "Control the range of rotation for the tilt channel of the fixture"), fixture.maxMinTilt);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //FIXTURE SETTINGS
            GUILayout.Label("Fixture Settings", l);
            serializedObject.FindProperty("enableAutoSpin").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Projection Spin",
            "Enable projection spinning (Udon Override Only)."), fixture.enableAutoSpin);
            serializedObject.FindProperty("enableStrobe").boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Strobe Functionality",
            "Enable strobe effects (via DMX Only)."), fixture.enableStrobe);
            serializedObject.FindProperty("tiltOffsetBlue").floatValue = EditorGUILayout.Slider(new GUIContent("Tilt Offset",
            "Tilt (Up/Down) offset/movement. Directly controls tilt when in Udon Mode; is an offset when in DMX mode."), fixture.tiltOffsetBlue, 0.0f, 360.0f);
            serializedObject.FindProperty("panOffsetBlueGreen").floatValue = EditorGUILayout.Slider(new GUIContent("Pan Offset",
            "Pan (Left/Right) offset/movement. Directly controls pan when in Udon Mode; is an offset when in DMX mode."), fixture.panOffsetBlueGreen, 0.0f, 360.0f);
            serializedObject.FindProperty("selectGOBO").intValue = EditorGUILayout.IntSlider(new GUIContent("Projection GOBO Selection",
            "The meshes used to make up the light. You need atleast 1 mesh in this group for the script to work properly."), fixture.selectGOBO, 1, 8);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //MESH SETTINGS
            GUILayout.Label("Mesh Settings", l);
            serializedObject.FindProperty("coneWidth").floatValue = EditorGUILayout.Slider(new GUIContent("Fixture Cone Width",
            "Controls the radius of a mover/spot light."), fixture.coneWidth, 0.0f, 5.5f);
            serializedObject.FindProperty("coneLength").floatValue = EditorGUILayout.Slider(new GUIContent("Fixture Cone Length",
            "Controls the length of the cone of a mover/spot light."), fixture.coneLength, 0.5f, 10.0f);
            serializedObject.FindProperty("maxConeLength").floatValue = EditorGUILayout.Slider("Max Cone Length", fixture.maxConeLength, 0.275f, 10.0f);
            
            SerializedProperty meshRends = serializedObject.FindProperty("objRenderers");
            EditorGUILayout.PropertyField(meshRends, true);
            ApplyChangedPropertiesToTargets();
        }
    }
    #endif

    #if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CanEditMultipleObjects]
    public abstract class VRSL_AudioLinkUdonEditor<TFixture> : VRSL_FixtureUdonEditor<TFixture>
        where TFixture : UnityEngine.Object
    {
        protected static GUIStyle SectionLabel()
        {
            return CreateLabelStyle(14, FontStyle.Bold, new Color(0.8f, 0.8f, 0.8f));
        }

        protected void DrawEditorHeader()
        {
            DrawLogo();
            ShurikenHeaderCentered(GetVersion());
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        protected void DrawSection(string title)
        {
            EditorGUILayout.Space();
            DrawGuiLine();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, SectionLabel());
        }

        protected void DrawProperty(string propertyName, string label, string tooltip = "", bool includeChildren = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if(property == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip), includeChildren);
        }

        protected SerializedProperty DrawAndGetProperty(string propertyName, string label, string tooltip = "", bool includeChildren = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if(property == null)
            {
                return null;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip), includeChildren);
            return property;
        }

        protected void DrawAudioLinkOptions()
        {
            DrawSection("AudioLink Settings");
            DrawProperty("enableAudioLink", "Enable AudioLink", "Enable or disable AudioLink reaction for this fixture.");
            DrawProperty("band", "Band", "The frequency band of the spectrum to react to.");
            DrawProperty("delay", "Delay", "The level of delay to add to the reaction.");
            DrawProperty("bandMultiplier", "Band Multiplier", "Multiplier for the sensitivity of the reaction.");
            DrawProperty("enableColorChord", "Enable Color Chord", "Enable ColorChord tinting of the light emission.");
        }

        protected void DrawGeneralOptions(bool drawComponentIntensityControls)
        {
            DrawSection("General Settings");
            DrawProperty("globalIntensity", "Global Intensity", "Sets the overall intensity of the shader.");
            DrawFinalIntensityOptions(drawComponentIntensityControls);
            DrawProperty("lightColorTint", "Emission Color", "The main color of the light.");
        }

        protected void DrawFinalIntensityOptions(bool drawComponentIntensityControls)
        {
            if(drawComponentIntensityControls)
            {
                SerializedProperty componentMode = DrawAndGetProperty("finalIntensityComponentMode", "Control Component Intensities", "Choose between setting the final intensity globally or per mesh component.");
                if(componentMode != null && componentMode.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty("finalIntensityVolumetric", "Volumetric Intensity", "Sets the maximum brightness for volumetric meshes only.");
                    DrawProperty("finalIntensityProjection", "Projection Intensity", "Sets the maximum brightness for projection meshes only.");
                    DrawProperty("finalIntensityFixture", "Fixture/Other Intensity", "Sets the maximum brightness for all other meshes.");
                    EditorGUI.indentLevel--;
                    return;
                }
            }

            DrawProperty("finalIntensity", "Final Intensity", "Sets the maximum brightness value of Global Intensity.");
        }

        protected void DrawColorSamplingOptions()
        {
            DrawSection("Color Sampling Settings");
            SerializedProperty colorSampling = DrawAndGetProperty("enableColorTextureSampling", "Enable Color Texture Sampling", "Sample a separate texture for the light color.");
            if(colorSampling != null && colorSampling.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawProperty("traditionalColorTextureSampling", "Traditional Color Sampling", "Use traditional color sampling instead of white to black conversion.");
                DrawProperty("textureSamplingCoordinates", "Texture Sampling Coordinates", "The UV coordinates to sample the color texture from.");
                EditorGUI.indentLevel--;
            }

            SerializedProperty themeSampling = DrawAndGetProperty("enableThemeColorSampling", "Enable Theme Color Sampling", "Enable AudioLink theme color sampling.");
            if(themeSampling != null && themeSampling.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawProperty("themeColorTarget", "Theme Color Target", "Theme color to sample from.");
                EditorGUI.indentLevel--;
            }
        }

        protected void DrawMeshOptions()
        {
            DrawSection("Mesh Settings");
            DrawProperty("objRenderers", "Object Renderers", "The meshes used to make up the light.", true);
        }

        protected void DrawConeOptions(string sectionTitle)
        {
            DrawSection(sectionTitle);
            DrawProperty("coneWidth", "Cone Width", "Controls the radius of the light cone.");
            DrawProperty("coneLength", "Cone Length", "Controls the length of the light cone.");
            DrawProperty("maxConeLength", "Max Cone Length", "Controls the maximum mesh length of the light cone.");
        }

        private void DrawGuiLine(int height = 1)
        {
            try
            {
                Rect rect = EditorGUILayout.GetControlRect(false, height);
                rect.height = height;
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            }
            catch(Exception e)
            {
                e.GetType();
            }
        }
    }
    #endif

    #if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VRStageLighting_AudioLink_Laser))]
    [CanEditMultipleObjects]
    public class VRStageLighting_AudioLink_Laser_Editor : VRSL_AudioLinkUdonEditor<VRStageLighting_AudioLink_Laser>
    {
        protected override void UpdateFixtureProperties(VRStageLighting_AudioLink_Laser fixture)
        {
            fixture._SetProps();
            fixture._UpdateInstancedProperties();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawEditorHeader();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawLaserEditor();
            ApplyChangedPropertiesToTargets();
        }

        private void DrawLaserEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(false);
            DrawColorSamplingOptions();
            DrawLaserOptions();
            DrawMeshOptions();
        }

        private void DrawLaserOptions()
        {
            DrawSection("Laser Settings");
            DrawProperty("coneWidth", "Cone Width", "Controls the radius of the laser cone.");
            DrawProperty("coneLength", "Cone Length", "Controls the length of the laser cone.");
            DrawProperty("coneFlatness", "Cone Flatness", "Controls how flat or round the cone is.");
            DrawProperty("coneXRotation", "X Rotation Offset", "X rotation offset for the laser cone.");
            DrawProperty("coneYRotation", "Y Rotation Offset", "Y rotation offset for the laser cone.");
            DrawProperty("coneZRotation", "Z Rotation Offset", "Z rotation offset for the laser cone.");
            DrawProperty("laserCount", "Beam Count", "Number of laser beams in the cone.");
            DrawProperty("laserThickness", "Beam Thickness", "Controls how thick the laser beams are.");
            DrawProperty("laserScroll", "Scroll Speed", "Controls the speed of the laser scroll animation.");
        }
    }
    #endif

    #if !COMPILER_UDONSHARP && UNITY_EDITOR
    [InitializeOnLoad]
    [CustomEditor(typeof(VRStageLighting_AudioLink_Static))]
    [CanEditMultipleObjects]
    public class VRStageLighting_AudioLink_Static_Editor : VRSL_AudioLinkUdonEditor<VRStageLighting_AudioLink_Static>
    {
        protected override void UpdateFixtureProperties(VRStageLighting_AudioLink_Static fixture)
        {
            fixture._SetProps();
            fixture._UpdateInstancedProperties();
            fixture._CheckAvailableConstraints(fixture);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawEditorHeader();

            VRStageLighting_AudioLink_Static fixture = (VRStageLighting_AudioLink_Static)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawFixtureTypeOptions(fixture);
            DrawFixtureEditor(ResolveFixtureType(fixture));
            ApplyChangedPropertiesToTargets();
        }

        private void DrawFixtureTypeOptions(VRStageLighting_AudioLink_Static fixture)
        {
            DrawSection("Fixture Editor");
            SerializedProperty fixtureType = DrawAndGetProperty("fixtureType", "Fixture Type", "Use Auto to infer the editor from the prefab or renderer names.");
            if(fixtureType != null && !fixtureType.hasMultipleDifferentValues && (AudioLinkFixtureType)fixtureType.enumValueIndex == AudioLinkFixtureType.Auto)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Detected Type", InferFixtureType(fixture).ToString());
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFixtureEditor(AudioLinkFixtureType fixtureType)
        {
            switch(fixtureType)
            {
                case AudioLinkFixtureType.Spotlight:
                    DrawSpotlightEditor();
                    break;
                case AudioLinkFixtureType.Washlight:
                    DrawWashlightEditor();
                    break;
                case AudioLinkFixtureType.DiscoBall:
                    DrawDiscoBallEditor();
                    break;
                case AudioLinkFixtureType.Blinder:
                    DrawBlinderEditor();
                    break;
                case AudioLinkFixtureType.LightBar:
                    DrawLightBarEditor();
                    break;
                case AudioLinkFixtureType.Flasher:
                    DrawFlasherEditor();
                    break;
                case AudioLinkFixtureType.Parlight:
                case AudioLinkFixtureType.Auto:
                default:
                    DrawParlightEditor();
                    break;
            }
        }

        private AudioLinkFixtureType ResolveFixtureType(VRStageLighting_AudioLink_Static fixture)
        {
            SerializedProperty fixtureType = serializedObject.FindProperty("fixtureType");
            if(fixtureType != null && !fixtureType.hasMultipleDifferentValues)
            {
                AudioLinkFixtureType configuredType = (AudioLinkFixtureType)fixtureType.enumValueIndex;
                if(configuredType != AudioLinkFixtureType.Auto)
                {
                    return configuredType;
                }
            }

            return InferFixtureType(fixture);
        }

        private AudioLinkFixtureType InferFixtureType(VRStageLighting_AudioLink_Static fixture)
        {
            string fixtureText = GetFixtureSearchText(fixture);
            if(fixtureText.Contains("wash"))
            {
                return AudioLinkFixtureType.Washlight;
            }
            if(fixtureText.Contains("spot"))
            {
                return AudioLinkFixtureType.Spotlight;
            }
            if(fixtureText.Contains("disco"))
            {
                return AudioLinkFixtureType.DiscoBall;
            }
            if(fixtureText.Contains("blinder"))
            {
                return AudioLinkFixtureType.Blinder;
            }
            if(fixtureText.Contains("lightbar") || fixtureText.Contains("light bar"))
            {
                return AudioLinkFixtureType.LightBar;
            }
            if(fixtureText.Contains("flasher") || fixtureText.Contains("flash"))
            {
                return AudioLinkFixtureType.Flasher;
            }
            if(fixtureText.Contains("parlight") || fixtureText.Contains("par light") || fixtureText.Contains("par-"))
            {
                return AudioLinkFixtureType.Parlight;
            }

            return AudioLinkFixtureType.Parlight;
        }

        private string GetFixtureSearchText(VRStageLighting_AudioLink_Static fixture)
        {
            if(fixture == null)
            {
                return string.Empty;
            }

            string fixtureText = fixture.name + " " + fixture.gameObject.name;
            if(fixture.objRenderers != null)
            {
                for(int i = 0; i < fixture.objRenderers.Length; i++)
                {
                    MeshRenderer renderer = fixture.objRenderers[i];
                    if(renderer == null)
                    {
                        continue;
                    }

                    fixtureText += " " + renderer.name + " " + renderer.gameObject.name;
                    if(renderer.sharedMaterial != null)
                    {
                        fixtureText += " " + renderer.sharedMaterial.name;
                    }
                }
            }

            return fixtureText.ToLowerInvariant();
        }

        private void DrawSpotlightEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawMovementOptions();
            DrawGoboOptions();
            DrawConeOptions("Beam Settings");
            DrawMeshOptions();
        }

        private void DrawWashlightEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawMovementOptions();
            DrawConeOptions("Beam Settings");
            DrawMeshOptions();
        }

        private void DrawDiscoBallEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawMeshOptions();
        }

        private void DrawBlinderEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawConeOptions("Projection Settings");
            DrawMeshOptions();
        }

        private void DrawLightBarEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawMeshOptions();
        }

        private void DrawFlasherEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawMeshOptions();
        }

        private void DrawParlightEditor()
        {
            DrawAudioLinkOptions();
            DrawGeneralOptions(true);
            DrawColorSamplingOptions();
            DrawConeOptions("Projection Settings");
            DrawMeshOptions();
        }

        private void DrawMovementOptions()
        {
            DrawSection("Movement Settings");
            DrawProperty("targetToFollow", "Target To Follow", "The target for this mover to follow.");
        }

        private void DrawGoboOptions()
        {
            DrawSection("Gobo Settings");
            DrawProperty("selectGOBO", "Projection Gobo Selection", "Use this to change which projection is selected.");
            SerializedProperty projectionSpin = DrawAndGetProperty("enableAutoSpin", "Enable Projection Spin", "Enable projection spinning.");
            if(projectionSpin != null && projectionSpin.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawProperty("spinSpeed", "Projection Spin Speed", "Projection spin speed.");
                EditorGUI.indentLevel--;
            }
        }
    }
    #endif

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    // ensure class initializer is called whenever scripts recompile
    [InitializeOnLoad]
    public static class PlayModeStateChanged
    {
        // register an event handler when the class is initialized
        static PlayModeStateChanged()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
            EditorApplication.update += RunOnce;
            //LoadFixtureSettings();
        }

        static void RunOnce()
        {
            LoadFixtureSettings();
        //  Debug.Log("Running Once... " + EditorApplication.update);
            EditorApplication.update -= RunOnce;
        }

        static void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            //Debug.LogFormat("SceneOpened: {0}", scene.name);
            LoadFixtureSettings();
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
    //        Debug.Log(state);
            if(state == PlayModeStateChange.EnteredEditMode)
            {
                LoadFixtureSettings();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void LoadFixtureSettings()
        {
            GameObject[] objs;
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                objs = scene.GetRootGameObjects();
            }
            catch(NullReferenceException e)
            {
                e.GetType();
                return;
            }
            try
            {  
                foreach(GameObject obj in objs)
                {
                    #pragma warning disable 0618 //suppressing obsoletion warnings
                    //VRStageLighting_RAW_Static[] staticLights = obj.GetUdonSharpComponentsInChildren<VRStageLighting_RAW_Static>();
                    VRStageLighting_AudioLink_Static[] audioLinkLights = obj.GetUdonSharpComponentsInChildren<VRStageLighting_AudioLink_Static>();
                    // VRStageLighting_Animated_Static[] animatedLights = obj.GetUdonSharpComponentsInChildren<VRStageLighting_Animated_Static>();
                    VRStageLighting_DMX_Static[] dmxLights = obj.GetUdonSharpComponentsInChildren<VRStageLighting_DMX_Static>();
                    //VRStageLighting_RAW_Laser[] rawLasers = obj.GetUdonSharpComponentsInChildren<VRStageLighting_RAW_Laser>();
                    VRStageLighting_AudioLink_Laser[] audioLinkLasers = obj.GetUdonSharpComponentsInChildren<VRStageLighting_AudioLink_Laser>();
                    // VRStageLighting_DMX_Static[] dmxLights = obj.GetUdonSharpComponentsInChildren<VRStageLighting_DMX_Static>();
                    VRSL_LocalUIControlPanel[] controlPanels = obj.GetUdonSharpComponentsInChildren<VRSL_LocalUIControlPanel>();
                    #pragma warning restore 0618 //suppressing obsoletion warnings
                    if(dmxLights != null)
                    {
                        foreach(VRStageLighting_DMX_Static fixture in dmxLights)
                        {
                            fixture._SetProps();
                            fixture._UpdateInstancedProperties();
                        }
                    }

                    if(audioLinkLasers != null)
                    {
                        foreach(VRStageLighting_AudioLink_Laser fixture in audioLinkLasers)
                        {
                            if(fixture != null)
                            {
                                fixture._SetProps();
                                fixture._UpdateInstancedProperties();
                            }
                        }
                    }
                    if(audioLinkLights != null)
                    {
                        foreach(VRStageLighting_AudioLink_Static fixture in audioLinkLights)
                        {
                            if(fixture != null)
                            {
                                fixture._SetProps();
                                fixture._UpdateInstancedProperties();

                            }
                        }
                    }

                    if(controlPanels != null)
                    {
                        foreach(VRSL_LocalUIControlPanel panel in controlPanels)
                        {
                            panel._CheckDepthLightStatus();
                            //Debug.Log("AutoChecking Status");
                        }
                    }

                }
            }
            catch(NullReferenceException e)
            {
                e.GetType();
                return;
            }
        }
    }
    #endif
}

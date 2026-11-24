using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace RSPP.EditorTools
{
    /// <summary>
    /// EDITOR ONLY. Turns the project into a VR project for Meta Quest 2/3 so nothing has to be
    /// ticked by hand in Project Settings:
    ///
    ///   - XR Plug-in Management: OpenXR enabled for Android (Quest build) and for PC (Quest Link in the Editor)
    ///   - OpenXR: Oculus Touch / Meta Quest Touch Plus controller profiles + "Meta Quest Support"
    ///   - Player: Input System enabled, IL2CPP, ARM64, minimum Android API 29
    ///
    /// It runs once automatically when the project is opened, and any time from the menu
    ///   RSPP > Set Up XR For Meta Quest
    /// Without these settings the app starts as a flat 2D window on the headset with no controllers.
    /// </summary>
    [InitializeOnLoad]
    public static class RSPPQuestSetup
    {
        private const string DoneKey = "RSPP_QuestSetup_Done_v2_";
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        static RSPPQuestSetup()
        {
            // Wait until the Editor has finished loading before touching settings.
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void AutoRunOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string key = DoneKey + Application.dataPath;
            if (EditorPrefs.GetBool(key, false)) return;
            if (Run(false)) EditorPrefs.SetBool(key, true);
        }

        [MenuItem("RSPP/Set Up XR For Meta Quest")]
        private static void RunFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("RSPP", "Stop Play mode first, then run this again.", "OK");
                return;
            }
            Run(true);
        }

        /// <summary>Applies all settings. Returns true when everything succeeded.</summary>
        private static bool Run(bool showDialog)
        {
            var report = new List<string>();
            bool ok = true;

            try
            {
                XRGeneralSettingsPerBuildTarget perTarget = GetOrCreateXRSettings();
                ok &= EnableOpenXR(perTarget, BuildTargetGroup.Android, report);
                ok &= EnableOpenXR(perTarget, BuildTargetGroup.Standalone, report);
                EditorUtility.SetDirty(perTarget);

                ok &= EnableQuestFeatures(BuildTargetGroup.Android, true, report);
                ok &= EnableQuestFeatures(BuildTargetGroup.Standalone, false, report);

                ApplyPlayerSettings(report);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                ok = false;
                report.Add("ERROR: " + e.Message);
                Debug.LogException(e);
            }

            string text = string.Join("\n", report.ToArray());
            if (ok) Debug.Log("[RSPP] XR setup for Meta Quest finished.\n" + text);
            else Debug.LogWarning("[RSPP] XR setup for Meta Quest finished WITH PROBLEMS (see README section 6 for the manual steps).\n" + text);

            if (showDialog)
                EditorUtility.DisplayDialog("RSPP - XR setup " + (ok ? "finished" : "had problems"), text, "OK");
            return ok;
        }

        // ------------------------------------------------------------------ XR Plug-in Management

        private static XRGeneralSettingsPerBuildTarget GetOrCreateXRSettings()
        {
            XRGeneralSettingsPerBuildTarget settings;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out settings);
            if (settings != null) return settings;

            // An asset may exist without being registered.
            string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                AssetDatabase.SaveAssets();
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            return settings;
        }

        private static bool EnableOpenXR(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group, List<string> report)
        {
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
            if (general != null) general.InitManagerOnStart = true;

            XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(group);
            if (manager == null)
            {
                report.Add(group + ": could not create XR manager settings.");
                return false;
            }

            if (XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderType, group))
            {
                report.Add(group + ": OpenXR was already enabled.");
                return true;
            }

            if (manager.activeLoaders != null && manager.activeLoaders.Count > 0)
            {
                // Another plug-in (for example Oculus) is ticked. Two plug-ins must not be active together.
                report.Add(group + ": another XR plug-in is enabled (" + manager.activeLoaders[0].name +
                           "). Untick it in Project Settings > XR Plug-in Management, then run this menu again.");
                return false;
            }

            bool assigned = XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderType, group);
            report.Add(group + ": OpenXR " + (assigned ? "enabled." : "could NOT be enabled."));
            EditorUtility.SetDirty(manager);
            return assigned;
        }

        // ------------------------------------------------------------------ OpenXR features

        private static bool EnableQuestFeatures(BuildTargetGroup group, bool enableQuestSupport, List<string> report)
        {
            // Makes OpenXR create its settings asset and discover all features for this platform.
            FeatureHelpers.RefreshFeatures(group);

            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
            {
                report.Add(group + ": OpenXR settings not found.");
                return false;
            }

            int profiles = 0;
            foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRInteractionFeature>())
            {
                if (feature is OculusTouchControllerProfile || feature is MetaQuestTouchPlusControllerProfile)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                    profiles++;
                }
            }
            report.Add(group + ": " + profiles + " Quest controller profile(s) enabled.");

            if (enableQuestSupport)
            {
                MetaQuestFeature quest = settings.GetFeature<MetaQuestFeature>();
                if (quest != null)
                {
                    quest.enabled = true;
                    EditorUtility.SetDirty(quest);
                    report.Add(group + ": Meta Quest Support enabled.");
                }
                else
                {
                    report.Add(group + ": Meta Quest Support feature not found (is Android Build Support installed?).");
                    return false;
                }
            }

            EditorUtility.SetDirty(settings);
            return profiles > 0;
        }

        // ------------------------------------------------------------------ Player settings

        private static void ApplyPlayerSettings(List<string> report)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 29)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
            report.Add("Android: IL2CPP, ARM64, minimum API level 29.");

            // "Active Input Handling" has no public API, so it is changed through the settings file:
            // 0 = old Input Manager, 1 = Input System package, 2 = both. The game needs the Input System.
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets != null && assets.Length > 0)
            {
                var serialized = new SerializedObject(assets[0]);
                SerializedProperty handler = serialized.FindProperty("activeInputHandler");
                if (handler != null && handler.intValue == 0)
                {
                    handler.intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    report.Add("Input System enabled. RESTART THE UNITY EDITOR NOW for this to take effect.");
                }
            }
        }
    }
}

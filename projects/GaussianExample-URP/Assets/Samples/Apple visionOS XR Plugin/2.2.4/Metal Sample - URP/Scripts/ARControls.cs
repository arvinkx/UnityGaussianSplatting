using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
#endif

namespace UnityEngine.XR.VisionOS.Samples.URP
{
    public class ARControls : MonoBehaviour
    {
#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
        const string k_EnableHandTrackingTest = "Enable Hand Tracking";
        const string k_DisableHandTrackingTest = "Disable Hand Tracking";
#endif

        const string k_EnablePlaneTrackingText = "Enable Plane Tracking";
        const string k_DisablePlaneTrackingText = "Disable Plane Tracking";
        const string k_ShowPlanesText = "Show Planes";
        const string k_HidePlanesText = "Hide Planes";

        const string k_EnableMeshTrackingText = "Enable Mesh Tracking";
        const string k_DisableMeshTrackingText = "Disable Mesh Tracking";
        const string k_ShowMeshesText = "Show Meshes";
        const string k_HideMeshesText = "Hide Meshes";

        const string k_EnableImageTrackingText = "Enable Image Tracking";
        const string k_DisableImageTrackingText = "Disable Images Tracking";
        const string k_ShowImagesText = "Show Images";
        const string k_HideImagesText = "Hide Images";

        const string k_EnableObjectTrackingText = "Enable Object Tracking";
        const string k_DisableObjectTrackingText = "Disable Object Tracking";
        const string k_ShowObjectsText = "Show Objects";
        const string k_HideObjectsText = "Hide Objects";

        const string k_EnableEnvironmentProbesText = "Enable Environment Probes";
        const string k_DisableEnvironmentProbesText = "Disable Environment Probes";
        const string k_ShowShinySphereText = "Show Shiny Sphere";
        const string k_HideShinySphereText = "Hide Shiny Sphere";

        [SerializeField]
        ARPlaneManager m_PlaneManager;

        [SerializeField]
        ARMeshManager m_MeshManager;

        [SerializeField]
        ARTrackedImageManager m_ImageManager;

        [SerializeField]
        ARTrackedObjectManager m_ObjectManager;

        [SerializeField]
        ARAnchorManager m_AnchorManager;

        [SerializeField]
        AREnvironmentProbeManager m_EnvironmentProbeManager;

        [SerializeField]
        GameObject m_ShinySphere;

        [SerializeField]
        Text m_PlaneTrackingToggleText;

        [SerializeField]
        Text m_PlaneVisualsToggleText;

        [SerializeField]
        Text m_MeshTrackingToggleText;

        [SerializeField]
        Text m_MeshVisualsToggleText;

        [SerializeField]
        Text m_ImageTrackingToggleText;

        [SerializeField]
        Text m_ImageVisualsToggleText;

        [SerializeField]
        Text m_ObjectTrackingToggleText;

        [SerializeField]
        Text m_ObjectVisualsToggleText;

        [SerializeField]
        Text m_EnvironmentProbesToggleText;

        [SerializeField]
        Text m_ShinySphereToggleText;

        [SerializeField]
        GameObject m_HandTrackingToggleButton;

        [SerializeField]
        Text m_HandTrackingToggleText;

#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
        VisionOSLoader m_Loader;
        XRHandSubsystem m_HandSubsystem;
#endif

        bool m_PlaneVisualsEnabled = true;
        bool m_MeshVisualsEnabled = true;
        bool m_ImageVisualsEnabled = true;
        bool m_ObjectVisualsEnabled = true;

        static readonly List<ARAnchor> k_AnchorsToDestroy = new();

        void Awake()
        {
            UpdatePlaneTrackingToggleText();
            UpdatePlaneVisualsToggleText();
            UpdateMeshTrackingToggleText();
            UpdateMeshVisualsToggleText();
            UpdateEnvironmentProbesToggleText();
            UpdateShinySphereToggleText();

#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
                m_Loader = XRGeneralSettings.Instance.Manager.ActiveLoaderAs<VisionOSLoader>();

            // If the button doesn't exist, there's no point in setting up the rest of the hand tracking fields
            if (m_HandTrackingToggleButton == null)
                return;

            // If building in Windowed or Mixed Reality mode, VisionOSLoader may not be active
            if (m_Loader == null)
            {
                m_HandTrackingToggleButton.SetActive(false);
                return;
            }

            m_HandSubsystem = m_Loader.handSubsystem;
            UpdateHandTrackingToggleText();
#else
            if (m_HandTrackingToggleButton != null)
                m_HandTrackingToggleButton.SetActive(false);
#endif
        }

#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
        void UpdateHandTrackingToggleText()
        {
            if (m_HandTrackingToggleText == null)
            {
                Debug.LogError("Hand Tracking Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_HandTrackingToggleText.text = m_HandSubsystem == null ? k_EnableHandTrackingTest : k_DisableHandTrackingTest;
        }
#endif

        public void TogglePlaneTracking()
        {
            if (m_PlaneManager == null)
            {
                Debug.LogError("Plane Manager is null. Please set it in the inspector.");
                return;
            }

            m_PlaneManager.enabled = !m_PlaneManager.enabled;

            UpdatePlaneTrackingToggleText();
        }

        void UpdatePlaneTrackingToggleText()
        {
            if (m_PlaneTrackingToggleText == null)
            {
                Debug.LogError("Plane Tracking Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_PlaneTrackingToggleText.text = m_PlaneManager.enabled ? k_DisablePlaneTrackingText : k_EnablePlaneTrackingText;
        }

        public void ToggleMeshTracking()
        {
            if (m_MeshManager == null)
            {
                Debug.LogError("Mesh Manager is null. Please set it in the inspector.");
                return;
            }

            m_MeshManager.enabled = !m_MeshManager.enabled;

            UpdateMeshTrackingToggleText();
        }

        void UpdateMeshTrackingToggleText()
        {
            if (m_MeshTrackingToggleText == null)
            {
                Debug.LogError("Mesh Tracking Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_MeshTrackingToggleText.text = m_MeshManager.enabled ? k_DisableMeshTrackingText : k_EnableMeshTrackingText;
        }

        public void ToggleImageTracking()
        {
            if (m_ImageManager == null)
            {
                Debug.LogError("Image Manager is null. Please set it in the inspector.");
                return;
            }

            m_ImageManager.enabled = !m_ImageManager.enabled;

            UpdateImageTrackingToggleText();
        }

        void UpdateImageTrackingToggleText()
        {
            if (m_ImageTrackingToggleText == null)
            {
                Debug.LogError("Image Tracking Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_ImageTrackingToggleText.text = m_ImageManager.enabled ? k_DisableImageTrackingText : k_EnableImageTrackingText;
        }

        public void ToggleObjectTracking()
        {
            if (m_ObjectManager == null)
            {
                Debug.LogError("Object Manager is null. Please set it in the inspector.");
                return;
            }

            m_ObjectManager.enabled = !m_ObjectManager.enabled;

            UpdateObjectTrackingToggleText();
        }

        void UpdateObjectTrackingToggleText()
        {
            if (m_ObjectTrackingToggleText == null)
            {
                Debug.LogError("Object Tracking Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_ObjectTrackingToggleText.text = m_ObjectManager.enabled ? k_DisableObjectTrackingText : k_EnableObjectTrackingText;
        }

        public void ToggleEnvironmentProbes()
        {
            if (m_EnvironmentProbeManager == null)
            {
                Debug.LogError("Environment Probe Manager is null. Please set it in the inspector.");
                return;
            }

            m_EnvironmentProbeManager.enabled = !m_EnvironmentProbeManager.enabled;

            UpdateEnvironmentProbesToggleText();
        }

        void UpdateEnvironmentProbesToggleText()
        {
            if (m_EnvironmentProbesToggleText == null)
            {
                Debug.LogError("Environment Probes Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_EnvironmentProbesToggleText.text = m_EnvironmentProbeManager.enabled ? k_DisableEnvironmentProbesText : k_EnableEnvironmentProbesText;
        }

        public void TogglePlaneVisuals()
        {
            if (m_PlaneManager == null)
            {
                Debug.LogError("Plane Manager is null. Please set it in the inspector.");
                return;
            }

            m_PlaneVisualsEnabled = !m_PlaneVisualsEnabled;

            // Disable plane tracking so that new planes don't appear
            if (!m_PlaneVisualsEnabled && m_PlaneManager.enabled)
            {
                m_PlaneManager.enabled = false;
                UpdatePlaneTrackingToggleText();
            }

            foreach (var plane in m_PlaneManager.trackables)
            {
                plane.gameObject.SetActive(m_PlaneVisualsEnabled);
            }

            UpdatePlaneVisualsToggleText();
        }

        void UpdatePlaneVisualsToggleText()
        {
            if (m_PlaneVisualsToggleText == null)
            {
                Debug.LogError("Plane Visuals Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_PlaneVisualsToggleText.text = m_PlaneVisualsEnabled ? k_HidePlanesText : k_ShowPlanesText;
        }

        public void ToggleMeshVisuals()
        {
            if (m_MeshManager == null)
            {
                Debug.LogError("Mesh Manager is null. Please set it in the inspector.");
                return;
            }

            m_MeshVisualsEnabled = !m_MeshVisualsEnabled;

            // Disable mesh tracking so that new meshes don't appear
            if (!m_MeshVisualsEnabled && m_MeshManager.enabled)
            {
                m_MeshManager.enabled = false;
                UpdateMeshTrackingToggleText();
            }

            foreach (var mesh in m_MeshManager.meshes)
            {
                mesh.gameObject.SetActive(m_MeshVisualsEnabled);
            }

            UpdateMeshVisualsToggleText();
        }

        void UpdateMeshVisualsToggleText()
        {
            if (m_MeshVisualsToggleText == null)
            {
                Debug.LogError("Mesh Visuals Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_MeshVisualsToggleText.text = m_MeshVisualsEnabled ? k_HideMeshesText : k_ShowMeshesText;
        }

        public void ToggleImageVisuals()
        {
            if (m_ImageManager == null)
            {
                Debug.LogError("Image Manager is null. Please set it in the inspector.");
                return;
            }

            m_ImageVisualsEnabled = !m_ImageVisualsEnabled;

            // Disable image tracking so that new images don't appear
            if (!m_ImageVisualsEnabled && m_ImageManager.enabled)
            {
                m_ImageManager.enabled = false;
                UpdateImageTrackingToggleText();
            }

            foreach (var image in m_ImageManager.trackables)
            {
                image.gameObject.SetActive(m_ImageVisualsEnabled);
            }

            UpdateImageVisualsToggleText();
        }

        void UpdateImageVisualsToggleText()
        {
            if (m_ImageVisualsToggleText == null)
            {
                Debug.LogError("Image Visuals Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_ImageVisualsToggleText.text = m_ImageVisualsEnabled ? k_HideImagesText : k_ShowImagesText;
        }

        public void ToggleObjectVisuals()
        {
            if (m_ObjectManager == null)
            {
                Debug.LogError("Object Manager is null. Please set it in the inspector.");
                return;
            }

            m_ObjectVisualsEnabled = !m_ObjectVisualsEnabled;

            // Disable image tracking so that new images don't appear
            if (!m_ObjectVisualsEnabled && m_ObjectManager.enabled)
            {
                m_ObjectManager.enabled = false;
                UpdateObjectTrackingToggleText();
            }

            foreach (var trackedObject in m_ObjectManager.trackables)
            {
                trackedObject.gameObject.SetActive(m_ObjectVisualsEnabled);
            }

            UpdateObjectVisualsToggleText();
        }

        void UpdateObjectVisualsToggleText()
        {
            if (m_ObjectVisualsToggleText == null)
            {
                Debug.LogError("Object Visuals Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_ObjectVisualsToggleText.text = m_ObjectVisualsEnabled ? k_HideObjectsText : k_ShowObjectsText;
        }

        public void ToggleShinySphere()
        {
            if (m_ShinySphere == null)
            {
                Debug.LogError("Shiny Sphere is null. Please set it in the inspector.");
                return;
            }

            m_ShinySphere.SetActive(!m_ShinySphere.activeSelf);
            UpdateShinySphereToggleText();
        }

        void UpdateShinySphereToggleText()
        {
            if (m_ShinySphereToggleText == null)
            {
                Debug.LogError("Shiny Sphere Toggle Text is null. Please set it in the inspector.");
                return;
            }

            m_ShinySphereToggleText.text = m_ShinySphere.activeSelf ? k_HideShinySphereText : k_ShowShinySphereText;
        }

        public void ToggleHandSubsystem()
        {
#if INCLUDE_UNITY_XR_HANDS && (UNITY_VISIONOS || UNITY_EDITOR)
            if (m_Loader == null)
            {
                Debug.LogError("Cannot toggle hand tracking; VisionOS Loader is null.");
                return;
            }

            if (m_HandSubsystem == null)
            {
                m_Loader.CreateHandSubsystem();
                m_Loader.StartHandSubsystem();
                m_HandSubsystem = m_Loader.handSubsystem;
            }
            else
            {
                m_Loader.DestroyHandSubsystem();
                m_HandSubsystem = null;
            }

            UpdateHandTrackingToggleText();
#endif
        }

        public void ClearWorldAnchors()
        {
            if (m_AnchorManager == null)
            {
                Debug.LogError("Cannot clear world anchors; Anchor Manager is null. Please set it in the inspector.");
                return;
            }

            var anchorSubsystem = m_AnchorManager.subsystem;
            if (anchorSubsystem == null || !anchorSubsystem.running)
            {
                Debug.LogWarning("Cannot clear anchors if subsystem is not running");
                return;
            }

            // Copy anchors to a reusable list to avoid InvalidOperationException caused by Destroy modifying the list of anchors
            k_AnchorsToDestroy.Clear();
            foreach (var anchor in m_AnchorManager.trackables)
            {
                if (anchor == null)
                    continue;

                k_AnchorsToDestroy.Add(anchor);
            }

            foreach (var anchor in k_AnchorsToDestroy)
            {
                Debug.Log($"Destroying anchor with trackable id: {anchor.trackableId.ToString()}");
                Destroy(anchor.gameObject);
            }

            k_AnchorsToDestroy.Clear();
        }
    }
}

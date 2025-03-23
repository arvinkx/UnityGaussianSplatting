using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

#if UNITY_HAS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UnityEngine.XR.VisionOS.Samples.URP
{
    public class SampleUIController : MonoBehaviour
    {
        const string k_ShowPassthroughText = "Show Passthrough";
        const string k_ShowSkyboxText = "Show Skybox";

        const string k_HandTrackingAuthorizationFormat = "Hand Tracking Authorization: {0}";
        const string k_WorldSensingAuthorizationFormat = "World Sensing Authorization: {0}";

#if UNITY_HAS_URP
        const string k_EnableHDRText = "Enable HDR";
        const string k_DisableHDRText = "Disable HDR";

        const string k_EnablePostProcessingText = "Enable Post Processing";
        const string k_DisablePostProcessingText = "Disable Post Processing";
#endif

        [SerializeField]
        ParticleSystem m_ParticleSystem;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        GameObject m_FloorObject;

        [SerializeField]
        Text m_SkyboxToggleText;

        [SerializeField]
        Text m_HDRToggleText;

        [SerializeField]
        Text m_PostProcessingToggleText;

        [SerializeField]
        Text m_HandTrackingAuthorizationText;

        [SerializeField]
        Text m_WorldSensingAuthorizationText;

        [SerializeField]
        Button m_QuitButton;

        void Awake()
        {
            UpdateSkyboxToggleText();

#if UNITY_HAS_URP
            UpdateHDRText();
            UpdatePostProcessingText();
#endif

#if UNITY_VISIONOS || UNITY_EDITOR
            UpdateAuthorizationText();
#endif

#if UNITY_EDITOR
            // Disable quit button in Editor (play mode) because `Application.Quit` doesn't do anything in play mode
            m_QuitButton.interactable = false;
#endif
        }

#if UNITY_VISIONOS || UNITY_EDITOR
        void OnEnable()
        {
            VisionOS.AuthorizationChanged += OnAuthorizationChanged;
        }

        void OnDisable()
        {
            VisionOS.AuthorizationChanged += OnAuthorizationChanged;
        }

        void UpdateAuthorizationText()
        {
            var type = VisionOSAuthorizationType.HandTracking;
            var status = VisionOS.QueryAuthorizationStatus(type);
            OnAuthorizationChanged(new VisionOSAuthorizationEventArgs { type = type, status = status });

            type = VisionOSAuthorizationType.WorldSensing;
            status = VisionOS.QueryAuthorizationStatus(type);
            OnAuthorizationChanged(new VisionOSAuthorizationEventArgs { type = type, status = status });
        }

        void OnAuthorizationChanged(VisionOSAuthorizationEventArgs args)
        {
            switch (args.type)
            {
                case VisionOSAuthorizationType.HandTracking:
                    m_HandTrackingAuthorizationText.text = string.Format(k_HandTrackingAuthorizationFormat, args.status);
                    break;
                case VisionOSAuthorizationType.WorldSensing:
                    m_WorldSensingAuthorizationText.text = string.Format(k_WorldSensingAuthorizationFormat, args.status);
                    break;
                // We do not support CameraAccess yet so ignore it
            }
        }
#endif

        public void SetParticleStartSpeed(float speed)
        {
            var mainModule = m_ParticleSystem.main;
            mainModule.simulationSpeed = speed;
        }

        public void ToggleSkybox()
        {
            if (m_Camera.clearFlags == CameraClearFlags.Skybox)
            {
                m_Camera.clearFlags = CameraClearFlags.Color;

                // A clear color with alpha = 0 is required to show passthrough, as well as setting the Metal Immersion Style to Automatic or Mixed
                // You must reset the background color every time, because setting the clear flags to Skybox will override it
                m_Camera.backgroundColor = Color.clear;

                // The floor looks weird and is no longer necessary when passthrough is visible
                if (m_FloorObject != null)
                    m_FloorObject.SetActive(false);
            }
            else
            {
                m_Camera.clearFlags = CameraClearFlags.Skybox;

                if (m_FloorObject != null)
                    m_FloorObject.SetActive(true);
            }

            UpdateSkyboxToggleText();
        }

        void UpdateSkyboxToggleText()
        {
            m_SkyboxToggleText.text = m_Camera.clearFlags == CameraClearFlags.Skybox ? k_ShowPassthroughText : k_ShowSkyboxText;
        }

        public void ToggleHDR()
        {
#if UNITY_HAS_URP
            UniversalRenderPipeline.asset.supportsHDR = !UniversalRenderPipeline.asset.supportsHDR;
            var additionalCameraData = m_Camera.GetUniversalAdditionalCameraData();
            m_Camera.allowHDR = UniversalRenderPipeline.asset.supportsHDR;
            additionalCameraData.allowHDROutput = UniversalRenderPipeline.asset.supportsHDR;
            UpdateHDRText();
#endif
        }

        public void TogglePostProcessing()
        {
#if UNITY_HAS_URP
            var additionalCameraData = m_Camera.GetUniversalAdditionalCameraData();
            additionalCameraData.renderPostProcessing = !additionalCameraData.renderPostProcessing;
            UpdatePostProcessingText();
#endif
        }

#if UNITY_HAS_URP
        void UpdateHDRText()
        {
            m_HDRToggleText.text = UniversalRenderPipeline.asset.supportsHDR ? k_DisableHDRText : k_EnableHDRText;
        }

        void UpdatePostProcessingText()
        {
            m_PostProcessingToggleText.text = m_Camera.GetUniversalAdditionalCameraData().renderPostProcessing ? k_DisablePostProcessingText : k_EnablePostProcessingText;
        }
#endif

        public void DoQuit()
        {
            Application.Quit();
        }

        public void OnImageTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            // Deactivate any updated trackables that are no longer fully tracked
            foreach (var updatedTrackable in args.updated)
            {
                var isTracked = updatedTrackable.trackingState == TrackingState.Tracking;
                updatedTrackable.gameObject.SetActive(isTracked);
            }
        }

        public void OnObjectTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedObject> args)
        {
            // Deactivate any updated trackables that are no longer fully tracked
            foreach (var updatedTrackable in args.updated)
            {
                var isTracked = updatedTrackable.trackingState == TrackingState.Tracking;
                updatedTrackable.gameObject.SetActive(isTracked);
            }
        }
    }
}

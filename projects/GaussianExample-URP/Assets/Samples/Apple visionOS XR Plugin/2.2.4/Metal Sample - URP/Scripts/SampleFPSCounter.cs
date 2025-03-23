using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.XR.VisionOS.Samples.URP
{
    public class SampleFPSCounter : MonoBehaviour
    {
        [SerializeField]
        float m_RefreshPeriod = 0.75f;

        [SerializeField]
        Text m_FPSText;

        float m_LastRefreshTime;
        int m_LastRefreshFrame;

        void Awake()
        {
            if (m_FPSText == null)
            {
                Debug.LogError("FPS Text is not set on SampleFPSCounter. Please set it in the inspector.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            m_LastRefreshTime = 0;
        }

        void Update()
        {
            var unscaledTime = Time.unscaledTime;
            if (unscaledTime - m_RefreshPeriod < m_LastRefreshTime)
                return;

            var currentFrame = Time.frameCount;
            var elapsedTime = unscaledTime - m_LastRefreshTime;
            var elapsedFrames = currentFrame - m_LastRefreshFrame;
            m_FPSText.text = $"Average Frame Rate: {elapsedFrames / elapsedTime:0.0} FPS";

            m_LastRefreshTime = unscaledTime;
            m_LastRefreshFrame = currentFrame;
        }
    }
}

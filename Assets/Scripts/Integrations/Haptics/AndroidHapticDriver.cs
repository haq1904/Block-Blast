using System;
using BlockBlast.Core.Feedback;
using UnityEngine;

namespace BlockBlast.Integrations.Haptics
{
    /// <summary>
    /// Android platform haptic driver utilizing Vibrator and VibrationEffect APIs.
    /// Safely handles legacy API 25 and modern API 26+ amplitude control.
    /// </summary>
    internal sealed class AndroidHapticDriver : IHapticPlatformDriver
    {
        private readonly HapticCapabilities _capabilities;

#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly int _sdkInt;
        private readonly bool _hasAmplitudeControl;
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _vibrationEffectClass;
        private bool _isDisposed;
#endif

        public HapticCapabilities Capabilities => _capabilities;

        public AndroidHapticDriver()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _sdkInt = versionClass.GetStatic<int>("SDK_INT");
                }

                using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (currentActivity != null)
                    {
                        _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }

                if (_vibrator != null && _vibrator.Call<bool>("hasVibrator"))
                {
                    HapticCapabilities caps = HapticCapabilities.BasicPulse | HapticCapabilities.Pattern;

                    if (_sdkInt >= 26)
                    {
                        _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                        _hasAmplitudeControl = _vibrator.Call<bool>("hasAmplitudeControl");
                        if (_hasAmplitudeControl)
                        {
                            caps |= HapticCapabilities.Intensity;
                        }
                    }

                    _capabilities = caps;
                }
                else
                {
                    _capabilities = HapticCapabilities.None;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AndroidHapticDriver] Failed to initialize Android Vibrator: {ex.Message}");
                _capabilities = HapticCapabilities.None;
            }
#else
            _capabilities = HapticCapabilities.None;
#endif
        }

        public bool TryPlay(in HapticRequest request)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_isDisposed || _vibrator == null || _capabilities == HapticCapabilities.None)
            {
                return false;
            }

            try
            {
                int amplitude = _hasAmplitudeControl
                    ? Mathf.Clamp(Mathf.RoundToInt(request.Intensity * 255f), 1, 255)
                    : -1;

                switch (request.Pattern)
                {
                    case HapticPattern.Selection:
                        PlayPulse(15, amplitude);
                        return true;

                    case HapticPattern.ImpactLight:
                        PlayPulse(25, amplitude);
                        return true;

                    case HapticPattern.ImpactMedium:
                        PlayPulse(40, amplitude);
                        return true;

                    case HapticPattern.ImpactHeavy:
                        PlayPulse(65, amplitude);
                        return true;

                    case HapticPattern.Success:
                        PlayWaveform(
                            new long[] { 0, 30, 50, 45 },
                            new int[] { 0, (int)(amplitude * 0.7f), 0, amplitude });
                        return true;

                    case HapticPattern.Warning:
                        PlayWaveform(
                            new long[] { 0, 45, 50, 45 },
                            new int[] { 0, amplitude, 0, amplitude });
                        return true;

                    case HapticPattern.Error:
                        PlayWaveform(
                            new long[] { 0, 40, 40, 40, 40, 50 },
                            new int[] { 0, amplitude, 0, (int)(amplitude * 0.8f), 0, amplitude });
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AndroidHapticDriver] Error playing haptic request: {ex.Message}");
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void PlayPulse(long durationMs, int amplitude)
        {
            if (_sdkInt >= 26 && _vibrationEffectClass != null)
            {
                using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                {
                    _vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                _vibrator.Call("vibrate", durationMs);
            }
        }

        private void PlayWaveform(long[] timings, int[] amplitudes)
        {
            if (_sdkInt >= 26 && _vibrationEffectClass != null)
            {
                if (_hasAmplitudeControl)
                {
                    using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
            }
            else
            {
                _vibrator.Call("vibrate", timings, -1);
            }
        }
#endif

        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _vibrationEffectClass?.Dispose();
                _vibrationEffectClass = null;

                _vibrator?.Dispose();
                _vibrator = null;
            }
            catch
            {
                // Suppress disposal errors during teardown
            }
#endif
        }
    }
}

using BlockBlast.Core.Feedback;
using Unity.Cinemachine;
using UnityEngine;

namespace BlockBlast.Integrations.Cinemachine
{
    /// <summary>
    /// Cinemachine-specific camera shake profile housing CinemachineImpulseDefinition and default direction.
    /// </summary>
    [CreateAssetMenu(fileName = "CinemachineCameraShakeProfile", menuName = "Feedback/Camera Shake/Cinemachine Profile")]
    public sealed class CinemachineCameraShakeProfileSO : CameraShakeProfileSO
    {
        [SerializeField]
        [Tooltip("Impulse definition governing impulse shape, duration, type, dissipation, and channel.")]
        private CinemachineImpulseDefinition _impulseDefinition = new CinemachineImpulseDefinition();

        [SerializeField]
        [Tooltip("Default direction vector for the impulse if none is explicitly specified in the request.")]
        private Vector3 _defaultDirection = Vector3.down;

        public CinemachineImpulseDefinition ImpulseDefinition => _impulseDefinition;

        public Vector3 DefaultDirection
        {
            get
            {
                if (_defaultDirection.sqrMagnitude < 0.0001f)
                {
                    return Vector3.down;
                }
                return _defaultDirection.normalized;
            }
        }

        public void Initialize(float baseAmplitude, Vector3 defaultDirection, CinemachineImpulseDefinition impulseDefinition)
        {
            SetBaseAmplitudeForTesting(baseAmplitude);
            _defaultDirection = defaultDirection;
            if (impulseDefinition != null)
            {
                _impulseDefinition = impulseDefinition;
            }
        }
    }
}

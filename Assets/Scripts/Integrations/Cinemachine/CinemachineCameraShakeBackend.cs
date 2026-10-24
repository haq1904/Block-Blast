using BlockBlast.Core.Feedback;
using Unity.Cinemachine;
using UnityEngine;

namespace BlockBlast.Integrations.Cinemachine
{
    /// <summary>
    /// Cinemachine 3 impulse adapter executing camera shake requests via CinemachineImpulseDefinition.
    /// </summary>
    public sealed class CinemachineCameraShakeBackend : MonoBehaviour, ICameraShakeBackend
    {
        [SerializeField]
        [Tooltip("Optional transform serving as default emitter origin. If unassigned, uses this GameObject's transform.")]
        private Transform _defaultEmitterOrigin;

        public bool IsSupported => enabled && gameObject.activeInHierarchy;

        public bool TryPlay(in CameraShakeRequest request)
        {
            if (!IsSupported)
            {
                return false;
            }

            CinemachineCameraShakeProfileSO profile = request.Profile as CinemachineCameraShakeProfileSO;
            if (profile == null)
            {
                return false;
            }

            CinemachineImpulseDefinition definition = profile.ImpulseDefinition;
            if (definition == null)
            {
                return false;
            }

            float effectiveAmplitude = request.EffectiveAmplitude;
            if (effectiveAmplitude <= 0f)
            {
                return false;
            }

            Vector3 direction = request.ExplicitDirection;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = profile.DefaultDirection;
            }
            else
            {
                direction.Normalize();
            }

            Vector3 velocity = direction * effectiveAmplitude;

            Vector3 origin;
            if (request.ExplicitPosition.HasValue)
            {
                origin = request.ExplicitPosition.Value;
            }
            else if (_defaultEmitterOrigin != null)
            {
                origin = _defaultEmitterOrigin.position;
            }
            else
            {
                origin = transform.position;
            }

            definition.CreateEvent(origin, velocity);
            return true;
        }

        public void SetDefaultEmitterOriginForTesting(Transform origin)
        {
            _defaultEmitterOrigin = origin;
        }
    }
}

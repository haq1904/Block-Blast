using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.GameFlow.Play
{
    /// <summary>
    /// Keeps PlayScene content dormant until the scene becomes Unity's active scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaySceneActivationController : MonoBehaviour
    {
        [SerializeField] private GameObject _gameplayEnvironment;
        [SerializeField] private GameObject _gameplaySystems;

        public bool IsGameplayActive { get; private set; }

        public GameObject GameplayEnvironment
        {
            get => _gameplayEnvironment;
            set => _gameplayEnvironment = value;
        }

        public GameObject GameplaySystems
        {
            get => _gameplaySystems;
            set => _gameplaySystems = value;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            if (SceneManager.GetActiveScene() == gameObject.scene)
            {
                ActivateGameplay();
            }
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        public void ActivateGameplay()
        {
            if (IsGameplayActive)
            {
                return;
            }

            if (!ValidateConfiguration())
            {
                return;
            }

            _gameplayEnvironment.SetActive(true);
            _gameplaySystems.SetActive(true);
            IsGameplayActive = true;
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (nextScene == gameObject.scene)
            {
                ActivateGameplay();
            }
        }

        private bool ValidateConfiguration()
        {
            if (_gameplayEnvironment == null || _gameplaySystems == null)
            {
                Debug.LogError("[PlaySceneActivationController] Gameplay environment and systems must be assigned.", this);
                return false;
            }

            if (_gameplayEnvironment == _gameplaySystems ||
                _gameplayEnvironment.scene != gameObject.scene ||
                _gameplaySystems.scene != gameObject.scene)
            {
                Debug.LogError("[PlaySceneActivationController] Gameplay roots must be distinct objects in PlayScene.", this);
                return false;
            }

            return true;
        }
    }
}

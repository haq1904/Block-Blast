using System;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    [Serializable]
    public sealed class WorldDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _thumbnail;
        [SerializeField] private WorldScenePrefabReferences _worldScene = new WorldScenePrefabReferences();
        [SerializeField] private LobbyScenePrefabReferences _lobbyScene = new LobbyScenePrefabReferences();
        [SerializeField] private PlayScenePrefabReferences _playScene = new PlayScenePrefabReferences();

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Thumbnail => _thumbnail;
        public WorldScenePrefabReferences WorldScene => _worldScene;
        public LobbyScenePrefabReferences LobbyScene => _lobbyScene;
        public PlayScenePrefabReferences PlayScene => _playScene;

        public WorldDefinition() { }

        public WorldDefinition(
            string id,
            string displayName,
            Sprite thumbnail,
            WorldScenePrefabReferences worldScene,
            LobbyScenePrefabReferences lobbyScene,
            PlayScenePrefabReferences playScene)
        {
            _id = id;
            _displayName = displayName;
            _thumbnail = thumbnail;
            _worldScene = worldScene ?? new WorldScenePrefabReferences();
            _lobbyScene = lobbyScene ?? new LobbyScenePrefabReferences();
            _playScene = playScene ?? new PlayScenePrefabReferences();
        }

        public bool TryGetPrefab(WorldEnvironmentPart part, out GameObject prefab)
        {
            prefab = null;
            switch (part)
            {
                case WorldEnvironmentPart.WorldGround:
                    prefab = _worldScene?.Ground;
                    return prefab != null;
                case WorldEnvironmentPart.LobbyControllerTab:
                    prefab = _lobbyScene?.ControllerTab;
                    return prefab != null;
                case WorldEnvironmentPart.LobbyNamePanel:
                    prefab = _lobbyScene?.NamePanel;
                    return prefab != null;
                case WorldEnvironmentPart.PlayScorePanel:
                    prefab = _playScene?.ScorePanel;
                    return prefab != null;
                case WorldEnvironmentPart.PlayGround:
                    prefab = _playScene?.Ground;
                    return prefab != null;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public sealed class WorldScenePrefabReferences
    {
        [SerializeField] private GameObject _ground;

        public GameObject Ground => _ground;

        public WorldScenePrefabReferences() { }

        public WorldScenePrefabReferences(GameObject ground)
        {
            _ground = ground;
        }
    }

    [Serializable]
    public sealed class LobbyScenePrefabReferences
    {
        [SerializeField] private GameObject _controllerTab;
        [SerializeField] private GameObject _namePanel;

        public GameObject ControllerTab => _controllerTab;
        public GameObject NamePanel => _namePanel;

        public LobbyScenePrefabReferences() { }

        public LobbyScenePrefabReferences(GameObject controllerTab, GameObject namePanel)
        {
            _controllerTab = controllerTab;
            _namePanel = namePanel;
        }
    }

    [Serializable]
    public sealed class PlayScenePrefabReferences
    {
        [SerializeField] private GameObject _scorePanel;
        [SerializeField] private GameObject _ground;

        public GameObject ScorePanel => _scorePanel;
        public GameObject Ground => _ground;

        public PlayScenePrefabReferences() { }

        public PlayScenePrefabReferences(GameObject scorePanel, GameObject ground)
        {
            _scorePanel = scorePanel;
            _ground = ground;
        }
    }
}

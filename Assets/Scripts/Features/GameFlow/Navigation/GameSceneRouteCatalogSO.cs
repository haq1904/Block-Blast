using System;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation
{
    /// <summary>
    /// Configuration asset mapping logical GameSceneRoute enum values to Unity Build Settings scene indexes.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSceneRouteCatalog", menuName = "Block Blast/Game Flow/Scene Route Catalog")]
    public class GameSceneRouteCatalogSO : ScriptableObject
    {
        [Header("Scene Build Indexes")]
        [Tooltip("Build Settings index for IntroScene.")]
        [SerializeField] private int _introBuildIndex = 1;

        [Tooltip("Build Settings index for LobbyScene.")]
        [SerializeField] private int _lobbyBuildIndex = 2;

        [Tooltip("Build Settings index for PlayScene.")]
        [SerializeField] private int _playBuildIndex = 3;

        [Tooltip("Build Settings index for WorldScene.")]
        [SerializeField] private int _worldBuildIndex = 4;

        public int IntroBuildIndex => _introBuildIndex;
        public int LobbyBuildIndex => _lobbyBuildIndex;
        public int PlayBuildIndex => _playBuildIndex;
        public int WorldBuildIndex => _worldBuildIndex;

        /// <summary>
        /// Resolves the Unity Build Settings index for the given logical game route.
        /// </summary>
        public int GetBuildIndex(GameSceneRoute route)
        {
            return route switch
            {
                GameSceneRoute.Intro => ValidateIndex(_introBuildIndex, GameSceneRoute.Intro),
                GameSceneRoute.Lobby => ValidateIndex(_lobbyBuildIndex, GameSceneRoute.Lobby),
                GameSceneRoute.Play => ValidateIndex(_playBuildIndex, GameSceneRoute.Play),
                _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unsupported GameSceneRoute value.")
            };
        }

        private static int ValidateIndex(int index, GameSceneRoute route)
        {
            if (index < 0)
            {
                throw new InvalidOperationException($"Build index for route {route} is invalid ({index}). Must be non-negative.");
            }
            return index;
        }

        /// <summary>
        /// Validates that all route build indexes are non-negative and distinct.
        /// </summary>
        public void ValidateConfiguration()
        {
            if (_introBuildIndex < 0 || _lobbyBuildIndex < 0 || _playBuildIndex < 0 || _worldBuildIndex < 0)
            {
                throw new InvalidOperationException("All scene build indexes in route catalog must be non-negative.");
            }

            var set = new System.Collections.Generic.HashSet<int>
            {
                _introBuildIndex,
                _lobbyBuildIndex,
                _playBuildIndex,
                _worldBuildIndex
            };

            if (set.Count < 4)
            {
                throw new InvalidOperationException("Duplicate build indexes detected in GameSceneRouteCatalog.");
            }
        }

        public void SetIndexesForTesting(int introIndex, int lobbyIndex, int playIndex, int worldIndex = 4)
        {
            _introBuildIndex = introIndex;
            _lobbyBuildIndex = lobbyIndex;
            _playBuildIndex = playIndex;
            _worldBuildIndex = worldIndex;
        }
    }
}

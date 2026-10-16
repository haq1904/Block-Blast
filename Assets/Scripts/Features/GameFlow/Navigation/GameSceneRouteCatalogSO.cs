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

        public int IntroBuildIndex => _introBuildIndex;
        public int LobbyBuildIndex => _lobbyBuildIndex;
        public int PlayBuildIndex => _playBuildIndex;

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

        public void SetIndexesForTesting(int introIndex, int lobbyIndex, int playIndex)
        {
            _introBuildIndex = introIndex;
            _lobbyBuildIndex = lobbyIndex;
            _playBuildIndex = playIndex;
        }
    }
}

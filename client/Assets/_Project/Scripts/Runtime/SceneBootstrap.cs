using UnityEngine;

namespace BattleHunter.Client
{
    /// <summary>
    /// Raiz de cada cena. Monta a tela correspondente em código (sem prefabs nesta fatia),
    /// para o projeto ser reproduzível a partir do repositório.
    /// </summary>
    public sealed class SceneBootstrap : MonoBehaviour
    {
        public string sceneName = "Boot";

        private void Start()
        {
            switch (sceneName)
            {
                case "Boot":
                    gameObject.AddComponent<Screens.BootScreen>();
                    break;
                case "Guilda":
                    gameObject.AddComponent<Screens.GuildScreen>();
                    break;
                case "Lobby":
                    gameObject.AddComponent<Screens.LobbyScreen>();
                    break;
                case "Partida":
                    gameObject.AddComponent<Screens.MatchScreen>();
                    break;
                case "Resultado":
                    gameObject.AddComponent<Screens.ResultScreen>();
                    break;
                default:
                    Debug.LogError($"Cena desconhecida: {sceneName}");
                    break;
            }
        }
    }
}

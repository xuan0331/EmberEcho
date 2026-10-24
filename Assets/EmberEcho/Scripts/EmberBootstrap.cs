using UnityEngine;

namespace EmberEcho
{
    public static class EmberBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Object.FindAnyObjectByType<EmberGame>() != null)
            {
                return;
            }

            var gameObject = new GameObject("EmberEchoGame");
            gameObject.AddComponent<EmberGame>();
        }
    }
}

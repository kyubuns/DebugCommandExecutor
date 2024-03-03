using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace DebugCommandExecutor
{
    public class RemoteExecutor : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static readonly Guid DebugCommandMessage = Guid.Parse("889c3bf6-de3c-4024-aaf3-d6859c32a43f");

        [Conditional("DEVELOPMENT_BUILD")]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnRuntimeInitializeOnLoadMethod()
        {
            var gameObject = new GameObject("DebugCommand.RemoteExecutor");
            gameObject.AddComponent<RemoteExecutor>();
            DontDestroyOnLoad(gameObject);
        }

        protected void OnEnable()
        {
            var connection = PlayerConnection.instance;
            if (connection != null)
            {
                connection.Register(DebugCommandMessage, OnReceiveTextMessage);
            }
        }

        protected void OnDisable()
        {
            var connection = PlayerConnection.instance;
            if (connection != null)
            {
                connection.Unregister(DebugCommandMessage, OnReceiveTextMessage);
            }
        }

        private static void OnReceiveTextMessage(MessageEventArgs args)
        {
            var text = System.Text.Encoding.UTF8.GetString(args.data);
            DebugCommand.Execute(text);
        }
#endif
    }
}

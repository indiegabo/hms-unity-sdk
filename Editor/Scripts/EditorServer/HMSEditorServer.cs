using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HMSUnitySDK.Editor
{
    [InitializeOnLoad]
    public class HMSEditorServer
    {
        private const int PORT = 54999;
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static volatile bool _isRunning;
        private static readonly object _lockObject = new();
        private static readonly Queue<Action> mainThreadActions = new();
        private static volatile bool processingMainThreadActions = false;

        static HMSEditorServer()
        {
            // Register callbacks for recompilations
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            StartServer();
            EditorApplication.quitting += StopServer;
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServer();
        }

        private static void OnAfterAssemblyReload()
        {
            StartServer();
        }

        private static void StartServer()
        {
            if (!CanStartServer())
            {
                StopServer();
                return;
            }

            lock (_lockObject)
            {
                if (_isRunning) return;

                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://localhost:{PORT}/");
                    _listener.Start();

                    _isRunning = true;
                    _serverThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Name = "HMSEditorServer"
                    };
                    _serverThread.Start();

                    // Ensure the log only occurs after the thread is actually started
                    // EditorApplication.delayCall += () =>
                    // {
                    //     Debug.Log($"Editor status server started on http://localhost:{PORT}/");
                    // };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to start editor server:");
                    Debug.LogException(e);
                    _isRunning = false;
                }
            }
        }

        private static void StopServer()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                _isRunning = false;

                try
                {
                    if (_listener != null && _listener.IsListening)
                    {
                        _listener.Stop();
                        _listener.Close();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error while stopping listener: {e}");
                }

                try
                {
                    if (_serverThread != null && _serverThread.IsAlive)
                    {
                        if (!_serverThread.Join(1000))
                        {
                            _serverThread.Abort();
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error while stopping server thread: {e}");
                }
            }
        }

        private static void Listen()
        {
            while (_isRunning)
            {
                try
                {
                    // Add a small delay to reduce CPU consumption
                    Thread.Sleep(10); // 10ms pause between iterations

                    // Use BeginGetContext to avoid blocking the thread
                    var asyncResult = _listener.BeginGetContext(new System.AsyncCallback(ListenerCallback), null);

                    // Wait non-blockingly
                    while (!asyncResult.IsCompleted && _isRunning)
                    {
                        Thread.Sleep(1); // Shorter wait during asynchronous operation
                    }

                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    // Normal shutdown
                    break;
                }
                catch (System.Exception e)
                {
                    if (_isRunning) // Only log if it's not an intentional shutdown
                    {
                        Debug.LogError($"Error in editor server: {e}");
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        private static void ProcessRequest(object state)
        {
            var context = (HttpListenerContext)state;

            try
            {
                if (context.Request.Url.AbsolutePath == "/status")
                {
                    HandleStatusRequest(context);
                }
                else if (context.Request.Url.AbsolutePath == "/start")
                {
                    // Enfileira a ação para a main thread e responde imediatamente
                    EnqueueMainThreadAction(() => HandleStartRequest(context));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    WriteResponse(context, "Not Found");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing request: {e}");
                context.Response.StatusCode = 500;
                WriteResponse(context, "Internal Server Error");
            }
            finally
            {
                if (context.Request.Url.AbsolutePath != "/start") // Já respondemos /start antecipadamente
                {
                    context.Response.Close();
                }
            }
        }

        private static void HandleStatusRequest(HttpListenerContext context)
        {
            // Resposta simples indicando que o editor está aberto
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            var responseData = new StatusResponse
            {
                status = "running",
            };
            string jsonResponse = JsonUtility.ToJson(responseData);

            WriteResponse(context, jsonResponse);
        }

        private static void ProcessMainThreadActions()
        {
            Action actionToExecute = null;

            lock (mainThreadActions)
            {
                if (mainThreadActions.Count > 0)
                {
                    actionToExecute = mainThreadActions.Dequeue();
                }
                else
                {
                    processingMainThreadActions = false;
                    return;
                }
            }

            try
            {
                actionToExecute?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing main thread action: {e}");
            }

            // Agenda o próximo processamento
            EditorApplication.delayCall += ProcessMainThreadActions;
        }

        private static void EnqueueMainThreadAction(Action action)
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(action);

                if (!processingMainThreadActions)
                {
                    processingMainThreadActions = true;
                    EditorApplication.delayCall += ProcessMainThreadActions;
                }
            }
        }

        private static void HandleStartRequest(HttpListenerContext context)
        {
            try
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;

                var initialResponse = "{\"status\":\"started\"}";
                var buffer = Encoding.UTF8.GetBytes(initialResponse);
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Flush();

                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Initialization.unity");

                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.isPlaying = true;
                    };
                }
                else
                {
                    EditorApplication.isPlaying = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in start request: {e}");
                // Não podemos modificar a resposta aqui - já foi enviada
            }
            finally
            {
                context.Response.Close();
            }
        }

        private static void WriteResponse(HttpListenerContext context, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        private static void ListenerCallback(IAsyncResult result)
        {
            try
            {
                if (!_isRunning || !_listener.IsListening) return;

                var context = _listener.EndGetContext(result);
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in listener callback: {e}");
            }
        }

        private static bool CanStartServer()
        {
            var runtimeInfo = HMSRuntimeInfo.GetFromResources();
            return runtimeInfo != null && runtimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor;
        }
    }

    [System.Serializable]
    struct StatusResponse
    {
        public string status;
    }
}

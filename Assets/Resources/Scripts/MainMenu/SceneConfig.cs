using FishNet.Managing;
using FishNet.Managing.Client;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using FishNet.Utility;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using FishNet.Object;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
using FishNet;

public class SceneConfig : MonoBehaviour
{
    [SerializeField, Scene]
    private string _offlineScene;
    [SerializeField, Scene]
    private string _lobbyScene;
    [SerializeField, Scene]
    private string _gameScene;

    private LocalConnectionState _clientState = LocalConnectionState.Stopped;
    private LocalConnectionState _serverState = LocalConnectionState.Stopped;

    private NetworkManager _networkManager;

    private bool lobbySceneLoaded;
    private bool gameSceneLoaded;

    public bool startedAtHost;

    private void Awake()
    {
        InitializeOnce();
    }

    private void OnDestroy()
    {
        if (! ApplicationState.IsQuitting() && _networkManager != null && _networkManager.Initialized)
        {
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;

        }
    }

    private void InitializeOnce()
    {
        _networkManager = GetComponentInParent<NetworkManager>();

        if (_networkManager == null)
        {
            if (NetworkManager.StaticCanLog(LoggingType.Error))
                Debug.LogError($"NetworkManager not found on {gameObject.name} or any parent objects.DefaultScene will not work.");
            return;
        }

        if (!_networkManager.Initialized)
            return;
        if(_lobbyScene == string.Empty || _offlineScene == string.Empty || _gameScene == string.Empty)
        {
            if (_networkManager.CanLog(LoggingType.Warning))
                Debug.LogWarning("Scene(s) not specified.Scenee in SceneConfig will not load.");
        }

        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        _serverState = obj.ConnectionState;

        if (_serverState == LocalConnectionState.Started)
        {
            SceneLoadData sld = new SceneLoadData(GetNameScene(_lobbyScene));
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }
        else if(_serverState == LocalConnectionState.Stopped)
        {
            LoadOfflineScene();
        }
    }

    private void LoadOfflineScene()
    {
        if (UnitySceneManager.GetActiveScene().name == GetNameScene(_offlineScene))
            return;
        UnitySceneManager.LoadScene(_offlineScene);
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        _clientState = obj.ConnectionState;

        if (_clientState == LocalConnectionState.Stopped)
        {
            if (!_networkManager.IsServer)
                LoadOfflineScene();
        }
    }

    private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs obj)
    {
        lobbySceneLoaded = false;
        gameSceneLoaded = false;
        foreach (Scene s in obj.LoadedScenes)
        {
            if(s.name == GetNameScene(_lobbyScene))
            {
                lobbySceneLoaded = true;
            }
            else if (s.name == GetNameScene(_gameScene))
            {
                lobbySceneLoaded = false;
            }
        }

        
        if (lobbySceneLoaded)
        {
            UnloadOfflineScene();

            Button startGameButton = GameObject.Find("Button_StartGame").GetComponent<Button>();
            Button readyButton = GameObject.Find("Button_Ready").GetComponent<Button>();


            GameObject go = Instantiate(Resources.Load("Prefabs/NetworkServer")) as GameObject;
            go.name = go.name.Replace("(Clone)", "");
            InstanceFinder.ServerManager.Spawn(go, null);
            
            if (_networkManager.IsServer)
            {
                startGameButton.onClick.AddListener(() => { LoadGameScene(); });
                GamePlayManager.Instance.StartGameButton = startGameButton.gameObject;

                if (startedAtHost)
                {
                    readyButton.onClick.AddListener(() =>
                    {
                        if (!Player.Instance)
                            Debug.LogError("Player not loaded yet !");
                        else
                            Player.Instance.ToggleReadyState();
                    });
                }
                else
                {
                    readyButton.gameObject.SetActive(false);
                }
            }
            else if (_networkManager.IsClient)
            {
                readyButton.onClick.AddListener(() =>
                {
                    if (!Player.Instance)
                        Debug.LogError("Player not loaded yet !");
                    else
                        Player.Instance.ToggleReadyState();
                });

                if (!_networkManager.IsServer)
                {
                    startGameButton.gameObject.SetActive(false);
                }

                
            }


        }


    }

    private string GetNameScene(string fullpath)
    {
        return Path.GetFileNameWithoutExtension(fullpath);
    }
    private void LoadGameScene()
    {
        lobbySceneLoaded = false;
        SceneLoadData sld = new SceneLoadData(GetNameScene(_gameScene));
        sld.ReplaceScenes = ReplaceOption.All;
        sld.MovedNetworkObjects = new NetworkObject[] { GameObject.Find("Player(Clone)").GetComponent<NetworkObject>(), GameObject.Find("NetworkServer").GetComponent<NetworkObject>()};
        _networkManager.SceneManager.LoadGlobalScenes(sld);
    }
    private void UnloadLobbyScene()
    {
        Scene s = UnitySceneManager.GetSceneByName(GetNameScene(_lobbyScene));
        if (string.IsNullOrEmpty(s.name))
            return;
        UnitySceneManager.UnloadSceneAsync(s);
    }
    private void UnloadOfflineScene()
    {
        Scene s = UnitySceneManager.GetSceneByName(GetNameScene(_offlineScene));
        if (string.IsNullOrEmpty(s.name))
            return;
        UnitySceneManager.UnloadSceneAsync(s);
    }
    public void onClickServer()
    {
        startedAtHost = false;
        if (_networkManager == null)
            return;
        if (_serverState != LocalConnectionState.Stopped)
            _networkManager.ServerManager.StopConnection(true);
        else
            _networkManager.ServerManager.StartConnection();
    }

    public void onClickClient()
    {
        startedAtHost = false;
        if (_networkManager == null)
            return;
        if (_clientState != LocalConnectionState.Stopped)
            _networkManager.ClientManager.StopConnection();
        else
            _networkManager.ClientManager.StartConnection();
    }

    public void onClickHost()
    {
        onClickServer();
        onClickClient();
        startedAtHost = true;
    }
}

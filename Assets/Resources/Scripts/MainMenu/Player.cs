using FishNet.Object.Synchronizing;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public static Player Instance;

    [SerializeField]
    GameObject lobbyPlayerCard;

    [SerializeField]
    Text playerNameText;

    [field: SerializeField]
    [field: SyncVar(OnChange = nameof(OnChangePlayerReady))]
    public bool IsReady { get; private set; }

    [field: SyncVar(OnChange = nameof(OnChangePlayerName))]
    public string playerName;

    public void OnChangePlayerReady(bool oldValue, bool newValue, bool isServer)
    {
        if (!isServer)
            playerNameText.text = playerName + "\nReady : " + IsReady;
    }

    public void OnChangePlayerName(string oldValue, string newValue, bool isServer)
    {
        if (!isServer)
            playerNameText.text = playerName + "\nReady : " + IsReady;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        GameObject playerCardPanel = GameObject.Find("Panel_Players");
        if (!playerCardPanel)
        {
            Debug.LogError("ui lobby player for panel not found");
        }
        else
        {
            GameObject playerCard = Instantiate(lobbyPlayerCard, playerCardPanel.transform);
            playerNameText = playerCard.GetComponentInChildren<Text>();
            if (!playerNameText)
                Debug.Log("playerNameText not found");

        }
        if (!IsOwner)
            return;
        Instance = this;
        ChangePlayerName("Player: " + OwnerId);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GamePlayManager.Instance.players.Add(this);
    }

    [ServerRpc]
    public void ToggleReadyState()
    {
        IsReady = !IsReady;
    }

    [ServerRpc]
    public void ChangePlayerName(string name)
    {
        playerName = name;
    }
    void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

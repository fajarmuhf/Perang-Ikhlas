using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : NetworkBehaviour
{
    public static GamePlayManager Instance;

    public GameObject StartGameButton;

    [field: SyncObject]
    public readonly SyncList<Player> players = new SyncList<Player>();
    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameObject.Find("Button_StartGame") != null)
        {
            StartGameButton = GameObject.Find("Button_StartGame").gameObject;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer)
            return;

        if (StartGameButton != null)
        {
            bool allPlayerReady = players.Count > 0;
            foreach (Player player in players)
            {
                if (!player.IsReady)
                    allPlayerReady = false;
            }

            if (allPlayerReady && !StartGameButton.activeSelf)
                StartGameButton.SetActive(true);
            else if (!allPlayerReady && StartGameButton.activeSelf)
                StartGameButton.SetActive(false);
        }
    }
}

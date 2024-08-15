using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Unity.Collections;
using System.Text;

public class Multiplayer : NetworkBehaviour
{
    public GameObject DisconnectedCanvas;
    public GameObject VictoryCanvas;
    public GameObject GameLoadCanvas; // Will be called on SCENE CHANGE SHOULD BE ACTIVE
    public Transform WhitePiecePrefab;
    public Transform BlackPiecePrefab;
    private Piece[,] Pieces = new Piece[8, 8];

    public NetworkVariable<bool> SetCurrentTurnNetworkVariable = new NetworkVariable<bool>(true);

    private Vector3 BoardOffset = new Vector3(-4f, 0, -4f);
    private Vector3 PieceOffset = new Vector3(0.5f, 0.125f, 0.5f);

    private Vector2 mouseOver;
    private Vector2 startDrag = -Vector2.one;
    private Vector2 endDrag;

    private Piece SelectedPiece;
    public List<Piece> forcedPieces = new List<Piece>();

    private bool hasKilled;
    public NetworkVariable<bool> isWhite;
    private bool isWhiteTurn = true;
    public bool TogglePieceDrag = true;
    private int connectedclients = 0;
    private float timer = -9999F;

    private NetworkVariable<FixedString32Bytes> displayName = new NetworkVariable<FixedString32Bytes>();
    private NetworkVariable<bool> AlreadyRematched = new NetworkVariable<bool>(false);

    public CanvasGroup alertCanvas;
    private float lastAlert;
    public bool alertActive;


    public void RematchButton()
    {
        RematchServerRpc();

        if (IsLocalPlayer)
        {
            //Debug.LogError("Started creating pieces for rematch, Owner ID: " + OwnerClientId);
            StartCoroutine(CreatePiecesCoroutine());
        }
    }

    public IEnumerator RematchCoroutine()
    {
        yield return new WaitForSeconds(10f);
        RematchButton();
        IndicateTurn(0);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RematchServerRpc()
    {
        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;

        if (IsLocalPlayer && AlreadyRematched.Value == false)
        {
            foreach (NetworkClient ncl in clients)
            {
                ncl.PlayerObject.GetComponent<Multiplayer>().AlreadyRematched.Value = true;
            }
            //Debug.LogError("DESTROYED ALL PIECES.. SHOULD BE ONLY CALLED ONCE");
            DestroyRemainingPieces();
        }
        SetCurrentTurnNetworkVariable.Value = true;
        RematchClientRpc();
    }

    [ClientRpc(RequireOwnership = false)]
    private void RematchClientRpc()
    {
        SelectedPiece = null;
        timer = -15f;
        //IndicateTurn(0);
    }

    private void DestroyRemainingPieces()
    {
        GameObject[] piece = GameObject.FindGameObjectsWithTag("Piece");
        for (int i = 0; i < piece.Length; i++)
        {
            if (piece[i] != null && piece[i].activeSelf)
            {
                piece[i].GetComponent<NetworkObject>().Despawn();
                DestroyImmediate(piece[i].gameObject);
            }
        }
        //Debug.LogError("Pieces Destroyed count: " + piece.Length);
    }

    public void Alert(string text)
    {
        alertCanvas.GetComponentInChildren<TextMeshProUGUI>().text = text;
        lastAlert = Time.time;
        alertCanvas.alpha = 1;
        alertActive = true;
    }

    public void UpdateAlert()
    {
        if (alertActive)
        {
            if (Time.time - lastAlert < 2.5f)
            {
                alertCanvas.alpha = 2.5f - (Time.time - lastAlert);
                if (Time.time - lastAlert > 2.6f)
                {
                    alertCanvas.alpha = -1f;
                    alertActive = false;
                }
            }
        }
    }

    private void HandleTurnValueChanged(bool prevVal,bool newVal)
    {
        if(newVal == true)
        {
            IndicateTurn(0);
        }
        else
        {
            IndicateTurn(1);
        }
    }
    private void OnEnable()
    {
        displayName.OnValueChanged += HandleDisplayNameChanged;
        SetCurrentTurnNetworkVariable.OnValueChanged += HandleTurnValueChanged;
    }

    private void OnDisable()
    {
        displayName.OnValueChanged -= HandleDisplayNameChanged;
        SetCurrentTurnNetworkVariable.OnValueChanged -= HandleTurnValueChanged;
    }

    private void HandleDisplayNameChanged(FixedString32Bytes prev, FixedString32Bytes newValue)
    {
        //Debug.LogError("PLAYER NAME VALUE CHANGED...");
        displayName.Value = newValue;
    }

    public override void OnNetworkSpawn()
    {
        SceneManager.activeSceneChanged += StartSceneInitialise;
        if(!IsOwner)
        {
            //Debug.Log("Not owner. Start method not called for ID : " + OwnerClientId);
            return;
        }
        AssignRolesServerRPC();
        SceneManager.activeSceneChanged += GameSceneStart;
        byte[] data = Encoding.ASCII.GetBytes(MyNetworkManager.instance.PlayerName);
        InitializeClientDataServerRPC(OwnerClientId, data);
        NetworkManager.Singleton.OnServerStopped += ServerDisconnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientStopped += ServerDisconnected;
    }

    private void ClientDisconnected(ulong e)
    {
        //Debug.LogError("Client has disconnected, changing scenes");
        DisconnectedCanvas.SetActive(true);
    }

    private void ServerDisconnected(bool e)
    {
        //Debug.LogError("Server has disconnected, changing scenes");
        DisconnectedCanvas.SetActive(true);
    }

    [ServerRpc]
    private void InitializeClientDataServerRPC(ulong clientID, byte[] data)
    {
        PlayerNetworkManager.InitialisePlayerDataClient(data, clientID);
    }

    [ServerRpc]
    private void AssignRolesServerRPC()
    {
        IReadOnlyList<NetworkClient> clients = NetworkManager.ConnectedClientsList;
        connectedclients = clients.Count;
        for(int i = 0; i < connectedclients; i++)
        {
            if(i == 0)
            {
                clients[i].PlayerObject.GetComponent<Multiplayer>().isWhite.Value = true;
            }
            if (i == 1)
            {
                clients[i].PlayerObject.GetComponent<Multiplayer>().isWhite.Value = false;
            }
        }
        Debug.LogError("Connected clients: " + connectedclients);
    }

    private IEnumerator CreatePiecesCoroutine()
    {
        yield return new WaitForSeconds(3f);
        GenerateBoard();
        IndicateTurn(0);
        yield return new WaitForSeconds(5f); // 
        GameLoadCanvas.SetActive(false);
        if (VictoryCanvas != null)
        {
            VictoryCanvas.SetActive(false);
        }
    }

    private void GameSceneStart(Scene e,Scene s)
    {
        timer = -10f;
        SceneManager.activeSceneChanged -= GameSceneStart;
        GameLoadCanvas = GameObject.Find("GameLoadCanvas");
        VictoryCanvas = GameObject.Find("VictoryCanvas");
        DisconnectedCanvas = GameObject.Find("Disconnected");
        DisconnectedCanvas.SetActive(false);
        VictoryCanvas.SetActive(false);
        //Debug.LogError("Scene CHANGED...");
        StartCoroutine(CreatePiecesCoroutine());
    }

    private void StartSceneInitialise(Scene e, Scene s)
    {
        SceneManager.activeSceneChanged -= StartSceneInitialise;
        alertCanvas = GameObject.Find("AlertCanvas").GetComponent<CanvasGroup>();;
    }

    // Works only on the Owner of the script
    private void Update()
    {
        UpdateAlert();
        if (!IsSpawned) return;
        if (!IsOwner) return;

        Debug.Log(mouseOver);
        UpdateMouseOver();


        if (isWhite.Value ? SetCurrentTurnNetworkVariable.Value : !SetCurrentTurnNetworkVariable.Value)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (SelectedPiece != null && TogglePieceDrag == true)
            {
                //Debug.Log("SELECTED PIECE IS NOT NULL");
                UpdatePieceDrag(SelectedPiece);
            }

            if (Input.GetMouseButtonDown(0))
            {
                 SelectPiece(x, y);
            }

            if (Input.GetMouseButtonUp(0))
            {
                TogglePieceDrag = true;
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
            }
        }
        timer += Time.deltaTime;
        if (timer >= 2f)
        {
            timer = 0f;
            CheckPieces();
        }

    }

    public void TryMove(int x1, int y1, int x2, int y2)
    {
        TogglePieceDrag = true;
        Debug.LogWarning("Trymove called.");
        forcedPieces = ScanForPossibleMove();
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        Piece pice = PiecePresent(x1, y1);
        SelectedPiece = pice; // CHANGED

        //out of bounds
        if (x2 < 0 || x2 >= Pieces.Length || y2 < 0 || y2 >= Pieces.Length)
        {
            if (SelectedPiece != null)
            {
                //MovePiece(SelectedPiece, x1, y1);
                SelectedPiece.transform.position = (Vector3.right * x1) + (Vector3.forward * y1) + BoardOffset + PieceOffset;
                MoveSelectedPiece(x1, y1, x1, y1);
            }
            startDrag = -Vector2.one;
            TogglePieceDrag = false;
            SelectedPiece = null;
            return;
        }
        //check if out of bounds

        if (SelectedPiece != null)
        {
            Debug.LogWarning("Trymove in Selected piece is not null.");
            //if it has not moved, move the piece back to where it was
            if (endDrag == startDrag)
            {
                //MovePiece(SelectedPiece, x1, y1);
                SelectedPiece.transform.position = (Vector3.right * x1) + (Vector3.forward * y1) + BoardOffset + PieceOffset;
                MoveSelectedPiece(x1, y1, x1, y1);
                //MoveSelectedPieceLocal(x1, y1, x2, y2);

                startDrag = -Vector2.one;
                TogglePieceDrag = false;
                SelectedPiece = null;
                return;
            }
            // check if it is a valid move
            if (SelectedPiece.ValidMove(Pieces, x1, y1, x2, y2) == true)
            {
                Debug.LogWarning("Trymove called, Valid move...");
                // Did we kill anything
                //if this is jump
                if (Mathf.Abs(x1 - x2) == 2)
                {
                    Piece pi = PiecePresent((x1 + x2) / 2, (y1 + y2) / 2); // CHANGES
                    if (pi != null)
                    {
                        MoveSelectedPiece(x1, y1, x2, y2);
                        //MoveSelectedPieceLocal(x1, y1, x2, y2);
                        DespawnObjectServerRpc((x1 + x2) / 2, (y1 + y2) / 2);
                        hasKilled = true;
                    }
                }

                //were we supposed to kill anything?
                if (forcedPieces.Count != 0 && !hasKilled)
                {
                    // invalid move
                    Debug.LogError("FORCED MOVE.. TURN NOT REGISTERED.");
                    //MovePiece(SelectedPiece, x1, y1);
                    SelectedPiece.transform.position = (Vector3.right * x1) + (Vector3.forward * y1) + BoardOffset + PieceOffset;
                    MoveSelectedPiece(x1, y1, x1, y1);
                    //MoveSelectedPieceLocal(x1, y1, x1, y1);

                    startDrag = -Vector2.one;
                    SelectedPiece = null;
                    return;
                }

                //Piece p = PiecePresent(x1, y1); // CHANGESW
                //SelectedPiece = p;
                SelectedPiece.transform.position = (Vector3.right * x2) + (Vector3.forward * y2) + BoardOffset + PieceOffset;
                MoveSelectedPiece(x1, y1, x2, y2);
                //MoveSelectedPieceLocal(x1, y1, x2, y2);
                TogglePieceDrag = false;
                //SelectedPiece = null;
                EndTurn();
            }
            else
            {
                Debug.LogWarning("Trymove , Turn Failed.");
                //MovePiece(SelectedPiece, x1, y1);
                TogglePieceDrag = false;
                SelectedPiece.transform.position = (Vector3.right * x1) + (Vector3.forward * y1) + BoardOffset + PieceOffset;
                MoveSelectedPiece(x1, y1, x1, y1);
                //MoveSelectedPieceLocal(x1, y1, x1, y1);
                startDrag = -Vector2.one;
                AudioManager.instance.MoveFailed();
                return;
            }
        }
        //if there is a selected piece
    }

    [ServerRpc]
    private void DespawnObjectServerRpc(int x,int y)
    {
        Piece pi = PiecePresent(x, y);
        pi.GetComponent<NetworkObject>().Despawn(); // CHANGES
        DestroyImmediate(pi.gameObject);
    }


    private void EndTurn()
    {
        if(!IsOwner) return;
        AudioManager.instance.MoveSuccessful();
        ScanForPossibleMove();
        TogglePieceDrag = true;
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        //Promotions
        if (SelectedPiece != null)
        {
            if (SelectedPiece.isWhite && !SelectedPiece.isKing && y == 7)
            {
                SelectedPiece.isKing = true;
                //SelectedPiece.transform.Rotate(Vector3.right * 180f);
                SelectedPiece.GetComponentInChildren<Animator>().SetTrigger("Flip");
            }
            else if (!SelectedPiece.isWhite && !SelectedPiece.isKing && y == 0)
            {
                SelectedPiece.isKing = true;
                //SelectedPiece.transform.Rotate(Vector3.right * 180f);
                SelectedPiece.GetComponentInChildren<Animator>().SetTrigger("Flip");

            }
        }
        if(SelectedPiece == null)
        {
            Debug.LogError("Selected piece in end Turn.");
        }
        if (ScanForPossibleMove(SelectedPiece, x, y).Count != 0 && hasKilled)
        {
            TogglePieceDrag = false; // NEW
            Debug.LogError("Exttra move granted...");
            return;
        }
        SelectedPiece = null;
        startDrag = -Vector2.one;
        SwitchTurnsServerServerRpc();
        hasKilled = false;
        CheckPieces();
        ScanForPossibleMove();
    }

    private void CheckPieces()
    {
        var ps = NetworkObject.FindObjectsOfType<Piece>();
        Debug.LogError(" Array length: " + ps.Length);
        int black = 0;
        int white = 0;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].gameObject != null && ps[i].gameObject.activeSelf && ps[i].isWhite)
            {
                white++;
            }
            else if (ps[i].gameObject != null && ps[i].gameObject.activeSelf && !ps[i].isWhite)
            {
                black++;
            }
        }

        Debug.LogError("Black pieces: " + black + " | White pieces: " + white);
        if (!IsOwner) return; // NEW
        if (white == 0)
        {
            if (black == 0) return;
            Debug.LogError("BLACK TEAM HAS WON");
            ReportWinnerServerRpc(1);
            if (AlreadyRematched.Value == false);
            StartCoroutine(RematchCoroutine());
            //RematchButton(); // NEW
        }
        if (black == 0)
        {
            if (white == 0) return;
            Debug.LogError("WHITE TEAM HAS WON");
            ReportWinnerServerRpc(0);
            if (AlreadyRematched.Value == false) ;
            StartCoroutine(RematchCoroutine());
            //RematchButton(); // NEW
        }
    }

    [ServerRpc]
    private void SwitchTurnsServerServerRpc() // TURN INTO ANOTHER SCRIPT,,, ONLY ONE INSTANCE
    {
        // Broadcast the turn change to all clients (optional)
        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;
        isWhiteTurn = !isWhiteTurn;

        foreach (NetworkClient ncl in clients)
        {
            ncl.PlayerObject.GetComponent<Multiplayer>().SetCurrentTurnNetworkVariable.Value = !ncl.PlayerObject.GetComponent<Multiplayer>().SetCurrentTurnNetworkVariable.Value;
            Debug.Log("connected lcients in switch turn : " + clients.Count);
        }
        ScanForPossibleMovesClientRpc();
        CheckPieces();
    }

    [ClientRpc]
    private void ScanForPossibleMovesClientRpc()
    {
        ScanForPossibleMove();
    }

    private void IndicateTurn(ulong clientID)
    {
        ReportPlayerTurnClientRpc(clientID);
    }

    [ClientRpc]
    private void ReportPlayerTurnClientRpc(ulong clientID)
    {
        ReportPlayerTurnServerRpc(clientID);
    }

    [ServerRpc]
    void ReportWinnerServerRpc(ulong winnerId)
    {
        // Store winner information (e.g., winnerId)
        // Broadcast winner information to all clients
        string name = PlayerNetworkManager.GetPlayerData(winnerId).Value.PlayerName.ToString();
        BroadcastWinner(winnerId,name);
    }

    void BroadcastWinner(ulong winnerId, string name)
    {
        // Call a client RPC to update UI or display winner message
        UpdateWinnerClientRpc(winnerId,name);
    }

    [ClientRpc]
    void UpdateWinnerClientRpc(ulong winnerId,string name)
    {
        // Update UI or display winner message based on winnerId
        if (IsLocalPlayer)
        {
            VictoryCanvas.SetActive(true);
            VictoryCanvas.GetComponentInChildren<TextMeshProUGUI>().text = name + " has won the game!";
            timer = -10000f;
            if(winnerId == OwnerClientId)
            {
                AudioManager.instance.GameWon();
            }
            else
            {
                AudioManager.instance.GameLose();
            }
        }
        Debug.Log("Client: Player " + winnerId + " has won!");
        // Update UI elements here (text, animations, etc.)
    }

    [ServerRpc(RequireOwnership = false)]
    void ReportPlayerTurnServerRpc(ulong clientID)
    {
        string name = PlayerNetworkManager.GetPlayerData(clientID).Value.PlayerName.ToString();
        BroadcastTurn(name);
    }

    void BroadcastTurn(string name)
    {
        // Call a client RPC to update UI or display winner message
        UpdateTurnDisplayClientRpc(name);
    }

    [ClientRpc(RequireOwnership = false)]
    void UpdateTurnDisplayClientRpc(string name)
    {
        // Update UI or display winner message based on winnerId
        //if (IsLocalPlayer)
        {
            Alert(name + "'s turn");
        }
        Debug.Log(name + "'s turn");
        // Update UI elements here (text, animations, etc.)
    }


    private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
    {
        forcedPieces = new List<Piece>();

        //Piece pice = PiecePresent(x, y);
        Piece pice = SelectedPiece; // CHANGES

        if (pice == null)
        {
            Debug.LogError($"No piece present at : " + x + ' ' + y);
            return forcedPieces;
        }
        //if (Pieces[x, y].IsForceToMove(Pieces, x, y))
        if (pice.IsForceToMove(x, y)) // CHANGES
        {
            forcedPieces.Add(pice);
            //forcedPieces.Add(Pieces[x,y]);
            //Pieces[x, y].transform.GetChild(0).gameObject.SetActive(true);
            pice.transform.GetChild(0).gameObject.SetActive(true); // CHANGES
        }
        else
        {
            //Pieces[x, y].transform.GetChild(0).gameObject.SetActive(false);
            pice.transform.GetChild(0).gameObject.SetActive(false); // CHANGES
        }

        return forcedPieces;
    }
    private List<Piece> ScanForPossibleMove()
    {
        forcedPieces = new List<Piece>();
        // check for all pieces
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Piece pice = PiecePresent(i, j); // CHANGES
                //if (Pieces[i,j] != null && Pieces[i,j].isWhite == isWhiteTurn)
                if (pice != null && pice.isWhite == isWhite.Value/*SetCurrentTurnNetworkVariable.Value*/)
                {
                    //if (Pieces[i, j].IsForceToMove(Pieces, i, j))
                    if (pice.IsForceToMove(i, j)) // CHANGED
                    {
                        //forcedPieces.Add(Pieces[i,j]);
                        forcedPieces.Add(pice); // CHANGED
                        //Pieces[i, j].transform.GetChild(0).gameObject.SetActive(true);
                        pice.transform.GetChild(0).gameObject.SetActive(true); // CHANGES
                    }
                    else
                    {
                        //Pieces[i, j].transform.GetChild(0).gameObject.SetActive(false);
                        pice.transform.GetChild(0).gameObject.SetActive(false); // CHANGES
                    }
                }
            }
        }
        return forcedPieces;
    }
    private void UpdateMouseOver()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit Hit, 30f, LayerMask.GetMask("Board")))
        {
            mouseOver.x = (int)(Hit.point.x - BoardOffset.x);
            mouseOver.y = (int)(Hit.point.z - BoardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }
    private void UpdatePieceDrag(Piece p) // CHANGED
    {
        Debug.LogWarning("Update piece drag called.");
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit Hit, 30f, LayerMask.GetMask("Board")))
        {
            p.transform.position = Hit.point + Vector3.up;
        }
    }
    private void SelectPiece(int x, int y)
    {
        if (!IsOwner)
        {
            Debug.LogWarning("Another client is trying to select the piece, permission denied...");
            return;
        }
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return;
        }
        //Piece p = Pieces[x, y];
        Piece p = PiecePresent(x, y); // CHANGED

        Debug.LogWarning("Select piece called.");

        if (p != null && p.isWhite == isWhite.Value)
        {
            Debug.LogWarning("Select piece in.");

            if (forcedPieces.Count == 0)
            {
                SelectedPiece = p;
                startDrag = mouseOver;
            }
            else
            {
                //look for piece under our forcedpiece list
                if (forcedPieces.Find(fp => fp == p) == null)
                {
                    return;
                }
                SelectedPiece = p;
                startDrag = mouseOver;
            }
        }
        else
        {
            if (p == null) Debug.Log("NO PIECE SELECTED.");
            Debug.Log("No piece found at this position");
        }
        Debug.LogWarning("Select piece end.");
        TogglePieceDrag = true;

    }
    private void GenerateBoard()
    {
        SpawnPiecesServerRPC(isWhite.Value);
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPiecesServerRPC(bool isWhite)
    {
        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;

        foreach (NetworkClient ncl in clients)
        {
            ncl.PlayerObject.GetComponent<Multiplayer>().AlreadyRematched.Value = false;
        }
        Debug.Log("Client Id: " + OwnerClientId + " IS OWNER: " + IsOwner);
        if (isWhite)
        {
            if (PiecePresent(0, 0) != null) return;
            for (int y = 0; y < 3; y++)
            {
                bool isEvenRow = (y % 2 == 0);
                for (int x = 0; x < 8; x += 2)
                {
                    GeneratePeice(isEvenRow ? x : x + 1, y);
                }
            }
        } 
        else
        {
            if (PiecePresent(7, 7) != null) return;
            for (int y = 7; y > 4; y--)
            {
                bool isEvenRow = (y % 2 == 0);
                for (int x = 0; x < 8; x += 2)
                {
                    GeneratePeice(isEvenRow ? x : x + 1, y);
                }
            }
        }
    }
    private void GeneratePeice(int x, int y)
    {
        Debug.Log("Piece generated by: OwnerClientID : "+OwnerClientId);
        bool isBlackPiece = (y > 3) ? true : false;
        Transform go = Instantiate(isBlackPiece ? BlackPiecePrefab : WhitePiecePrefab);
        go.GetComponent<NetworkObject>().Spawn();

        go.GetComponent<NetworkObject>().TrySetParent(transform);
        Piece piece = go.GetComponent<Piece>();

        MovePiece(piece, x, y);
        if (piece.IsOwnedByServer)
        {
            StartCoroutine(TransferOwnershipToClient(piece, OwnerClientId));
        }
    }
    public IEnumerator TransferOwnershipToClient(Piece Piece, ulong clientId)
    {
        yield return new WaitForSeconds(1f);
        if (IsServer)
        {
            if (!(Piece.GetComponent<NetworkObject>().OwnerClientId == clientId))
            {
                Piece.GetComponent<NetworkObject>().ChangeOwnership(clientId);
                Debug.Log("Transferred Piece ownership to : " + clientId);
            }
        }
        else
        {
            Debug.LogError("TransferOwnershipToClient can only be called on the server!");
        }
    }
    private void MovePiece(Piece p, int x, int y)
    {
        p.xPos.Value = x; // CHANGES
        p.yPos.Value = y; // CHANGES
        Debug.Log("X : " + p.xPos.Value);
        Debug.Log("Y : " + p.yPos.Value);
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + BoardOffset + PieceOffset;
        //SelectedPiece = null; // NEW
    }
    private void MoveSelectedPiece(int x1,int y1,int x2,int y2)
    {
        Debug.Log("Moved Selected piece to : " + x2 + ' ' + y2);
        UpdatePiecePositionServerRpc(x1,y1,x2, y2);
    }
    [ServerRpc]
    private void UpdatePiecePositionServerRpc(int x1,int y1,int x2,int y2)
    {
        Piece SelectedPiece = PiecePresent(x1, y1);
        if (SelectedPiece == null) 
        {
            Debug.LogError("No selected piece... cannot move to : " + x2 + " " + y2 + " owner id: " + OwnerClientId);
            return;
        }
        Debug.LogError("Selected piece found UPDATEPIECERPC, oWNER ID: " + OwnerClientId);
        MovePiece(SelectedPiece, x2, y2);
        SelectedPiece.xPos.Value = x2;
        SelectedPiece.yPos.Value = y2;
    }
    public Piece PiecePresent(int x, int y)
    {
        Piece p = null;
        GameObject[] piece = GameObject.FindGameObjectsWithTag("Piece");
        for (int i = 0; i < piece.Length; i++)
        {
            if (piece[i].TryGetComponent<Piece>(out Piece pc) && piece[i].activeSelf)
            {
                if (pc.xPos.Value == x && pc.yPos.Value == y)
                {
                    p = pc;
                    break;
                }
            }
        }

        if (p == null) Debug.LogWarning("No piece found at : " + x +" "+ y);
        else
        {
            Debug.LogWarning("Piece found at : " + x +" "+ y);

        }

        return p;
    }
}

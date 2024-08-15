using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckersBoard : MonoBehaviour // SINGLEPLAYER SUPPORT
{
    public static CheckersBoard Instance { get; set; }

    public Transform WhitePiecePrefab;
    public Transform BlackPiecePrefab;
    public CanvasGroup alertCanvas;
    public GameObject VictoryCanvas;
    private float lastAlert;
    public bool alertActive;

    private PieceHotseat[,] Pieces = new PieceHotseat[8, 8];
    private Vector3 BoardOffset = new Vector3(-4f, 0, -4f);
    private Vector3 PieceOffset = new Vector3(0.5f, 0.125f, 0.5f);

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag;


    private PieceHotseat SelectedPiece;
    private List<PieceHotseat> forcedPieces = new List<PieceHotseat>();

    private bool hasKilled;
    public bool isWhite = true;
    public bool isWhiteTurn = true;
    private string TeamWonText = "White team has won!";
    private bool GameHasEnded = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        forcedPieces = new List<PieceHotseat>();
        GenerateBoard();
        Alert(isWhiteTurn ? "White side's turn" : "Black side's turn");
    }

    private void Update()
    {
        if (GameHasEnded) return;
        UpdateAlert();
        UpdateMouseOver();

        if(isWhite?isWhiteTurn : !isWhiteTurn)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (SelectedPiece != null)
            {
                UpdatePieceDrag(SelectedPiece);
            }

            if (Input.GetMouseButtonDown(0))
            {
                SelectPiece(x, y);
            }

            if (Input.GetMouseButtonUp(0))
            {
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
            }
        }
        
    }

    public void PlayAgainButton()
    {
        StartCoroutine(PlayAgainCoroutine());
    }

    private IEnumerator PlayAgainCoroutine()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Pieces[i, j] != null)
                {
                    DestroyImmediate(Pieces[i, j].gameObject);
                    Pieces[i, j] = null;
                }
            }
        }
        yield return new WaitForSeconds(1f);
        GenerateBoard();
        isWhite = true;
        isWhiteTurn = true;
        GameHasEnded = false;
        SelectedPiece = null;
        VictoryCanvas.SetActive(false);
        Alert(isWhiteTurn ? "White side's turn" : "Black side's turn");
    }
    public void TryMove(int x1, int y1, int x2, int y2)
    {
        forcedPieces = ScanForPossibleMove();
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        SelectedPiece = Pieces[x1, y1];

        //out of bounds
        if (x2 < 0 || x2 >= Pieces.Length || y2 < 0 || y2 >= Pieces.Length)
        {
            if(SelectedPiece != null)
            {
                MovePiece(SelectedPiece, x1, y1);
            }
            startDrag = Vector2.zero;
            SelectedPiece = null;
            AudioManager.instance.MoveFailed();
            return;
        }
        //check if out of bounds

        if(SelectedPiece != null)
        {
            //if it has not moved, move the piece back to where it was
            if(endDrag == startDrag)
            {
                MovePiece(SelectedPiece, x1, y1);
                startDrag = Vector2.zero;
                SelectedPiece = null;
                return;
            }
            // check if it is a valid move
            if (SelectedPiece.ValidMove(Pieces,x1,y1,x2,y2) == true)
            {
                // Did we kill anything
                //if this is jump
                if(Mathf.Abs(x1 - x2) == 2)
                {
                    PieceHotseat pi = Pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (pi != null)
                    {
                        //Pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                        DestroyImmediate(pi.gameObject);
                        hasKilled = true;
                        AudioManager.instance.MoveSuccessful();
                    }
                }

                //were we supposed to kill anything?
                if(forcedPieces.Count != 0 && !hasKilled)
                {
                    // invalid move
                    MovePiece(SelectedPiece, x1, y1);
                    startDrag = Vector2.zero;
                    SelectedPiece = null;
                    AudioManager.instance.MoveFailed();
                    return;
                }

                Pieces[x2, y2] = SelectedPiece;
                Pieces[x1, y1] = null;
                MovePiece(SelectedPiece, x2, y2);
                AudioManager.instance.MoveSuccessful();
                EndTurn();
            }
            else
            {
                MovePiece(SelectedPiece, x1, y1);
                startDrag = Vector2.zero;
                SelectedPiece = null;
                AudioManager.instance.MoveFailed();
                return;
            }
        }
        //if there is a selected piece
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        //Promotions
        if(SelectedPiece != null)
        {
            if(SelectedPiece.isWhite && !SelectedPiece.isKing && y == 7)
            {
                SelectedPiece.isKing = true;
                SelectedPiece.GetComponentInChildren<Animator>().SetTrigger("Flip");
            }
            else if(!SelectedPiece.isWhite && !SelectedPiece.isKing && y == 0)
            {
                SelectedPiece.isKing = true;
                SelectedPiece.GetComponentInChildren<Animator>().SetTrigger("Flip");

            }
        }

        SelectedPiece = null;
        startDrag = Vector2.zero;

        if (ScanForPossibleMove(SelectedPiece,x,y).Count != 0 && hasKilled)
        {
            return;
        }

        //change turns
        isWhiteTurn = !isWhiteTurn;
        isWhite = !isWhite;
        Alert(isWhiteTurn ? "White side's turn" : "Black side's turn");
        hasKilled = false;
        ScanForPossibleMove();
        CheckVictory();
    }

    private void CheckVictory()
    {
        var ps = FindObjectsOfType<PieceHotseat>();
        bool hasWhite = false, hasBlack = false;
        for(int i = 0; i < ps.Length; i++)
        {
            if (ps[i].isWhite)
            {
                hasWhite = true;
            }
            else
            {
                hasBlack = true;
            }
        }

        if (!hasWhite)
        {
            Victory(false);
        }
        if (!hasBlack)
        {
            Victory(true);
        }
    }

    private void Victory(bool isWhite)
    {
        if (isWhite)
        {
            Alert("White team has won!");
            Debug.Log("White team has won.");
            TeamWonText = "WHITE TEAM HAS WON THE GAME";
            StartCoroutine(GameEndCoroutine());
        }
        if (!isWhite)
        {
            Alert("Black team has won!");
            Debug.Log("Black team has won.");
            TeamWonText = "BLACK TEAM HAS WON THE GAME";
            StartCoroutine(GameEndCoroutine());
        }
        GameHasEnded = true;
        AudioManager.instance.GameWon();
    }

    private IEnumerator GameEndCoroutine()
    {
        yield return new WaitForSeconds(3f);
        VictoryCanvas.SetActive(true);
        VictoryCanvas.GetComponentInChildren<TextMeshProUGUI>().text = TeamWonText;
    }

    private List<PieceHotseat> ScanForPossibleMove(PieceHotseat p,int x,int y)
    {
        forcedPieces = new List<PieceHotseat>();

        if (Pieces[x, y].IsForceToMove(Pieces, x, y))
        {
            forcedPieces.Add(Pieces[x, y]);
            Pieces[x, y].transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            Pieces[x, y].transform.GetChild(0).gameObject.SetActive(false);
        }

        return forcedPieces;
    }
    private List<PieceHotseat> ScanForPossibleMove()
    {
        forcedPieces = new List<PieceHotseat>();
        // check for all pieces
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j< 8; j++)
            {
                if (Pieces[i, j] != null && Pieces[i, j].isWhite == isWhiteTurn)
                {
                    if (Pieces[i, j].IsForceToMove(Pieces, i, j))
                    {
                        forcedPieces.Add(Pieces[i, j]);
                        Pieces[i, j].transform.GetChild(0).gameObject.SetActive(true);
                    }
                    else
                    {
                        Pieces[i, j].transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
            }
        }
        return forcedPieces;
    }
    private void UpdateMouseOver()
    {
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),out RaycastHit Hit, 30f, LayerMask.GetMask("Board")))
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

    private void UpdatePieceDrag(PieceHotseat p) // CHANGED
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit Hit, 30f, LayerMask.GetMask("Board")))
        {
            p.transform.position = Hit.point + Vector3.up;
        }
    }
    private void SelectPiece(int x,int y)
    {
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return;
        }
        PieceHotseat p = Pieces[x, y];

        if(p != null && p.isWhite == isWhite)
        {
            if (forcedPieces.Count == 0)
            {
                SelectedPiece = p;
                startDrag = mouseOver;
            }
            else
            {
                //look for piece under our forcedpiece list
                if(forcedPieces.Find(fp => fp == p) == null)
                {
                    return;
                }
                SelectedPiece = p;
                startDrag = mouseOver;
            }
        }
        else
        {
            Debug.Log("No piece found at this position");
        }
    }
    private void GenerateBoard()
    {
        for (int y = 0; y < 3; y++)
        {
            bool isEvenRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece(isEvenRow ? x : x + 1, y);
            }
        }

        for (int y = 7; y > 4; y--)
        {
            bool isEvenRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece(isEvenRow ? x : x + 1, y);
            }
        }
    }

    private void GeneratePiece(int x,int y)
    {
        bool isBlackPiece = (y > 3) ? true : false;
        Transform go = Instantiate(isBlackPiece ? BlackPiecePrefab : WhitePiecePrefab);

        go.SetParent(transform);
        PieceHotseat piece = go.GetComponent<PieceHotseat>();

        Pieces[x,y] = piece;
        MovePiece(piece, x, y);
    }
  
    private void MovePiece(PieceHotseat p,int x,int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + BoardOffset + PieceOffset;
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
            if(Time.time - lastAlert < 2.5f)
            {
                alertCanvas.alpha = 2.5f - (Time.time - lastAlert);
                if (Time.time - lastAlert > 2.5f)
                {
                    alertActive = false;
                }
            }
        }
    }

}

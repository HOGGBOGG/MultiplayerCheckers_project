using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceHotseat : MonoBehaviour
{
    public bool isWhite;
    public bool isKing;
    public bool IsForceToMove(PieceHotseat[,] board,int x, int y)
    {
        if (isWhite || isKing)
        {
            //top left
            if (x >= 2 & y <= 5)
            {
                PieceHotseat p = board[x - 1, y + 1];
                // if there is a piece and is not the same color as ours
                if (p != null && p.isWhite != isWhite)
                {
                    //Check if its possible to land after jump
                    if (board[x - 2, y + 2] == null)
                    {
                        return true;
                    }
                }
            }
            // top right
            if (x <= 5 & y <= 5)
            {
                PieceHotseat p = board[x + 1, y + 1];
                // if there is a piece and is not the same color as ours
                if (p != null && p.isWhite != isWhite)
                {
                    //Check if its possible to land after jump
                    if (board[x + 2, y + 2] == null)
                    {
                        return true;
                    }
                }
            }
        }
        if (!isWhite || isKing)
        {
            //bottom left
            if (x >= 2 & y >= 2)
            {
                PieceHotseat p = board[x - 1, y - 1];
                if (p != null && p.isWhite != isWhite)
                {
                    //Check if its possible to land after jump
                    if (board[x - 2, y - 2] == null)
                    {
                        return true;
                    }
                }
            }
            //bottom right
            if (x <= 5 & y >= 2)
            {
                PieceHotseat p = board[x + 1, y - 1];
                if (p != null && p.isWhite != isWhite)
                {
                    if (board[x + 2, y - 2] == null)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public bool ValidMove(PieceHotseat[,] board, int x1, int y1, int x2, int y2)
    {
        if (board[x2, y2] != null)
        {
            return false;
        }
        int deltaMoveX = Mathf.Abs(x1 - x2);
        int deltaMoveY = y2 - y1;
        if (isWhite || isKing)
        {
            if (deltaMoveX == 1)
            {
                if (deltaMoveY == 1)
                {
                    return true;
                }
            }
            if (deltaMoveX == 2)
            {
                if (deltaMoveY == 2)
                {
                    PieceHotseat p = board[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null && p.isWhite != isWhite)
                    {
                        return true;
                    }
                }
            }
        }

        if (!isWhite || isKing)
        {
            if (deltaMoveX == 1)
            {
                if (deltaMoveY == -1)
                {
                    return true;
                }
            }
            if (deltaMoveX == 2)
            {
                if (deltaMoveY == -2)
                {
                    PieceHotseat p = board[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null && p.isWhite != isWhite)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

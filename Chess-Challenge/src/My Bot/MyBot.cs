using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot {
  // Piece values: null, pawn, knight, bishop, rook, queen, king
  static float[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };

  // Depth values for each remaining piece count.
  static int[] depthValues = { 0, 0, 0, 10, 10, 9, 8, 7, 6, 5, 4, 3 };

  public Move Think(Board board, Timer timer) {
    var allMoves = PrioritizeMoves(board, board.GetLegalMoves());
    var depth = depthValues[Math.Min(11, board.GetAllPieceLists().Select(x => x.Count).Aggregate(0, (acc, x) => acc + x))];

    // Reduce search depth if time is running out.
    if (timer.MillisecondsRemaining < 10000)
      depth = Math.Max(depth / 2, 1);

    return NegamaxWithAlphaBeta(board, depth, -float.MaxValue, float.MaxValue, 1, timer).Item2 ?? allMoves[0];
  }

  /// <summary>
  /// Evaluate a position. Positive values are good for white, negative values are good for black.
  /// </summary>
  public static float EvaluatePosition(Board board) {
    // 1. Material score.
    var material = 0f;

    // Assign material score.
    foreach (var pieceList in board.GetAllPieceLists())
      if (pieceList.IsWhitePieceList)
        foreach (var piece in pieceList)
          material += pieceValues[(int)piece.PieceType];
      else
        foreach (var piece in pieceList)
          material -= pieceValues[(int)piece.PieceType];

    return material;
  }

  /// <summary>
  /// Prioritize moves with handcrafted heuristics.
  /// </summary>
  public static Move[] PrioritizeMoves(Board board, Move[] moves) {
    Random rng = new();

    for (var i = 0; i < moves.Length; i++) {
      var randomIndex = rng.Next(moves.Length);
      (moves[randomIndex], moves[i]) = (moves[i], moves[randomIndex]);
    }

    return moves;
  }

  /// <summary>
  /// Alpha-beta pruning algorithm to find best score of a position. `playerSign` should be -1 if that move is in the perspective of the player, and 1 if it is in the perspective of the opponent.
  /// </summary>
  public (float, Move?) NegamaxWithAlphaBeta(Board board, int depth, float alpha, float beta, float color, Timer timer) {
    // We've found a checkmate, so we'll return a very high value.
    if (board.IsInCheckmate())
      return (100 * color, null);

    // We don't want to draw, so we'll just return a slightly negative value.
    if (board.IsDraw())
      return (-1 * color, null);

    if (depth == 0)
      return (EvaluatePosition(board) * (0 < color == board.IsWhiteToMove ? -1 : 1), null);

    var moves = PrioritizeMoves(board, board.GetLegalMoves());
    var value = -float.MaxValue;
    Move? move = null;

    foreach (var childMove in moves) {
      if (200000000 < timer.MillisecondsElapsedThisTurn)
        break;

      board.MakeMove(childMove);
      var moveValue = -NegamaxWithAlphaBeta(board, depth - 1, -beta, -alpha, -color, timer).Item1;
      board.UndoMove(childMove);

      if (moveValue > value) {
        value = moveValue;
        move = childMove;
      }

      alpha = Math.Max(alpha, value);

      if (alpha >= beta)
        break;
    }

    return (value, move);
  }
}

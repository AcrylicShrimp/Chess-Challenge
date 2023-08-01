using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot {
  // Depth values for each remaining piece count.
  static int[] depthValues = { 0, 0, 0, 20, 18, 12, 10, 8, 7, 6, 5, 4 };

  public Move Think(Board board, Timer timer) {
    var allMoves = PrioritizeMoves(board, board.GetLegalMoves());
    var depth = depthValues[Math.Min(11, board.GetAllPieceLists().Select(x => x.Count).Aggregate(0, (acc, x) => acc + x))];

    // Reduce search depth if time is running out.
    if (timer.MillisecondsRemaining < 10000)
      depth = Math.Max(depth / 2, 1);

    return NegamaxWithAlphaBeta(board, depth, -float.MaxValue, float.MaxValue, 1, timer).Item2 ?? allMoves[0];
  }

  /// <summary>
  /// Evaluate a position in perspective of current player.
  /// </summary>
  public static float EvaluatePosition(Board board) {
    var player = EvaluateSinglePosition(board);
    board.ForceSkipTurn();
    var opponent = EvaluateSinglePosition(board);
    board.UndoSkipTurn();
    return player - opponent;
  }

  /// <summary>
  /// Evaluate a position in perspective of current player. The value will be always positive.
  /// </summary>
  public static float EvaluateSinglePosition(Board board) {
    if (board.IsInCheckmate()) return -1000;

    // If we're in check, we're losing.
    var score = board.IsInCheck() ? -1f : 0;

    foreach (var pawn in board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove)) {
      score += 1; // 1 point for each pawn.
      score += 0.1f * (pawn.IsWhite ? pawn.Square.Rank : 7 - pawn.Square.Rank); // 0.1 point for each rank advanced.
      // TODO: Should we give additional points for passed pawns?
    }

    foreach (var knight in board.GetPieceList(PieceType.Knight, board.IsWhiteToMove)) {
      score += 3; // 3 points for each knight.
      score += 0.1f * (knight.IsWhite ? knight.Square.Rank : 7 - knight.Square.Rank); // 0.1 point for each rank advanced.
    }

    foreach (var bishop in board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove)) {
      score += 3; // 3 points for each bishop.
    }

    foreach (var rook in board.GetPieceList(PieceType.Rook, board.IsWhiteToMove)) {
      score += 5; // 5 points for each rook.
    }

    foreach (var queen in board.GetPieceList(PieceType.Queen, board.IsWhiteToMove)) {
      score += 9; // 9 points for each queen.
    }

    return score;
  }

  /// <summary>
  /// Prioritize moves with handcrafted heuristics.
  /// </summary>
  public static Move[] PrioritizeMoves(Board board, Move[] moves) {
    return moves.Select(x => new {
      x, score = ScoreMove(board, x)
    }).OrderByDescending(x => x.score).Select(x => x.x).ToArray();
  }

  /// <summary>
  /// Score a move with handcrafted heuristics.
  /// </summary>
  public static float ScoreMove(Board board, Move move) {
    board.MakeMove(move);
    var score = -EvaluatePosition(board);
    board.UndoMove(move);

    if (move.IsCastles) score += 0.5f;
    if (move.MovePieceType == PieceType.King) score -= 0.5f;

    score += (new Random().NextSingle() - 0.5f) * 0.1f;

    return score;
  }

  /// <summary>
  /// Alpha-beta pruning algorithm to find best score of a position. `playerSign` should be -1 if that move is in the perspective of the player, and 1 if it is in the perspective of the opponent.
  /// </summary>
  public (float, Move?) NegamaxWithAlphaBeta(Board board, int depth, float alpha, float beta, float color, Timer timer) {
    // We've found a checkmate, so we'll return a very high value.
    if (board.IsInCheckmate())
      return (1000 * color, null);

    if (board.IsDraw())
      return (0, null);

    if (depth == 0)
      return (EvaluatePosition(board), null);

    var moves = PrioritizeMoves(board, board.GetLegalMoves());
    var value = -float.MaxValue;
    Move? move = null;

    foreach (var childMove in moves) {
      if (4000 < timer.MillisecondsElapsedThisTurn)
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

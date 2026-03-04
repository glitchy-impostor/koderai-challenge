using DebateScoringEngine.Core.Domain.Enums;

namespace DebateScoringEngine.Core.FlowGraph;

/// <summary>
/// The central data structure of the scoring engine.
/// 
/// A directed graph where:
///   - Nodes are ArgumentNodes (one per argument in the debate)
///   - Edges are RebuttalEdges (source argues against target)
///
/// Built by FlowGraphBuilder from a Debate + FormatConfig + RoundConfig + ScoringConfig.
/// After construction, all nodes have:
///   - Resolved enrichment (via three-tier fallback)
///   - ComputedStrength
///   - Derived status (Active / Dropped / Conceded / Extended)
///
/// The scoring engine and all rules treat this as read-only after construction.
/// </summary>
public class FlowGraph
{
    // ── Storage ───────────────────────────────────────────────────────────────

    private readonly Dictionary<string, ArgumentNode> _nodes;
    private readonly List<RebuttalEdge> _edges;

    // ── Public read-only views ────────────────────────────────────────────────

    public IReadOnlyDictionary<string, ArgumentNode> Nodes => _nodes;
    public IReadOnlyList<RebuttalEdge> Edges => _edges;

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>The debate this graph was built from.</summary>
    public string DebateId { get; }

    /// <summary>When the graph was constructed (for audit/explanation output).</summary>
    public DateTime BuiltAt { get; } = DateTime.UtcNow;

    // ── Constructor ───────────────────────────────────────────────────────────

    internal FlowGraph(string debateId, Dictionary<string, ArgumentNode> nodes, List<RebuttalEdge> edges)
    {
        DebateId = debateId;
        _nodes   = nodes;
        _edges   = edges;
    }

    // ── Node queries ──────────────────────────────────────────────────────────

    /// <summary>Returns a node by argumentId, or null if not found.</summary>
    public ArgumentNode? GetNode(string argumentId) =>
        _nodes.TryGetValue(argumentId, out var node) ? node : null;

    /// <summary>Returns all nodes for a given side.</summary>
    public IEnumerable<ArgumentNode> GetNodesBySide(Side side) =>
        _nodes.Values.Where(n => n.Side == side);

    /// <summary>Returns all nodes tagged to a specific stock issue.</summary>
    public IEnumerable<ArgumentNode> GetNodesByIssue(string stockIssueTag) =>
        _nodes.Values.Where(n => n.StockIssueTag == stockIssueTag);

    /// <summary>Returns all nodes for a given side and stock issue.</summary>
    public IEnumerable<ArgumentNode> GetNodesBySideAndIssue(Side side, string stockIssueTag) =>
        _nodes.Values.Where(n => n.Side == side && n.StockIssueTag == stockIssueTag);

    /// <summary>Returns all nodes for a given speech.</summary>
    public IEnumerable<ArgumentNode> GetNodesBySpeech(string speechId) =>
        _nodes.Values.Where(n => n.SpeechId == speechId);

    /// <summary>Returns all dropped argument nodes.</summary>
    public IEnumerable<ArgumentNode> GetDroppedNodes() =>
        _nodes.Values.Where(n => n.Status == ArgumentStatus.Dropped);

    // ── Edge queries ──────────────────────────────────────────────────────────

    /// <summary>Returns all edges where the given argument is the source (i.e., this argument rebuts others).</summary>
    public IEnumerable<RebuttalEdge> GetOutgoingEdges(string argumentId) =>
        _edges.Where(e => e.SourceArgumentId == argumentId);

    /// <summary>Returns all edges where the given argument is the target (i.e., responses to this argument).</summary>
    public IEnumerable<RebuttalEdge> GetIncomingEdges(string argumentId) =>
        _edges.Where(e => e.TargetArgumentId == argumentId);

    /// <summary>
    /// Returns all responses to a given argument that come from speeches
    /// at or before a given speech index in the ordered speech list.
    /// Used by drop detection to check if a response arrived in time.
    /// </summary>
    public IEnumerable<RebuttalEdge> GetResponsesOnOrBefore(string argumentId, int speechCutoffIndex,
        Func<string, int> speechIndexLookup) =>
        _edges.Where(e =>
            e.TargetArgumentId == argumentId &&
            speechIndexLookup(e.SourceSpeechId) <= speechCutoffIndex);

    // ── Thread extraction ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns argument threads — each thread is a root node (no incoming edges)
    /// plus all nodes reachable from it via outgoing edges, ordered by speech index.
    ///
    /// A thread corresponds to one row in the flow sheet UI.
    /// </summary>
    public IEnumerable<ArgumentThread> GetThreads(Func<string, int> speechIndexLookup)
    {
        // Root nodes: arguments that do not themselves rebut any other argument.
        // In edge direction (source=rebuttal, target=original), roots are nodes
        // that appear in no edge as a SOURCE — i.e., they make no rebuttals.
        // This correctly identifies the original/opening arguments in each thread,
        // NOT the most recent (unanswered) ones.
        var sourceIds = new HashSet<string>(_edges.Select(e => e.SourceArgumentId));
        var roots = _nodes.Values
            .Where(n => !sourceIds.Contains(n.ArgumentId))
            .OrderBy(n => speechIndexLookup(n.SpeechId));

        foreach (var root in roots)
        {
            var members = CollectThread(root.ArgumentId);
            yield return new ArgumentThread(root, members.OrderBy(n => speechIndexLookup(n.SpeechId)).ToList());
        }
    }

    private List<ArgumentNode> CollectThread(string rootId)
    {
        var visited = new HashSet<string>();
        var result  = new List<ArgumentNode>();
        var queue   = new Queue<string>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!visited.Add(id)) continue;

            if (_nodes.TryGetValue(id, out var node))
            {
                result.Add(node);
                foreach (var edge in GetIncomingEdges(id))
                    queue.Enqueue(edge.SourceArgumentId);
            }
        }
        return result;
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    public FlowGraphSummary GetSummary() => new()
    {
        TotalArguments    = _nodes.Count,
        TotalEdges        = _edges.Count,
        DroppedArguments  = _nodes.Values.Count(n => n.Status == ArgumentStatus.Dropped),
        AffArguments      = _nodes.Values.Count(n => n.Side == Side.AFF),
        NegArguments      = _nodes.Values.Count(n => n.Side == Side.NEG),
        ArgumentsByIssue  = _nodes.Values
            .GroupBy(n => n.StockIssueTag)
            .ToDictionary(g => g.Key, g => g.Count())
    };
}

/// <summary>
/// A logical debate thread: one argument and all responses to it (recursively).
/// Corresponds to one row in the flow sheet UI.
/// </summary>
public class ArgumentThread
{
    public ArgumentNode Root { get; }
    public IReadOnlyList<ArgumentNode> AllNodes { get; }

    public ArgumentThread(ArgumentNode root, List<ArgumentNode> allNodes)
    {
        Root     = root;
        AllNodes = allNodes;
    }

    public string StockIssueTag => Root.StockIssueTag;
}

/// <summary>Summary statistics for the flow graph — used in explanation output.</summary>
public class FlowGraphSummary
{
    public int TotalArguments   { get; init; }
    public int TotalEdges       { get; init; }
    public int DroppedArguments { get; init; }
    public int AffArguments     { get; init; }
    public int NegArguments     { get; init; }
    public Dictionary<string, int> ArgumentsByIssue { get; init; } = new();
}

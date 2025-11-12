using System;
using System.Collections.Generic;
using System.Linq;

namespace PROG7312_POE_.Services
{
    public enum RequestStatus { Submitted, InProgress, Resolved, Rejected }

    public class ServiceRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Ward { get; set; } = "";
        public int Priority { get; set; }   // 1 = highest
        public DateTime Created { get; set; }
        public RequestStatus Status { get; set; }
    }

    // ---------- AVL TREE ----------
    public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        class Node { public TKey K; public TValue V; public Node? L, R; public int H = 1; public Node(TKey k, TValue v) { K = k; V = v; } }
        Node? root;
        static int Ht(Node? n) => n?.H ?? 0;
        static void Up(Node n) => n.H = Math.Max(Ht(n.L), Ht(n.R)) + 1;
        static int Bal(Node n) => Ht(n.L) - Ht(n.R);
        static Node RR(Node y) { var x = y.L!; y.L = x.R; x.R = y; Up(y); Up(x); return x; }
        static Node RL(Node x) { var y = x.R!; x.R = y.L; y.L = x; Up(x); Up(y); return y; }
        Node Ins(Node? n, TKey k, TValue v)
        {
            if (n == null) return new Node(k, v);
            int c = k.CompareTo(n.K);
            if (c < 0) n.L = Ins(n.L, k, v); else if (c > 0) n.R = Ins(n.R, k, v); else { n.V = v; return n; }
            Up(n); int b = Bal(n);
            if (b > 1 && k.CompareTo(n.L!.K) < 0) return RR(n);
            if (b < -1 && k.CompareTo(n.R!.K) > 0) return RL(n);
            if (b > 1 && k.CompareTo(n.L!.K) > 0) { n.L = RL(n.L!); return RR(n); }
            if (b < -1 && k.CompareTo(n.R!.K) < 0) { n.R = RR(n.R!); return RL(n); }
            return n;
        }
        public void Insert(TKey k, TValue v) => root = Ins(root, k, v);
        public bool TryGet(TKey k, out TValue? v) { var n = root; v = default; while (n != null) { int c = k.CompareTo(n.K); if (c == 0) { v = n.V; return true; } n = c < 0 ? n.L : n.R; } return false; }
        public IEnumerable<TValue> InOrder() { var st = new Stack<Node>(); var n = root; while (st.Count > 0 || n != null) { if (n != null) { st.Push(n); n = n.L; } else { n = st.Pop(); yield return n.V; n = n.R; } } }
    }

    // ---------- MIN HEAP ----------
    public class MinHeap<T>
    {
        readonly List<T> a = new(); readonly IComparer<T> cmp;
        public MinHeap(IComparer<T>? c = null) { cmp = c ?? Comparer<T>.Default; }
        public int Count => a.Count;
        public void Push(T x) { a.Add(x); Up(a.Count - 1); }
        public T Peek() { if (a.Count == 0) throw new InvalidOperationException(); return a[0]; }
        public T Pop() { var r = Peek(); a[0] = a[^1]; a.RemoveAt(a.Count - 1); if (a.Count > 0) Down(0); return r; }
        public IEnumerable<T> Items() => a.ToArray();
        void Up(int i) { while (i > 0) { int p = (i - 1) / 2; if (cmp.Compare(a[i], a[p]) >= 0) break; (a[i], a[p]) = (a[p], a[i]); i = p; } }
        void Down(int i) { while (true) { int l = i * 2 + 1, r = i * 2 + 2, m = i; if (l < a.Count && cmp.Compare(a[l], a[m]) < 0) m = l; if (r < a.Count && cmp.Compare(a[r], a[m]) < 0) m = r; if (m == i) break; (a[i], a[m]) = (a[m], a[i]); i = m; } }
    }

    // ---------- GRAPH (BFS/DFS/Prim MST) ----------
    public class Graph<T> where T : notnull
    {
        readonly Dictionary<T, List<(T to, int w)>> g = new();
        public void AddVertex(T v) { if (!g.ContainsKey(v)) g[v] = new(); }
        public void AddUndirectedEdge(T a, T b, int w) { AddVertex(a); AddVertex(b); g[a].Add((b, w)); g[b].Add((a, w)); }
        public IEnumerable<T> Bfs(T start) { var q = new Queue<T>(); var vis = new HashSet<T>(); q.Enqueue(start); vis.Add(start); while (q.Count > 0) { var u = q.Dequeue(); yield return u; foreach (var (v, _) in g[u]) if (vis.Add(v)) q.Enqueue(v); } }
        public IEnumerable<T> Dfs(T start) { var st = new Stack<T>(); var vis = new HashSet<T>(); st.Push(start); while (st.Count > 0) { var u = st.Pop(); if (!vis.Add(u)) continue; yield return u; foreach (var (v, _) in g[u]) st.Push(v); } }
        public (List<(T a, T b, int w)> edges, int total) PrimMst(T start)
        {
            var vis = new HashSet<T> { start };
            var pq = new PriorityQueue<(T a, T b, int w), int>();
            foreach (var e in g[start]) pq.Enqueue((start, e.to, e.w), e.w);
            var res = new List<(T, T, int)>(); int total = 0;
            while (pq.Count > 0 && vis.Count < g.Count)
            {
                var (a, b, w) = pq.Dequeue();
                if (vis.Contains(b)) continue;
                vis.Add(b);
                res.Add((a, b, w));
                total += w;
                foreach (var e in g[b]) if (!vis.Contains(e.to)) pq.Enqueue((b, e.to, e.w), e.w);
            }
            return (res, total);
        }
        public IEnumerable<T> Vertices() => g.Keys;
    }

    // ---------- SERVICE ----------
    public class RequestStatusService
    {
        private readonly AvlTree<int, ServiceRequest> _byId = new();
        private readonly MinHeap<ServiceRequest> _urgent;
        private readonly Graph<string> _wards = new();
        private readonly List<ServiceRequest> _all = new();

        public RequestStatusService()
        {
            _urgent = new MinHeap<ServiceRequest>(Comparer<ServiceRequest>.Create((a, b) =>
            {
                int c = a.Priority.CompareTo(b.Priority);
                if (c != 0) return c;
                return a.Created.CompareTo(b.Created);
            }));
            Seed();
        }

        private void Seed()
        {
            var rnd = new Random(42);
            string[] wards = { "Ward 1", "Ward 2", "Ward 3", "Ward 4", "Ward 5" };
            for (int i = 1; i <= 60; i++)
            {
                var r = new ServiceRequest
                {
                    Id = i,
                    Title = $"Issue {i}",
                    Ward = wards[rnd.Next(wards.Length)],
                    Priority = rnd.Next(1, 5),
                    Created = DateTime.Today.AddDays(-rnd.Next(0, 20)).AddMinutes(rnd.Next(0, 1440)),
                    Status = (RequestStatus)rnd.Next(0, 4)
                };
                _all.Add(r); _byId.Insert(r.Id, r); _urgent.Push(r);
            }

            _wards.AddUndirectedEdge("Ward 1", "Ward 2", 3);
            _wards.AddUndirectedEdge("Ward 2", "Ward 3", 5);
            _wards.AddUndirectedEdge("Ward 3", "Ward 4", 2);
            _wards.AddUndirectedEdge("Ward 4", "Ward 5", 4);
            _wards.AddUndirectedEdge("Ward 1", "Ward 5", 7);
            _wards.AddUndirectedEdge("Ward 2", "Ward 5", 6);
        }

        public IEnumerable<ServiceRequest> All() => _all.OrderBy(r => r.Id);
        public ServiceRequest? FindById(int id) => _byId.TryGet(id, out var v) ? v : null;
        public IEnumerable<ServiceRequest> UrgentTop(int n) => _urgent.Items().OrderBy(r => r.Priority).ThenBy(r => r.Created).Take(n);
        public ServiceRequest ServeNext() => _urgent.Pop();
        public IEnumerable<string> Wards() => _wards.Vertices();
        public IEnumerable<string> Traverse(string start, string algo) =>
            (algo?.ToUpperInvariant() == "DFS" ? _wards.Dfs(start) : _wards.Bfs(start)).Select(x => x.ToString());
        public (List<(string a, string b, int w)> edges, int total) Mst(string start)
        {
            var (e, t) = _wards.PrimMst(start);
            var converted = e.Select(x => (a: x.Item1!.ToString(), b: x.Item2!.ToString(), w: x.Item3)).ToList();
            return (converted, t);
        }
    }
}

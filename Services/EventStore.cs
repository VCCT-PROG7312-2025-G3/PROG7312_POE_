using PROG7312_POE.Domain;

namespace PROG7312_POE.Services
{
    public interface IEventStore
    {
        IEnumerable<MunicipalEvent> All();
        IEnumerable<MunicipalEvent> Upcoming(int count = 20);
        IEnumerable<MunicipalEvent> Search(string term);
        IEnumerable<MunicipalEvent> Recommend(string? term = null, int count = 5);
        MunicipalEvent Add(MunicipalEvent e);
        bool UndoLastAdd(); // demo of Stack (undo)
    }

    public class EventStore : IEventStore
    {
        private readonly object _lock = new();
        private int _nextId = 1;

        // Required DS from brief:
        private readonly Dictionary<int, MunicipalEvent> _byId = new();                  // Dictionary
        private readonly SortedDictionary<DateTime, List<int>> _byStart = new();         // SortedDictionary
        private readonly HashSet<string> _locations = new(StringComparer.OrdinalIgnoreCase); // HashSet
        private readonly PriorityQueue<int, int> _byUrgency = new();                     // PriorityQueue (use negative key)
        private readonly Queue<string> _recentSearches = new();                          // Queue
        private readonly Stack<int> _undoAdds = new();                                   // Stack

        public MunicipalEvent Add(MunicipalEvent e)
        {
            lock (_lock)
            {
                e.Id = _nextId++;
                _byId[e.Id] = e;

                if (!_byStart.TryGetValue(e.Start, out var list))
                    _byStart[e.Start] = list = new();
                list.Add(e.Id);

                _locations.Add(e.Location);
                _byUrgency.Enqueue(e.Id, -e.Priority); // higher priority first
                _undoAdds.Push(e.Id);
                return e;
            }
        }

        public bool UndoLastAdd()
        {
            lock (_lock)
            {
                if (_undoAdds.Count == 0) return false;
                var id = _undoAdds.Pop();
                if (!_byId.Remove(id)) return false;

                foreach (var kvp in _byStart.ToList())
                {
                    kvp.Value.Remove(id);
                    if (kvp.Value.Count == 0) _byStart.Remove(kvp.Key);
                }

                // rebuild urgency queue (simple + safe for demo)
                var all = _byId.Values.ToList();
                _byUrgency.Clear();
                foreach (var ev in all) _byUrgency.Enqueue(ev.Id, -ev.Priority);
                return true;
            }
        }

        public IEnumerable<MunicipalEvent> All()
        {
            lock (_lock) return _byId.Values.OrderBy(e => e.Start).ToList();
        }

        public IEnumerable<MunicipalEvent> Upcoming(int count = 20)
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                return _byStart
                    .Where(kvp => kvp.Key >= now)
                    .Take(count)
                    .SelectMany(kvp => kvp.Value)
                    .Select(id => _byId[id])
                    .ToList();
            }
        }

        public IEnumerable<MunicipalEvent> Search(string term)
        {
            term ??= string.Empty;
            term = term.Trim();
            if (!string.IsNullOrWhiteSpace(term))
            {
                lock (_lock)
                {
                    _recentSearches.Enqueue(term);
                    if (_recentSearches.Count > 25) _recentSearches.Dequeue();
                }
            }

            var q = term.ToLowerInvariant();
            return All().Where(e =>
                e.Title.ToLowerInvariant().Contains(q) ||
                e.Description.ToLowerInvariant().Contains(q) ||
                e.Location.ToLowerInvariant().Contains(q) ||
                e.Category.ToString().ToLowerInvariant().Contains(q));
        }

        public IEnumerable<MunicipalEvent> Recommend(string? term = null, int count = 5)
        {
            var baseList = All().OrderByDescending(e => e.Priority).ThenBy(e => e.Start);

            if (string.IsNullOrWhiteSpace(term))
                return baseList.Take(count);

            var q = term.ToLowerInvariant();
            return baseList.Where(e =>
                    e.Category.ToString().ToLowerInvariant().Contains(q) ||
                    e.Location.ToLowerInvariant().Contains(q) ||
                    e.Title.ToLowerInvariant().Contains(q))
                .Take(count);
        }
    }
}

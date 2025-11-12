using Microsoft.AspNetCore.Mvc;
using PROG7312_POE_.Services; // service namespace

namespace PROG7312_POE_.Controllers
{
    public class ServiceRequestStatusController : Controller
    {
        private readonly RequestStatusService _svc;

        public ServiceRequestStatusController(RequestStatusService svc)
        {
            _svc = svc;
        }

        // /ServiceRequestStatus
        public IActionResult Index()
        {
            ViewBag.Wards = _svc.Wards().OrderBy(x => x).ToArray();
            return View(); // Views/ServiceRequestStatus/Index.cshtml
        }

        // /ServiceRequestStatus/Search?id=12  (AVL lookup)
        [HttpGet]
        public IActionResult Search(int id)
        {
            var r = _svc.FindById(id);
            if (r == null) return NotFound(new { ok = false, message = "Request not found" });
            return Ok(new
            {
                ok = true,
                id = r.Id,
                title = r.Title,
                ward = r.Ward,
                priority = r.Priority,
                created = r.Created.ToString("s"),
                status = r.Status.ToString()
            });
        }

        // /ServiceRequestStatus/Urgent?take=10  (Heap peek/top-N)
        [HttpGet]
        public IActionResult Urgent(int take = 10)
        {
            var list = _svc.UrgentTop(take)
                .Select(r => new {
                    id = r.Id,
                    title = r.Title,
                    ward = r.Ward,
                    priority = r.Priority,
                    created = r.Created.ToString("s"),
                    status = r.Status.ToString()
                });
            return Ok(list);
        }

        // POST /ServiceRequestStatus/ServeNext  (Heap pop)
        [HttpPost]
        public IActionResult ServeNext()
        {
            var next = _svc.ServeNext();
            return Ok(new
            {
                ok = true,
                id = next.Id,
                title = next.Title,
                ward = next.Ward,
                priority = next.Priority,
                created = next.Created.ToString("s"),
                status = next.Status.ToString()
            });
        }

        // /ServiceRequestStatus/Traverse?start=Ward%201&algo=BFS  (Graph BFS/DFS)
        [HttpGet]
        public IActionResult Traverse(string start = "Ward 1", string algo = "BFS")
        {
            var order = _svc.Traverse(start, algo).ToArray();
            return Ok(new { start, algo, order });
        }

        // /ServiceRequestStatus/Mst?start=Ward%201  (Graph MST)
        // FIX: project tuple items into named properties so JSON serializes nicely
        [HttpGet]
        public IActionResult Mst(string start = "Ward 1")
        {
            var (edges, total) = _svc.Mst(start);
            // edges currently List<(string a, string b, int w)>
            var projected = edges.Select(e => new { a = e.a, b = e.b, w = e.w }).ToList();
            return Ok(new { total, edges = projected });
        }

        // Optional: show all (in-order traversal over AVL)
        [HttpGet]
        public IActionResult All()
        {
            var list = _svc.All().Select(r => new {
                id = r.Id,
                title = r.Title,
                ward = r.Ward,
                priority = r.Priority,
                created = r.Created.ToString("s"),
                status = r.Status.ToString()
            });
            return Ok(list);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;                 // 👈 add this
using PROG7312_POE.Services;
using PROG7312_POE.Domain;

namespace PROG7312_POE.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventStore _store;

        public EventsController(IEventStore store) => _store = store;

        // GET: /Events
        // Supports: text search (q), category (cat), and date range (from..to)
        [AllowAnonymous] // viewing is open to everyone
        public IActionResult Index(string? q, EventCategory? cat, DateTime? from, DateTime? to)
        {
            // Start from either search results or all events
            IEnumerable<MunicipalEvent> events =
                string.IsNullOrWhiteSpace(q) ? _store.All() : _store.Search(q);

            // Apply category filter
            if (cat.HasValue)
                events = events.Where(e => e.Category == cat.Value);

            // Apply date range (inclusive)
            if (from.HasValue)
                events = events.Where(e => e.Start >= from.Value);
            if (to.HasValue)
                events = events.Where(e => e.Start <= to.Value);

            var results = events.OrderBy(e => e.Start).ToList();

            // Supply UI state
            ViewBag.Query = q;
            ViewBag.Cat = cat;
            ViewBag.FromVal = from?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.ToVal = to?.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Recommended = _store.Recommend(q, 5);
            ViewBag.Total = results.Count;

            return View(results);
        }

        // GET: /Events/Create  (Admins only)
        [Authorize(Roles = "Admin")]
        public IActionResult Create() =>
            View(new MunicipalEvent { Start = DateTime.Now.AddHours(2) });

        // POST: /Events/Create  (Admins only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(MunicipalEvent model)
        {
            if (!ModelState.IsValid) return View(model);

            _store.Add(model);
            TempData["ok"] = "Event/Announcement added.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Events/Undo  (Admins only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Undo()
        {
            if (_store.UndoLastAdd()) TempData["ok"] = "Last add reverted.";
            else TempData["err"] = "Nothing to undo.";
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROG7312_POE.Data;
using PROG7312_POE.Domain;
using PROG7312_POE.ViewModels;

namespace PROG7312_POE.Controllers
{
    public class IssuesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly HashSet<string> _allowed;
        private readonly long _maxPerFile;
        private readonly long _maxTotal;

        public IssuesController(AppDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db;
            _env = env;

            _allowed = cfg.GetValue<string>("FileUpload:AllowedExtensions", ".jpg,.jpeg,.png,.pdf,.doc,.docx,.txt")
                          .Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim().ToLowerInvariant())
                          .ToHashSet();

            _maxPerFile = cfg.GetValue<int>("FileUpload:MaxFileSizeMb", 10) * 1024L * 1024L;
            _maxTotal = cfg.GetValue<int>("FileUpload:TotalMaxMb", 30) * 1024L * 1024L;
        }

        private static IEnumerable<SelectListItem> CatList() =>
            Enum.GetValues<IssueCategory>()
                .Select(c => new SelectListItem(c.ToString(), c.ToString()));

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = CatList();
            return View(new IssueCreateVm());
        }

        // Step 1: user submits form -> go to Confirm page
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(IssueCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = CatList();
                return View(vm);
            }

            TempData["Location"] = vm.Location;
            TempData["Category"] = vm.Category.ToString();
            TempData["Description"] = vm.Description;
            TempData["FollowUp"] = vm.WillingForFollowUp.ToString();
            TempData["Experience"] = vm.SubmissionExperience ?? "";

            return RedirectToAction(nameof(Confirm));
        }

        [HttpGet]
        public IActionResult Confirm()
        {
            var vm = new IssueCreateVm
            {
                Location = TempData["Location"]?.ToString() ?? "",
                Description = TempData["Description"]?.ToString() ?? "",
                WillingForFollowUp = bool.TryParse(TempData["FollowUp"]?.ToString(), out var f) && f,
                SubmissionExperience = TempData["Experience"]?.ToString()
            };
            Enum.TryParse<IssueCategory>(TempData["Category"]?.ToString(), out var cat);
            vm.Category = cat;

            // keep TempData around for POST
            TempData.Keep();
            return View(vm);
        }

        // Step 2: Confirm -> save to DB + save any uploaded files
        [HttpPost, ValidateAntiForgeryToken, ActionName("Confirm")]
        public async Task<IActionResult> ConfirmPost()
        {
            var vm = new IssueCreateVm
            {
                Location = TempData["Location"]?.ToString() ?? "",
                Description = TempData["Description"]?.ToString() ?? "",
                WillingForFollowUp = bool.TryParse(TempData["FollowUp"]?.ToString(), out var f) && f,
                SubmissionExperience = TempData["Experience"]?.ToString()
            };
            Enum.TryParse<IssueCategory>(TempData["Category"]?.ToString(), out var cat);
            vm.Category = cat;

            var issue = new Issue
            {
                Location = vm.Location.Trim(),
                Category = vm.Category,
                Description = vm.Description.Trim(),
                WillingForFollowUp = vm.WillingForFollowUp,
                SubmissionExperience = vm.SubmissionExperience
            };

            _db.Issues.Add(issue);
            await _db.SaveChangesAsync(); // gets Issue.Id

            long total = 0;
            var files = Request.Form.Files;
            if (files?.Count > 0)
            {
                var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "issues", issue.Id.ToString());
                Directory.CreateDirectory(uploadRoot);

                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_allowed.Contains(ext)) continue;
                    if (file.Length > _maxPerFile) continue;

                    total += file.Length;
                    if (total > _maxTotal) break;

                    var stored = $"{Guid.NewGuid():N}{ext}";
                    var savePath = Path.Combine(uploadRoot, stored);
                    using (var fs = System.IO.File.Create(savePath))
                        await file.CopyToAsync(fs);

                    _db.Attachments.Add(new Attachment
                    {
                        IssueId = issue.Id,
                        OriginalFileName = Path.GetFileName(file.FileName),
                        StoredFileName = stored,
                        ContentType = file.ContentType,
                        SizeBytes = file.Length
                    });
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = issue.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _db.Issues
                                 .Include(i => i.Attachments)
                                 .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            return View(issue);
        }
    }
}

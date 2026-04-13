using FixedAssetSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixedAssetSystem.Controllers
{
    public class FixedAssetController : Controller
    {
        private readonly FixedAssetSystemContext _context;

        public FixedAssetController(FixedAssetSystemContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            var requests = await _context.FixedAssetRequests
                .Include(r => r.RequestedByEmployee)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        public async Task<IActionResult> Details(int id)
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            var request = await _context.FixedAssetRequests
                .Include(r => r.RequestedByEmployee)
                .Include(r => r.EvaluatedByEmployee)
                .Include(r => r.ExistingUnitDetails)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            return View(request);
        }

        // GET: FixedAsset/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            var request = await _context.FixedAssetRequests
                .Include(r => r.ExistingUnitDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Optional: only allow editing of "Draft" requests
            if (request.RequestStatus != "Draft")
            {
                TempData["ErrorMessage"] = "Only requests with 'Draft' status can be edited.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // POST: FixedAsset/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FixedAssetRequest updatedRequest)
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            if (id != updatedRequest.Id) return NotFound();

            // Remove validation for navigation properties (they are not needed for update)
            ModelState.Remove("RequestedByEmployee");
            ModelState.Remove("EvaluatedByEmployee");

            // ========== ADD CUSTOM VALIDATIONS (same as Create) ==========
            if (updatedRequest.Quantity < 1)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }

            // We'll parse existing units first so we can validate them
            List<ExistingUnitDetail> parsedUnits = new List<ExistingUnitDetail>();
            if (updatedRequest.RequestType == "Additional")
            {
                int idx = 0;
                while (Request.Form.ContainsKey($"ExistingUnits[{idx}].Description"))
                {
                    var description = Request.Form[$"ExistingUnits[{idx}].Description"].ToString();
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        var itemNoStr = Request.Form[$"ExistingUnits[{idx}].ItemNo"].ToString();
                        int itemNo = string.IsNullOrEmpty(itemNoStr) ? idx + 1 : int.Parse(itemNoStr);
                        var location = Request.Form[$"ExistingUnits[{idx}].Location"].ToString();
                        var userName = Request.Form[$"ExistingUnits[{idx}].UserName"].ToString();
                        var remarks = Request.Form[$"ExistingUnits[{idx}].Remarks"].ToString();

                        parsedUnits.Add(new ExistingUnitDetail
                        {
                            ItemNo = itemNo,
                            Description = description,
                            Location = location ?? string.Empty,
                            UserName = userName ?? string.Empty,
                            Remarks = remarks ?? string.Empty
                        });
                    }
                    idx++;
                }

                if (!parsedUnits.Any())
                {
                    ModelState.AddModelError("ExistingUnits", "At least one existing unit with a Description is required when Request Type is 'Additional'.");
                }
            }
            // ============================================================

            if (!ModelState.IsValid)
            {
                return View(updatedRequest);
            }

            try
            {
                var existing = await _context.FixedAssetRequests
                    .Include(r => r.ExistingUnitDetails)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (existing == null) return NotFound();

                // Update basic fields
                existing.Department = updatedRequest.Department;
                existing.Section = updatedRequest.Section;
                existing.TargetDateNeeded = updatedRequest.TargetDateNeeded;
                existing.Quantity = updatedRequest.Quantity;
                existing.AssetType = updatedRequest.AssetType;
                existing.DetailedDescription = updatedRequest.DetailedDescription;
                existing.ReasonPurpose = updatedRequest.ReasonPurpose;
                existing.ProposedLocation = updatedRequest.ProposedLocation;
                existing.EstimatedLifeSpan = updatedRequest.EstimatedLifeSpan;
                existing.RequestType = updatedRequest.RequestType;
                existing.DamagedReportNo = updatedRequest.DamagedReportNo;
                existing.EvaluatedByName = updatedRequest.EvaluatedByName;
                existing.UpdatedAt = DateTime.Now;

                // Handle existing units
                // Remove all old units first
                _context.ExistingUnitDetails.RemoveRange(existing.ExistingUnitDetails);

                if (existing.RequestType == "Additional" && parsedUnits.Any())
                {
                    foreach (var unit in parsedUnits)
                    {
                        unit.FixedAssetRequestId = existing.Id;
                        _context.ExistingUnitDetails.Add(unit);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Request updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.FixedAssetRequests.Any(e => e.Id == id)) return NotFound();
                throw;
            }
        }

        // GET: FixedAsset/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            Console.WriteLine($"[DELETE GET] Called with id = {id}");
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null)
            {
                Console.WriteLine("[DELETE GET] No session, redirecting to login.");
                return RedirectToAction("Login", "Account");
            }
            Console.WriteLine("[DELETE GET] Session OK, fetching request...");

            var request = await _context.FixedAssetRequests
                .Include(r => r.ExistingUnitDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                Console.WriteLine("[DELETE GET] Request not found.");
                return NotFound();
            }

            if (request.RequestStatus != "Draft")
            {
                Console.WriteLine($"[DELETE GET] Request status is {request.RequestStatus}, not Draft. Redirecting.");
                TempData["ErrorMessage"] = "Only requests with 'Draft' status can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("[DELETE GET] Returning view.");
            try
            {
                return View(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DELETE GET] VIEW RENDERING EXCEPTION: {ex.ToString()}");
                throw; // re‑throw to see the crash, but at least we logged it
            }
        }

        // POST: FixedAsset/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"[DELETE POST] Called with id = {id}");
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null)
            {
                Console.WriteLine("[DELETE POST] No session, redirecting to login.");
                return RedirectToAction("Login", "Account");
            }
            Console.WriteLine("[DELETE POST] Session OK, fetching request with all children...");

            try
            {
                var request = await _context.FixedAssetRequests
                    .Include(r => r.ExistingUnitDetails)
                    .Include(r => r.FixedAssetRequestApprovals)
                    .Include(r => r.MemorandumReceipts)
                    .Include(r => r.FixedAssetPrintLogs)
                    .Include(r => r.RequestStatusHistories)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    Console.WriteLine("[DELETE POST] Request not found.");
                    return NotFound();
                }

                Console.WriteLine($"[DELETE POST] Found request. ExistingUnits: {request.ExistingUnitDetails?.Count() ?? 0}, " +
                                  $"Approvals: {request.FixedAssetRequestApprovals?.Count() ?? 0}, " +
                                  $"Memoranda: {request.MemorandumReceipts?.Count() ?? 0}, " +
                                  $"PrintLogs: {request.FixedAssetPrintLogs?.Count() ?? 0}, " +
                                  $"StatusHistory: {request.RequestStatusHistories?.Count() ?? 0}");

                // Remove children (RemoveRange works even if empty)
                _context.ExistingUnitDetails.RemoveRange(request.ExistingUnitDetails);
                _context.FixedAssetRequestApprovals.RemoveRange(request.FixedAssetRequestApprovals);
                _context.MemorandumReceipts.RemoveRange(request.MemorandumReceipts);
                _context.FixedAssetPrintLogs.RemoveRange(request.FixedAssetPrintLogs);
                _context.RequestStatusHistories.RemoveRange(request.RequestStatusHistories);
                Console.WriteLine("[DELETE POST] Child records removed from context.");

                _context.FixedAssetRequests.Remove(request);
                Console.WriteLine("[DELETE POST] Main request marked for removal.");
                await _context.SaveChangesAsync();
                Console.WriteLine("[DELETE POST] SaveChanges succeeded.");

                TempData["SuccessMessage"] = "Request deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DELETE POST] EXCEPTION: {ex.ToString()}");
                TempData["ErrorMessage"] = "An error occurred while deleting the request. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FixedAssetRequest request)
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            // Set foreign key and name BEFORE validation
            request.RequestedByEmployeeId = empId.Value;
            request.RequestedByName = HttpContext.Session.GetString("EmployeeName");

            // Manual parsing of TargetDateNeeded
            var targetDateStr = Request.Form["TargetDateNeeded"].ToString();
            if (!DateOnly.TryParseExact(targetDateStr, "yyyy-MM-dd", out var targetDate))
            {
                ModelState.AddModelError("TargetDateNeeded", "Invalid date format. Please use YYYY-MM-DD.");
                return View(request);
            }
            request.TargetDateNeeded = targetDate;

            // Remove validation for navigation properties
            ModelState.Remove("RequestedByEmployee");
            ModelState.Remove("EvaluatedByEmployee");

            // ========== Manu-manong kunin ang ExistingUnits (indexed fields) ==========
            List<ExistingUnitDetail> ExistingUnits = new List<ExistingUnitDetail>();
            int idx = 0;
            while (Request.Form.ContainsKey($"ExistingUnits[{idx}].Description"))
            {
                var description = Request.Form[$"ExistingUnits[{idx}].Description"].ToString();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    var itemNoStr = Request.Form[$"ExistingUnits[{idx}].ItemNo"].ToString();
                    int itemNo = string.IsNullOrEmpty(itemNoStr) ? idx + 1 : int.Parse(itemNoStr);
                    var location = Request.Form[$"ExistingUnits[{idx}].Location"].ToString();
                    var userName = Request.Form[$"ExistingUnits[{idx}].UserName"].ToString();
                    var remarks = Request.Form[$"ExistingUnits[{idx}].Remarks"].ToString();

                    ExistingUnits.Add(new ExistingUnitDetail
                    {
                        ItemNo = itemNo,
                        Description = description,
                        Location = location ?? string.Empty,
                        UserName = userName ?? string.Empty,
                        Remarks = remarks ?? string.Empty
                    });
                }
                idx++;
            }
            // ===========================================

            // Custom validations
            if (request.Quantity < 1)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }

            if (request.RequestType == "Additional")
            {
                bool hasValidUnit = ExistingUnits.Any(u => !string.IsNullOrWhiteSpace(u.Description));
                if (!hasValidUnit)
                {
                    ModelState.AddModelError("ExistingUnits", "At least one existing unit with a Description is required when Request Type is 'Additional'.");
                }
            }

            if (ModelState.IsValid)
            {
                request.CreatedAt = DateTime.Now;
                request.UpdatedAt = DateTime.Now;
                request.RequestStatus = "Draft";
                request.RequestedAt = DateTime.Now;
                request.DateRequested = DateOnly.FromDateTime(DateTime.Now);

                int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        if (attempt > 1)
                            request.ControlNo = GenerateControlNo(empId.Value, attempt - 1);
                        else
                            request.ControlNo = GenerateControlNo(empId.Value);

                        _context.Add(request);
                        await _context.SaveChangesAsync();

                        if (request.RequestType == "Additional" && ExistingUnits.Any())
                        {
                            foreach (var unit in ExistingUnits)
                            {
                                unit.FixedAssetRequestId = request.Id;
                                _context.ExistingUnitDetails.Add(unit);
                            }
                            await _context.SaveChangesAsync();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_FixedAssetRequests_ControlNo") == true)
                    {
                        if (attempt == maxRetries) throw;
                    }
                }
            }

            // Preserve existing units on validation failure
            if (request.RequestType == "Additional" && ExistingUnits.Any())
            {
                ViewBag.ExistingUnits = ExistingUnits;
            }

            return View(request);
        }

        public async Task<IActionResult> Print(int id)
        {
            var empId = HttpContext.Session.GetInt32("EmployeeId");
            if (empId == null) return RedirectToAction("Login", "Account");

            // Optional: stored procedure logging
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_PrintFixedAssetRequest @RequestID={0}, @PrintedByEmployeeID={1}",
                    id, empId.Value);
            }
            catch { }

            var request = await _context.FixedAssetRequests
                .Include(r => r.RequestedByEmployee)
                .Include(r => r.EvaluatedByEmployee)
                    .Include(r => r.ExistingUnitDetails)   // <--- IDAGDAG ITO
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            return View(request);
        }

        private string GenerateControlNo(int employeeId, int retryCount = 0)
        {
            if (retryCount > 5)
                throw new Exception("Unable to generate a unique control number after 5 attempts.");

            var employee = _context.Employees.Find(employeeId);
            string location = employee?.Location?.Trim() ?? "F1";
            string year = DateTime.Now.ToString("yy");
            string prefix = $"GAD-FAR-{location}-{year}-";

            // Kunin ang pinakamataas na umiiral na numero para sa prefix na ito
            var lastRequest = _context.FixedAssetRequests
                .Where(r => r.ControlNo != null && r.ControlNo.StartsWith(prefix))
                .OrderByDescending(r => r.ControlNo)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastRequest != null)
            {
                string lastNumberStr = lastRequest.ControlNo.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNum))
                    nextNumber = lastNum + 1;
            }

            string newControlNo = $"{prefix}{nextNumber:D3}";

            // Bago i‑return, tiyakin na wala pang gumagamit ng numerong ito (para iwasan ang bihirang race condition)
            bool alreadyExists = _context.FixedAssetRequests.Any(r => r.ControlNo == newControlNo);
            if (alreadyExists)
            {
                // Kung may umiiral na (napakabihirang), mag‑retry – tataas ang nextNumber sa susunod na tawag
                return GenerateControlNo(employeeId, retryCount + 1);
            }

            return newControlNo;
        }
    }
}
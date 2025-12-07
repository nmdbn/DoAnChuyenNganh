using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Areas.Admin.Controllers
{
    public class CourseController : AdminBaseController
    {
        private readonly DoAnChuyenNganhContext _context;

        public CourseController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: Course
        public async Task<IActionResult> Index()
        {
            var courses = _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation)
                .OrderByDescending(c => c.CreatedAt);

            return View(await courses.ToListAsync());
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation)
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Course/Create
        public IActionResult Create()
        {
            PopulateDropDownLists();
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Description,ShortDescription,SubjectId,GradeId,InstructorId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price")] Course course)
        {
            // Remove validation errors for fields we're setting manually
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("Instructor");
            ModelState.Remove("Subject");
            ModelState.Remove("Grade");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("IsPublished");
            ModelState.Remove("IsFeatured");
            ModelState.Remove("ViewCount");
            ModelState.Remove("EnrollmentCount");
            ModelState.Remove("AverageRating");
            ModelState.Remove("RatingCount");

            // Debug: Log validation errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                // You can set breakpoint here to see errors
                foreach (var error in errors)
                {
                    foreach (var subError in error.Errors)
                    {
                        Console.WriteLine($"Field: {error.Key}, Error: {subError.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values
                    course.CreatedAt = DateTime.Now;
                    course.UpdatedAt = DateTime.Now;
                    course.CreatedBy = 1; // TODO: Replace with actual logged-in user ID
                    course.IsPublished = false;
                    course.IsFeatured = false;
                    course.ViewCount = 0;
                    course.EnrollmentCount = 0;
                    course.AverageRating = 0;
                    course.RatingCount = 0;

                    // Set default price if null
                    if (course.Price == null)
                    {
                        course.Price = 0;
                    }

                    _context.Add(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Course created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating course: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", "Inner Exception: " + ex.InnerException.Message);
                    }
                }
            }
            else
            {
                // Add error message to show validation failed
                TempData["ErrorMessage"] = "Please correct the errors below.";
            }

            PopulateDropDownLists(course);
            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            PopulateDropDownLists(course);
            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Title,Slug,Description,ShortDescription,SubjectId,GradeId,InstructorId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price,IsPublished,IsFeatured")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }
            ModelState.Remove("CreatedByNavigation"); // <== KHẮC PHỤC LỖI CHÍNH
            ModelState.Remove("UpdatedByNavigation");
            // Remove validation errors for fields we're setting manually/preserving from DB
            // NOTE: It is critical to remove fields not included in the Bind attribute
            ModelState.Remove("CreatedAt");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("UpdatedBy"); // Will be set manually
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("Instructor");
            ModelState.Remove("Subject");
            ModelState.Remove("Grade");

            if (ModelState.IsValid) // ModelState.IsValid sẽ không kiểm tra CreatedByNavigation nữa
            {
                try
                {
                    // Get existing course to preserve values not in the form
                    var existingCourse = await _context.Courses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CourseId == id);
                    if (existingCourse == null)
                    {
                        return NotFound();
                    }

                    // Preserve original values
                    course.CreatedAt = existingCourse.CreatedAt;
                    course.CreatedBy = existingCourse.CreatedBy;
                    course.ViewCount = existingCourse.ViewCount;
                    course.EnrollmentCount = existingCourse.EnrollmentCount;
                    course.AverageRating = existingCourse.AverageRating;
                    course.RatingCount = existingCourse.RatingCount;
                    course.PublishedAt = existingCourse.PublishedAt;

                    // Update modified values
                    course.UpdatedAt = DateTime.Now;
                    course.UpdatedBy = 1; // TODO: Replace with actual logged-in user ID

                    // Set PublishedAt if publishing for first time
                    if (course.IsPublished == true && existingCourse.IsPublished == false)
                    {
                        course.PublishedAt = DateTime.Now;
                    }
                    else if (course.IsPublished == false)
                    {
                        course.PublishedAt = null;
                    }

                    _context.Update(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Course updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating course: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", "Inner Exception: " + ex.InnerException.Message);
                    }
                    TempData["ErrorMessage"] = "Error saving changes. Please check details.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the validation errors below.";
            }

            PopulateDropDownLists(course);
            return View(course);
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course != null)
                {
                    // Check if course has enrollments
                    var hasEnrollments = await _context.Enrollments
                        .AnyAsync(e => e.CourseId == id);

                    if (hasEnrollments)
                    {
                        TempData["ErrorMessage"] = "Cannot delete course with active enrollments. Please unpublish instead.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Course deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting course: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        // Helper method to populate dropdown lists
        private void PopulateDropDownLists(Course course = null)
        {
            // Subjects dropdown
            ViewData["SubjectId"] = new SelectList(
                _context.Subjects.Where(s => s.IsActive == true).OrderBy(s => s.SubjectName),
                "SubjectId",
                "SubjectName",
                course?.SubjectId
            );

            // Grades dropdown
            ViewData["GradeId"] = new SelectList(
                _context.Grades.Where(g => g.IsActive == true).OrderBy(g => g.GradeLevel),
                "GradeId",
                "GradeName",
                course?.GradeId
            );

            // Instructors dropdown - only show users with Instructor or Admin role
            var instructors = _context.Users
                .Where(u => u.IsActive == true && (u.RoleId == 3 || u.RoleId == 4))
                .Select(u => new
                {
                    u.UserId,
                    FullName = u.FirstName + " " + u.LastName + " (" + u.Username + ")"
                })
                .OrderBy(u => u.FullName);

            ViewData["InstructorId"] = new SelectList(
                instructors,
                "UserId",
                "FullName",
                course?.InstructorId
            );
        }
    }
}
﻿using AutoMapper;
using Lexicon_LMS.Core.Entities;
using Lexicon_LMS.Core.Entities.ViewModel;
using Lexicon_LMS.Data;
using Lexicon_LMS.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace Lexicon_LMS.Controllers
{
    public class StudentsController : Controller
    {
        private readonly Lexicon_LMSContext _context;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<User> _userManager;

        public StudentsController(IWebHostEnvironment webHostEnvironment, UserManager<User> userManager, Lexicon_LMSContext context, IMapper mapper)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            this.mapper = mapper;

        }

        // GET: StudentsController
        public async Task<ActionResult> WelcomePage()
        {
            var userId = _userManager.GetUserId(User);

            //var user = await _context.Users.Select(u => new StudentCourseViewModel
            //{
            //    Id = u.Id,
            //    CourseName = u.Course.CourseName,
            //    CourseDescription = u.Course.Description,
            //    Documents = u.Documents
            //    //Add more....
            //})
            //.FirstOrDefaultAsync(u => u.Id == userId);// _context.Users.Find(_userManager.GetUserId(User));

            var viewModel = mapper.ProjectTo<StudentCourseViewModel>(_context.Users).FirstOrDefault(u => u.Id == userId);
          
            return View(viewModel);
        }

        // GET: StudentsController
        public async Task<ActionResult> Index()
        {


            var logedinUser = _context.Users.Find(_userManager.GetUserId(User));

            var viewModel = GetStudents();

            if (logedinUser != null && logedinUser.CourseId != null)
            {  
                var CourseSuers = viewModel.Where(c => c.CourseId == logedinUser.CourseId);

                return View(CourseSuers.ToList());
            }
        
            return View(await viewModel.ToListAsync());
        }
 
        //public async Task<IActionResult> Welcome(int? id)
        //{
        //    var viewModel = await _context.Course
        //        .Select(a => new Course
        //        {
        //            CourseName = a.CourseName,
        //            Description = a.Description,
        //        })
        //        .FirstOrDefaultAsync(c => c.Id == id);

        //    var Details = viewModel.CourseName;


        //    return View(Details);


       // }

        // GET: StudentsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: StudentsController/Create
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult> CreateAsync()
        {
            var courses = await _context.Course.ToListAsync();
            var studentV = new StudentCreateViewModel
            {
                AvailableCourses = courses.Select(c => new SelectListItem
                {
                    Text = c.CourseName.ToString(),
                    Value = c.Id.ToString(),
                    Selected = false
                }).ToList()
            };     

            return View(studentV);
        }

        // POST: StudentsController/Create
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("FirstName,LastName,Email,CourseId")] User Student)
        {
            if (ModelState.IsValid)
            {
                Student.UserName = Student.Email;
                var result = await _userManager.CreateAsync(Student, "StudentPW123!");

                if (result.Succeeded)
                {
                    var result2 = await _userManager.AddToRoleAsync(Student, "Student");
                    if (!result2.Succeeded) throw new Exception(string.Join("\n", result.Errors));
                }


                return RedirectToAction(nameof(Index));
            }
            return View(nameof(Index));

        }

        // GET: StudentsController/Edit/5
        [Authorize(Roles = "Teacher")]
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: StudentsController/Edit/5
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: StudentsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: StudentsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        private IQueryable<StudentViewModel> GetStudents()
        {
            return _context.Users.Select(x => new StudentViewModel
            {
                Id = x.Id,
                CourseId = x.CourseId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                UserName = x.UserName,
                ImagePicture = x.ImagePicture

            });
        }

        public async Task<CurrentViewModel> CurrentCourse(int? id)
        {
            var course = _context.Course.Include(a => a.Users)
                 .Include(a => a.Modules)
                .ThenInclude(a => a.Activities)
                .FirstOrDefault(a => a.Id == id);
          
          //  var students = course.Users.Count();

            var assignments = await _context.Activity.Where(c => c.ActivityType.ActivityTypeName == "Assignment" && c.Module.CourseId == id)
              .OrderBy(a => a.StartDate)
              .Select(a => new AssignmentsViewModel
              {
                  Id = a.Id,
                  Name = a.ActivityName,
                  DueTime = a.EndDate,
                //  Finished = a.Documents.Where(d => d.IsFinished.Equals(true)).Count() * 100 / students
              })
              .ToListAsync();
            var timeNow = DateTime.Now;
            var module = course.Modules.OrderBy(t => Math.Abs((t.StartDate - timeNow).Ticks)).First();
            var model = new CurrentViewModel
            {
                course = course,
              Assignments = assignments,
               Module = module,

            };

            return model;
        }
        private List<ModuleViewModel> SetCurrentModule(List<ModuleViewModel> modules, int currentModuleId)
        {
            foreach (var module in modules)
            {
                if (module.Id == currentModuleId)
                {
                    module.IsCurrentModule = true;
                }
                else
                {
                    module.IsCurrentModule = false;
                }
            }

            return modules;
        }
        [AllowAnonymous]

        public async Task<IActionResult> MainPage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var current = await CurrentCourse(id);
            var currentCourse = current.course;


            if (current.course.Modules.Count == 0)

                return View(new TeacherViewModel
                {
                    Current = new CurrentViewModel
                    {
                        course = current.course,

                        Assignments = null,
                    },
                    AssignmentList = null,
                    ModuleList = null,
                    ActivityList = null
                });

            var assignmentList = await AssignmentListTeacher(id);
            var moduleList = await GetTeacherModuleListAsync(id);
            var module = moduleList.Find(y => y.IsCurrentModule);
            var activityList = new List<ActivityListViewModel>();
            
          

            if (module != null)
                activityList = await GetModuleActivityListAsync(module.Id);

            var model = new TeacherViewModel
            {
                Current= current,
                ModuleList = moduleList,
                ActivityList = activityList,
                AssignmentList = assignmentList

            };

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
        public async Task<IActionResult> GetTeacherActivityAjax(int? id)
        {
            if (id == null) return BadRequest();

            if (Request.IsAjax())
            {
                var module = await _context.Module.FirstOrDefaultAsync(m => m.Id == id);
                var current = await CurrentCourse(module.CourseId);

                var modules = await _context.Module
                    .Where(m => m.CourseId == module.CourseId)
                    .OrderBy(m => m.StartDate)
                    .Select(m => new ModuleViewModel
                    {
                        Id = m.Id,
                        Name = m.ModulName,
                        StartDate = m.StartDate,
                        //EndDate = m.EndDate,
                        IsCurrentModule = false

                    })
                    //.FirstOrDefaultAsync(m => m.Id == id);
                   .ToListAsync();


                var teacherModel = new TeacherViewModel()
                {
                    ModuleList = modules,
                    ActivityList = GetModuleActivityListAsync((int)id).Result,
                    Current = current

                };

                return PartialView("ModuleAndActivityPartial", teacherModel);
            }

            return BadRequest();
        }
      

     
        public async Task<List<AssignmentListViewModel>> AssignmentListTeacher(int? id)
        {
            var students = _context.Course.Find(id);


            var assignments = await _context.Activity
                .Where(a => a.ActivityType.ActivityTypeName == "Assignment" && a.Module.CourseId == id)
                .Select(a => new AssignmentListViewModel
                {
                    Id = a.Id,
                    Name = a.ActivityName,
                    StartDate = a.StartDate,
                 //   EndDate = a.EndDate,
                })
                .ToListAsync();

            return assignments;
        }
   
        private async Task<List<ActivityListViewModel>> GetModuleActivityListAsync(int id)
        {
            var model = await _context.Activity
                .Include(a => a.ActivityType)
                .Include(a => a.Documents)
                .Where(a => a.Module.Id == id)
                .OrderBy(a => a.StartDate)
                .Select(a => new ActivityListViewModel
                {
                    Id = a.Id,
                    ActivityName = a.ActivityName,
                    StartDate = a.StartDate,
                   // EndDate = a.EndDate,
                    ActivityTypeActivityTypeName = a.ActivityType.ActivityTypeName,
                    Documents = a.Documents
                })
                .ToListAsync();

            return model;
        }
        public async Task<List<ModuleViewModel>> GetTeacherModuleListAsync(int? id)
        {
            var timeNow = DateTime.Now;

            var modules = await _context.Module.Include(a => a.Course)
                .Where(a => a.Course.Id == id)
                .Select(a => new ModuleViewModel
                {
                    Id = a.Id,
                    Name = a.ModulName,
                    StartDate = a.StartDate,
                    //EndDate = a.EndDate,
                    IsCurrentModule = false
                })
                .OrderBy(m => m.StartDate)
                .ToListAsync();
            var currentModuleId = modules.OrderBy(t => Math.Abs((t.StartDate - timeNow).Ticks)).First().Id;

            SetCurrentModule(modules, currentModuleId);


            return modules;
        }




            //}
            //return View(viewModel);
      //  }
            public async Task<IActionResult> TeacherHome()
            {
            var logedinUser = _context.Users.Find(_userManager.GetUserId(User));
            var viewModel = await mapper.ProjectTo<CourseViewModel>(_context.Course.Include(a => a.Modules).Include(a => a.Documents))
                .OrderBy(s => s.Id)
              .ToListAsync();
            if (logedinUser != null && logedinUser.CourseId != null)
            {
                var course = await _context.Course
                .Include(c => c.Modules)
                .ThenInclude(m => m.Activities)
                .ThenInclude(a => a.ActivityType)
                .FirstOrDefaultAsync(c => c.Id == logedinUser.CourseId);

                var activities = course.Modules.SelectMany(m => m.Activities).Select(x => new ActivityListViewModel
                {
                    Id = x.Id,
                    ActivityName = x.ActivityName,
                    StartDate = x.StartDate,
                    //EndDate = x.EndDate,
                    ActivityTypeActivityTypeName = x.ActivityType.ActivityTypeName,
                    //ModuleId = x.Module.Id,

                    //ModulName = x.Module.ModulName

                }).ToList();

                return View(activities);

                //TempData["CourseId"] = id;

            }      return View(viewModel);


        }
        //[Authorize(Roles = "Teacher")]
        //public async Task<IActionResult> TeacherHome(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }
        //    var Elearnig = await ElearningtListTeacher(id);

        //    var assignmentList = await AssignmentListTeacher(id);
        //    var moduleList = await GetTeacherModuleListAsync(id);
        //    var module = moduleList.Find(y => y.IsCurrentModule);
        //    var activityList = new List<ActivityListViewModel>();

        //    if (module != null)
        //        activityList = await GetModuleActivityListAsync(module.Id);

        //    var model = new TeacherViewModel
        //    {
        //        Elearning = Elearnig,
        //        AssignmentList = assignmentList,
        //        ModuleList = moduleList,
        //        ActivityList = activityList
        //    };

        //    if (model == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(model);
        //}
        public IActionResult FileUpload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FileUpload(ActivityListViewModel viewModel)
        {
            await UploadFile(viewModel.UploadedFile);
            var DocumentFile = viewModel.UploadedFile;
            var DocumentPath = Path.GetFileName("Upload");
            TempData["msg"] = "File uploaded successfully";
            return View();
        }

        public async Task<bool> UploadFile(IFormFile file)
        {
            //var pToFile = $"upload/{Path.Combine(file.CourseName, "upload")}/{file.ModuleName}/{file.ActivityName}";
            //var test = Path.Combine(webHostEnvironment.WebRootPath, pToFile);
            //if (!Directory.Exists(test))
            //{
            //    Directory.CreateDirectory(test);

            //}

            string path = "";
            bool isCopy = false;
            try
            {
                if (file.Length > 0)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Upload"));
                    using (var filestream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(filestream);
                    }
                    isCopy = true;
                }
                else
                {
                    isCopy = false;
                }

            }
            catch (Exception)
            {
                throw;
            }
            return isCopy;
        }
    }
}

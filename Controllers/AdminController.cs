using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingRegister.Data;
using MeetingRegister.Models;
using System.Text;

namespace MeetingRegister.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // MULTI-ADMIN CHECK
            var accounts = _configuration.GetSection("AdminSettings:Accounts").GetChildren();
            bool isValid = false;

            foreach (var account in accounts)
            {
                var username = account["Username"];
                var password = account["Password"];

                if (model.Username == username && model.Password == password)
                {
                    isValid = true;
                    break;
                }
            }

            if (isValid)
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, model.Username) };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return RedirectToAction(nameof(Dashboard));
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Registration");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = User.Identity?.Name ?? string.Empty;
            
            var meetings = await _context.Meetings
                .Include(m => m.Attendees)
                .Where(m => m.CreatedBy == currentUser)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();
            return View(meetings);
        }

        [HttpGet]
        [Authorize]
        public IActionResult CreateMeeting()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateMeeting(Meeting meeting)
        {
            ModelState.Remove("MeetingCode");
            ModelState.Remove("CreatedBy");

            if (meeting.StartTime < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError("StartTime", "Meeting start time cannot be in the past.");
            }

            if (meeting.ExpiryTime < meeting.StartTime)
            {
                ModelState.AddModelError("ExpiryTime", "Meeting expiry time must be at or after the start time.");
            }

            if (ModelState.IsValid)
            {
                meeting.MeetingCode = GenerateMeetingCode();
                meeting.CreatedBy = User.Identity?.Name ?? "Unknown";
                
                while (await _context.Meetings.AnyAsync(m => m.MeetingCode == meeting.MeetingCode))
                {
                    meeting.MeetingCode = GenerateMeetingCode();
                }

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();

                TempData["NewMeetingCode"] = meeting.MeetingCode;
                TempData["SuccessMessage"] = $"Meeting '{meeting.Title}' created successfully!";
                
                return RedirectToAction(nameof(Dashboard));
            }
            return View(meeting);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MeetingDetails(int id)
        {
            var currentUser = User.Identity?.Name ?? string.Empty;
            
            var meeting = await _context.Meetings
                .Include(m => m.Attendees)
                    .ThenInclude(a => a.Sessions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null) return NotFound();
            
            if (meeting.CreatedBy != currentUser)
            {
                return Forbid();
            }

            if (DateTime.Now > meeting.ExpiryTime)
            {
                bool changesMade = false;
                foreach (var attendee in meeting.Attendees)
                {
                    var openSessions = attendee.Sessions.Where(s => s.OutTime == null).ToList();
                    foreach (var session in openSessions)
                    {
                        session.OutTime = meeting.ExpiryTime;
                        session.Duration = session.OutTime - session.InTime;
                        _context.Update(session);
                        changesMade = true;
                    }
                }

                if (changesMade)
                {
                    await _context.SaveChangesAsync();
                }
            }

            return View(meeting);
        }

        private string GenerateMeetingCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var result = new StringBuilder(5);
            for (int i = 0; i < 5; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }
    }
}

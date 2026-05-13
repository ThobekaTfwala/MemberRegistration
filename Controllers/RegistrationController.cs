using Microsoft.AspNetCore.Mvc;
using MeetingRegister.Data;
using MeetingRegister.Models;
using Microsoft.EntityFrameworkCore;

namespace MeetingRegister.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Meeting? meeting = null;
            MeetingAttendee? attendee = null;

            // 1. Find the meeting and attendee
            if (!string.IsNullOrWhiteSpace(model.MeetingCode))
            {
                // MODE: Registration or Scan with Code
                meeting = await _context.Meetings
                    .Include(m => m.Attendees)
                        .ThenInclude(a => a.Sessions)
                    .FirstOrDefaultAsync(m => m.MeetingCode == model.MeetingCode);

                if (meeting == null)
                {
                    ModelState.AddModelError("MeetingCode", "Invalid Meeting Code.");
                    return View(model);
                }
            }
            else
            {
                // MODE: Fast Scan In/Out (Only QR Code)
                // Find existing registration globally to determine the meeting
                attendee = await _context.MeetingAttendees
                    .Include(a => a.Meeting)
                        .ThenInclude(m => m.Attendees) // For reconciliation
                    .Include(a => a.Sessions)
                    .FirstOrDefaultAsync(a => a.QRCode == model.QRCode);

                if (attendee == null)
                {
                    ModelState.AddModelError("QRCode", "This QR code is not registered. Please use 'Register for a Meeting' first.");
                    return View(model);
                }

                meeting = attendee.Meeting;
            }

            // 2. Expiry Check & Reconciliation
            if (DateTime.Now > meeting.ExpiryTime)
            {
                // RECONCILIATION: Close any lingering sessions for this meeting
                bool changesMade = false;
                var allAttendees = _context.MeetingAttendees
                    .Include(a => a.Sessions)
                    .Where(a => a.MeetingId == meeting.Id)
                    .ToList();

                foreach (var att in allAttendees)
                {
                    var openSessions = att.Sessions.Where(s => s.OutTime == null).ToList();
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

                // Specific Error Message for Fast Scan
                if (string.IsNullOrWhiteSpace(model.MeetingCode))
                {
                    ModelState.AddModelError("QRCode", "Meeting with QR code already expired");
                }
                else
                {
                    ModelState.AddModelError("MeetingCode", "This meeting has already elapsed. Registration is closed.");
                }
                return View(model);
            }

            // 3. Uniqueness Checks (only for NEW registration)
            if (attendee == null)
            {
                // Find attendee for this meeting if we haven't already
                attendee = await _context.MeetingAttendees
                    .Include(a => a.Sessions)
                    .FirstOrDefaultAsync(a => a.QRCode == model.QRCode);

                if (attendee != null)
                {
                    // Global QR check
                    if (attendee.MeetingId != meeting.Id)
                    {
                        ModelState.AddModelError("QRCode", $"Invalid QR code. This QR code has already been used in another meeting: '{attendee.Meeting?.Title}'.");
                        return View(model);
                    }

                    // Identity check
                    if (!string.IsNullOrWhiteSpace(model.Name) && (attendee.Email != model.Email || attendee.Name != model.Name))
                    {
                        ModelState.AddModelError("QRCode", "QR code already in use by another attendee in the same meeting.");
                        return View(model);
                    }
                }
                else
                {
                    // Check for duplicate Email/Phone in same meeting
                    if (!string.IsNullOrWhiteSpace(model.Name))
                    {
                        var duplicateContact = await _context.MeetingAttendees
                            .AnyAsync(a => a.MeetingId == meeting.Id && (a.Email == model.Email || a.Cellphone == model.Cellphone));

                        if (duplicateContact)
                        {
                            ModelState.AddModelError("", "Duplicate entry, an attendee with the same phone/email has already registered.");
                            return View(model);
                        }
                    }
                }
            }

            // 4. Process Scan
            if (attendee == null)
            {
                // New registration for this meeting
                if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Cellphone) || string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("", "Please provide Name, Cellphone, and Email to register.");
                    return View(model);
                }

                attendee = new MeetingAttendee
                {
                    MeetingId = meeting.Id,
                    Name = model.Name,
                    Cellphone = model.Cellphone,
                    Email = model.Email,
                    QRCode = model.QRCode
                };

                var session = new AttendanceSession { InTime = DateTime.Now };
                attendee.Sessions.Add(session);
                
                _context.MeetingAttendees.Add(attendee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully registered for '{meeting.Title}' and scanned in at {session.InTime:hh:mm tt}";
            }
            else
            {
                // Existing registration: Toggle In/Out
                var latestSession = attendee.Sessions.OrderByDescending(s => s.InTime).FirstOrDefault();

                if (latestSession != null && latestSession.OutTime == null)
                {
                    // Scan Out
                    latestSession.OutTime = DateTime.Now;
                    latestSession.Duration = latestSession.OutTime - latestSession.InTime;
                    _context.Update(latestSession);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Successfully scanned out from '{meeting.Title}' at {latestSession.OutTime:hh:mm tt}. Session time: {latestSession.Duration?.ToString(@"hh\:mm")}";
                }
                else
                {
                    // Scan In Again
                    var newSession = new AttendanceSession { AttendeeId = attendee.Id, InTime = DateTime.Now };
                    _context.AttendanceSessions.Add(newSession);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Successfully scanned in again to '{meeting.Title}' at {newSession.InTime:hh:mm tt}";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

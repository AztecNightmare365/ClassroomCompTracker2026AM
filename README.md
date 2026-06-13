# Classroom Computer Tracker

A desktop inventory and issue-tracking app for school IT staff. Tracks computers across classroom and server room locations, logs service calls, and manages a spare equipment pool — with a full audit trail.

Built in C# / Windows Forms with a SQLite backend. Catppuccin Mocha dark theme.

---

## Features

- **Device inventory** — Track computers by status: Active, Inactive, Broken, Retired
- **Room canvas** — Draggable visual layout of where each computer sits, color-coded by status
- **Service calls** — Log issues from Open → In Progress → Resolved with full history
- **Hot swap pool** — Flag spare computers and track peripheral inventory (monitors, keyboards, mice)
- **Activity log** — Append-only audit trail of every action, attributed by username
- **User accounts** — Login/register system; all operations require authentication

---

## Requirements

- Windows OS
- .NET Framework 4.7.2+ or .NET 6+
- `System.Data.SQLite` (NuGet package)

---

## Setup

1. Clone the repo
2. Open `ClassroomComputerTracker.sln` in Visual Studio
3. Restore NuGet packages
4. Build and run

Database (`Data/ClassroomComputers.db`) is created automatically on first launch. No manual setup needed.

---

## File Structure

```
ClassroomComputerTracker/
├── Program.cs              — startup + login loop
├── AppSession.cs           — static session state
├── LoginForm.cs            — authentication
├── MainForm.cs             — 5-tab main window
├── RoomTabPanel.cs         — room layout + computer grid (shared for both locations)
├── ComputerEditForm.cs     — add/edit computer dialog
├── ServiceCallForm.cs      — add/edit service call dialog
├── HotSwapItemForm.cs      — add/edit peripheral dialog
├── Models/
│   ├── Computer.cs
│   ├── ServiceCall.cs
│   ├── HotSwapItem.cs
│   ├── User.cs
│   └── ChangeLogEntry.cs
└── Data/
    └── DatabaseManager.cs  — singleton, all DB access
```

---

## Notes

- Passwords hashed with SHA-256 (no salt — suitable for internal single-machine use only)
- All SQL queries use parameterized statements (no injection risk)
- Drag-to-reposition on the room canvas does not log to the audit trail
- Database file is unencrypted on disk

---

## Author

Aaron Meyer — [aqmeyer123@gmail.com](mailto:aqmeyer123@gmail.com)

# Feature Specification: Hide the window title bar in fullscreen

**Feature Branch**: `002-fullscreen-hide-titlebar`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "При разворачивании окна в полный экран (на любой ОС) скрывать заголовок окна с разделителем — When the window enters fullscreen on any OS, hide the window title bar and its separator; show it again on exit"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Title bar vanishes in fullscreen and returns on exit (Priority: P1)

A producer working in Nota enters the OS fullscreen mode (for example the macOS green
window button). Today the slim custom title bar — the 36-px band with the centered
project name and the hairline separator under it — stays visible at the top and wastes
vertical space. With this feature, the moment the window enters fullscreen the title bar
band and its separator disappear, and the workspace content extends all the way to the
top edge of the screen for a distraction-free, full-bleed view. When the user leaves
fullscreen, the title bar and separator come back exactly as they were: centered project
name, draggable area, separator line.

**Why this priority**: This is the entire point of the request — reclaiming the title bar's
space in fullscreen. Everything else is reach or consistency.

**Independent Test**: Enter fullscreen on any supported OS and visually confirm the top
band and separator are gone and content reaches the top edge; leave fullscreen and
confirm the title bar is back. Deliverable on its own.

**Acceptance Scenarios**:

1. **Given** the main window in normal or maximized state, **When** the user enters the OS
   fullscreen mode, **Then** the custom title bar band and its bottom separator are hidden
   and the workspace fills the freed space up to the top edge of the screen.
2. **Given** the main window in fullscreen with the title bar hidden, **When** the user
   leaves fullscreen, **Then** the title bar and separator are visible again with the same
   appearance as before entering fullscreen (centered project name, drag behaviour).
3. **Given** the main window in fullscreen, **When** the project is renamed or switched,
   **Then** no title remnant appears at the top; the restored title bar (after exit) shows
   the current project name.

---

### User Story 2 - The behaviour works on every supported OS (Priority: P2)

The user asked for this on "any OS". On macOS fullscreen is reachable through the green
window control; on Linux the OS-drawn frame already hides itself in native fullscreen.
Windows has no built-in fullscreen gesture for a regular desktop window, and Nota today
ships no way to enter fullscreen there — so on Windows the feature would be unreachable
unless the app provides an entry point.

**Why this priority**: The core visual change (Story 1) already covers macOS and Linux;
this story makes the promise true on Windows and is what the user explicitly asked for
("на любой ОС").

**Independent Test**: On Windows, invoke the new fullscreen entry point and observe the
same hide/restore behaviour as on macOS; toggle back and forth repeatedly.

**Acceptance Scenarios**:

1. **Given** Nota running on Windows, **When** the user triggers the fullscreen entry
   point (View menu command or F11), **Then** the window covers the entire screen
   (including where the taskbar was) and the title bar and separator are hidden.
2. **Given** Nota fullscreened on Windows, **When** the user triggers the entry point
   again (or the window otherwise leaves fullscreen), **Then** the window returns to its
   previous state with the title bar visible.
3. **Given** Nota running on Linux in native fullscreen, **When** the user enters and
   leaves fullscreen, **Then** the app shows no regression (the OS frame handles its own
   title bar; no custom bar appears).

---

### User Story 3 - Secondary windows behave consistently (Priority: P3)

Secondary Nota windows (About, What's New, dialogs) share the same custom chrome family:
a slim title bar with a hairline separator on macOS/Windows. If a user fullscreens one
of them, its title bar should hide the same way, so every window in the app reads as one
family. The device pop-out window's top band is a functional toolbar (preset picker,
bypass, size toggles) rather than pure chrome, so it stays.

**Why this priority**: Rare flow — users seldom fullscreen secondary windows — but cheap
to cover since the windows share the same chrome, and inconsistency would look broken.

**Independent Test**: Fullscreen a secondary chrome window (e.g. About) on macOS and
confirm its title bar band and separator hide and restore like the main window's.

**Acceptance Scenarios**:

1. **Given** a secondary window with custom chrome in normal state, **When** the user
   enters fullscreen, **Then** its title bar band and separator hide.
2. **Given** a secondary window in fullscreen, **When** the user leaves fullscreen,
   **Then** its title bar and separator restore.

---

### Edge Cases

- **Maximized is not fullscreen**: the app starts maximized by default; in maximized
  state the title bar and separator stay visible. Confirmed with the user: only the OS
  fullscreen state hides the bar; maximized never does.
- **macOS traffic lights**: in macOS fullscreen the close/minimize/zoom controls
  auto-reveal on hover near the top edge; hiding the title bar must not block them or
  leave a dead zone that swallows their clicks.
- **Rapid toggling**: entering and leaving fullscreen quickly (or during a window
  animation) must not leave the title bar stuck hidden or stuck visible, nor duplicate
  separators.
- **Where does the project name go?** In fullscreen the centered project name disappears
  along with the bar — it is not relocated anywhere else; it returns on exit. (Assumption;
  see Assumptions.)
- **Window dragging**: the title bar is the drag area in windowed states; in fullscreen
  dragging is not applicable, and after leaving fullscreen dragging by the restored bar
  works again.
- **Launch state**: fullscreen is not persisted as a special case by this feature — the
  window simply renders per its current state at launch (title bar hidden only while
  actually fullscreened).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When the main window enters the OS fullscreen state, the system MUST hide
  the custom title bar band (including the centered project name) and its bottom
  separator, and extend the workspace content into the freed space to the top edge of
  the screen.
- **FR-002**: When the main window leaves the fullscreen state, the system MUST restore
  the title bar and separator to their pre-fullscreen appearance and behaviour
  (centered project name, drag area, separator).
- **FR-003**: The hide/restore behaviour MUST apply on macOS, Windows, and Linux. On
  Linux (where the OS draws the window frame) the requirement is no regression: the
  native frame's own handling stays intact.
- **FR-004**: The title bar MUST remain visible in normal and maximized window states;
  only the fullscreen state hides it.
- **FR-005**: In fullscreen, OS window controls (e.g., the macOS traffic lights) MUST
  remain operable and reachable; hiding the title bar MUST not block pointer input to
  them.
- **FR-006**: The window state transition handling MUST be robust to rapid repeated
  fullscreen/windowed toggles: the title bar visibility always matches the current
  window state after a transition completes.
- **FR-007**: On Windows, users MUST be able to enter and leave fullscreen from within
  the app via a View menu command and the F11 keyboard shortcut, so the feature is
  reachable on that OS. macOS and Linux keep their existing OS fullscreen mechanisms;
  no new entry point is added there.
- **FR-008**: Secondary Nota windows whose top band is pure title-bar chrome (About,
  What's New, save-changes dialog, …) MUST hide and restore that band and its separator
  on fullscreen transitions the same way as the main window. A secondary window whose
  top band is a functional toolbar (the device pop-out window's preset/bypass bar) keeps
  it in fullscreen — hiding it would remove live controls, not chrome.
- **FR-009**: The feature is purely a window-chrome/presentation change: it MUST NOT
  touch the audio engine, the C ABI, or playback behaviour in any way.

### Key Entities

Not applicable — this is a window-presentation behaviour with no new data entities. (The
only implicit state is the window's current display state — windowed / maximized /
fullscreen — which the OS already owns.)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On each supported OS, entering fullscreen shows no title bar band or
  separator and content reaches the top screen edge within the window's transition (no
  leftover strip, no flash of the old bar after the transition settles).
- **SC-002**: Toggling fullscreen 10 times in a row leaves the title bar correctly
  restored on every exit (visible, correctly styled, draggable) — no stuck states, no
  duplicated separators.
- **SC-003**: Behaviour in normal and maximized states is unchanged from before the
  feature (title bar visible, double-click title bar still toggles maximize, project
  name centered).
- **SC-004**: OS window controls remain usable in fullscreen on macOS (traffic lights
  reveal and respond to clicks); no dead click zones appear along the top edge.
- **SC-005**: All pre-existing window, playback, and smoke-test behaviour passes exactly
  as before the change.

## Assumptions

- "Полный экран" (fullscreen) means the OS fullscreen display state — the mode entered
  via the macOS green button / Enter Full Screen — and NOT the maximized window state.
  The app starts maximized, and the title bar is expected to stay in maximized state.
  Confirmed with the user.
- The centered project name is not relocated while fullscreened; it simply hides with
  the bar and returns on exit. If users later want the name visible in fullscreen, that
  is a separate follow-up.
- Fullscreen itself is entered through the OS mechanisms (macOS green button/menu) plus
  an in-app entry point on Windows (Q2); this feature does not change how fullscreen is
  entered on macOS/Linux, only what the app's own chrome does in response.
- No persistence of "was fullscreen" is added by this feature beyond whatever the
  OS/windowing layer already does.
- Scope is the desktop app's window chrome; audio engine, project data, and file formats
  are untouched.

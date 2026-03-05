# Project Memory: Rangerstr (Crestron CH5)

## Project Overview
**Rangers Training Room Control Interface**
A Crestron HTML5 (CH5) user interface for controlling AV equipment in the Rangers Training Room.
- **Target Platform:** Crestron Touch Panels (TSW-770/1070) & WebXPanel.
- **Core Technologies:** HTML5, CSS3, Vanilla JavaScript, Crestron CH5 Library (`cr-com-lib`).

## Architecture
- **Entry Point:** `index.html` - Main layout structure.
- **Logic:** `js/app.js` - Contains all business logic, join maps, signal subscriptions, and UI updates.
- **Styles:** `css/style.css` - Custom styling (Red/Black theme).
- **Configuration:** `project-config.json` - CH5 project settings.
- **Build Artifact:** `rangerstr.ch5z` - compiled archive for panel loading.

## Development Workflow

### 1. Build & Deploy
- **Build Archive:** `npm run build` (Outputs `rangerstr.ch5z` to parent dir).
- **Deploy:** Upload the `.ch5z` file to the Crestron processor via Web Interface or Toolbox, or load directly to the touch panel.

### 2. Local Development
- **No Hot-Reload:** This project uses a standard file structure. Changes require a refresh in the browser.
- **Debugging:** Open `index.html` in a browser. Use Chrome DevTools.
    - *Note:* `cr-com-lib` functions (subscribe/publish) will fail or mock in a standard browser unless connected to a processor via WebSocket or XPanel. The app logic detects this environment.

## Join Map Reference (Contract)
*Synced with `js/app.js`*

| Signal Name | Join Type | ID | Notes |
| :--- | :--- | :--- | :--- |
| **Sources** | Digital | 10-15 | Off(10), Wallplate(11), Cable1(12), Cable2(13), Appspace(14), Clickshare(15) |
| **Volume Level** | Analog | 1 | 0-65535 (Master) |
| **Volume Mute** | Digital | 1 | Toggle |
| **Cloud Music** | Digital | 101 | Toggle |
| **LED Brightness** | Analog | 2 | 0-65535 |
| **Camera Select** | Digital | 21, 22 | Podium(21), Room(22) |
| **Camera PTZ** | Digital | 23-28 | Tilt U/D(23/24), Pan L/R(25/26), Zoom I/O(27/28) |
| **Camera Presets** | Digital | 31-35 | Presets 1-5 |
| **Cable Box** | Digital | 41-67 | Nav, Numpad, Transport (Shared Logic) |
| **Admin Auto-On** | Digital | 110-116 | 5am-10am, Disable |
| **Admin Auto-Off** | Digital | 120-126 | 4pm-9pm, Disable |

## Technical Learnings & Conventions

### Touch Events
- **Ghost Clicks:** `touchstart` handlers MUST call `e.preventDefault()` to prevent ghost clicks and context menus on touch panels.
- **Press & Hold:** PTZ controls use `mousedown`/`touchstart` to start signal high, and `mouseup`/`touchend` to set signal low.

### UI/UX
- **Responsiveness:** Uses Flexbox for layout to adapt to slight resolution variances.
- **Feedback:** UI updates are driven by processor feedback (subscriptions) rather than local optimistic updates (except for simple nav).
- **Assets:** SVGs are preferred for icons over font icons for consistent rendering on panels.

### Best Practices
- **Cache Busting:** Increment the `?v=` query string in `index.html` CSS/JS links after deployment to ensure panels load the new version immediately.
- **Safe Subscriptions:** Always check if `window.CrComLib` exists before calling `subscribeState` to avoid runtime errors in non-CH5 environments.

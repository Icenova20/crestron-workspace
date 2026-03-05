# Project Memory: Lockton Dunning Benefits (Crestron CH5)

## Project Overview
**Break Room 09.002 AV Control**
A Crestron HTML5 (CH5) user interface for the Break Room Video Wall and Audio system.
- **Target Platform:** Crestron TSW-1070 & WebXPanel.
- **Theme:** Dark/Modern (Lockton Branding).
- **Core Technologies:** HTML5, CSS3 (Grid Layout), Vanilla JavaScript.

## Architecture
- **Entry Point:** `index.html` - Sidebar + Main Content Grid.
- **Logic:** `js/app.js` - Handles Source Switching, Volume Feedback, and Modals.
- **Styles:** `css/style.css` - Custom Dark Theme with Lockton Blue accents.

## Join Map Reference (Contract)
*Synced with `js/app.js`*

| Signal Name | Join Type | ID | Notes |
| :--- | :--- | :--- | :--- |
| **System** | | | |
| Power Off | Digital | 10 | Shuts down room, returns to Welcome Screen |
| Settings Modal | Digital | 11 | Opens Admin/Advanced Modal |
| **Source Selection** | Digital | | Mutually Exclusive |
| AirMedia | Digital | 21 | Wireless Presentation |
| Media Player | Digital | 22 | Video Wall Input |
| **Audio** | | | |
| Master Volume | Analog | 1 | 0-65535 (Break Room) |
| Master Mute | Digital | 1 | Toggle |
| Restroom Volume | Analog | 2 | 0-65535 (Advanced Tab) |
| Restroom Mute | Digital | 2 | Toggle (Advanced Tab) |
| **Microphones** | | | Voice Lift (Advanced Tab) |
| Handheld Level | Analog | 11 | 0-65535 |
| Handheld Mute | Digital | 11 | Toggle |
| Bodypack Level | Analog | 12 | 0-65535 |
| Bodypack Mute | Digital | 12 | Toggle |
| **Sonos** | Digital | | Transport Controls (Advanced Tab) |
| Previous | Digital | 41 | |
| Play/Pause | Digital | 42 | |
| Next | Digital | 43 | |
| **Scheduling** | Digital | | Auto Power Logic |
| Auto On (7am-9am) | Digital | 101-103 | 104=Disable |
| Auto Off (5pm-7pm)| Digital | 111-113 | 114=Disable |

## Development Notes
- **Volume Handling:** The Master Volume slider is visible on the main page. Restroom and Mic levels are hidden in the Settings Modal to prevent accidental adjustment.
- **Video Wall:** The UI treats the Video Wall as a single destination. Routing logic (NVX) is handled by the control processor based on the selected Source (21 or 22).
- **Colors:**
  - Sapphire: `#003478`
  - Cerulean: `#009EE3`

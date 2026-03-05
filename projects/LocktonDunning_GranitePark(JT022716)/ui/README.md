# Lockton Dunning Benefits - Granite Park AV UI

This repository contains the Crestron HTML5 (CH5) user interface for the Break Room Video Wall and Audio system.

## 📂 Directory Structure
- `index.html`: Main entry point for the UI.
- `js/`: Application logic (`app.js`).
- `css/`: Custom styling (`style.css`).
- `img/`: Assets and branding.
- `appui/`: CH5 build artifacts.
- `project-config.json`: Crestron CH5 project configuration.

## 🛠 Development
The UI is built using vanilla HTML, CSS, and JavaScript.

### Prerequisites
- [Node.js](https://nodejs.org/) (Recommended for `ch5-cli`)
- [Crestron CH5 Utilities CLI](https://www.npmjs.com/package/@crestron/ch5-utilities-cli)

### Building for Deployment
To generate the `.ch5z` file for loading onto a touchpanel:
```bash
npm install
npm run build
```
The compiled archive will be generated based on the settings in `project-config.json`.

## ⚠️ Important Notes
- **Video Wall:** The UI treats the Video Wall as a single destination. Routing logic is handled by the control processor.
- **Volume:** Master Volume is primary; Restroom/Mic levels are in the Settings modal.

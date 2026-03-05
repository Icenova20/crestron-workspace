# CH5 Development Workspace (Dev-Nginx)

This workspace is a centralized environment for developing and previewing Crestron HTML5 (CH5) user interfaces.

## 🚀 Quick Start
- **Preview URL:** `http://localhost:8001`
- **Nginx Config:** `nginx.conf`
- **Docker Command:** `docker compose up -d`

## 📂 Directory Structure
- `/html/`: Put your project folders here. They are instantly served.
- `/html/_template/`: The "Gold Master" template for new projects.
- `/builds/`: **All compiled `.ch5z` files end up here.** (Prevents recursion errors).
- `new-project.sh`: Script to scaffold a new project from the template.
- `build-all.sh`: Script to compile every project in `/html/` into `/builds/`.

## 🛠 Project Management

### 1. Creating a New Project
Run the helper script with the official project name:
```bash
./new-project.sh "My New Project Name"
```
This creates `html/My New Project Name/` with all necessary CH5 libraries and a safe configuration.

### 2. Building for Deployment
To generate the `.ch5z` files for loading onto a touchpanel:
```bash
./build-all.sh
```
**Note:** Do not run `npm run build` inside a project folder manually unless you specify the output directory as `../../builds/`. The `build-all.sh` script handles this safely by generating a URL-safe **SLUG** for the filename.

## ⚠️ Important Rules
1. **Never Build Into Source:** The `ch5-cli` tool will recursively archive its own output if you build into the same folder, leading to 3GB+ files. Always output to `../../builds/`.
2. **Project Config:** Every project must have a `project-config.json` with `"type": "ch5"`. If it's missing, the CLI might hang or fail.
3. **Clean Up:** If a build fails, check for a `temp/` folder inside your project directory and delete it before retrying.

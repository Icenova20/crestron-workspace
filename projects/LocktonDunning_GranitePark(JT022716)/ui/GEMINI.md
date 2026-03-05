# Gemini Project Context: Dev-Nginx (CH5 Preview)

## Project Overview
This Nginx instance serves as the development preview for Crestron CH5 projects. 

## Architecture
- **Location:** `d:\Antigravity\ch5-workspace\`
- **Port:** `8001`
- **Config:** Managed via local `nginx.conf`.
- **Auto-Discovery:** The landing page (`index.html`) dynamically fetches a project list from `/api/projects/` (Nginx `autoindex_format json`).

## Project Management
- **Adding Projects:**
    1.  Place project folders directly into the `html/` directory.
    2.  Alternatively, use `./new-project.ps1 -ProjectName "Project Name"` to scaffold from the template.
- **Restart:** Automatic (files in `html/` are served instantly). 

## Caching & Performance
- **Cloudflare Integration:** On 2026-02-20, caching was enabled in Cloudflare.
- **Nginx Configuration:**
    - **Gzip:** Enabled for text, CSS, JS, and JSON to reduce payload size.
    - **Current State:** The local `nginx.conf` currently lacks the explicit `Cache-Control` headers for static assets and HTML/API mentioned in previous documentation. (Pending Update)
- **Header Control:** Previous documentation mentioned optimized `Cache-Control` headers; however, the active `nginx.conf` uses default behavior.

## Deployment Note
This stack was moved to the top-level `Documents` folder on 2026-01-23. On 2026-02-06, it was upgraded from a static success screen to a dynamic, Material-styled project selector.
Refactored for Windows 11 on 2026-03-02 (Removed WSL/Docker dependencies).

## Windows Development
- Use `PowerShell` for all scripts.
- Recommended tools: Node.js (for `ch5-cli`), Nginx for Windows or VS Code Live Server extension.

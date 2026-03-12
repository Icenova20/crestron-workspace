import fitz
import os

pdf_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons(JT022715)\docs\Lockton Dunning  - Victory Commons AV RFP (JT022715, JT023560, 783, 822, 861, 995) LINE DWG.pdf"
page_idx = 23 # Page 24

doc = fitz.open(pdf_path)
page = doc.load_page(page_idx)

# Search for GSM4248PX (known switch part)
rects = page.search_for("GSM4248PX")
if rects:
    # Use the first match to define a crop area
    rect = rects[0]
    # Expand to see the whole switch and labels to the left/right
    crop_rect = fitz.Rect(rect.x0 - 800, rect.y0 - 100, rect.x1 + 800, rect.y1 + 1200)
    crop_rect = crop_rect & page.rect
    
    zoom = 600 / 72
    mat = fitz.Matrix(zoom, zoom)
    pix = page.get_pixmap(matrix=mat, clip=crop_rect)
    pix.save("page24_switch_investigation.png")
    print("Saved page24_switch_investigation.png")
else:
    print("GSM4248PX not found on Page 24 via geometry search.")
doc.close()

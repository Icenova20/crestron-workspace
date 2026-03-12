import fitz  # PyMuPDF
import os

pdf_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons(JT022715)\docs\DivisibleTrainingRoom14.089_LineDrawing.pdf"
output_png = "line_drawing_high_res.png"

print(f"Opening {pdf_path}...")
try:
    doc = fitz.open(pdf_path)
    page = doc.load_page(0)  # load the first page
    
    # Render at 300 DPI (zoom factor 300/72 = 4.166...)
    zoom = 4.0
    mat = fitz.Matrix(zoom, zoom)
    pix = page.get_pixmap(matrix=mat)
    
    print(f"Saving to {output_png} (Size: {pix.width}x{pix.height})...")
    pix.save(output_png)
    print("Done.")
    doc.close()
except Exception as e:
    print(f"Error: {e}")

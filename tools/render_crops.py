import fitz  # PyMuPDF
import os

pdf_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons(JT022715)\docs\DivisibleTrainingRoom14.089_LineDrawing.pdf"

print(f"Opening {pdf_path}...")
try:
    doc = fitz.open(pdf_path)
    page = doc.load_page(0)
    
    # Search for switch identifier
    # The user mentioned M4250-40G8XF-PoE+
    search_term = "M4250"
    rects = page.search_for(search_term)
    
    if rects:
        for i, rect in enumerate(rects):
            print(f"Found {search_term} at {rect}")
            
            # Create a large crop area (e.g. 1000 pixels around it)
            # In PDF units (points), 1 inch = 72 points.
            # Let's crop a 10x10 inch area around the match.
            crop_rect = fitz.Rect(rect.x0 - 500, rect.y0 - 200, rect.x1 + 1000, rect.bottom + 1500)
            
            # Clip to page boundaries
            crop_rect = crop_rect & page.rect
            
            # Render at 1000 DPI
            zoom = 1000 / 72
            mat = fitz.Matrix(zoom, zoom)
            pix = page.get_pixmap(matrix=mat, clip=crop_rect)
            
            output_crop = f"m4250_crop_{i}.png"
            print(f"Saving crop to {output_crop} ({pix.width}x{pix.height})...")
            pix.save(output_crop)
    else:
        print(f"'{search_term}' not found in PDF geometry.")
        # If search_for fails (because it's an image-only PDF), we'll do manual quadrants
        # But wait, earlier extract_words found nothing, so search_for might fail too.
        # If so, I'll just render the whole page at 600 DPI in 4 quadrants.
        print("Falling back to quadrant rendering...")
        zoom = 8.0 # ~576 DPI
        mat = fitz.Matrix(zoom, zoom)
        
        step_x = page.rect.width / 2
        step_y = page.rect.height / 2
        
        for row in range(2):
            for col in range(2):
                q_rect = fitz.Rect(col*step_x, row*step_y, (col+1)*step_x, (row+1)*step_y)
                pix = page.get_pixmap(matrix=mat, clip=q_rect)
                q_name = f"quadrant_{row}_{col}.png"
                print(f"Saving {q_name}...")
                pix.save(q_name)
    
    doc.close()
except Exception as e:
    print(f"Error: {e}")

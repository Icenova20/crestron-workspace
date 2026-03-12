import pdfplumber
import pandas as pd
import os

pdf_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons(JT022715)\docs\DivisibleTrainingRoom14.089_LineDrawing.pdf"

print(f"Opening {pdf_path}...")
try:
    with pdfplumber.open(pdf_path) as pdf:
        page = pdf.pages[0]
        
        # Try full text extraction first
        text = page.extract_text()
        if text:
            print(f"Sample Text Found ({len(text)} chars):")
            print(text[:500])
        else:
            print("No text found via extract_text().")

        words = page.extract_words()
        print(f"Words extracted: {len(words)}")
        
        if not words:
            print("The PDF appears to have no text elements (maybe purely vector/scanned).")
            # If no words, let's look at objects
            print(f"Objects found: {page.objects.keys()}")
            exit()

        df = pd.DataFrame(words)
        print(f"DataFrame Columns: {df.columns.tolist()}")
        
        if 'text' in df.columns:
            switch_matches = df[df['text'].str.contains("M4250", case=False, na=False)]
            if not switch_matches.empty:
                print("\nFound M4250 Switch at:")
                print(switch_matches[['text', 'x0', 'top', 'x1', 'bottom']])
        
except Exception as e:
    import traceback
    traceback.print_exc()

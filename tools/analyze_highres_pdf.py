import pdfplumber
import pandas as pd
import os

pdf_path = r"D:\Antigravity\Crestron-Workspace\projects\LocktonDunning_VictoryCommons(JT022715)\docs\Lockton Dunning  - Victory Commons AV RFP (JT022715, JT023560, 783, 822, 861, 995) LINE DWG.pdf"
pages_to_check = [23, 24, 29] # 1-indexed for user, 0-indexed for pdfplumber = [22, 23, 28]

print(f"Opening {pdf_path}...")
try:
    with pdfplumber.open(pdf_path) as pdf:
        all_words = []
        for p_num in pages_to_check:
            print(f"Checking Page {p_num}...")
            page = pdf.pages[p_num - 1]
            words = page.extract_words()
            print(f"  Found {len(words)} words.")
            for w in words:
                w['page'] = p_num
                all_words.append(w)
        
        if not all_words:
            print("No text found on these pages.")
            exit()

        df = pd.DataFrame(all_words)
        output_csv = "high_res_text_map.csv"
        df.to_csv(output_csv, index=False)
        print(f"Extracted {len(df)} words to {output_csv}")
        
        # Look for M4250 or Switch
        matches = df[df['text'].str.contains("M4250|Switch", case=False, na=False)]
        if not matches.empty:
            print("\nFound Potential Switch Matches:")
            print(matches[['page', 'text', 'x0', 'top', 'x1', 'bottom']])
        else:
            print("\nNo direct 'M4250' or 'Switch' text found.")
            
except Exception as e:
    import traceback
    traceback.print_exc()

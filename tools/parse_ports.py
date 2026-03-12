import pandas as pd

df = pd.read_csv('high_res_text_map.csv')
page24 = df[df['page'] == 24].copy()

# Sort by top
page24 = page24.sort_values(['top', 'x0'])

results = []

# Identify numbers that represent Port IDs
# Looking at the pattern, Port ID is usually a number near 'LAN' or 'RJ45'
# Coordinates from previous output show Port ID at x0 ~ 1626
ports = page24[(page24['text'].str.get(0).str.isdigit()) & (page24['x0'] > 1620) & (page24['x0'] < 1635)]

for idx, port_row in ports.iterrows():
    port_num = port_row['text']
    top_val = port_row['top']
    
    # Look for device names on the same horizontal line (plus/minus 2 units)
    # Search to the left (x0 < 1600) and right (x0 > 1650)
    horizontal_match = page24[(page24['top'] >= top_val - 2) & (page24['top'] <= top_val + 2)]
    
    # Filter out common labels
    labels = horizontal_match[~horizontal_match['text'].str.contains('LAN|POE|RJ45|PORT|VID|CTRL|DANTE|SFP|INPUT|OUTPUT', case=False, na=False)]
    
    device_name = " ".join(labels['text'].tolist())
    
    results.append({
        "Port": port_num,
        "Device_Raw": device_name,
        "Top": top_val
    })

# Deduplicate and clean
clean_df = pd.DataFrame(results).sort_values(by="Port")
clean_df.to_csv("extracted_ports_page24.csv", index=False)
print(clean_df.to_string())

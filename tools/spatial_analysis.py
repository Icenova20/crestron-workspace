import pandas as pd
import numpy as np

# Load the extracted text data
df = pd.read_csv('high_res_text_map.csv')

# Focus on Page 24 as it has dense switch data
page24 = df[df['page'] == 24].copy()

# Sort by top and x0 for spatial analysis
page24 = page24.sort_values(['top', 'x0'])

print("--- Data Sample for Page 24 ---")
# Print a block of data where we suspect the switch is
# NETGEAR was at top=482
switch_area = page24[(page24['top'] > 400) & (page24['top'] < 1000)]
print(switch_area[['text', 'x0', 'top']].to_string())

# Strategy: Find "PORT" or numbers that look like port IDs
# Then look for the device string to the left or right on the same/nearby 'top' line.

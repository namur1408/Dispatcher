import sys

with open(r'e:\Dispatcher\Assets\Scripts\TV\TVDisplayInfo.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

brace_level = 0
for i, line in enumerate(lines):
    # This ignores braces in strings or comments, but TVDisplayInfo.cs doesn't have {} in strings/comments that would throw this off, wait! It might!
    
    # Let's do a simple count
    brace_level += line.count('{')
    brace_level -= line.count('}')
    
    if brace_level < 0:
        print(f"Error: Negative brace level at line {i+1}")
    
    if 'void UpdateResourcesText' in line:
        print(f'UpdateResourcesText: line {i+1}, level {brace_level}')
    if 'void StyleButtons' in line:
        print(f'StyleButtons: line {i+1}, level {brace_level}')
    if 'void UpdateBaseStatsUI' in line:
        print(f'UpdateBaseStatsUI: line {i+1}, level {brace_level}')
    if 'void DisplayFlights' in line:
        print(f'DisplayFlights: line {i+1}, level {brace_level}')
    if 'AddFrame' in line:
        print(f'AddFrame: line {i+1}, level {brace_level}')
    if 'CreateBorderRect' in line:
        print(f'CreateBorderRect: line {i+1}, level {brace_level}')
    if 'UpdateSelectionVisuals' in line:
        print(f'UpdateSelectionVisuals: line {i+1}, level {brace_level}')

print(f"Final brace level: {brace_level}")

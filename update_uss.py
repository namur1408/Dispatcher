import re

file_path = r'e:\Dispatcher\Assets\Resources\UI\EndOfDay.uss'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

def repl_font(m):
    val = int(m.group(1))
    new_val = int(val * 1.5)
    return f'font-size: {new_val}px;'

content = re.sub(r'font-size:\s*(\d+)px;', repl_font, content)

content = content.replace('width: 66px;', 'width: 100px;') # .stat-val
content = content.replace('width: 70px;', 'width: 105px;') # .stat-bar-bg

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated USS file successfully.')

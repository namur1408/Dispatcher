import re

file_path = r'd:\govno\game\Dispatcher\Assets\Scripts\Radar\UIAirplane.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add AirplaneVisuals field and initialization
content = content.replace('    public AirplaneAudio audioSystem;\n',
                          '    public AirplaneAudio audioSystem;\n    public AirplaneVisuals visuals;\n')

content = content.replace('        audioSystem = new AirplaneAudio(this, source);\n',
                          '        audioSystem = new AirplaneAudio(this, source);\n        visuals = new AirplaneVisuals(this, canvasGroup, callsignText, hitboxVisual, routeSegmentPrefab, waypointMarkerPrefab, transform.parent);\n')

# 2. Replace invocations
content = re.sub(r'\bSetVisualState\(', r'visuals?.SetVisualState(', content)
content = re.sub(r'\bRebuildRouteLayer\(\)', r'visuals?.RebuildRouteLayer(isLandingPhase)', content)
content = re.sub(r'\bSyncRouteAlpha\(\)', r'visuals?.SyncRouteAlpha()', content)
content = re.sub(r'\bUpdateHitboxColor\(\)', r'visuals?.UpdateHitboxColor()', content)
content = re.sub(r'\bUpdateFirstSegment\(\)', r'visuals?.UpdateFirstSegment()', content)

# 3. Replace Cleanup logic in ResetPlane
reset_pattern = r'        foreach \(var marker in activeMarkers\).*?lineSegments\.Clear\(\);'
reset_replacement = r'        visuals?.CleanupRouteVisuals();'
content = re.sub(reset_pattern, reset_replacement, content, flags=re.DOTALL)

# 4. Remove method definitions (using simple regex matching the block with balanced braces)
def remove_method(name, code):
    # This matches `private void Name(...) { ... }` up to the first closing brace at the same indentation level
    # Actually, a simpler way is to find the method signature and then count braces
    match = re.search(r'(private|public)\s+void\s+' + name + r'\s*\(', code)
    if not match: return code
    start_idx = match.start()
    brace_count = 0
    in_method = False
    for i in range(start_idx, len(code)):
        if code[i] == '{':
            brace_count += 1
            in_method = True
        elif code[i] == '}':
            brace_count -= 1
            if in_method and brace_count == 0:
                return code[:start_idx] + code[i+1:]
    return code

methods_to_remove = [
    'SetVisualState',
    'RebuildRouteLayer',
    'SetMarker',
    'SetSegment',
    'UpdateFirstSegment',
    'UpdateSegmentLook',
    'UpdateHitboxColor',
    'SyncRouteAlpha'
]

for m in methods_to_remove:
    content = remove_method(m, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Refactored UIAirplane.cs Visuals")

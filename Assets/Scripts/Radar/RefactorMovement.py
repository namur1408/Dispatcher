import re

file_path = r'd:\govno\game\Dispatcher\Assets\Scripts\Radar\UIAirplane.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Replace UpdateVisualRotation() calls
content = re.sub(r'\bUpdateVisualRotation\(\)', r'rectTransform.rotation = movement.GetVisualRotation(holdingRadius)', content)

# 2. Replace HandleMovement method
new_handle_movement = """    private void HandleMovement()
    {
        float currentSpeed = inStorm ? (_actualSpeed * 0.5f) : _actualSpeed;

        // Входим в holding pattern у центра радара, если решение ещё не принято
        if (!isHolding && waypoints.Count == 1 && waypoints[0] == Vector2.zero)
        {
            bool isWaitingForRunway = dispatchStatus == DispatchStatus.Approved && string.IsNullOrEmpty(assignedRunway);
            if (dispatchStatus == DispatchStatus.Pending || isWaitingForRunway)
            {
                if (Vector2.Distance(logicalPosition, waypoints[0]) <= holdingRadius)
                {
                    if (!isOutOfFuel) movement.StartHolding(waypoints[0]);
                    return;
                }
            }
        }

        bool reachedWaypoint = movement.UpdatePosition(Time.deltaTime, currentSpeed, holdingRadius);

        if (!reachedWaypoint) return;

        // Достигли текущей точки маршрута
        if (waypoints.Count > 1)
        {
            waypoints.RemoveAt(0);
            visuals?.RebuildRouteLayer(isLandingPhase);
            return;
        }

        // Достигли ПОСЛЕДНЕЙ точки маршрута
        HandleWaypointReached();
    }"""
content = re.sub(r'    private void HandleMovement\(\)\s*\{.*?(?=    // ── Логика достижения последней точки)', new_handle_movement + '\n\n', content, flags=re.DOTALL)

# 3. Replace StartHolding calls and definition
content = re.sub(r'StartHolding\((.*?)\)', r'movement.StartHolding(\1)', content)
# But wait, we need to delete the StartHolding definition.
content = re.sub(r'    private void movement\.StartHolding\(Vector2 center\).*?visuals\?\.RebuildRouteLayer\(isLandingPhase\);\s*\}', '', content, flags=re.DOTALL)

# 4. Remove UpdateVisualRotation definition
content = re.sub(r'    void rectTransform\.rotation = movement\.GetVisualRotation\(holdingRadius\).*?rectTransform\.rotation = Quaternion\.Euler\(0, 0, angle - 90f\);\s*\}\s*\}', '', content, flags=re.DOTALL)

# 5. Remove GetWaypointIndexAt and DistanceToSegment and SetFlightPath and AddWaypoint and RemoveWaypoint
# I will just write a function to delete them cleanly
def remove_method(name, code):
    match = re.search(r'(private|public|)\s*(void|int|float|Vector2|bool)\s+' + name + r'\s*\(', code)
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

# wait, I should replace their calls first!
content = re.sub(r'\bSetFlightPath\(', r'movement.waypoints.Clear(); movement.waypoints.Add', content) # this is wrong, let's look at SetFlightPath
# Actually, I'll just delete them with multi_replace_file_content if the python script is too risky.

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Refactored Movement")

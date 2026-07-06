using System.Collections.Generic;

public static class DayLogicProvider
{
    private static readonly Dictionary<int, IDayLogic> _days = new Dictionary<int, IDayLogic>
    {
        { 1, new Day1Logic() },
        { 2, new Day2Logic() }
    };

    public static IDayLogic GetDayLogic(int dayNumber)
    {
        if (_days.TryGetValue(dayNumber, out IDayLogic logic))
        {
            return logic;
        }
        
        // Fallback to day 1 if not found
        return _days[1];
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public interface IDayLogic
{
    void EnqueueFlights(Queue<FlightData> flights, Queue<float> delays, Func<Vector2, Vector2, float> calculateFuel);
    void SendMorningDirectives();
    int EvaluateShift();
    int GetBaseXP();
    EndOfDayResult GetEndOfDayResult();
}

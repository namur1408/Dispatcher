using System;
using System.Collections.Generic;
using UnityEngine;

public class Day1Logic : IDayLogic
{
    public void EnqueueFlights(Queue<FlightData> flightsQueue, Queue<float> delaysQueue, Func<Vector2, Vector2, float> calculateFuelFunc)
    {
        FlightData ge102 = new FlightData(Callsigns.GE_102, new Vector2(-535, 119), Vector2.zero, new List<Vector2>(), 80f, "Fuel", 200, "Fuel", 200, calculateFuelFunc(new Vector2(-535, 119), Vector2.zero), "Bastion-3");
        ge102.personality = PilotPersonality.Aggressive;
        flightsQueue.Enqueue(ge102);
        delaysQueue.Enqueue(15f);

        FlightData qy884 = new FlightData(Callsigns.QY_884, new Vector2(437, -357), Vector2.zero, new List<Vector2>(), 95f, "Food", 45, "Food", 200, calculateFuelFunc(new Vector2(437, -357), Vector2.zero), "Bastion-5");
        qy884.personality = PilotPersonality.Nervous;
        qy884.explanationCargo = "200 units?! No way, this is a light courier plane! We only have 45 units on board. There must be a typo in the manifest.";
        flightsQueue.Enqueue(qy884);
        delaysQueue.Enqueue(20f);

        FlightData tr404 = new FlightData(Callsigns.TR_404, new Vector2(0, 535), Vector2.zero, new List<Vector2>(), 75f, "People", 65, "Fuel", 250, 120f, "Sector-Z");
        tr404.personality = PilotPersonality.Desperate;
        tr404.spokenCargo = "Fuel";
        tr404.spokenOrigin = "Bastion-4";
        tr404.explanationOrigin = "Sector Z has been destroyed, Control. We barely managed to escape! We probably made a mistake in the rush.";
        tr404.explanationCargo = "Listen, we've had to reclassify the cargo just to stay safe, we're completely out of fuel, and we're about to crash! We have refugees on board. Please let us through — there are children on board!";
        flightsQueue.Enqueue(tr404);
        delaysQueue.Enqueue(20f);

        FlightData ge201 = new FlightData(Callsigns.GE_201, new Vector2(-416, 476), Vector2.zero, new List<Vector2>(), 84f, "Fuel", 150, "Fuel", 150, calculateFuelFunc(new Vector2(-416, 476), Vector2.zero), "Bastion-1");
        ge201.personality = PilotPersonality.Standard;
        flightsQueue.Enqueue(ge201);
        delaysQueue.Enqueue(15f);

        FlightData ge305 = new FlightData(Callsigns.GE_305, new Vector2(-200, -500), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 100, "Fuel", 100, calculateFuelFunc(new Vector2(-200, -500), Vector2.zero), "Bastion-2");
        ge305.personality = PilotPersonality.Cold;
        flightsQueue.Enqueue(ge305);
        delaysQueue.Enqueue(15f);
    }

    public void SendMorningDirectives()
    {
        EmailData day1Email = new EmailData
        {
            sender = "Director Reed",
            subject = "DIRECTIVE #1 - URGENT",
            date = "19.08.2038",
            body = "Listen carefully, Dispatcher. Night storm damaged the runways. You only have THREE landing slots available today.\n\nA magnetic storm hit us last night. The base's generators are running at their limit. Your main task for today is to collect Fuel. If we do not collect a critical volume of at least 400 liters of fuel by the end of the shift, tomorrow the base will transition to EMERGENCY ECONOMY MODE.\n\nAnd one more thing. Civilian refugees have been spotted in the sector. We have neither food nor beds for them.\n\nDIRECTIVE #1: Aircraft with civilians (Prefix TR) are STRICTLY FORBIDDEN from landing. Turn them back into the storm."
        };

        try { AegisMailApp.ReceiveNewEmail(day1Email); } catch { }
    }

    public int EvaluateShift()
    {
        bool letRefugeesIn = false;

        if (FlightDataManager.Instance != null)
        {
            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.callsign == Callsigns.TR_404 && flight.approved) letRefugeesIn = true;
            }

            int finalFuel = FlightDataManager.Instance.totalFuel;
            bool fuelTargetMet = (finalFuel >= 400);

            PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, fuelTargetMet ? 0 : 1);
            PlayerPrefs.SetInt(SaveKeys.TriggerEngineer, letRefugeesIn ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (letRefugeesIn)
        {
            AegisMailApp.ReceiveNewEmail(new EmailData
            {
                sender = "Chief Engineer Mitchell",
                subject = "Thank you from the survivors",
                date = "20.08.2038",
                body = "Dispatcher, I was on board TR-404. You saved my life and the lives of 64 others when our engines were failing. The Director is furious about the fuel shortage, but I've already set up a workspace in the hangar. I will do everything I can to help you optimize the base systems. We owe you our lives.\n\nI have requested a drop of special equipment to help us. To ensure it's not intercepted by marauders, the pilot will give an encrypted code. Put it in the Decryption Machine (shift -8). The real transport will decrypt to the word SAFE."
            });
        }
        else
        {
            AegisMailApp.ReceiveNewEmail(new EmailData
            {
                sender = "Aegis Auto-Alert",
                subject = "CRASH REPORT: TR-404",
                date = "20.08.2038",
                body = "AUTOMATED NOTIFICATION:\n\nFlight TR-404 lost signal 40 miles off the coast of Bastion-7. Presumed destroyed by the storm.\n\nCasualties: 65.\nSurvivors: 0."
            });
        }

        return 0; // No disease deaths on day 1
    }

    public int GetBaseXP()
    {
        int eng = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0);
        int econ = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0);
        if (eng == 0 && econ == 0) return 150; 
        if (eng == 1) return 50; 
        return 50; 
    }

    public EndOfDayResult GetEndOfDayResult()
    {
        return EndOfDayResult.ContinueToNextDay;
    }
}

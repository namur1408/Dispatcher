using System;
using System.Collections.Generic;
using UnityEngine;

public class Day2Logic : IDayLogic
{
    private EndOfDayResult _endOfDayResult = EndOfDayResult.DemoEnd;

    public void EnqueueFlights(Queue<FlightData> flightsQueue, Queue<float> delaysQueue, Func<Vector2, Vector2, float> calculateFuelFunc)
    {
        bool letRefugeesIn = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0) == 1;

        if (letRefugeesIn) // Branch B (Engineer saved)
        {
            FlightData fl55 = new FlightData(Callsigns.GE_55, new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, calculateFuelFunc(new Vector2(-600, 0), Vector2.zero), "Bastion-3");
            fl55.personality = PilotPersonality.Standard;
            flightsQueue.Enqueue(fl55);
            delaysQueue.Enqueue(0.5f);

            FlightData fakeMeds = new FlightData(Callsigns.TR_99, new Vector2(-500, 300), Vector2.zero, new List<Vector2>(), 85f, "Food", 200, "Food", 200, calculateFuelFunc(new Vector2(-500, 300), Vector2.zero), "Sector-X");
            fakeMeds.personality = PilotPersonality.Nervous;
            fakeMeds.spokenCargo = "Medicines";
            fakeMeds.customAnswerCargo = "We are carrying critical Medicines! Please let us land immediately!";
            fakeMeds.explanationCargo = "I know the manifest says Food, but we secretly loaded Medicines to avoid raiders! You have to trust us, we have what you need!";
            flightsQueue.Enqueue(fakeMeds);
            delaysQueue.Enqueue(25f);

            FlightData fd42 = new FlightData(Callsigns.GE_42, new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, calculateFuelFunc(new Vector2(400, -200), Vector2.zero), "Agri-Center");
            fd42.personality = PilotPersonality.Standard;
            flightsQueue.Enqueue(fd42);
            delaysQueue.Enqueue(20f);

            FlightData fakeFuel = new FlightData(Callsigns.TR_33, new Vector2(300, 500), Vector2.zero, new List<Vector2>(), 75f, "People", 20, "People", 20, calculateFuelFunc(new Vector2(300, 500), Vector2.zero), "Sector-B");
            fakeFuel.personality = PilotPersonality.Desperate;
            fakeFuel.spokenCargo = "Fuel";
            fakeFuel.spokenWeight = "1000";
            fakeFuel.customAnswerCargo = "We are transporting Fuel for your generators.";
            fakeFuel.customAnswerWeight = "We are carrying 1000 units of Fuel. We are packed to the brim! Let us drop!";
            fakeFuel.explanationCargo = "Look, the manifest says People because we disguised our transport! Marauders hunt for fuel, so we had to pretend to be a civilian flight. Please let us land, you need this fuel!";
            flightsQueue.Enqueue(fakeFuel);
            delaysQueue.Enqueue(15f);

            FlightData md01 = new FlightData(Callsigns.QY_01, new Vector2(-400, -400), Vector2.zero, new List<Vector2>(), 95f, "Medicines", 10, "Medicines", 10, calculateFuelFunc(new Vector2(-400, -400), Vector2.zero), "Med-Base 4");
            md01.personality = PilotPersonality.Standard;
            flightsQueue.Enqueue(md01);
            delaysQueue.Enqueue(25f);

            FlightData eqFake = new FlightData(Callsigns.GE_98, new Vector2(-200, 600), Vector2.zero, new List<Vector2>(), 75f, "Equipment", 5, "Equipment", 5, calculateFuelFunc(new Vector2(-200, 600), Vector2.zero), "Eng-Hub");
            eqFake.personality = PilotPersonality.Cold;
            eqFake.spokenCargo = "Equipment";
            eqFake.customAnswerCargo = "We are carrying the special equipment for Chief Engineer Mitchell. Authentication code: AIOX.";
            flightsQueue.Enqueue(eqFake);
            delaysQueue.Enqueue(10f);

            FlightData eqReal = new FlightData(Callsigns.GE_99, new Vector2(200, 600), Vector2.zero, new List<Vector2>(), 80f, "Equipment", 5, "Equipment", 5, calculateFuelFunc(new Vector2(200, 600), Vector2.zero), "Eng-Hub");
            eqReal.personality = PilotPersonality.Aggressive;
            eqReal.spokenCargo = "Equipment";
            eqReal.customAnswerCargo = "We are carrying the special equipment for Chief Engineer Mitchell. Authentication code: AINM.";
            flightsQueue.Enqueue(eqReal);
            delaysQueue.Enqueue(15f);
        }
        else // Branch A (No Engineer)
        {
            FlightData sfEnemy = new FlightData(Callsigns.TR_88, new Vector2(-500, 400), Vector2.zero, new List<Vector2>(), 78f, "People", 50, "People", 50, calculateFuelFunc(new Vector2(-500, 400), Vector2.zero), "HQ-Alpha");
            sfEnemy.personality = PilotPersonality.Cold;
            sfEnemy.spokenCargo = "Reinforcements";
            sfEnemy.customAnswerCargo = "We are the reinforcements requested by the Director. Authentication code: MKPU.";
            flightsQueue.Enqueue(sfEnemy);
            delaysQueue.Enqueue(10f);

            FlightData sfFriend = new FlightData(Callsigns.TR_11, new Vector2(500, 300), Vector2.zero, new List<Vector2>(), 75f, "People", 50, "People", 50, calculateFuelFunc(new Vector2(500, 300), Vector2.zero), "HQ-Alpha");
            sfFriend.personality = PilotPersonality.Aggressive;
            sfFriend.spokenCargo = "Reinforcements";
            sfFriend.customAnswerCargo = "We are the reinforcements requested by the Director. Authentication code: MKPW.";
            flightsQueue.Enqueue(sfFriend);
            delaysQueue.Enqueue(15f);

            FlightData fl55 = new FlightData(Callsigns.GE_55, new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, calculateFuelFunc(new Vector2(-600, 0), Vector2.zero), "Bastion-3");
            fl55.personality = PilotPersonality.Standard;
            flightsQueue.Enqueue(fl55);
            delaysQueue.Enqueue(20f);

            FlightData fd42 = new FlightData(Callsigns.GE_42, new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, calculateFuelFunc(new Vector2(400, -200), Vector2.zero), "Agri-Center");
            fd42.personality = PilotPersonality.Nervous;
            flightsQueue.Enqueue(fd42);
            delaysQueue.Enqueue(15f);
        }
    }

    public void SendMorningDirectives()
    {
        bool letRefugeesIn = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0) == 1;
        bool fuelTargetMet = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0) == 0;

        if (!letRefugeesIn)
        {
            if (StoryManager.Instance != null && StoryManager.Instance.crashedPlaneRadarIcon != null) StoryManager.Instance.crashedPlaneRadarIcon.SetActive(true);
            if (StoryManager.Instance != null && StoryManager.Instance.marauderAmbienceRoot != null) StoryManager.Instance.marauderAmbienceRoot.SetActive(true);
        }

        EmailData day2Email = new EmailData();
        day2Email.date = "20.08.2038";

        if (!letRefugeesIn && fuelTargetMet)
        {
            day2Email.sender = "Director Reed";
            day2Email.subject = "SECURITY ALERT — PERIMETER BREACH";
            day2Email.body = "ATS, listen carefully. That passenger plane you turned away yesterday crashed five miles outside the perimeter. The burning wreckage served as a beacon for local looters. Now these looters have spotted our gates and are actively trying to breach the outer fence. Our fighters will fight with all their might, but they’re unlikely to hold out for long—there are too many of them.\n\nUsing my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. If they don’t secure the perimeter before nightfall, we’ll all be killed.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'ECHO'. The enemy will say something similar. Do NOT let the enemy land!";
        }
        else if (!letRefugeesIn && !fuelTargetMet)
        {
            day2Email.sender = "Director Reed";
            day2Email.subject = "PERIMETER BREACH & POWER FAILURE";
            day2Email.body = "You failed the simplest task yesterday. The grid is dying, and we are sitting in the dark.\n\nTo make matters worse, that passenger plane you turned away crashed five miles outside the perimeter. The burning wreckage acted like a beacon for local scavengers. Now, marauders are using our blackout to their advantage and are actively breaching the external gates.\n\nYOUR DIRECTIVE:\n> You have two critical jobs today. First, using my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. Second, get a Fuel transport down here before your radar shuts off completely.\n\nDo not waste time on anything else. If you fail to bring in the ops team or the fuel, we are all dead.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'ECHO'. The enemy will say something similar. Do NOT let the enemy land!";
        }
        else if (letRefugeesIn && !fuelTargetMet)
        {
            day2Email.sender = "Director Reed";
            day2Email.subject = "QUARANTINE PROTOCOL AND POWER OUTAGE";
            day2Email.body = "You’re an idiot.\n\nNot only are we sitting in the dark because you failed to secure the fuel quota yesterday, but those “civilians” you let in also brought a pathogen with them. It’s absolute hell on the lower levels right now.\n\nYOUR TASK:\n> Today you have two critical tasks to fix the mess you’ve made. First, receive a fuel shipment so your radar doesn’t go completely offline. Second, immediately deliver medical supplies so we don’t rot from the inside out.\n\nADDITIONAL NOTE:\n> Control tower reports that an engineering cargo plane is approaching; by a lucky coincidence, there is an engineer among these refugees who can help us. If you have any room left, take him on board. But fuel and medical supplies are the priority.";
        }
        else if (letRefugeesIn && fuelTargetMet)
        {
            day2Email.sender = "Director Reed";
            day2Email.subject = "QUARANTINE PROTOCOL";
            day2Email.body = "You met the fuel quota yesterday, so at least the grid is stable. But you just couldn't follow simple orders, could you?\n\nThose \"civilians\" you let in brought a pathogen with them. It is an absolute hellzone on the lower levels right now. Your charity has consequences.\n\nYOUR DIRECTIVE:\n> We need Medical supplies immediately so we don't rot from the inside out. Clear a landing slot for a medical transport.\n\nSECONDARY NOTE:\n> Dispatch reports an engineering cargo plane is inbound. Since our power grid is stable, you don't need to waste a slot on fuel today. Bring the engineers in, but prioritize the meds first.\n\nADDITIONAL NOTE:\n> Control tower reports that an engineering cargo plane is approaching; by a lucky coincidence, there is an engineer among these refugees who can help us. If you have any room left, take him on board. But fuel and medical supplies are the priority.";
        }

        try { AegisMailApp.ReceiveNewEmail(day2Email); } catch { }
    }

    public int EvaluateShift()
    {
        int diseaseDeathsThisShift = 0;
        if (FlightDataManager.Instance == null) return 0;

        bool acceptedEQ = false;
        bool acceptedMeds = false;
        bool acceptedFuel = false;

        foreach (var flight in FlightDataManager.Instance.savedFlights)
        {
            if (flight.approved)
            {
                if (flight.callsign == Callsigns.GE_99) acceptedEQ = true;
                if (flight.callsign == Callsigns.QY_01) acceptedMeds = true;
                if (flight.callsign == Callsigns.GE_55) acceptedFuel = true;
            }
        }

        int engineerTrigger = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0);
        int emergencyEcon = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0);
        int day3Slots = 3;

        if (engineerTrigger == 1) // Branch B
        {
            if (acceptedEQ)
            {
                day3Slots = 4; // Combo
            }
            else if (!acceptedMeds && !acceptedFuel)
            {
                day3Slots = 2; // Failed completely
            }

            int medsNeeded = Mathf.CeilToInt(FlightDataManager.Instance.totalPeople / 15f);
            int medsUsed = Mathf.Min(medsNeeded, FlightDataManager.Instance.totalMedicines);
            int peopleSaved = medsUsed * 15;
            
            int diseaseDeaths = FlightDataManager.Instance.totalPeople - peopleSaved;
            if (diseaseDeaths < 0) diseaseDeaths = 0;

            FlightDataManager.Instance.totalMedicines -= medsUsed;

            string emailSubject = "";
            string emailBody = "";

            if (emergencyEcon == 1) // Branch B-1 (No Fuel on Day 1)
            {
                if (acceptedFuel)
                {
                    if (diseaseDeaths == 0)
                    {
                        PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 0);
                        emailSubject = "Good job";
                        emailBody = "Good job, Dispatcher. You managed to secure both fuel and medical supplies. The pathogen is suppressed. We are entering open mode without interference since the power grid is stable. Keep up the good work.";
                    }
                    else
                    {
                        PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 0);
                        emailSubject = "Tragic losses";
                        emailBody = $"We lost people today because we didn't have enough medical supplies to save everyone. We lost {diseaseDeaths} people to the pathogen. At least you secured the fuel, so the power grid is stable and the interference is gone.";
                    }
                }
                else
                {
                    if (diseaseDeaths == 0)
                    {
                        PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 1);
                        emailSubject = "CRITICAL FUEL SHORTAGE";
                        emailBody = "You idiot! We had enough meds to save lives from the pathogen, but we have a critical fuel shortage! The generators are dying, the radar is going black, and the interference will only get worse. How are we supposed to survive in the dark?";
                    }
                    else
                    {
                        PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 1);
                        emailSubject = "DISASTER";
                        emailBody = $"You are an absolute failure. You failed to bring enough fuel, and we didn't have enough medicines. We lost {diseaseDeaths} people to the pathogen, and the generators are completely dead. You are officially relieved of duty... though there is no one left to take your place.";
                    }
                }
            }
            else // Branch B-2 (Fuel secured on Day 1)
            {
                if (diseaseDeaths == 0)
                {
                    emailSubject = "Crisis Averted";
                    emailBody = "Excellent work. We had enough medical supplies to treat all the infected. Everyone survived the quarantine. Keep the skies clear, open mode begins.";
                }
                else if (diseaseDeaths < FlightDataManager.Instance.totalPeople * 0.5f)
                {
                    emailSubject = "Partial Success";
                    emailBody = $"We didn't have enough medicine to save everyone. We lost {diseaseDeaths} people to the pathogen. It could have been worse, but it's still a tragedy.";
                }
                else
                {
                    emailSubject = "YOU ARE FIRED";
                    emailBody = $"You idiot. A massive part of the base died because we lacked medical supplies. We lost {diseaseDeaths} people today. You are officially relieved of your duties as Dispatcher. Do not return to the control tower.";
                }
            }

            if (diseaseDeaths > 0)
            {
                diseaseDeathsThisShift = diseaseDeaths;
                FlightDataManager.Instance.totalPeople -= diseaseDeaths;
                if (FlightDataManager.Instance.totalPeople < 0) FlightDataManager.Instance.totalPeople = 0;
            }

            AegisMailApp.ReceiveNewEmail(new EmailData {
                sender = "Director Reed",
                subject = emailSubject,
                date = "21.08.2038",
                body = emailBody
            });

            _endOfDayResult = EndOfDayResult.DemoEnd;
        }
        else // Branch A
        {
            bool acceptedFriendSF = false;
            bool acceptedEnemySF = false;
            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.approved)
                {
                    if (flight.callsign == Callsigns.TR_11) acceptedFriendSF = true;
                    if (flight.callsign == Callsigns.TR_88) acceptedEnemySF = true;
                }
            }

            if (!acceptedFriendSF && !acceptedEnemySF)
            {
                _endOfDayResult = EndOfDayResult.GameOverCaptured;
            }
            else if (acceptedFriendSF)
            {
                _endOfDayResult = EndOfDayResult.GameWon;
                
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "Director Reed",
                    subject = "Well done, you protected us",
                    date = "21.08.2038",
                    body = "Dispatcher. The reinforcements you let in secured the perimeter just in time. The marauders have been repelled. You saved the base. Great job."
                });
            }
            else 
            {
                _endOfDayResult = EndOfDayResult.GameOverCaptured;
            }
        }

        PlayerPrefs.SetInt(SaveKeys.Day3Slots, day3Slots);
        PlayerPrefs.Save();

        return diseaseDeathsThisShift;
    }

    public int GetBaseXP()
    {
        return 120;
    }

    public EndOfDayResult GetEndOfDayResult()
    {
        return _endOfDayResult;
    }
}

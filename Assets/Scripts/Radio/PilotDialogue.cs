using UnityEngine;

/// <summary>
/// Generator of pilot dialogues based on personality type.
/// Used as a fallback - custom strings from FlightData take precedence.
/// </summary>
public static class PilotDialogue
{
    // ========================
    // GREETINGS
    // ========================

    public static string GetGreeting(PilotPersonality personality, string callsign)
    {
        switch (personality)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    $"Bastion-7, this is {callsign}. Open the corridor, now.",
                    $"Control, {callsign}. I don't have time for your protocols. Corridor. Now.",
                    $"{callsign} to tower. Clear me for landing, we've wasted enough time.",
                    $"Tower, {callsign}. We're coming in. Get the corridor open or explain to command why you held us up.",
                    $"Bastion-7, {callsign}. Landing. Not asking twice.",
                    $"{callsign} here. Skip the pleasantries, Control. Open the corridor."
                );

            case PilotPersonality.Nervous:
                return Pick(
                    $"Uhh... Bastion-7? This is {callsign}... r-requesting landing, please.",
                    $"Control, this is... wait... {callsign} here. Landing corridor, please?",
                    $"B-Bastion-7, {callsign} requesting... uh... permission to land.",
                    $"Hello? Bastion-7? Can you hear me? This is {callsign}... we need to land...",
                    $"Bastion-7, {callsign}... sorry to bother, is there a free corridor?",
                    $"Um... Control? {callsign} on approach... at least I think this is the right frequency..."
                );

            case PilotPersonality.Cold:
                return Pick(
                    $"{callsign}. Landing corridor.",
                    $"Bastion-7. {callsign}. Requesting approach.",
                    $"Control. {callsign} inbound. Awaiting clearance.",
                    $"{callsign}. Inbound. Corridor.",
                    $"Bastion-7. {callsign}. Approach vector requested.",
                    $"{callsign} to Control. Landing."
                );

            case PilotPersonality.Desperate:
                return Pick(
                    $"Bastion-7, PLEASE, this is {callsign}! We need to land immediately!",
                    $"Control! {callsign} here! We're running out of options, requesting emergency landing!",
                    $"For God's sake, Bastion-7! {callsign} requesting immediate landing corridor!",
                    $"MAYDAY, Bastion-7! {callsign}! We can't stay airborne much longer!",
                    $"Bastion-7, this is {callsign}! We have a critical situation on board! PLEASE respond!",
                    $"Control, {callsign}! If you don't let us land NOW, we're going to crash!"
                );

            default: // Standard
                return Pick(
                    "Bastion-7, requesting landing corridor.",
                    "Control, requesting approach clearance.",
                    "Bastion-7, inbound for landing. Requesting corridor assignment."
                );
        }
    }

    // ========================
    // ANSWERS TO QUESTIONS
    // ========================

    public static string GetAnswer(PilotPersonality personality, string topic, string value)
    {
        switch (topic)
        {
            case "cargo":  return GetCargoAnswer(personality, value);
            case "origin": return GetOriginAnswer(personality, value);
            case "weight": return GetWeightAnswer(personality, value);
            case "speed":  return GetSpeedAnswer(personality, value);
            default:       return $"Confirmed: {value}.";
        }
    }

    static string GetCargoAnswer(PilotPersonality p, string cargo)
    {
        switch (p)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    $"It's {cargo}. Satisfied? Can we land now?",
                    $"{cargo}. Check the manifest if you don't believe me.",
                    $"We're hauling {cargo}. How many times do I have to say it?",
                    $"{cargo}, dispatcher. I shouldn't have to spell this out for you.",
                    $"Read the paperwork — {cargo}. Or do I need to file a complaint first?",
                    $"Our hold is full of {cargo}. Are you done wasting my fuel with questions?"
                );
            case PilotPersonality.Nervous:
                return Pick(
                    $"It's... it's {cargo}, sir. That's what we have. I think.",
                    $"Uhh, {cargo}? Yes, {cargo}. That's correct. Sorry.",
                    $"Our cargo is... let me check... yes, {cargo}.",
                    $"The loadmaster said it was {cargo}... I didn't check myself, but that's what the papers say.",
                    $"{cargo}. At least that's what they told me at departure... right?",
                    $"Should be {cargo}. Hold on, let me look at the manifest again... yes, {cargo}."
                );
            case PilotPersonality.Cold:
                return Pick(
                    $"{cargo}.",
                    $"Cargo: {cargo}. Confirmed.",
                    $"Transporting {cargo}.",
                    $"Cargo is {cargo}. Over.",
                    $"{cargo}. As filed.",
                    $"Affirmative. {cargo}."
                );
            case PilotPersonality.Desperate:
                return Pick(
                    $"We have {cargo}! Please, just let us through!",
                    $"{cargo}, Control! We can't stay in the air much longer!",
                    $"It's {cargo}! Does it matter?! We need to land NOW!",
                    $"{cargo}! Critical {cargo}! People are counting on this, Control!",
                    $"We're carrying {cargo}! You have to understand, every second counts!",
                    $"It's {cargo}, and if we don't land soon, it won't matter what we're carrying!"
                );
            default:
                return Pick(
                    $"We are transporting {cargo}.",
                    $"Cargo on board: {cargo}.",
                    $"Confirmed, carrying {cargo}."
                );
        }
    }

    static string GetOriginAnswer(PilotPersonality p, string origin)
    {
        switch (p)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    $"We came from {origin}. Is that a problem?",
                    $"{origin}. Next question.",
                    $"Flight originated from {origin}. As stated in the documents.",
                    $"{origin}. It's in the manifest. Try reading it.",
                    $"We departed {origin} three hours ago. Anything else you want to waste time on?",
                    $"Origin is {origin}. Do you interrogate every flight like this?"
                );
            case PilotPersonality.Nervous:
                return Pick(
                    $"We... we took off from {origin}. I'm pretty sure.",
                    $"Origin is {origin}... yes, definitely {origin}.",
                    $"Umm, {origin}? Let me double-check... yes, {origin}.",
                    $"I... think it was {origin}? Yes! {origin}. Sorry, I keep second-guessing myself.",
                    $"We left from {origin}... or... no, yes, definitely {origin}. The co-pilot confirms.",
                    $"{origin}, sir. It was {origin}. I wrote it down somewhere..."
                );
            case PilotPersonality.Cold:
                return Pick(
                    $"{origin}.",
                    $"Origin: {origin}.",
                    $"Departed from {origin}.",
                    $"Point of origin: {origin}. Over.",
                    $"{origin}. As logged.",
                    $"Affirmative. {origin}."
                );
            case PilotPersonality.Desperate:
                return Pick(
                    $"We barely made it out of {origin}! The situation there is dire!",
                    $"{origin}, Control! We had to leave in a hurry!",
                    $"From {origin}! Please, we've been flying for hours!",
                    $"{origin}! It's chaos back there, we were lucky to take off at all!",
                    $"We fled from {origin}! There was no time for proper paperwork!",
                    $"{origin}... we left everything behind. Please, just let us land."
                );
            default:
                return Pick(
                    $"Flight originated from {origin}.",
                    $"We departed from {origin}.",
                    $"Origin point: {origin}."
                );
        }
    }

    static string GetWeightAnswer(PilotPersonality p, string weight)
    {
        switch (p)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    $"{weight} UNITS. It's on the manifest. Read it.",
                    $"Manifest says {weight} UNITS. Are we done?",
                    $"{weight} UNITS. Standard load. Anything else?",
                    $"Exactly {weight} UNITS. Weighed it myself. Happy now?",
                    $"{weight} UNITS. I've been flying longer than you've been dispatching — the numbers are right.",
                    $"The scale said {weight}. The manifest says {weight}. I say {weight}. Enough?"
                );
            case PilotPersonality.Nervous:
                return Pick(
                    $"The weight is... {weight} UNITS, I believe. Let me recheck... yes, {weight}.",
                    $"Uhh, {weight} UNITS? That's what the instruments say...",
                    $"It should be {weight} UNITS. I hope that's right.",
                    $"I... I think {weight} UNITS? The loadmaster handles that part... but yes, {weight}.",
                    $"Let me see... {weight} UNITS. Wait, yes. {weight}. Sorry, the numbers all blend together.",
                    $"{weight} UNITS? Or was it... no, {weight}. Definitely {weight}. I wrote it down."
                );
            case PilotPersonality.Cold:
                return Pick(
                    $"{weight} UNITS.",
                    $"Weight: {weight}.",
                    $"Payload {weight} UNITS. Confirmed.",
                    $"{weight}. Nominal.",
                    $"Gross payload: {weight} UNITS. Over.",
                    $"Affirmative. {weight} UNITS loaded."
                );
            case PilotPersonality.Desperate:
                return Pick(
                    $"{weight} UNITS! Every unit counts right now, Control!",
                    $"We're carrying {weight} UNITS. Please, we're almost out of fuel!",
                    $"{weight} UNITS. People are depending on this delivery!",
                    $"{weight} UNITS of critical supplies! If we ditch, it's all lost!",
                    $"It's {weight} UNITS! We loaded as much as we could carry, please let us through!",
                    $"{weight} UNITS! We risked our lives getting this, don't turn us away!"
                );
            default:
                return Pick(
                    $"Manifest states {weight} UNITS.",
                    $"Total payload: {weight} UNITS.",
                    $"Carrying {weight} UNITS on board."
                );
        }
    }

    static string GetSpeedAnswer(PilotPersonality p, string speed)
    {
        switch (p)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    $"{speed} KTS. Within regulations. Are you going to clear us or not?",
                    $"Instruments show {speed} KTS. We know what we're doing.",
                    $"{speed} KTS. Do you need my blood type too?",
                    $"{speed} KTS. Well within parameters. Unlike this interrogation.",
                    $"Holding at {speed} KTS. Which is exactly what the regs say. You're welcome.",
                    $"Airspeed {speed} KTS. Now, are we landing or am I circling until I run dry?"
                );
            case PilotPersonality.Nervous:
                return Pick(
                    $"Our speed is... uh... {speed} KTS? Yes, {speed} KTS.",
                    $"I'm reading {speed} KTS on the instruments. I think that's normal?",
                    $"Uhh, {speed} KTS. Sorry, the gauges are a bit hard to read up here.",
                    $"{speed} KTS... is that too fast? Too slow? I can adjust if needed...",
                    $"The needle says {speed} KTS. My co-pilot agrees, so... {speed} KTS.",
                    $"Let me look... {speed} KTS. Yeah. Wait, is that the right gauge? Yes. {speed}."
                );
            case PilotPersonality.Cold:
                return Pick(
                    $"{speed} KTS.",
                    $"Speed: {speed} KTS.",
                    $"Holding {speed} KTS.",
                    $"Airspeed {speed}. Stable.",
                    $"{speed} KTS indicated. Over.",
                    $"Maintaining {speed}."
                );
            case PilotPersonality.Desperate:
                return Pick(
                    $"{speed} KTS! We can't maintain this much longer!",
                    $"We're doing {speed} KTS, barely holding together!",
                    $"{speed} KTS! Engine is making terrible sounds, please hurry!",
                    $"{speed} KTS and dropping! One engine is already sputtering!",
                    $"Struggling to hold {speed} KTS! We're losing altitude!",
                    $"{speed} KTS! The airframe is shaking, we don't have much time!"
                );
            default:
                return Pick(
                    $"Instruments show {speed} KTS.",
                    $"Current airspeed: {speed} KTS.",
                    $"Confirmed, {speed} KTS."
                );
        }
    }

    // ========================
    // CONFRONTATION
    // ========================

    public static string GetConfrontResponse(PilotPersonality personality)
    {
        switch (personality)
        {
            case PilotPersonality.Aggressive:
                return Pick(
                    "You're wasting our time with this nonsense! I'll report you to command!",
                    "Discrepancy?! Check your own instruments before accusing ME!",
                    "I'm not going to sit here and be interrogated. Let us land or I'm going over your head!",
                    "That's YOUR error, not ours. I've been doing this for 15 years!",
                    "Unbelievable. You're going to question MY flight data? File your complaint AFTER we land.",
                    "I don't answer to desk jockeys. Clear us or get your supervisor on the line."
                );
            case PilotPersonality.Nervous:
                return Pick(
                    "W-what? A discrepancy? That can't be right... I must have made an error somewhere...",
                    "Oh no... I... I don't know how that happened. I'm sorry, I'm so sorry...",
                    "Please, it's a mistake! I'm new and the paperwork is confusing! Don't deny us!",
                    "I... I swear it was correct when we left! Maybe I wrote it down wrong? Oh God...",
                    "A discrepancy?! That's... that's impossible... unless I messed up the forms again...",
                    "Please don't report this! I'll lose my license! It has to be a clerical error!"
                );
            case PilotPersonality.Cold:
                return Pick(
                    "Noted. Instruments may vary. Awaiting your decision.",
                    "Possible calibration error. Standing by.",
                    "Acknowledged. Proceed with your assessment.",
                    "Copy. Data variance within expected tolerance. Standing by.",
                    "Understood. We have nothing to add. Your call, Control.",
                    "Noted. Recommend you verify on your end. Awaiting instructions."
                );
            case PilotPersonality.Desperate:
                return Pick(
                    "I know it looks bad, but you have to believe us! People will die!",
                    "PLEASE! We can explain everything after landing! There's no time!",
                    "We didn't have a choice! The situation forced our hand! Just let us land!",
                    "There are wounded on board! You can sort out the paperwork AFTER we land!",
                    "I'M BEGGING YOU! Whatever the numbers say, we need to be on the ground NOW!",
                    "We'll answer all your questions on the tarmac! But if we stay up here, nobody gets answers!"
                );
            default:
                return Pick(
                    "Atmospheric interference, dispatcher. Everything is normal.",
                    "Must be a data transmission error. All readings are nominal on our end.",
                    "Acknowledged. We'll review our instruments. Standing by for your decision."
                );
        }
    }

    // ========================
    // RESPONSE DELAY
    // ========================

    /// <summary>
    /// Returns (min, max) the pilot response delay in seconds.
    /// A nervous person answers quickly (hurries to justify himself), a cold person answers slowly.
    /// </summary>
    public static (float min, float max) GetResponseDelay(PilotPersonality personality)
    {
        switch (personality)
        {
            case PilotPersonality.Aggressive: return (1.0f, 2.0f);
            case PilotPersonality.Nervous:    return (0.5f, 1.5f);
            case PilotPersonality.Cold:       return (3.0f, 5.0f);
            case PilotPersonality.Desperate:  return (0.5f, 1.5f);
            default:                          return (2.0f, 3.0f);
        }
    }

    // ========================
    // RANDOM SELECTION FOR PROCEDURE FLIGHTS
    // ========================

    /// <summary>
    /// Returns a random identity for random (non-story) aircraft.
    /// </summary>
    public static PilotPersonality GetRandomPersonality()
    {
        PilotPersonality[] pool = new PilotPersonality[]
        {
            PilotPersonality.Standard,
            PilotPersonality.Standard,    // increased chance Standard
            PilotPersonality.Aggressive,
            PilotPersonality.Nervous,
            PilotPersonality.Cold,
            PilotPersonality.Desperate
        };
        return pool[Random.Range(0, pool.Length)];
    }

    // ========================
    // UTILITIES
    // ========================

    static string Pick(params string[] variants)
    {
        return variants[Random.Range(0, variants.Length)];
    }
}

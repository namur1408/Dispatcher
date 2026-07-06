using UnityEngine;

/// <summary>
/// Handles document fact checking, discrepancies, and truth evaluation.
/// Extracted from CommsManager to prevent God Object anti-pattern.
/// </summary>
public class CommsDocumentLogic
{
    public string firstFactID = "";
    public string currentLieTopic = "";
    
    private FlightData currentData;
    private FlightInterrogationState currentState;

    public void SetFlightData(FlightData data, FlightInterrogationState state)
    {
        currentData = data;
        currentState = state;
    }

    public string GetStatedOrigin() => !string.IsNullOrEmpty(currentData.spokenOrigin) ? currentData.spokenOrigin : currentData.manifestOrigin;
    public string GetStatedCargo() => !string.IsNullOrEmpty(currentData.spokenCargo) ? currentData.spokenCargo : currentData.manifestCargo;
    public string GetStatedWeight() => !string.IsNullOrEmpty(currentData.spokenWeight) ? currentData.spokenWeight : currentData.manifestCargoAmount.ToString();
    public string GetStatedSpeed() => !string.IsNullOrEmpty(currentData.spokenSpeed) ? currentData.spokenSpeed : (currentData.speed * 5f).ToString();
    public string GetPlaneClass() => currentData.callsign.StartsWith("TR") ? "Passenger" : (currentData.callsign.StartsWith("GE") ? "Cargo" : "Courier");

    /// <summary>
    /// Checks if two selected facts contradict each other or form a valid matching pair.
    /// Returns true if the pair is a valid comparison, and outputs whether it's a lie and what topic the lie is about.
    /// </summary>
    public bool CheckContradiction(string firstFactID, string secondFactID, out bool isLie, out string lieTopic)
    {
        bool isValid = false;
        isLie = false;
        lieTopic = "";

        if (firstFactID.StartsWith("rule_") || secondFactID.StartsWith("rule_"))
        {
            string rule = firstFactID.StartsWith("rule_") ? firstFactID : secondFactID;
            string fact = firstFactID.StartsWith("rule_") ? secondFactID : firstFactID;

            if (rule.Contains("_ge_") && currentData.callsign.StartsWith("GE"))
            {
                if (rule == "rule_ge_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 5f : (float.TryParse(GetStatedSpeed(), out float s) ? s : 0f);
                    if (speedToCheck >= 425f) isLie = true;
                }
                else if (rule == "rule_ge_weight" && (fact == "man_weight" || fact == "rep_weight"))
                {
                    isValid = true;
                    float weightToCheck = fact == "man_weight" ? currentData.manifestCargoAmount : (float.TryParse(GetStatedWeight(), out float w) ? w : 0f);
                    if (weightToCheck > 500) isLie = true;
                }
            }
            else if (rule.Contains("_tr_") && currentData.callsign.StartsWith("TR"))
            {
                if (rule == "rule_tr_cargo" && (fact == "man_cargo" || fact == "rep_cargo"))
                {
                    isValid = true;
                    string cargoToCheck = fact == "man_cargo" ? currentData.manifestCargo : GetStatedCargo();
                    if (cargoToCheck != "People") isLie = true;
                }
                else if (rule == "rule_tr_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 5f : (float.TryParse(GetStatedSpeed(), out float s) ? s : 0f);
                    if (speedToCheck < 350f || speedToCheck > 390f) isLie = true;
                }
            }
            else if (rule.Contains("_qy_") && currentData.callsign.StartsWith("QY"))
            {
                if (rule == "rule_qy_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 5f : (float.TryParse(GetStatedSpeed(), out float s) ? s : 0f);
                    if (speedToCheck <= 400f) isLie = true;
                }
                else if (rule == "rule_qy_weight" && (fact == "man_weight" || fact == "rep_weight"))
                {
                    isValid = true;
                    float weightToCheck = fact == "man_weight" ? currentData.manifestCargoAmount : (float.TryParse(GetStatedWeight(), out float w) ? w : 0f);
                    if (weightToCheck > 50) isLie = true;
                }
            }
        }
        else
        {
            if (CheckPair(firstFactID, secondFactID, "rad_class", "man_cargo") || CheckPair(firstFactID, secondFactID, "rad_class", "rep_cargo"))
            {
                isValid = true;
                string cargo = firstFactID.Contains("cargo") ?
                    (firstFactID == "man_cargo" ? currentData.manifestCargo : GetStatedCargo()) :
                    (secondFactID == "man_cargo" ? currentData.manifestCargo : GetStatedCargo());

                if (currentData.callsign.StartsWith("TR") && cargo != "People") isLie = true;
                if (currentData.callsign.StartsWith("GE") && cargo == "People") isLie = true;
            }
            else if (CheckPair(firstFactID, secondFactID, "rad_sensor", "man_cargo") || CheckPair(firstFactID, secondFactID, "rad_sensor", "rep_cargo"))
            {
                isValid = true;
                string cargoToCompare = (secondFactID == "man_cargo" || firstFactID == "man_cargo") ? currentData.manifestCargo : GetStatedCargo();
                isLie = (currentData.cargo.ToUpper() != cargoToCompare.ToUpper());
            }
            else if (CheckPair(firstFactID, secondFactID, "man_cargo", "rep_cargo"))
            {
                isValid = true; isLie = (currentData.manifestCargo.ToUpper() != GetStatedCargo().ToUpper());
            }
            else if (CheckPair(firstFactID, secondFactID, "man_origin", "rep_origin"))
            {
                isValid = true; isLie = (currentData.manifestOrigin.ToUpper() != GetStatedOrigin().ToUpper());
            }
            else if (CheckPair(firstFactID, secondFactID, "man_weight", "rep_weight"))
            {
                isValid = true; isLie = (currentData.manifestCargoAmount.ToString() != GetStatedWeight());
            }
            else if (CheckPair(firstFactID, secondFactID, "rad_speed", "rep_speed"))
            {
                isValid = true; isLie = ((currentData.speed * 5f).ToString() != GetStatedSpeed());
            }
        }

        if (isLie)
        {
            if (firstFactID.Contains("cargo") || secondFactID.Contains("cargo") || firstFactID.Contains("class") || secondFactID.Contains("class")) lieTopic = "cargo";
            else if (firstFactID.Contains("origin") || secondFactID.Contains("origin")) lieTopic = "origin";
            else if (firstFactID.Contains("weight") || secondFactID.Contains("weight")) lieTopic = "weight";
            else if (firstFactID.Contains("speed") || secondFactID.Contains("speed")) lieTopic = "speed";
        }

        if (isValid && !isLie && (firstFactID.Contains("cargo") || secondFactID.Contains("cargo")))
        {
            if (currentData.manifestCargo.ToUpper() == currentData.cargo.ToUpper())
            {
                if (currentState != null) currentState.isCargoKnown = true;
            }
        }

        return isValid;
    }

    private bool CheckPair(string i1, string i2, string t1, string t2) => (i1 == t1 && i2 == t2) || (i1 == t2 && i2 == t1);
}

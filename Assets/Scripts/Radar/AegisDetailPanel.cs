using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AegisDetailPanel : MonoBehaviour
{
    [Header("─── Callsign & Type ───")]
    public TMP_Text callsignText;  
    public TMP_Text aircraftTypeText;

    [Header("─── Data Rows ───")]
    public TMP_Text speedValue;
    public TMP_Text cargoValue;

    [Header("─── Status Block ───")]
    public TMP_Text statusValueText; 

    [Header("─── Fuel Bar ───")]
    public TMP_Text fuelLabelText;  
    public Image    fuelBarFill;     

    [Header("─── Empty State ───")]
    public GameObject noSelectionGroup; 
    public GameObject dataGroup;      

    private static readonly Color colorApproved = new Color(0f,   1f,   0.314f, 1f);
    private static readonly Color colorDenied   = new Color(1f,   0.25f,0.25f,  1f);
    private static readonly Color colorPending  = new Color(0f,   1f,   0.314f, 0.4f);
    private static readonly Color colorAmber    = new Color(1f,   0.67f,0f,     1f);
    private static readonly Color colorCyan     = new Color(0f,   0.75f,1f,     1f);
    private static readonly Color colorDim      = new Color(0f,   1f,   0.314f, 0.35f);

    public void ShowPlane(UIAirplane plane)
    {
        if (plane == null) { ShowEmpty(); return; }

        SetVisible(true);

        string callsign  = plane.callsignText != null ? plane.callsignText.text : "???";
        bool   isTransit = plane.targetPosition != Vector2.zero && string.IsNullOrEmpty(plane.assignedRunway);

        if (callsignText) callsignText.text = callsign;
        string typeLabel = isTransit ? "TRANSIT AIRCRAFT" :
                           (plane.isTakingOff || plane.isDeparting) ? "DEPARTING AIRCRAFT" : "INBOUND AIRCRAFT";
        if (aircraftTypeText) aircraftTypeText.text = typeLabel;
        if (speedValue) speedValue.text = $"SPEED        {plane.speed * 5f:0} KTS";
        if (cargoValue)
        {
            if (isTransit)
            {
                cargoValue.text  = $"CARGO        —";
                cargoValue.color = colorDim;
            }
            else
            {
                string cargo = GetCargoString(plane, out bool known);
                cargoValue.text  = $"CARGO        {cargo}";
                cargoValue.color = known ? colorApproved : colorDenied;
            }
        }
        if (statusValueText)
        {
            if (isTransit)
            {
                statusValueText.text  = "TRANSIT (XSIT)";
                statusValueText.color = colorCyan;
            }
            else
            {
                switch (plane.dispatchStatus)
                {
                    case UIAirplane.DispatchStatus.Approved:
                        statusValueText.text  = "APPROVED";
                        statusValueText.color = colorApproved;
                        break;
                    case UIAirplane.DispatchStatus.Denied:
                        statusValueText.text  = "DENIED";
                        statusValueText.color = colorDenied;
                        break;
                    default:
                        statusValueText.text  = "PENDING";
                        statusValueText.color = colorPending;
                        break;
                }
            }
        }

        if (!isTransit)
        {
            int fuel    = Mathf.RoundToInt(plane.currentFuel);
            float pct   = Mathf.Clamp01(plane.currentFuel / 100f);

            if (fuelLabelText)
            {
                string fuelStr = fuel > 0 ? $"{fuel} L" : "CRITICAL (0 L)";
                fuelLabelText.text  = $"// FUEL RESERVES   {fuelStr}";
                fuelLabelText.color = fuel <= 0 ? colorDenied : colorDim;
            }

            if (fuelBarFill)
            {
                if (fuelBarFill.type == Image.Type.Filled)
                {
                    fuelBarFill.fillAmount = pct;
                }
                else
                {
                    RectTransform bgRT = fuelBarFill.transform.parent.GetComponent<RectTransform>();
                    if (bgRT)
                    {
                        RectTransform fillRT = fuelBarFill.GetComponent<RectTransform>();
                        fillRT.anchorMin = Vector2.zero;
                        fillRT.anchorMax = new Vector2(pct, 1f);
                        fillRT.offsetMin = Vector2.zero;
                        fillRT.offsetMax = Vector2.zero;
                    }
                }

                fuelBarFill.color = fuel <= 15 ? colorDenied :
                                    fuel <= 40 ? colorAmber  : colorApproved;
            }
        }
        else
        {
            if (fuelLabelText) fuelLabelText.text = "";
            if (fuelBarFill)   fuelBarFill.gameObject.transform.parent.gameObject.SetActive(false);
        }
    }
    
    public void ShowPlane(FlightData plane)
    {
        if (plane == null) { ShowEmpty(); return; }

        SetVisible(true);

        string callsign  = plane.callsign;
        bool   isTransit = plane.targetPosition != Vector2.zero && string.IsNullOrEmpty(plane.assignedRunway);
        if (callsignText) callsignText.text = callsign;
        string typeLabel = isTransit ? "TRANSIT AIRCRAFT" :
                           (plane.isTakingOff || plane.isDeparting) ? "DEPARTING AIRCRAFT" : "INBOUND AIRCRAFT";
        if (aircraftTypeText) aircraftTypeText.text = typeLabel;
        if (speedValue) speedValue.text = $"SPEED        {plane.speed * 5f:0} KTS";
        if (cargoValue)
        {
            if (isTransit)
            {
                cargoValue.text  = $"CARGO        —";
                cargoValue.color = colorDim;
            }
            else
            {
                string cargo = GetCargoString(plane, out bool known);
                cargoValue.text  = $"CARGO        {cargo}";
                cargoValue.color = known ? colorApproved : colorDenied;
            }
        }
        if (statusValueText)
        {
            if (isTransit)
            {
                statusValueText.text  = "TRANSIT (XSIT)";
                statusValueText.color = colorCyan;
            }
            else
            {
                if (plane.decisionMade)
                {
                    if (plane.approved)
                    {
                        statusValueText.text  = "APPROVED";
                        statusValueText.color = colorApproved;
                    }
                    else
                    {
                        statusValueText.text  = "DENIED";
                        statusValueText.color = colorDenied;
                    }
                }
                else
                {
                    statusValueText.text  = "PENDING";
                    statusValueText.color = colorPending;
                }
            }
        }
        if (!isTransit)
        {
            int fuel    = Mathf.RoundToInt(plane.currentFuel);
            float pct   = Mathf.Clamp01(plane.currentFuel / Mathf.Max(1f, plane.planeMaxFuel));

            if (fuelLabelText)
            {
                string fuelStr = fuel > 0 ? $"{fuel} L" : "CRITICAL (0 L)";
                fuelLabelText.text  = $"// FUEL RESERVES   {fuelStr}";
                fuelLabelText.color = fuel <= 0 ? colorDenied : colorDim;
            }

            if (fuelBarFill)
            {
                if (fuelBarFill.transform.parent != null) fuelBarFill.transform.parent.gameObject.SetActive(true);
                
                if (fuelBarFill.type == Image.Type.Filled)
                {
                    fuelBarFill.fillAmount = pct;
                }
                else
                {
                    RectTransform bgRT = fuelBarFill.transform.parent.GetComponent<RectTransform>();
                    if (bgRT)
                    {
                        RectTransform fillRT = fuelBarFill.GetComponent<RectTransform>();
                        fillRT.anchorMin = Vector2.zero;
                        fillRT.anchorMax = new Vector2(pct, 1f);
                        fillRT.offsetMin = Vector2.zero;
                        fillRT.offsetMax = Vector2.zero;
                    }
                }

                fuelBarFill.color = fuel <= 15 ? colorDenied :
                                    fuel <= 40 ? colorAmber  : colorApproved;
            }
        }
        else
        {
            if (fuelLabelText) fuelLabelText.text = "";
            if (fuelBarFill && fuelBarFill.transform.parent != null) fuelBarFill.transform.parent.gameObject.SetActive(false);
        }
    }

    public void ShowEmpty()
    {
        SetVisible(false);
    }

    void SetVisible(bool hasData)
    {
        if (dataGroup)        dataGroup.SetActive(hasData);
        if (noSelectionGroup) noSelectionGroup.SetActive(!hasData);
    }

    string GetCargoString(UIAirplane plane, out bool known)
    {
        known = true;
        if (plane == null || string.IsNullOrEmpty(plane.cargo)) return "NONE";

        string name = plane.cargo.ToUpper();
        string unit = name switch
        {
            "MEDICINES" => " BOX",
            "FOOD"      => " KG",
            "FUEL"      => " L",
            "PEOPLE"    => " PPL",
            "SCRAP"     => " KG",
            _           => ""
        };

        // If we are in the main game and there is a manager, we take the exact number and status
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == plane.callsignText.text);
            if (fd != null)
            {
                var state = FlightDataManager.Instance.GetOrCreateInterrogationState(fd.callsign);
                if (!state.isCargoKnown) { known = false; return "UNKNOWN"; }
                return name == "NONE" ? "NONE" : $"{name} ({fd.cargoAmount}{unit})";
            }
        }

        // If we just test the UI in a canvas without other scenes, we display what is on the plane
        return name == "NONE" ? "NONE" : $"{name} (---{unit})";
    }

    string GetCargoString(FlightData plane, out bool known)
    {
        known = true;
        if (plane == null || string.IsNullOrEmpty(plane.cargo)) return "NONE";

        string name = plane.cargo.ToUpper();
        string unit = name switch
        {
            "MEDICINES" => " BOX",
            "FOOD"      => " KG",
            "FUEL"      => " L",
            "PEOPLE"    => " PPL",
            "SCRAP"     => " KG",
            _           => ""
        };

        if (FlightDataManager.Instance != null) 
        {
            var state = FlightDataManager.Instance.GetOrCreateInterrogationState(plane.callsign);
            if (!state.isCargoKnown) { known = false; return "UNKNOWN"; }
        }
        else 
        {
            // Fallback for tests
            known = true; 
        }
        return name == "NONE" ? "NONE" : $"{name} ({plane.cargoAmount}{unit})";
    }
}

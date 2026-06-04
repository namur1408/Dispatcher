using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AEGIS-стиль для Detail Panel.
/// Замена selectedPlaneText — управляет отдельными объектами в иерархии.
///
/// Иерархия Detail Panel (собери в Unity):
///
/// DetailPanel
///   ├── Header
///   │     ├── CallsignBig     ← TMP (VT323, 42pt, #00FF50)   ← подключи в callsignText
///   │     └── AircraftType    ← TMP (Share Tech Mono, 10pt, #00FF5059)  ← подключи в aircraftTypeText
///   │
///   ├── DataRows               ← просто контейнер (Vertical Layout Group)
///   │     ├── RowSpeed        ← DataRow prefab / просто 2 TMP рядом
///   │     ├── RowCargo
///   │     ├── RowRunway
///   │     └── RowDest
///   │
///   ├── StatusBlock            ← Image (тёмная панель с рамкой) + SectionBorder
///   │     ├── StatusLabel     ← TMP "// STATUS" (10pt, dim green)
///   │     └── StatusValue     ← TMP (VT323, 22pt)  ← подключи в statusValueText
///   │
///   ├── FuelLabel             ← TMP "// FUEL RESERVES  0 L"  ← подключи в fuelLabelText
///   ├── FuelBarBG             ← Image (тёмная полоска)
///   │     └── FuelBarFill    ← Image (зелёная заливка)  ← подключи в fuelBarFill
///   │
///   └── NoSelectionGroup     ← объект "нет выбора"  ← подключи в noSelectionGroup
/// </summary>
public class AegisDetailPanel : MonoBehaviour
{
    [Header("─── Callsign & Type ───")]
    public TMP_Text callsignText;    // VT323, 42pt, #00FF50
    public TMP_Text aircraftTypeText;// Share Tech Mono, 10pt, dim

    [Header("─── Data Rows ───")]
    public TMP_Text speedValue;
    public TMP_Text cargoValue;

    [Header("─── Status Block ───")]
    public TMP_Text statusValueText; // VT323, 22pt

    [Header("─── Fuel Bar ───")]
    public TMP_Text fuelLabelText;   // "// FUEL RESERVES   74 L"
    public Image    fuelBarFill;     // Image с Fill Amount или через sizeDelta

    [Header("─── Empty State ───")]
    public GameObject noSelectionGroup; // объект "NO TARGET SELECTED"
    public GameObject dataGroup;        // весь контент (прячем когда нет выбора)

    // Цвета статуса
    private static readonly Color colorApproved = new Color(0f,   1f,   0.314f, 1f);
    private static readonly Color colorDenied   = new Color(1f,   0.25f,0.25f,  1f);
    private static readonly Color colorPending  = new Color(0f,   1f,   0.314f, 0.4f);
    private static readonly Color colorAmber    = new Color(1f,   0.67f,0f,     1f);
    private static readonly Color colorCyan     = new Color(0f,   0.75f,1f,     1f);
    private static readonly Color colorDim      = new Color(0f,   1f,   0.314f, 0.35f);

    // ─── Внешний API ───────────────────────────────────────────────────────────

    /// <summary>Показать данные выбранного самолёта (из Radar)</summary>
    public void ShowPlane(UIAirplane plane)
    {
        if (plane == null) { ShowEmpty(); return; }

        SetVisible(true);

        string callsign  = plane.callsignText != null ? plane.callsignText.text : "???";
        bool   isTransit = plane.targetPosition != Vector2.zero && string.IsNullOrEmpty(plane.assignedRunway);

        // ── Callsign ──
        if (callsignText) callsignText.text = callsign;

        // ── Тип воздушного судна ──
        string typeLabel = isTransit ? "TRANSIT AIRCRAFT" :
                           (plane.isTakingOff || plane.isDeparting) ? "DEPARTING AIRCRAFT" : "INBOUND AIRCRAFT";
        if (aircraftTypeText) aircraftTypeText.text = typeLabel;

        // ── Speed ──
        if (speedValue) speedValue.text = $"SPEED        {plane.speed * 10f:0} KTS";

        // ── Cargo ──
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


        // ── Status ──
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

        // ── Fuel ──
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
                // Если Image с Type = Filled → используем fillAmount
                if (fuelBarFill.type == Image.Type.Filled)
                {
                    fuelBarFill.fillAmount = pct;
                }
                else
                {
                    // Иначе двигаем через sizeDelta (FuelBarFill должен быть child FuelBarBG)
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
            // Транзит — прячем топливо
            if (fuelLabelText) fuelLabelText.text = "";
            if (fuelBarFill)   fuelBarFill.gameObject.transform.parent.gameObject.SetActive(false);
        }
    }

    /// <summary>Показать данные выбранного самолёта (из TVDisplayInfo/FlightData)</summary>
    public void ShowPlane(FlightData plane)
    {
        if (plane == null) { ShowEmpty(); return; }

        SetVisible(true);

        string callsign  = plane.callsign;
        bool   isTransit = plane.targetPosition != Vector2.zero && string.IsNullOrEmpty(plane.assignedRunway);

        // ── Callsign ──
        if (callsignText) callsignText.text = callsign;

        // ── Тип воздушного судна ──
        string typeLabel = isTransit ? "TRANSIT AIRCRAFT" :
                           (plane.isTakingOff || plane.isDeparting) ? "DEPARTING AIRCRAFT" : "INBOUND AIRCRAFT";
        if (aircraftTypeText) aircraftTypeText.text = typeLabel;

        // ── Speed ──
        if (speedValue) speedValue.text = $"SPEED        {plane.speed * 10f:0} KTS";

        // ── Cargo ──
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

        // ── Status ──
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

        // ── Fuel ──
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
            // Транзит — прячем топливо
            if (fuelLabelText) fuelLabelText.text = "";
            if (fuelBarFill && fuelBarFill.transform.parent != null) fuelBarFill.transform.parent.gameObject.SetActive(false);
        }
    }

    /// <summary>Показать состояние "нет выбора"</summary>
    public void ShowEmpty()
    {
        SetVisible(false);
    }

    // ─── Вспомогательные ───────────────────────────────────────────────────────

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

        // Если мы в основной игре и есть менеджер - берем точное количество и статус
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == plane.callsignText.text);
            if (fd != null)
            {
                if (!fd.isCargoKnown) { known = false; return "UNKNOWN"; }
                return name == "NONE" ? "NONE" : $"{name} ({fd.cargoAmount}{unit})";
            }
        }

        // Если мы просто тестируем UI в канвасе без других сцен - выводим то что есть в самолете
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

        if (!plane.isCargoKnown) { known = false; return "UNKNOWN"; }
        return name == "NONE" ? "NONE" : $"{name} ({plane.cargoAmount}{unit})";
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "NewAirplane", menuName = "Game Data/Airplane Record")]
public class AirplaneData : ScriptableObject
{
    [Header("Airplane Information")]
    public string airplaneName = "Unknown Aircraft";
    
    [TextArea(3, 10)] 
    public string description = "Enter details about the aircraft here...";
    
    public Sprite airplanePhoto;
}
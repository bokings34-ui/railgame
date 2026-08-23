using UnityEngine;

public enum SectionType
{
    None,
    Engine,
    WaterTank,
    Cargo,
    Production,

}
public class TrainSection : MonoBehaviour
{
    [SerializeField] private SectionType sectionType = SectionType.None;

}

using UnityEngine;

public enum AeratorType
{
    Sphere, Capsule
}

public class AeratorTip : MonoBehaviour
{
    [SerializeField] private AeratorType aeratorType;
    [SerializeField, Range(0, 1000)] private int power;
    
    public Transform Transform => transform;
    public int Power => power;
    public AeratorType AeratorType => aeratorType;
}

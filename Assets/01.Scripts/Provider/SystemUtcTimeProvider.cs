using System;
using UnityEngine;

public sealed class SystemUtcTimeProvider : MonoBehaviour, ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

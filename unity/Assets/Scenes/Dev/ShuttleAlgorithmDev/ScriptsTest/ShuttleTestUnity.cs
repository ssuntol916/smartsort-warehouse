using UnityEngine;

public class ShuttleTestUnity : MonoBehaviour
{
    public class ShuttleTest : Shuttle
    {
        public ShuttleTest(string id) : base(id) { }
    }

    public ShuttleTest shuttleTest;

    public void Initialize(string id)
    {
        shuttleTest = new ShuttleTest(id);
    }
}

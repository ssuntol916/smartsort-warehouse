using UnityEngine;

public class BinTestUnity : MonoBehaviour
{
    public class BinTest : Bin
    {
        public BinTest(string id) : base(id) { }
    }

    public BinTest binTest;

    public void Initialize(string id)
    {
        binTest = new BinTest(id);
    }
}
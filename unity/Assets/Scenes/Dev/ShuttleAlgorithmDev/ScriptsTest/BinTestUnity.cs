using UnityEngine;

public class BinTestUnity : MonoBehaviour
{
    public class BinTest : Bin
    {
        public BinTest(string id) : base(id) { }
    }

    public BinTest binTest;
    Vector3Int _lastFromCell;
    [SerializeField] string _id;
    [SerializeField] bool _onDuty;
    public void Initialize(string id)
    {
        binTest = new BinTest(id);
        _lastFromCell = binTest.FromCell;
    }

    void Update()
    {
        _onDuty = binTest.OnDuty;
        if (binTest == null) return;
        if (binTest.FromCell != _lastFromCell)
        {
            _lastFromCell = binTest.FromCell;
            if (_lastFromCell == new Vector3Int(-1, -1, -1))
            {
                Destroy(gameObject);
                return;
            }
            transform.position = new Vector3(_lastFromCell.x, _lastFromCell.y, _lastFromCell.z);
        }
    }
}
using UnityEngine;

public class ShuttleTestUnity : MonoBehaviour
{
    public class ShuttleTest : Shuttle
    {
        public ShuttleTest(string id) : base(id) { }
    }

    public ShuttleTest shuttleTest;
    Vector3Int _lastFromCell;
    [SerializeField] string _id;
    [SerializeField] bool _onDuty;

    public void Initialize(string id)
    {
        shuttleTest = new ShuttleTest(id);
        _lastFromCell = shuttleTest.FromCell;
    }

    void Update()
    {
        _onDuty = shuttleTest.OnDuty;
        if (shuttleTest == null) return;
        if (shuttleTest.FromCell != _lastFromCell)
        {
            _lastFromCell = shuttleTest.FromCell;
            if (_lastFromCell == new Vector3Int(-1, -1, -1))
            {
                Destroy(gameObject);
                return;
            }
            transform.position = new Vector3(_lastFromCell.x, _lastFromCell.y, _lastFromCell.z);
        }
    }
}

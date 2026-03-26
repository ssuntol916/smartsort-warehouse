using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BinTransferUnity : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject binTestPrefab;
    public GameObject shuttleTestPrefab;

    [Header("Generate")]
    public Button genBin;
    public TMP_InputField genBinInputId;
    public TMP_InputField genBinInputX;
    public TMP_InputField genBinInputZ;
    public TMP_InputField genBinInputY;
    public Button genShuttle;
    public TMP_InputField genShuttleInputId;
    public TMP_InputField genShuttleInputX;
    public TMP_InputField genShuttleInputZ;

    [Header("Degenerate")]
    public Button degenBinId;
    public TMP_InputField degenBinIdInputId;
    public Button degenBinCoord;
    public TMP_InputField degenBinCoordInputX;
    public TMP_InputField degenBinCoordInputZ;
    public TMP_InputField degenBinCoordInputY;
    public Button degenShuttle;
    public TMP_InputField degenShuttleInputId;

    [Header("Transfer")]
    public Button transferXZ;
    public TMP_InputField transferXZInputFromX;
    public TMP_InputField transferXZInputFromZ;
    public TMP_InputField transferXZInputToX;
    public TMP_InputField transferXZInputToZ;

    [Header("Shuttle")]
    public Button shuttleToHome;
    public TMP_InputField shuttleToHomeInputId;

    BinTransfer _binTransfer;
    readonly Dictionary<string, BinTestUnity> _binInstances = new Dictionary<string, BinTestUnity>();
    readonly Dictionary<string, ShuttleTestUnity> _shuttleInstances = new Dictionary<string, ShuttleTestUnity>();

    void Awake()
    {
        _binTransfer = new BinTransfer();
    }

    void Start()
    {
        genBin.onClick.AddListener(OnGenBin);
        genShuttle.onClick.AddListener(OnGenShuttle);
        degenBinId.onClick.AddListener(OnDegenBinId);
        degenBinCoord.onClick.AddListener(OnDegenBinCoord);
        degenShuttle.onClick.AddListener(OnDegenShuttle);
        transferXZ.onClick.AddListener(OnTransferXZ);
        shuttleToHome.onClick.AddListener(OnShuttleToHome);
    }

    // ============================================================
    // Generate
    // ============================================================
    /**@brief
     * @ 1. Id,X,Z만 작성되면 BinTestUnity prefab 생성뒤, BinTest가 Id를 가지도록 하고, BinRegister 메서드에 bin과 X,Z 넣음.
	 * 2. Id,X,Z,Y 가 작성되면 BinTestUnity prefab 생성뒤, BinTest가 Id를 가지도록 하고, BinRegister 메서드에 bin과 X,Z,Y 넣음.
     * 
     */
    void OnGenBin()
    {
        // 입력 검증
        string id = genBinInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] GenBin: Id는 필수 입력입니다.");
            return;
        }
        if (!int.TryParse(genBinInputX.text, out int x) ||
            !int.TryParse(genBinInputZ.text, out int z))
        {
            Debug.LogError("[BinTransferUnity] GenBin: X, Z는 필수 입력입니다.");
            return;
        }

        // BinTest 프리팹을 인스턴스화하고 초기화
        GameObject go = Instantiate(binTestPrefab);
        BinTestUnity binUnity = go.GetComponent<BinTestUnity>();
        binUnity.Initialize(id);

        // BinRegister 호출 (Y 입력 여부에 따라 오버로드된 메서드 선택)
        Vector3Int placed;
        if (int.TryParse(genBinInputY.text, out int y))
            placed = _binTransfer.BinRegister(binUnity.binTest, new Vector3Int(x, y, z));
        else
            placed = _binTransfer.BinRegister(binUnity.binTest, x, z);

        // Bin이 배치된 위치로 GameObject 이동
        go.transform.position = new Vector3(placed.x, placed.y, placed.z);
        _binInstances[id] = binUnity;
    }

    void OnGenShuttle()
    {
        // 입력 검증
        string id = genShuttleInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] GenShuttle: Id는 필수 입력입니다.");
            return;
        }
        if (!int.TryParse(genShuttleInputX.text, out int x) ||
            !int.TryParse(genShuttleInputZ.text, out int z))
        {
            Debug.LogError("[BinTransferUnity] GenShuttle: X, Z는 필수 입력입니다.");
            return;
        }

        // ShuttleTest 프리팹을 인스턴스화하고 초기화
        GameObject go = Instantiate(shuttleTestPrefab);
        ShuttleTestUnity shuttleUnity = go.GetComponent<ShuttleTestUnity>();
        shuttleUnity.Initialize(id);

        // ShuttleRegister 호출
        Vector3Int placed = _binTransfer.ShuttleRegister(shuttleUnity.shuttleTest, x, z);
        go.transform.position = new Vector3(placed.x, placed.y, placed.z);
        _shuttleInstances[id] = shuttleUnity;
    }

    // ============================================================
    // Degenerate
    // ============================================================

    void OnDegenBinId()
    {
        // 입력 검증
        string id = degenBinIdInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] DegenBinId: Id는 필수 입력입니다.");
            return;
        }

        // BinUnregister 호출
        _binTransfer.BinUnregister(id);

        // 해당 Bin이 존재하면 GameObject 제거
        if (_binInstances.TryGetValue(id, out BinTestUnity binUnity))
        {
            Destroy(binUnity.gameObject);
            _binInstances.Remove(id);
        }
    }

    void OnDegenBinCoord()
    {
        if (!int.TryParse(degenBinCoordInputX.text, out int x) ||
            !int.TryParse(degenBinCoordInputZ.text, out int z))
        {
            Debug.LogError("[BinTransferUnity] DegenBinCoord: X, Z는 필수 입력입니다.");
            return;
        }

        string removedId = null;

        if (int.TryParse(degenBinCoordInputY.text, out int y))
        {
            Vector3Int cell = new Vector3Int(x, y, z);
            foreach (var kvp in _binInstances)
            {
                if (kvp.Value.binTest.FromCell == cell)
                {
                    removedId = kvp.Key;
                    break;
                }
            }
            _binTransfer.BinUnregister(cell);
        }
        else
        {
            int maxY = _binTransfer.FindMaxY(x, z);
            foreach (var kvp in _binInstances)
            {
                if (kvp.Value.binTest.FromCell.x == x &&
                    kvp.Value.binTest.FromCell.z == z &&
                    kvp.Value.binTest.FromCell.y == maxY)
                {
                    removedId = kvp.Key;
                    break;
                }
            }
            _binTransfer.BinUnregister(x, z);
        }

        if (removedId != null && _binInstances.TryGetValue(removedId, out BinTestUnity binUnity))
        {
            Destroy(binUnity.gameObject);
            _binInstances.Remove(removedId);
        }
    }

    void OnDegenShuttle()
    {
        string id = degenShuttleInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] DegenShuttle: Id는 필수 입력입니다.");
            return;
        }

        _binTransfer.ShuttleUnregister(id);

        if (_shuttleInstances.TryGetValue(id, out ShuttleTestUnity shuttleUnity))
        {
            Destroy(shuttleUnity.gameObject);
            _shuttleInstances.Remove(id);
        }
    }

    // ============================================================
    // Transfer
    // ============================================================

    void OnTransferXZ()
    {
        if (!int.TryParse(transferXZInputFromX.text, out int fromX) ||
            !int.TryParse(transferXZInputFromZ.text, out int fromZ) ||
            !int.TryParse(transferXZInputToX.text, out int toX) ||
            !int.TryParse(transferXZInputToZ.text, out int toZ))
        {
            Debug.LogError("[BinTransferUnity] TransferXZ: FromX, FromZ, ToX, ToZ는 필수 입력입니다.");
            return;
        }

        _binTransfer.TransferXZ(fromX, fromZ, toX, toZ);
    }

    // ============================================================
    // Shuttle
    // ============================================================

    void OnShuttleToHome()
    {
        string id = shuttleToHomeInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] ShuttleToHome: Id는 필수 입력입니다.");
            return;
        }

        if (_shuttleInstances.TryGetValue(id, out ShuttleTestUnity shuttleUnity))
        {
            _binTransfer.MoveShuttleToHome(shuttleUnity.shuttleTest);
        }
        else
        {
            Debug.LogError($"[BinTransferUnity] ShuttleToHome: id '{id}'에 해당하는 Shuttle을 찾을 수 없습니다.");
        }
    }
}

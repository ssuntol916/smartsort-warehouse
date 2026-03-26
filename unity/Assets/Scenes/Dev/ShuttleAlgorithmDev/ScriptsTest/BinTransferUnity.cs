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

    [Header("Shuttle")]
    public Button shuttleToCell;
    public TMP_InputField shuttleToCellInputId;
    public TMP_InputField shuttleToCellInputToX;
    public TMP_InputField shuttleToCellInputToZ;
    public Button shuttleToHome;
    public TMP_InputField shuttleToHomeInputId;

    [Header("Transfer")]
    public Button transferXZ;
    public TMP_InputField transferXZInputFromX;
    public TMP_InputField transferXZInputFromZ;
    public TMP_InputField transferXZInputToX;
    public TMP_InputField transferXZInputToZ;

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
        shuttleToCell.onClick.AddListener(OnShuttleToCell);
        shuttleToHome.onClick.AddListener(OnShuttleToHome);
    }

    // ============================================================
    // Generate
    // ============================================================
    /**@brief
     * @ 1. Id,X,Z만 작성되면 BinTestUnity prefab 생성뒤, BinTest가 Id를 가지도록 하고, BinRegister 메서드에 bin과 X,Z 넣음.
	 * 2. Id,X,Z,Y 가 작성되면 BinTestUnity prefab 생성뒤, BinTest가 Id를 가지도록 하고, BinRegister 메서드에 bin과 X,Z,Y 넣음.
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
        if (_binInstances.ContainsKey(id))
        {
            Debug.LogError($"[BinTransferUnity] GenBin: Id '{id}'가 이미 존재합니다.");
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

        // Bin이 배치된 위치로 GameObject 이동
        Vector3Int placed;
        try
        {
            placed = _binTransfer.BinRegister(binUnity.binTest, x, z);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BinTransferUnity] GenBin: 등록 실패 → {e.Message}");
            Destroy(go);
            return;
        }
        go.transform.position = new Vector3(placed.x, placed.y, placed.z);
        _binInstances[id] = binUnity;
        genBinInputId.text = "";
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
        if (_shuttleInstances.ContainsKey(id))
        {
            Debug.LogError($"[BinTransferUnity] GenShuttle: Id '{id}'가 이미 존재합니다.");
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
        Vector3Int placed;
        try
        {
            placed = _binTransfer.ShuttleRegister(shuttleUnity.shuttleTest, x, z);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BinTransferUnity] GenShuttle: 등록 실패 → {e.Message}");
            Destroy(go);
            return;
        }
        go.transform.position = new Vector3(placed.x, placed.y, placed.z);
        _shuttleInstances[id] = shuttleUnity;
        genShuttleInputId.text = "";
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
        _binInstances.Remove(id);
        degenBinIdInputId.text = "";
    }

    void OnDegenBinCoord()
    {
        if (!int.TryParse(degenBinCoordInputX.text, out int x) ||
            !int.TryParse(degenBinCoordInputZ.text, out int z))
        {
            Debug.LogError("[BinTransferUnity] DegenBinCoord: X, Z는 필수 입력입니다.");
            return;
        }

        Bin removedBin;

        if (int.TryParse(degenBinCoordInputY.text, out int y))
        {
            Vector3Int cell = new Vector3Int(x, y, z);
            removedBin = _binTransfer.FindBinByCell(cell);
            _binTransfer.BinUnregister(cell);
        }
        else
        {
            removedBin = _binTransfer.FindBinByCell(x, z);
            _binTransfer.BinUnregister(x, z);
        }

        if (removedBin != null)
        {
            _binInstances.Remove(removedBin.Id);
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

        _shuttleInstances.Remove(id);
        degenShuttleInputId.text = "";
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

    void OnShuttleToCell()
    {
        string id = shuttleToCellInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] ShuttleToCell: Id는 필수 입력입니다.");
            return;
        }
        if (!int.TryParse(shuttleToCellInputToX.text, out int toX) ||
            !int.TryParse(shuttleToCellInputToZ.text, out int toZ))
        {
            Debug.LogError("[BinTransferUnity] ShuttleToCell: ToX, ToZ는 필수 입력입니다.");
            return;
        }

        Shuttle shuttle = _binTransfer.FindShuttleById(id);
        if (shuttle == null)
        {
            Debug.LogError($"[BinTransferUnity] ShuttleToCell: id '{id}'에 해당하는 Shuttle을 찾을 수 없습니다.");
            return;
        }

        try
        {
            _binTransfer.MoveShuttleToCell(shuttle, toX, toZ);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BinTransferUnity] ShuttleToCell: 이동 실패 → {e.Message}");
        }
    }

    void OnShuttleToHome()
    {
        string id = shuttleToHomeInputId.text;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[BinTransferUnity] ShuttleToHome: Id는 필수 입력입니다.");
            return;
        }

        Shuttle shuttle = _binTransfer.FindShuttleById(id);
        if (shuttle == null)
        {
            Debug.LogError($"[BinTransferUnity] ShuttleToHome: id '{id}'에 해당하는 Shuttle을 찾을 수 없습니다.");
            return;
        }

        try
        {
            _binTransfer.MoveShuttleToHome(shuttle);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BinTransferUnity] ShuttleToHome: 이동 실패 → {e.Message}");
        }
    }
}

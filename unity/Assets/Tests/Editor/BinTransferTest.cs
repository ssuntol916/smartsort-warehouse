// ============================================================
// 파일명  : BinTransferTest.cs
// 역할    : BinTransfer 클래스 EditMode 테스트
// 위치    : Assets/Tests/Editor/BinTransferTest.cs
// ============================================================
using System;
using NUnit.Framework;
using UnityEngine;

// ============================================================
// 테스트용 구체 클래스 (abstract Bin, Shuttle 구현)
// ============================================================
public class TestBin : Bin
{
    public TestBin(string id) : base(id) { }
}

public class TestShuttle : Shuttle
{
    public TestShuttle(string id) : base(id) { }
}

// ============================================================
// BinTransfer 테스트
// ============================================================
public class BinTransferTest
{
    // ─────────────────────────────────────────────
    // 공통 헬퍼
    // ─────────────────────────────────────────────

    /// <summary>빈 BinTransfer 인스턴스를 반환한다.</summary>
    private BinTransfer NewBT() => new BinTransfer();

    /// <summary>Bin 하나를 (x, z)에 등록한 BinTransfer를 반환한다.</summary>
    private BinTransfer BTWithBin(string binId, int x, int z, out TestBin bin)
    {
        var bt = NewBT();
        bin = new TestBin(binId);
        bt.BinRegister(bin, x, z);
        return bt;
    }

    /// <summary>Shuttle 하나를 (x, z)에 등록한 BinTransfer를 반환한다.</summary>
    private BinTransfer BTWithShuttle(string sId, int x, int z, out TestShuttle shuttle)
    {
        var bt = NewBT();
        shuttle = new TestShuttle(sId);
        bt.ShuttleRegister(shuttle, x, z);
        return bt;
    }

    // ============================================================
    // FindMaxY
    // ============================================================

    [Test]
    public void FindMaxY_EmptyCell_ReturnsMinusOne()
    {
        var bt = NewBT();
        Assert.AreEqual(-1, bt.FindMaxY(0, 0));
    }

    [Test]
    public void FindMaxY_OneBin_ReturnsZero()
    {
        var bt = BTWithBin("B1", 0, 0, out _);
        Assert.AreEqual(0, bt.FindMaxY(0, 0));
    }

    [Test]
    public void FindMaxY_TwoBins_ReturnsOne()
    {
        var bt = NewBT();
        bt.BinRegister(new TestBin("B1"), 0, 0);
        bt.BinRegister(new TestBin("B2"), 0, 0);
        Assert.AreEqual(1, bt.FindMaxY(0, 0));
    }

    [Test]
    public void FindMaxY_ThreeBins_ReturnsTwo()
    {
        var bt = NewBT();
        bt.BinRegister(new TestBin("B1"), 0, 0);
        bt.BinRegister(new TestBin("B2"), 0, 0);
        bt.BinRegister(new TestBin("B3"), 0, 0);
        Assert.AreEqual(2, bt.FindMaxY(0, 0));
    }

    [Test]
    public void FindMaxY_DifferentColumn_ReturnsMinusOne()
    {
        var bt = BTWithBin("B1", 0, 0, out _);
        // (1, 0)에는 Bin이 없으므로 -1
        Assert.AreEqual(-1, bt.FindMaxY(1, 0));
    }

    // ============================================================
    // BinRegister
    // ============================================================

    [Test]
    public void BinRegister_Basic_PlacesAtY0()
    {
        var bt = NewBT();
        var bin = new TestBin("B1");
        Vector3Int placed = bt.BinRegister(bin, 0, 0);
        Assert.AreEqual(new Vector3Int(0, 0, 0), placed);
    }

    [Test]
    public void BinRegister_SecondBin_PlacesAtY1()
    {
        var bt = NewBT();
        bt.BinRegister(new TestBin("B1"), 1, 1);
        var bin2 = new TestBin("B2");
        Vector3Int placed = bt.BinRegister(bin2, 1, 1);
        Assert.AreEqual(new Vector3Int(1, 1, 1), placed);
    }

    [Test]
    public void BinRegister_BinFromCellIsUpdated()
    {
        var bt = NewBT();
        var bin = new TestBin("B1");
        bt.BinRegister(bin, 2, 2);
        Assert.AreEqual(new Vector3Int(2, 0, 2), bin.FromCell);
    }

    [Test]
    public void BinRegister_OutOfRangeX_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.BinRegister(new TestBin("B1"), BinTransfer.gridSize, 0));
    }

    [Test]
    public void BinRegister_OutOfRangeZ_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.BinRegister(new TestBin("B1"), 0, BinTransfer.gridSize));
    }

    [Test]
    public void BinRegister_NegativeX_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.BinRegister(new TestBin("B1"), -1, 0));
    }

    [Test]
    public void BinRegister_FullColumn_ThrowsInvalidOperation()
    {
        var bt = NewBT();
        for (int i = 0; i < BinTransfer.gridSize; i++)
            bt.BinRegister(new TestBin($"B{i}"), 0, 0);
        // 한 칸 더 넣으면 가득 참
        Assert.Throws<InvalidOperationException>(
            () => bt.BinRegister(new TestBin("BX"), 0, 0));
    }

    [Test]
    public void BinRegister_DuplicateId_ThrowsInvalidOperation()
    {
        var bt = BTWithBin("B1", 0, 0, out _);
        Assert.Throws<InvalidOperationException>(
            () => bt.BinRegister(new TestBin("B1"), 1, 0));
    }

    [Test]
    public void BinRegister_CornerCell_PlacesCorrectly()
    {
        var bt = NewBT();
        int max = BinTransfer.gridSize - 1;
        var bin = new TestBin("B1");
        Vector3Int placed = bt.BinRegister(bin, max, max);
        Assert.AreEqual(new Vector3Int(max, 0, max), placed);
    }

    // ============================================================
    // BinUnregister(string)
    // ============================================================

    [Test]
    public void BinUnregister_ById_RemovesBin()
    {
        var bt = BTWithBin("B1", 0, 0, out _);
        bt.BinUnregister("B1");
        Assert.IsNull(bt.FindBinById("B1"));
    }

    [Test]
    public void BinUnregister_ById_ResetsBinFromCell()
    {
        var bt = BTWithBin("B1", 0, 0, out var bin);
        bt.BinUnregister("B1");
        Assert.AreEqual(new Vector3Int(-1, -1, -1), bin.FromCell);
    }

    [Test]
    public void BinUnregister_ById_NonExistent_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(() => bt.BinUnregister("NONE"));
    }

    [Test]
    public void BinUnregister_ById_OnDuty_ThrowsInvalidOperation()
    {
        var bt = BTWithBin("B1", 0, 0, out var bin);
        bin.OnDuty = true;
        Assert.Throws<InvalidOperationException>(() => bt.BinUnregister("B1"));
    }

    // ============================================================
    // BinUnregister(int, int)
    // ============================================================

    [Test]
    public void BinUnregister_ByXZ_RemovesTopBin()
    {
        var bt = NewBT();
        bt.BinRegister(new TestBin("B1"), 0, 0);
        bt.BinRegister(new TestBin("B2"), 0, 0);
        bt.BinUnregister(0, 0);         // 맨 위 B2 제거
        Assert.IsNull(bt.FindBinById("B2"));
        Assert.IsNotNull(bt.FindBinById("B1"));
    }

    [Test]
    public void BinUnregister_ByXZ_EmptyCell_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(() => bt.BinUnregister(0, 0));
    }

    // ============================================================
    // BinUnregister(Vector3Int)
    // ============================================================

    [Test]
    public void BinUnregister_ByVector_RemovesBin()
    {
        var bt = BTWithBin("B1", 1, 2, out _);
        bt.BinUnregister(new Vector3Int(1, 0, 2));
        Assert.IsNull(bt.FindBinById("B1"));
    }

    [Test]
    public void BinUnregister_ByVector_NonExistent_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(
            () => bt.BinUnregister(new Vector3Int(0, 0, 0)));
    }

    // ============================================================
    // LowerUpperBins (BinUnregister 연계 확인)
    // ============================================================

    [Test]
    public void BinUnregister_LowersUpperBins_AfterRemoveBottom()
    {
        var bt = NewBT();
        var b0 = new TestBin("B0");
        var b1 = new TestBin("B1");
        var b2 = new TestBin("B2");
        bt.BinRegister(b0, 0, 0);   // y=0
        bt.BinRegister(b1, 0, 0);   // y=1
        bt.BinRegister(b2, 0, 0);   // y=2

        bt.BinUnregister(new Vector3Int(0, 0, 0));  // 맨 아래 b0 제거

        // b1 → y=0, b2 → y=1로 내려와야 함
        Assert.AreEqual(0, b1.FromCell.y);
        Assert.AreEqual(1, b2.FromCell.y);
    }

    [Test]
    public void BinUnregister_LowersUpperBins_AfterRemoveMiddle()
    {
        var bt = NewBT();
        var b0 = new TestBin("B0");
        var b1 = new TestBin("B1");
        var b2 = new TestBin("B2");
        bt.BinRegister(b0, 0, 0);   // y=0
        bt.BinRegister(b1, 0, 0);   // y=1
        bt.BinRegister(b2, 0, 0);   // y=2

        bt.BinUnregister(new Vector3Int(0, 1, 0));  // 중간 b1 제거

        // b2 → y=1로 내려와야 함. b0 그대로
        Assert.AreEqual(0, b0.FromCell.y);
        Assert.AreEqual(1, b2.FromCell.y);
    }

    [Test]
    public void BinUnregister_OtherColumnsUnaffected()
    {
        var bt = NewBT();
        var bA = new TestBin("A");
        var bB = new TestBin("B");
        bt.BinRegister(bA, 0, 0);   // (0,0,0)
        bt.BinRegister(bB, 1, 0);   // (1,0,0)

        bt.BinUnregister("A");      // (0,0,0) 제거

        // (1,0) 컬럼의 B는 영향 없어야 함
        Assert.AreEqual(new Vector3Int(1, 0, 0), bB.FromCell);
    }

    // ============================================================
    // ShuttleRegister
    // ============================================================

    [Test]
    public void ShuttleRegister_Basic_PlacesAtGridSizeY()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        Assert.AreEqual(BinTransfer.gridSize, shuttle.FromCell.y);
    }

    [Test]
    public void ShuttleRegister_HomeCellSet()
    {
        var bt = BTWithShuttle("S1", 1, 2, out var shuttle);
        Assert.AreEqual(new Vector3Int(1, BinTransfer.gridSize, 2), shuttle.HomeCell);
    }

    [Test]
    public void ShuttleRegister_LiftLevelSet()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        Assert.AreEqual(BinTransfer.gridSize, shuttle.LiftLevel);
    }

    [Test]
    public void ShuttleRegister_OutOfRange_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.ShuttleRegister(new TestShuttle("S1"), BinTransfer.gridSize, 0));
    }

    [Test]
    public void ShuttleRegister_DuplicateId_ThrowsInvalidOperation()
    {
        var bt = BTWithShuttle("S1", 0, 0, out _);
        Assert.Throws<InvalidOperationException>(
            () => bt.ShuttleRegister(new TestShuttle("S1"), 1, 0));
    }

    [Test]
    public void ShuttleRegister_SameCell_ThrowsInvalidOperation()
    {
        var bt = BTWithShuttle("S1", 0, 0, out _);
        Assert.Throws<InvalidOperationException>(
            () => bt.ShuttleRegister(new TestShuttle("S2"), 0, 0));
    }

    // ============================================================
    // ShuttleUnregister
    // ============================================================

    [Test]
    public void ShuttleUnregister_Basic_RemovesShuttle()
    {
        var bt = BTWithShuttle("S1", 0, 0, out _);
        bt.ShuttleUnregister("S1");
        Assert.IsNull(bt.FindShuttleById("S1"));
    }

    [Test]
    public void ShuttleUnregister_ResetsFromCell()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        bt.ShuttleUnregister("S1");
        Assert.AreEqual(new Vector3Int(-1, -1, -1), shuttle.FromCell);
    }

    [Test]
    public void ShuttleUnregister_NonExistent_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(() => bt.ShuttleUnregister("NONE"));
    }

    [Test]
    public void ShuttleUnregister_OnDuty_ThrowsInvalidOperation()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        shuttle.OnDuty = true;
        Assert.Throws<InvalidOperationException>(() => bt.ShuttleUnregister("S1"));
    }

    // ============================================================
    // FindBinById
    // ============================================================

    [Test]
    public void FindBinById_Exists_ReturnsCorrectBin()
    {
        var bt = BTWithBin("B1", 0, 0, out var bin);
        Assert.AreEqual(bin, bt.FindBinById("B1"));
    }

    [Test]
    public void FindBinById_NotExists_ReturnsNull()
    {
        var bt = NewBT();
        Assert.IsNull(bt.FindBinById("NONE"));
    }

    // ============================================================
    // FindBinByCell
    // ============================================================

    [Test]
    public void FindBinByCell_XYZ_ReturnsCorrectBin()
    {
        var bt = BTWithBin("B1", 1, 2, out var bin);
        Assert.AreEqual(bin, bt.FindBinByCell(1, 0, 2));
    }

    [Test]
    public void FindBinByCell_Vector_ReturnsCorrectBin()
    {
        var bt = BTWithBin("B1", 0, 0, out var bin);
        Assert.AreEqual(bin, bt.FindBinByCell(new Vector3Int(0, 0, 0)));
    }

    [Test]
    public void FindBinByCell_XZ_ReturnsTopBin()
    {
        var bt = NewBT();
        bt.BinRegister(new TestBin("B1"), 0, 0);
        var b2 = new TestBin("B2");
        bt.BinRegister(b2, 0, 0);   // y=1 (맨 위)
        Assert.AreEqual(b2, bt.FindBinByCell(0, 0));
    }

    [Test]
    public void FindBinByCell_WrongCoord_ReturnsNull()
    {
        var bt = BTWithBin("B1", 0, 0, out _);
        Assert.IsNull(bt.FindBinByCell(1, 0, 0));
    }

    // ============================================================
    // FindShuttleById
    // ============================================================

    [Test]
    public void FindShuttleById_Exists_ReturnsCorrectShuttle()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        Assert.AreEqual(shuttle, bt.FindShuttleById("S1"));
    }

    [Test]
    public void FindShuttleById_NotExists_ReturnsNull()
    {
        var bt = NewBT();
        Assert.IsNull(bt.FindShuttleById("NONE"));
    }

    // ============================================================
    // FindShuttleByXZ
    // ============================================================

    [Test]
    public void FindShuttleByXZ_Exists_ReturnsShuttle()
    {
        var bt = BTWithShuttle("S1", 1, 2, out var shuttle);
        Assert.AreEqual(shuttle, bt.FindShuttleByXZ(1, 2));
    }

    [Test]
    public void FindShuttleByXZ_NotExists_ReturnsNull()
    {
        var bt = NewBT();
        Assert.IsNull(bt.FindShuttleByXZ(0, 0));
    }

    [Test]
    public void FindShuttleByXZ_Exclude_ReturnsNull()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        // 자기 자신을 exclude하면 null
        Assert.IsNull(bt.FindShuttleByXZ(0, 0, shuttle));
    }

    // ============================================================
    // MoveShuttleToCell
    // ============================================================

    [Test]
    public void MoveShuttleToCell_MovesShuttleXZ()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        bt.MoveShuttleToCell(shuttle, 2, 2);
        Assert.AreEqual(2, shuttle.FromCell.x);
        Assert.AreEqual(2, shuttle.FromCell.z);
    }

    [Test]
    public void MoveShuttleToCell_SamePosition_NoChange()
    {
        var bt = BTWithShuttle("S1", 1, 1, out var shuttle);
        bt.MoveShuttleToCell(shuttle, 1, 1);
        Assert.AreEqual(1, shuttle.FromCell.x);
        Assert.AreEqual(1, shuttle.FromCell.z);
    }

    [Test]
    public void MoveShuttleToCell_OccupiedByOther_ThrowsInvalidOperation()
    {
        var bt = NewBT();
        var s1 = new TestShuttle("S1");
        var s2 = new TestShuttle("S2");
        bt.ShuttleRegister(s1, 0, 0);
        bt.ShuttleRegister(s2, 1, 0);
        // s1을 s2가 있는 곳으로 이동하면 예외
        Assert.Throws<InvalidOperationException>(
            () => bt.MoveShuttleToCell(s1, 1, 0));
    }

    [Test]
    public void MoveShuttleToCell_ByVector_MovesShuttle()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        bt.MoveShuttleToCell(shuttle, new Vector3Int(2, 99, 2));  // y는 무시
        Assert.AreEqual(2, shuttle.FromCell.x);
        Assert.AreEqual(2, shuttle.FromCell.z);
    }

    [Test]
    public void MoveShuttleToCell_ById_MovesShuttle()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        bt.MoveShuttleToCell("S1", 2, 1);
        Assert.AreEqual(2, shuttle.FromCell.x);
        Assert.AreEqual(1, shuttle.FromCell.z);
    }

    [Test]
    public void MoveShuttleToCell_ById_NonExistent_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(
            () => bt.MoveShuttleToCell("NONE", 0, 0));
    }

    [Test]
    public void MoveShuttleToCell_ByFromXZ_MovesShuttle()
    {
        var bt = BTWithShuttle("S1", 0, 0, out var shuttle);
        bt.MoveShuttleToCell(0, 0, 2, 2);
        Assert.AreEqual(2, shuttle.FromCell.x);
        Assert.AreEqual(2, shuttle.FromCell.z);
    }

    [Test]
    public void MoveShuttleToCell_ByFromXZ_NoShuttle_ThrowsArgumentException()
    {
        var bt = NewBT();
        Assert.Throws<System.ArgumentException>(
            () => bt.MoveShuttleToCell(0, 0, 1, 1));
    }

    // ============================================================
    // MoveShuttleToHome
    // ============================================================

    [Test]
    public void MoveShuttleToHome_ReturnsToHomeCell()
    {
        var bt = BTWithShuttle("S1", 1, 1, out var shuttle);
        bt.MoveShuttleToCell(shuttle, 2, 2);        // 이동
        bt.MoveShuttleToHome(shuttle);              // 홈으로 복귀
        Assert.AreEqual(shuttle.HomeCell.x, shuttle.FromCell.x);
        Assert.AreEqual(shuttle.HomeCell.z, shuttle.FromCell.z);
    }

    // ============================================================
    // TransferXZ
    // ============================================================

    [Test]
    public void TransferXZ_Basic_MovesBinToDestination()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        var bin = new TestBin("B1");
        bt.BinRegister(bin, 0, 0);

        Vector3Int dest = bt.TransferXZ(0, 0, 2, 2);

        Assert.AreEqual(new Vector3Int(2, 0, 2), dest);
        Assert.AreEqual(new Vector3Int(2, 0, 2), bin.FromCell);
    }

    [Test]
    public void TransferXZ_BinOnDutyReset_AfterTransfer()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        var bin = new TestBin("B1");
        bt.BinRegister(bin, 0, 0);

        bt.TransferXZ(0, 0, 2, 2);
        Assert.IsFalse(bin.OnDuty);
    }

    [Test]
    public void TransferXZ_ShuttleOnDutyReset_AfterTransfer()
    {
        var bt = NewBT();
        var shuttle = new TestShuttle("S1");
        bt.ShuttleRegister(shuttle, 0, 0);
        bt.BinRegister(new TestBin("B1"), 0, 0);

        bt.TransferXZ(0, 0, 2, 2);
        Assert.IsFalse(shuttle.OnDuty);
    }

    [Test]
    public void TransferXZ_StacksOnExistingBin()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        bt.BinRegister(new TestBin("B1"), 0, 0);
        var b2 = new TestBin("B2");
        bt.BinRegister(b2, 2, 2);   // 목적지에 이미 Bin 있음

        Vector3Int dest = bt.TransferXZ(0, 0, 2, 2);
        Assert.AreEqual(1, dest.y);     // y=0 위에 쌓여 y=1
    }

    [Test]
    public void TransferXZ_FromOutOfRange_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.TransferXZ(BinTransfer.gridSize, 0, 0, 0));
    }

    [Test]
    public void TransferXZ_ToOutOfRange_ThrowsIndexOutOfRange()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        bt.BinRegister(new TestBin("B1"), 0, 0);
        Assert.Throws<System.IndexOutOfRangeException>(
            () => bt.TransferXZ(0, 0, BinTransfer.gridSize, 0));
    }

    [Test]
    public void TransferXZ_NoBinAtSource_ThrowsArgumentException()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        Assert.Throws<System.ArgumentException>(
            () => bt.TransferXZ(0, 0, 1, 1));
    }

    [Test]
    public void TransferXZ_DestinationFull_ThrowsInvalidOperation()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        bt.BinRegister(new TestBin("SRC"), 0, 0);
        // 목적지를 가득 채운다
        for (int i = 0; i < BinTransfer.gridSize; i++)
            bt.BinRegister(new TestBin($"D{i}"), 1, 1);
        Assert.Throws<InvalidOperationException>(
            () => bt.TransferXZ(0, 0, 1, 1));
    }

    [Test]
    public void TransferXZ_SourceBinOnDuty_ThrowsInvalidOperation()
    {
        var bt = NewBT();
        bt.ShuttleRegister(new TestShuttle("S1"), 0, 0);
        var bin = new TestBin("B1");
        bt.BinRegister(bin, 0, 0);
        bin.OnDuty = true;
        Assert.Throws<InvalidOperationException>(
            () => bt.TransferXZ(0, 0, 2, 2));
    }

    [Test]
    public void TransferXZ_NoAvailableShuttle_ThrowsInvalidOperation()
    {
        var bt = NewBT();
        var shuttle = new TestShuttle("S1");
        bt.ShuttleRegister(shuttle, 0, 0);
        bt.BinRegister(new TestBin("B1"), 0, 0);
        shuttle.OnDuty = true;  // 모든 셔틀 작업 중
        Assert.Throws<InvalidOperationException>(
            () => bt.TransferXZ(0, 0, 2, 2));
    }
}

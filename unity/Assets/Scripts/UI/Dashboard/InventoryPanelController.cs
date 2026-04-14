// ============================================================
// 파일명  : InventoryPanelController.cs
// 역할    : 스마트 창고 대시보드 UI 컨트롤러
//           작업현황 / 재고현황 / 입고내역 / 출고내역 View 전환 및 데이터 표시
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력: 
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelController : MonoBehaviour
{
    // ───────────────────────────── 색상 상수 ─────────────────────────────
    private static readonly Color32 ColorRowEven = new Color32(52, 73, 94, 255);  // 행 배경 (짝수)
    private static readonly Color32 ColorRowOdd = new Color32(44, 62, 80, 255);  // 행 배경 (홀수)
    private static readonly Color32 ColorBtnActive = new Color32(26, 37, 53, 255);  // 활성 버튼
    private static readonly Color32 ColorBtnInactive = new Color32(52, 73, 94, 255);  // 비활성 버튼

    // ───────────────────────────── 레이아웃 상수 ─────────────────────────────
    private const float RowHeight = 40f;  // Row 높이 (px)
    private const float ScrollbarWidth = 20f;  // 스크롤바 너비 (px)

    // ───────────────────────────── SerializeField ─────────────────────────────
    [SerializeField] private GameObject _workStatusView;                          // 작업현황 View

    [SerializeField] private RectTransform _tableHeaderRect;                      // 재고현황 TableHeader (스크롤바 패딩 조정용)
    [SerializeField] private RectTransform _inboundHeaderRect;                    // 입고내역 TableHeader (스크롤바 패딩 조정용)
    [SerializeField] private RectTransform _outboundHeaderRect;                   // 출고내역 TableHeader (스크롤바 패딩 조정용)

    [SerializeField] private Button _homeBtn;                                     // 홈 버튼
    [SerializeField] private Button _inventoryBtn;                                // 재고현황 버튼
    [SerializeField] private Button _inboundBtn;                                  // 입고내역 버튼
    [SerializeField] private Button _outboundBtn;                                 // 출고내역 버튼

    [SerializeField] private GameObject _inventoryView;                           // 재고현황 View
    [SerializeField] private GameObject _inboundView;                             // 입고내역 View
    [SerializeField] private GameObject _outboundView;                            // 출고내역 View

    [SerializeField] private GameObject _inventoryRowPrefab;                  // 재고현황 Row 프리팹
    [SerializeField] private Transform _content;                             // 재고현황 Content
    [SerializeField] private TMP_InputField _searchInput;                         // 재고현황 검색 입력
    [SerializeField] private TMP_Dropdown _filterDropdown;                      // 재고현황 필터 드롭다운

    [SerializeField] private Button _colPartNameBtn;                              // 부품명 정렬 버튼
    [SerializeField] private Button _colTotalQtyBtn;                              // 제품 총 수량 정렬 버튼
    [SerializeField] private Button _colWeightBtn;                                // 총 중량 정렬 버튼
    [SerializeField] private Button _colBinIdBtn;                                 // BinID 정렬 버튼
    [SerializeField] private Button _colQrBtn;                                    // QR코드 정렬 버튼
    [SerializeField] private Button _colCategoryBtn;                              // 카테고리 정렬 버튼
    [SerializeField] private Button _colBoxCountBtn;                              // BOX 수량 정렬 버튼
    [SerializeField] private Button _colBoxQtyBtn;                                // BOX당 제품수량 정렬 버튼
    [SerializeField] private Button _colPositionBtn;                              // 위치 정렬 버튼

    [SerializeField] private TextMeshProUGUI _colPartNameText;                    // 부품명 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colTotalQtyText;                    // 제품 총 수량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colWeightText;                      // 총 중량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colBinIdText;                       // BinID 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colQrText;                          // QR코드 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colCategoryText;                    // 카테고리 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colBoxCountText;                    // BOX 수량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colBoxQtyText;                      // BOX당 제품수량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _colPositionText;                    // 위치 헤더 텍스트 (▲▼)

    [SerializeField] private GameObject _transactionRowPrefab;                // 입고/출고 Row 프리팹
    [SerializeField] private Transform _inboundContent;                      // 입고내역 Content
    [SerializeField] private Transform _outboundContent;                     // 출고내역 Content
    [SerializeField] private TMP_InputField _inboundSearchInput;                  // 입고내역 검색 입력
    [SerializeField] private TMP_InputField _outboundSearchInput;                 // 출고내역 검색 입력
    [SerializeField] private TMP_Dropdown _inboundFilterDropdown;               // 입고내역 필터 드롭다운
    [SerializeField] private TMP_Dropdown _outboundFilterDropdown;              // 출고내역 필터 드롭다운

    [SerializeField] private Button _inColTransactionNoBtn;                       // 입고번호 정렬 버튼
    [SerializeField] private Button _inColQrBtn;                                  // QR코드 정렬 버튼
    [SerializeField] private Button _inColPartNameBtn;                            // 부품명 정렬 버튼
    [SerializeField] private Button _inColCategoryBtn;                            // 카테고리 정렬 버튼
    [SerializeField] private Button _inColBoxQtyBtn;                              // 박스당 수량 정렬 버튼
    [SerializeField] private Button _inColBinIdBtn;                               // BinID 정렬 버튼
    [SerializeField] private Button _inColStatusBtn;                              // 입고 상태 정렬 버튼
    [SerializeField] private Button _inColDateTimeBtn;                            // 입고 시각 정렬 버튼

    [SerializeField] private TextMeshProUGUI _inColTransactionNoText;             // 입고번호 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColQrText;                        // QR코드 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColPartNameText;                  // 부품명 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColCategoryText;                  // 카테고리 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColBoxQtyText;                    // 박스당 수량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColBinIdText;                     // BinID 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColStatusText;                    // 입고 상태 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _inColDateTimeText;                  // 입고 시각 헤더 텍스트 (▲▼)

    [SerializeField] private Button _outColTransactionNoBtn;                      // 출고번호 정렬 버튼
    [SerializeField] private Button _outColQrBtn;                                 // QR코드 정렬 버튼
    [SerializeField] private Button _outColPartNameBtn;                           // 부품명 정렬 버튼
    [SerializeField] private Button _outColCategoryBtn;                           // 카테고리 정렬 버튼
    [SerializeField] private Button _outColBoxQtyBtn;                             // 박스당 수량 정렬 버튼
    [SerializeField] private Button _outColBinIdBtn;                              // BinID 정렬 버튼
    [SerializeField] private Button _outColStatusBtn;                             // 출고 상태 정렬 버튼
    [SerializeField] private Button _outColDateTimeBtn;                           // 출고 시각 정렬 버튼

    [SerializeField] private TextMeshProUGUI _outColTransactionNoText;            // 출고번호 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColQrText;                       // QR코드 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColPartNameText;                 // 부품명 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColCategoryText;                 // 카테고리 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColBoxQtyText;                   // 박스당 수량 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColBinIdText;                    // BinID 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColStatusText;                   // 출고 상태 헤더 텍스트 (▲▼)
    [SerializeField] private TextMeshProUGUI _outColDateTimeText;                 // 출고 시각 헤더 텍스트 (▲▼)

    // ───────────────────────────── 런타임 데이터 ─────────────────────────────

    private List<InventoryData> _dummyData = new List<InventoryData>();    // 재고현황 더미 데이터 - TODO: Supabase 연동
    private List<TransactionData> _inboundDummyData = new List<TransactionData>(); // 입고 더미 데이터 - TODO: Supabase 연동
    private List<TransactionData> _outboundDummyData = new List<TransactionData>(); // 출고 더미 데이터 - TODO: Supabase 연동

    private string _currentSortColumn = "";    // 재고현황 현재 정렬 컬럼
    private int _sortState = 0;     // 재고현황 정렬 상태 (0=기본, 1=오름차순, 2=내림차순)

    private string _inboundSortColumn = "";    // 입고내역 현재 정렬 컬럼
    private int _inboundSortState = 0;     // 입고내역 정렬 상태
    private string _outboundSortColumn = "";    // 출고내역 현재 정렬 컬럼
    private int _outboundSortState = 0;     // 출고내역 정렬 상태

    private string _currentFilter = "전체";   // 재고현황 현재 필터
    private string _currentKeyword = "";        // 재고현황 현재 검색어
    private string _inboundFilter = "전체";   // 입고내역 현재 필터
    private string _inboundKeyword = "";        // 입고내역 현재 검색어
    private string _outboundFilter = "전체";  // 출고내역 현재 필터
    private string _outboundKeyword = "";       // 출고내역 현재 검색어

    // ───────────────────────────── Unity 생명주기 ─────────────────────────────

    /**
     * @brief 초기화 - 각 View 초기화 후 WorkStatusView 활성화
     */
    void Awake()
    {
        InitMenuButtons();
        InitInventoryView();
        InitInboundView();
        InitOutboundView();

        HideAllViews();
        _workStatusView.SetActive(true);
    }

    // ───────────────────────────── 초기화 ─────────────────────────────

    /**
     * @brief 메뉴 버튼 클릭 이벤트 등록
     */
    private void InitMenuButtons()
    {
        _homeBtn.onClick.AddListener(OnHomeBtnClicked);
        _inventoryBtn.onClick.AddListener(OnInventoryBtnClicked);
        _inboundBtn.onClick.AddListener(OnInboundBtnClicked);
        _outboundBtn.onClick.AddListener(OnOutboundBtnClicked);
    }

    /**
     * @brief 재고현황 View 초기화 - 더미데이터 / 검색 / 필터 / 정렬
     */
    private void InitInventoryView()
    {
        // TODO: Supabase parts / inventories / bins 테이블 연동으로 교체
        _dummyData.Add(new InventoryData { binId = "A-1-1", qrCode = "BOX-001", partName = "클립", category = "문구", boxCount = 2, boxQty = 50, totalQty = 100, weight = 0.5f, position = "1-1-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-2", qrCode = "BOX-002", partName = "가위", category = "문구", boxCount = 3, boxQty = 30, totalQty = 90, weight = 1.2f, position = "1-1-2" });
        _dummyData.Add(new InventoryData { binId = "A-2-1", qrCode = "BOX-003", partName = "테이프", category = "생활용품", boxCount = 5, boxQty = 20, totalQty = 100, weight = 0.8f, position = "1-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-1", qrCode = "BOX-004", partName = "볼펜", category = "문구", boxCount = 4, boxQty = 100, totalQty = 400, weight = 0.3f, position = "2-1-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-2", qrCode = "BOX-005", partName = "지우개", category = "문구", boxCount = 6, boxQty = 40, totalQty = 240, weight = 0.3f, position = "2-1-2" });
        _dummyData.Add(new InventoryData { binId = "B-2-1", qrCode = "BOX-006", partName = "칫솔", category = "생활용품", boxCount = 3, boxQty = 24, totalQty = 72, weight = 0.4f, position = "2-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-2-2", qrCode = "BOX-007", partName = "샴푸", category = "생활용품", boxCount = 2, boxQty = 12, totalQty = 24, weight = 3.5f, position = "2-2-2" });
        _dummyData.Add(new InventoryData { binId = "C-1-1", qrCode = "BOX-008", partName = "건전지", category = "전자", boxCount = 4, boxQty = 20, totalQty = 80, weight = 1.0f, position = "3-1-1" });
        _dummyData.Add(new InventoryData { binId = "C-1-2", qrCode = "BOX-009", partName = "이어폰", category = "전자", boxCount = 3, boxQty = 10, totalQty = 30, weight = 0.5f, position = "3-1-2" });
        _dummyData.Add(new InventoryData { binId = "C-2-1", qrCode = "BOX-010", partName = "마스크", category = "생활용품", boxCount = 5, boxQty = 50, totalQty = 250, weight = 0.2f, position = "3-2-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-1", qrCode = "BOX-011", partName = "클립", category = "문구", boxCount = 2, boxQty = 50, totalQty = 100, weight = 0.5f, position = "1-1-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-2", qrCode = "BOX-012", partName = "가위", category = "문구", boxCount = 3, boxQty = 30, totalQty = 90, weight = 1.2f, position = "1-1-2" });
        _dummyData.Add(new InventoryData { binId = "A-2-1", qrCode = "BOX-013", partName = "테이프", category = "생활용품", boxCount = 5, boxQty = 20, totalQty = 100, weight = 0.8f, position = "1-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-1", qrCode = "BOX-014", partName = "볼펜", category = "문구", boxCount = 4, boxQty = 100, totalQty = 400, weight = 0.3f, position = "2-1-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-2", qrCode = "BOX-015", partName = "지우개", category = "문구", boxCount = 6, boxQty = 40, totalQty = 240, weight = 0.3f, position = "2-1-2" });
        _dummyData.Add(new InventoryData { binId = "B-2-1", qrCode = "BOX-016", partName = "칫솔", category = "생활용품", boxCount = 3, boxQty = 24, totalQty = 72, weight = 0.4f, position = "2-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-2-2", qrCode = "BOX-017", partName = "샴푸", category = "생활용품", boxCount = 2, boxQty = 12, totalQty = 24, weight = 3.5f, position = "2-2-2" });
        _dummyData.Add(new InventoryData { binId = "C-1-1", qrCode = "BOX-018", partName = "건전지", category = "전자", boxCount = 4, boxQty = 20, totalQty = 80, weight = 1.0f, position = "3-1-1" });
        _dummyData.Add(new InventoryData { binId = "C-1-2", qrCode = "BOX-019", partName = "이어폰", category = "전자", boxCount = 3, boxQty = 10, totalQty = 30, weight = 0.5f, position = "3-1-2" });
        _dummyData.Add(new InventoryData { binId = "C-2-1", qrCode = "BOX-020", partName = "마스크", category = "생활용품", boxCount = 5, boxQty = 50, totalQty = 250, weight = 0.2f, position = "3-2-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-1", qrCode = "BOX-021", partName = "클립", category = "문구", boxCount = 2, boxQty = 50, totalQty = 100, weight = 0.5f, position = "1-1-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-2", qrCode = "BOX-022", partName = "가위", category = "문구", boxCount = 3, boxQty = 30, totalQty = 90, weight = 1.2f, position = "1-1-2" });
        _dummyData.Add(new InventoryData { binId = "A-2-1", qrCode = "BOX-023", partName = "테이프", category = "생활용품", boxCount = 5, boxQty = 20, totalQty = 100, weight = 0.8f, position = "1-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-1", qrCode = "BOX-024", partName = "볼펜", category = "문구", boxCount = 4, boxQty = 100, totalQty = 400, weight = 0.3f, position = "2-1-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-2", qrCode = "BOX-025", partName = "지우개", category = "문구", boxCount = 6, boxQty = 40, totalQty = 240, weight = 0.3f, position = "2-1-2" });
        _dummyData.Add(new InventoryData { binId = "B-2-1", qrCode = "BOX-026", partName = "칫솔", category = "생활용품", boxCount = 3, boxQty = 24, totalQty = 72, weight = 0.4f, position = "2-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-2-2", qrCode = "BOX-027", partName = "샴푸", category = "생활용품", boxCount = 2, boxQty = 12, totalQty = 24, weight = 3.5f, position = "2-2-2" });
        _dummyData.Add(new InventoryData { binId = "C-1-1", qrCode = "BOX-028", partName = "건전지", category = "전자", boxCount = 4, boxQty = 20, totalQty = 80, weight = 1.0f, position = "3-1-1" });
        _dummyData.Add(new InventoryData { binId = "C-1-2", qrCode = "BOX-029", partName = "이어폰", category = "전자", boxCount = 3, boxQty = 10, totalQty = 30, weight = 0.5f, position = "3-1-2" });
        _dummyData.Add(new InventoryData { binId = "C-2-1", qrCode = "BOX-030", partName = "마스크", category = "생활용품", boxCount = 5, boxQty = 50, totalQty = 250, weight = 0.2f, position = "3-2-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-1", qrCode = "BOX-031", partName = "클립", category = "문구", boxCount = 2, boxQty = 50, totalQty = 100, weight = 0.5f, position = "1-1-1" });
        _dummyData.Add(new InventoryData { binId = "A-1-2", qrCode = "BOX-032", partName = "가위", category = "문구", boxCount = 3, boxQty = 30, totalQty = 90, weight = 1.2f, position = "1-1-2" });
        _dummyData.Add(new InventoryData { binId = "A-2-1", qrCode = "BOX-033", partName = "테이프", category = "생활용품", boxCount = 5, boxQty = 20, totalQty = 100, weight = 0.8f, position = "1-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-1", qrCode = "BOX-034", partName = "볼펜", category = "문구", boxCount = 4, boxQty = 100, totalQty = 400, weight = 0.3f, position = "2-1-1" });
        _dummyData.Add(new InventoryData { binId = "B-1-2", qrCode = "BOX-035", partName = "지우개", category = "문구", boxCount = 6, boxQty = 40, totalQty = 240, weight = 0.3f, position = "2-1-2" });
        _dummyData.Add(new InventoryData { binId = "B-2-1", qrCode = "BOX-036", partName = "칫솔", category = "생활용품", boxCount = 3, boxQty = 24, totalQty = 72, weight = 0.4f, position = "2-2-1" });
        _dummyData.Add(new InventoryData { binId = "B-2-2", qrCode = "BOX-037", partName = "샴푸", category = "생활용품", boxCount = 2, boxQty = 12, totalQty = 24, weight = 3.5f, position = "2-2-2" });
        _dummyData.Add(new InventoryData { binId = "C-1-1", qrCode = "BOX-038", partName = "건전지", category = "전자", boxCount = 4, boxQty = 20, totalQty = 80, weight = 1.0f, position = "3-1-1" });
        _dummyData.Add(new InventoryData { binId = "C-1-2", qrCode = "BOX-039", partName = "이어폰", category = "전자", boxCount = 3, boxQty = 10, totalQty = 30, weight = 0.5f, position = "3-1-2" });
        _dummyData.Add(new InventoryData { binId = "C-2-1", qrCode = "BOX-040", partName = "마스크", category = "생활용품", boxCount = 5, boxQty = 50, totalQty = 250, weight = 0.2f, position = "3-2-1" });

        RefreshInventoryRows();

        _searchInput.onValueChanged.AddListener(OnSearchChanged);

        _filterDropdown.ClearOptions();
        _filterDropdown.AddOptions(new List<string> { "전체", "문구", "생활용품", "전자" });
        _filterDropdown.onValueChanged.AddListener(OnFilterChanged);

        _colPartNameBtn.onClick.AddListener(() => SortAndRefresh("partName"));
        _colTotalQtyBtn.onClick.AddListener(() => SortAndRefresh("totalQty"));
        _colWeightBtn.onClick.AddListener(() => SortAndRefresh("weight"));
        _colBinIdBtn.onClick.AddListener(() => SortAndRefresh("binId"));
        _colQrBtn.onClick.AddListener(() => SortAndRefresh("qrCode"));
        _colCategoryBtn.onClick.AddListener(() => SortAndRefresh("category"));
        _colBoxCountBtn.onClick.AddListener(() => SortAndRefresh("boxCount"));
        _colBoxQtyBtn.onClick.AddListener(() => SortAndRefresh("boxQty"));
        _colPositionBtn.onClick.AddListener(() => SortAndRefresh("position"));
    }

    /**
     * @brief 입고내역 View 초기화 - 더미데이터 / 검색 / 필터 / 정렬
     */
    private void InitInboundView()
    {
        // TODO: Supabase transactions(inbound) 테이블 연동으로 교체
        _inboundDummyData.Add(new TransactionData { transactionNo = "IN-001", qrCode = "BOX-001", partName = "클립", category = "문구", boxQty = 50, binId = "A-1-1", status = "완료", dateTime = "2026-04-10 09:00:00" });
        _inboundDummyData.Add(new TransactionData { transactionNo = "IN-002", qrCode = "BOX-002", partName = "가위", category = "문구", boxQty = 30, binId = "A-1-2", status = "완료", dateTime = "2026-04-10 09:30:00" });
        _inboundDummyData.Add(new TransactionData { transactionNo = "IN-003", qrCode = "BOX-003", partName = "테이프", category = "생활용품", boxQty = 20, binId = "A-2-1", status = "진행중", dateTime = "2026-04-10 10:00:00" });
        _inboundDummyData.Add(new TransactionData { transactionNo = "IN-004", qrCode = "BOX-004", partName = "볼펜", category = "문구", boxQty = 100, binId = "B-1-1", status = "완료", dateTime = "2026-04-10 10:30:00" });
        _inboundDummyData.Add(new TransactionData { transactionNo = "IN-005", qrCode = "BOX-005", partName = "지우개", category = "문구", boxQty = 40, binId = "B-1-2", status = "완료", dateTime = "2026-04-10 11:00:00" });

        RefreshTransactionRows(_inboundDummyData, _inboundContent, _inboundFilter, _inboundKeyword, _inboundHeaderRect);

        _inboundSearchInput.onValueChanged.AddListener(keyword =>
        {
            _inboundKeyword = keyword;
            RefreshTransactionRows(_inboundDummyData, _inboundContent, _inboundFilter, _inboundKeyword, _inboundHeaderRect);
        });

        _inboundFilterDropdown.ClearOptions();
        _inboundFilterDropdown.AddOptions(new List<string> { "전체", "문구", "생활용품", "전자" });
        _inboundFilterDropdown.onValueChanged.AddListener(index =>
        {
            _inboundFilter = _inboundFilterDropdown.options[index].text;
            RefreshTransactionRows(_inboundDummyData, _inboundContent, _inboundFilter, _inboundKeyword, _inboundHeaderRect);
        });

        _inColTransactionNoBtn.onClick.AddListener(() => SortTransactionAndRefresh("transactionNo", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColQrBtn.onClick.AddListener(() => SortTransactionAndRefresh("qrCode", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColPartNameBtn.onClick.AddListener(() => SortTransactionAndRefresh("partName", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColCategoryBtn.onClick.AddListener(() => SortTransactionAndRefresh("category", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColBoxQtyBtn.onClick.AddListener(() => SortTransactionAndRefresh("boxQty", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColBinIdBtn.onClick.AddListener(() => SortTransactionAndRefresh("binId", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColStatusBtn.onClick.AddListener(() => SortTransactionAndRefresh("status", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
        _inColDateTimeBtn.onClick.AddListener(() => SortTransactionAndRefresh("dateTime", _inboundDummyData, _inboundContent, ref _inboundSortColumn, ref _inboundSortState, _inboundFilter, _inboundKeyword, _inboundHeaderRect, GetInboundSortIconContext()));
    }

    /**
     * @brief 출고내역 View 초기화 - 더미데이터 / 검색 / 필터 / 정렬
     */
    private void InitOutboundView()
    {
        // TODO: Supabase transactions(outbound) 테이블 연동으로 교체
        _outboundDummyData.Add(new TransactionData { transactionNo = "OUT-001", qrCode = "BOX-001", partName = "클립", category = "문구", boxQty = 50, binId = "A-1-1", status = "완료", dateTime = "2026-04-10 13:00:00" });
        _outboundDummyData.Add(new TransactionData { transactionNo = "OUT-002", qrCode = "BOX-003", partName = "테이프", category = "생활용품", boxQty = 20, binId = "A-2-1", status = "진행중", dateTime = "2026-04-10 13:30:00" });
        _outboundDummyData.Add(new TransactionData { transactionNo = "OUT-003", qrCode = "BOX-006", partName = "칫솔", category = "생활용품", boxQty = 24, binId = "B-2-1", status = "완료", dateTime = "2026-04-10 14:00:00" });
        _outboundDummyData.Add(new TransactionData { transactionNo = "OUT-004", qrCode = "BOX-008", partName = "건전지", category = "전자", boxQty = 20, binId = "C-1-1", status = "완료", dateTime = "2026-04-10 14:30:00" });
        _outboundDummyData.Add(new TransactionData { transactionNo = "OUT-005", qrCode = "BOX-009", partName = "이어폰", category = "전자", boxQty = 10, binId = "C-1-2", status = "신규", dateTime = "2026-04-10 15:00:00" });

        RefreshTransactionRows(_outboundDummyData, _outboundContent, _outboundFilter, _outboundKeyword, _outboundHeaderRect);

        _outboundSearchInput.onValueChanged.AddListener(keyword =>
        {
            _outboundKeyword = keyword;
            RefreshTransactionRows(_outboundDummyData, _outboundContent, _outboundFilter, _outboundKeyword, _outboundHeaderRect);
        });

        _outboundFilterDropdown.ClearOptions();
        _outboundFilterDropdown.AddOptions(new List<string> { "전체", "문구", "생활용품", "전자" });
        _outboundFilterDropdown.onValueChanged.AddListener(index =>
        {
            _outboundFilter = _outboundFilterDropdown.options[index].text;
            RefreshTransactionRows(_outboundDummyData, _outboundContent, _outboundFilter, _outboundKeyword, _outboundHeaderRect);
        });

        _outColTransactionNoBtn.onClick.AddListener(() => SortTransactionAndRefresh("transactionNo", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColQrBtn.onClick.AddListener(() => SortTransactionAndRefresh("qrCode", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColPartNameBtn.onClick.AddListener(() => SortTransactionAndRefresh("partName", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColCategoryBtn.onClick.AddListener(() => SortTransactionAndRefresh("category", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColBoxQtyBtn.onClick.AddListener(() => SortTransactionAndRefresh("boxQty", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColBinIdBtn.onClick.AddListener(() => SortTransactionAndRefresh("binId", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColStatusBtn.onClick.AddListener(() => SortTransactionAndRefresh("status", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
        _outColDateTimeBtn.onClick.AddListener(() => SortTransactionAndRefresh("dateTime", _outboundDummyData, _outboundContent, ref _outboundSortColumn, ref _outboundSortState, _outboundFilter, _outboundKeyword, _outboundHeaderRect, GetOutboundSortIconContext()));
    }

    // ───────────────────────────── 이벤트 핸들러 ─────────────────────────────

    /**
     * @brief View 전환 - WorkStatusView (홈)
     */
    private void OnHomeBtnClicked()
    {
        HideAllViews();
        _workStatusView.SetActive(true);
        SetActiveButton(null);
    }

    /**
     * @brief View 전환 - 재고현황
     */
    private void OnInventoryBtnClicked()
    {
        HideAllViews();
        _inventoryView.SetActive(true);
        SetActiveButton(_inventoryBtn);
        RefreshInventoryRows();
    }

    /**
     * @brief View 전환 - 입고내역
     */
    private void OnInboundBtnClicked()
    {
        HideAllViews();
        _inboundView.SetActive(true);
        SetActiveButton(_inboundBtn);
        RefreshTransactionRows(_inboundDummyData, _inboundContent, _inboundFilter, _inboundKeyword, _inboundHeaderRect);
    }

    /**
     * @brief View 전환 - 출고내역
     */
    private void OnOutboundBtnClicked()
    {
        HideAllViews();
        _outboundView.SetActive(true);
        SetActiveButton(_outboundBtn);
        RefreshTransactionRows(_outboundDummyData, _outboundContent, _outboundFilter, _outboundKeyword, _outboundHeaderRect);
    }

    /**
     * @brief 재고현황 검색 - 부품명 기준
     */
    private void OnSearchChanged(string keyword)
    {
        _currentKeyword = keyword;
        RefreshInventoryRows();
    }

    /**
     * @brief 재고현황 필터 - 카테고리 기준
     */
    private void OnFilterChanged(int index)
    {
        _currentFilter = _filterDropdown.options[index].text;
        RefreshInventoryRows();
    }

    // ───────────────────────────── 데이터 갱신 ─────────────────────────────

    /**
     * @brief 재고현황 Row 갱신 - 필터 + 검색 + 스크롤 패딩 동시 적용
     */
    private void RefreshInventoryRows()
    {
        SpawnFilteredRows(_dummyData, _content, _inventoryRowPrefab,
            item => (_currentFilter == "전체" || item.category == _currentFilter)
                 && item.partName.Contains(_currentKeyword),
            (row, i, item) => row.GetComponent<InventoryRowView>().SetData(i + 1, item));

        AdjustHeaderPadding(_content, _tableHeaderRect);
    }

    /**
     * @brief 입고/출고 Row 갱신 - 필터 + 검색 + 스크롤 패딩 동시 적용
     */
    private void RefreshTransactionRows(List<TransactionData> data, Transform content,
        string filter, string keyword, RectTransform headerRect)
    {
        SpawnFilteredRows(data, content, _transactionRowPrefab,
            item => (filter == "전체" || item.category == filter) && item.partName.Contains(keyword),
            (row, i, item) => row.GetComponent<TransactionRowView>().SetData(i + 1, item));

        AdjustHeaderPadding(content, headerRect);
    }

    /**
     * @brief 검색/필터 Row 생성 공용 메서드
     */
    private void SpawnFilteredRows<T>(List<T> data, Transform content, GameObject prefab,
        System.Func<T, bool> filter, System.Action<GameObject, int, T> setup)
    {
        foreach (Transform child in content) Destroy(child.gameObject);

        int rowIndex = 0;
        foreach (var item in data)
        {
            if (!filter(item)) continue;
            GameObject row = Instantiate(prefab, content);
            setup(row, rowIndex, item);
            row.GetComponent<Image>().color = (rowIndex % 2 == 0) ? ColorRowEven : ColorRowOdd;
            rowIndex++;
        }
    }

    /**
     * @brief Content의 자식 수 기반으로 TableHeader Right 패딩 조정
     *        Row 높이 * 개수가 Viewport 높이를 초과하면 스크롤바 너비만큼 패딩 추가
     */
    private void AdjustHeaderPadding(Transform content, RectTransform headerRect)
    {
        if (headerRect == null) return;

        ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect == null) return;

        float viewportHeight = scrollRect.viewport.rect.height;
        bool needsScroll = content.childCount * RowHeight > viewportHeight;

        Vector2 offset = headerRect.offsetMax;
        offset.x = needsScroll ? -ScrollbarWidth : 0f;
        headerRect.offsetMax = offset;
    }

    // ───────────────────────────── 정렬 ─────────────────────────────

    /**
     * @brief 재고현황 정렬 - 컬럼 클릭 시 기본→오름차순→내림차순 순환
     */
    private void SortAndRefresh(string column)
    {
        if (_currentSortColumn == column) _sortState = (_sortState + 1) % 3;
        else { _currentSortColumn = column; _sortState = 1; }

        if (_sortState == 0)
            _dummyData.Sort((a, b) => a.qrCode.CompareTo(b.qrCode));
        else
        {
            bool asc = _sortState == 1;
            switch (column)
            {
                case "partName": _dummyData.Sort((a, b) => asc ? a.partName.CompareTo(b.partName) : b.partName.CompareTo(a.partName)); break;
                case "totalQty": _dummyData.Sort((a, b) => asc ? a.totalQty.CompareTo(b.totalQty) : b.totalQty.CompareTo(a.totalQty)); break;
                case "weight": _dummyData.Sort((a, b) => asc ? a.weight.CompareTo(b.weight) : b.weight.CompareTo(a.weight)); break;
                case "binId": _dummyData.Sort((a, b) => asc ? a.binId.CompareTo(b.binId) : b.binId.CompareTo(a.binId)); break;
                case "qrCode": _dummyData.Sort((a, b) => asc ? a.qrCode.CompareTo(b.qrCode) : b.qrCode.CompareTo(a.qrCode)); break;
                case "category": _dummyData.Sort((a, b) => asc ? a.category.CompareTo(b.category) : b.category.CompareTo(a.category)); break;
                case "boxCount": _dummyData.Sort((a, b) => asc ? a.boxCount.CompareTo(b.boxCount) : b.boxCount.CompareTo(a.boxCount)); break;
                case "boxQty": _dummyData.Sort((a, b) => asc ? a.boxQty.CompareTo(b.boxQty) : b.boxQty.CompareTo(a.boxQty)); break;
                case "position": _dummyData.Sort((a, b) => asc ? a.position.CompareTo(b.position) : b.position.CompareTo(a.position)); break;
            }
        }

        RefreshInventoryRows();
        UpdateSortIconsGeneric(column, _sortState, GetInventorySortIconContext());
    }

    /**
     * @brief 입고/출고 정렬 공용 - 컬럼 클릭 시 기본→오름차순→내림차순 순환
     */
    private void SortTransactionAndRefresh(string column, List<TransactionData> data, Transform content,
        ref string currentCol, ref int sortState, string filter, string keyword, RectTransform headerRect,
        (TextMeshProUGUI[] texts, string[] labels, string[] columns) ctx)
    {
        if (currentCol == column) sortState = (sortState + 1) % 3;
        else { currentCol = column; sortState = 1; }

        if (sortState == 0)
            data.Sort((a, b) => a.transactionNo.CompareTo(b.transactionNo));
        else
        {
            bool asc = sortState == 1;
            switch (column)
            {
                case "transactionNo": data.Sort((a, b) => asc ? a.transactionNo.CompareTo(b.transactionNo) : b.transactionNo.CompareTo(a.transactionNo)); break;
                case "qrCode": data.Sort((a, b) => asc ? a.qrCode.CompareTo(b.qrCode) : b.qrCode.CompareTo(a.qrCode)); break;
                case "partName": data.Sort((a, b) => asc ? a.partName.CompareTo(b.partName) : b.partName.CompareTo(a.partName)); break;
                case "category": data.Sort((a, b) => asc ? a.category.CompareTo(b.category) : b.category.CompareTo(a.category)); break;
                case "boxQty": data.Sort((a, b) => asc ? a.boxQty.CompareTo(b.boxQty) : b.boxQty.CompareTo(a.boxQty)); break;
                case "binId": data.Sort((a, b) => asc ? a.binId.CompareTo(b.binId) : b.binId.CompareTo(a.binId)); break;
                case "status": data.Sort((a, b) => asc ? a.status.CompareTo(b.status) : b.status.CompareTo(a.status)); break;
                case "dateTime": data.Sort((a, b) => asc ? a.dateTime.CompareTo(b.dateTime) : b.dateTime.CompareTo(a.dateTime)); break;
            }
        }

        RefreshTransactionRows(data, content, filter, keyword, headerRect);
        UpdateSortIconsGeneric(column, sortState, ctx);
    }

    // ───────────────────────────── UI 업데이트 ─────────────────────────────

    /**
     * @brief 정렬 아이콘 업데이트 공용 메서드
     * @param column    현재 정렬 컬럼 키
     * @param sortState 정렬 상태 (0=기본, 1=오름차순, 2=내림차순)
     * @param ctx       (텍스트 배열, 라벨 배열, 컬럼키 배열) 튜플
     */
    private void UpdateSortIconsGeneric(string column, int sortState,
        (TextMeshProUGUI[] texts, string[] labels, string[] columns) ctx)
    {
        for (int i = 0; i < ctx.texts.Length; i++)
            ctx.texts[i].text = ctx.labels[i];

        string icon = sortState == 1 ? " ▲" : sortState == 2 ? " ▼" : "";
        for (int i = 0; i < ctx.columns.Length; i++)
        {
            if (ctx.columns[i] == column)
            {
                ctx.texts[i].text += icon;
                break;
            }
        }
    }

    /**
     * @brief 재고현황 정렬 아이콘 컨텍스트 반환
     */
    private (TextMeshProUGUI[], string[], string[]) GetInventorySortIconContext() =>
    (
        new[] { _colPartNameText, _colTotalQtyText, _colWeightText, _colBinIdText, _colQrText, _colCategoryText, _colBoxCountText, _colBoxQtyText, _colPositionText },
        new[] { "부품명", "제품 총 수량", "총 중량(g)", "BinID", "QR코드", "카테고리", "BOX 수량", "BOX당 제품수량", "위치" },
        new[] { "partName", "totalQty", "weight", "binId", "qrCode", "category", "boxCount", "boxQty", "position" }
    );

    /**
     * @brief 입고내역 정렬 아이콘 컨텍스트 반환
     */
    private (TextMeshProUGUI[], string[], string[]) GetInboundSortIconContext() =>
    (
        new[] { _inColTransactionNoText, _inColQrText, _inColPartNameText, _inColCategoryText, _inColBoxQtyText, _inColBinIdText, _inColStatusText, _inColDateTimeText },
        new[] { "입고번호", "QR코드", "부품명", "카테고리", "박스당 수량", "BinID", "입고 상태", "입고 시각" },
        new[] { "transactionNo", "qrCode", "partName", "category", "boxQty", "binId", "status", "dateTime" }
    );

    /**
     * @brief 출고내역 정렬 아이콘 컨텍스트 반환
     */
    private (TextMeshProUGUI[], string[], string[]) GetOutboundSortIconContext() =>
    (
        new[] { _outColTransactionNoText, _outColQrText, _outColPartNameText, _outColCategoryText, _outColBoxQtyText, _outColBinIdText, _outColStatusText, _outColDateTimeText },
        new[] { "출고번호", "QR코드", "부품명", "카테고리", "박스당 수량", "BinID", "출고 상태", "출고 시각" },
        new[] { "transactionNo", "qrCode", "partName", "category", "boxQty", "binId", "status", "dateTime" }
    );

    /**
     * @brief 모든 View 비활성화
     */
    private void HideAllViews()
    {
        _workStatusView.SetActive(false);
        _inventoryView.SetActive(false);
        _inboundView.SetActive(false);
        _outboundView.SetActive(false);
    }

    /**
     * @brief 활성 버튼 색상 변경
     */
    private void SetActiveButton(Button activeBtn)
    {
        _inventoryBtn.image.color = ColorBtnInactive;
        _inboundBtn.image.color = ColorBtnInactive;
        _outboundBtn.image.color = ColorBtnInactive;

        if (activeBtn != null)
            activeBtn.image.color = ColorBtnActive;
    }

    // ───────────────────────────── 공개 메서드 ─────────────────────────────

    /**
     * @brief 대시보드 패널 토글 - 활성 시 닫기, 비활성 시 WorkStatusView로 열기
     */
    public void ToggleDashboard()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            HideAllViews();
            _workStatusView.SetActive(true);
        }
    }
}
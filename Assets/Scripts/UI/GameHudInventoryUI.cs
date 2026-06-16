// ============================================================
// GameHudInventoryUI.cs
// ============================================================
// 이 스크립트가 하는 일:
// 인벤토리 UI를 관리합니다.
// 아이템을 추가, 제거, 초기화할 수 있습니다.
// ============================================================

using UnityEngine;      // Unity 기본 기능 사용
using UnityEngine.UI;   // Image 컴포넌트 사용
using TMPro;            // TextMeshPro 텍스트 사용

/// <summary>
/// GameHudInventoryUI
///
/// [이 스크립트가 필요한 이유]
/// 플레이어가 가지고 있는 아이템을 보여주기 위해 필요합니다.
/// 인벤토리에서 아이템을 추가, 제거, 확인할 수 있습니다.
///
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - inventorySlots: 인벤토리 슬롯 배열
/// </summary>
public class GameHudInventoryUI : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("인벤토리 설정")]
    [Tooltip("인벤토리 슬롯 오브젝트 배열을 여기에 연결하세요")]
    [SerializeField] private InventorySlot[] inventorySlots;

    [Tooltip("슬롯 최대 개수입니다")]
    [SerializeField] private int maxSlots = 12;

    // ============================================================
    // 인벤토리 슬롯 클래스
    // ============================================================

    /// <summary>
    /// InventorySlot은 하나의 슬롯을 표현하는 클래스입니다.
    /// 각 슬롯은 아이콘, 이름, 수량을 가집니다.
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [Tooltip("슬롯 배경 Image")]
        public Image slotBackground;

        [Tooltip("아이콘 Image")]
        public Image iconImage;

        [Tooltip("아이템 이름 텍스트")]
        public TextMeshProUGUI nameText;

        [Tooltip("아이템 수량 텍스트")]
        public TextMeshProUGUI amountText;

        [Tooltip("이 슬롯에 있는 아이템 이름")]
        public string itemName;

        [Tooltip("이 슬롯에 있는 아이템 수량")]
        public int amount;

        /// <summary>
        /// IsEmpty는 이 슬롯이 비어있는지 확인하는 함수입니다.
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(itemName) || amount <= 0;
        }
    }

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// 인벤토리 슬롯을 초기화합니다.
    /// </summary>
    private void Start()
    {
        InitializeSlots();
    }

    // ============================================================
    // 슬롯 초기화 함수
    // ============================================================

    /// <summary>
    /// InitializeSlots()는 인벤토리 슬롯을 초기화하는 함수입니다.
    /// 빈 슬롯의 아이템 이름과 수량을 비웁니다.
    /// </summary>
    private void InitializeSlots()
    {
        if (inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            slot.itemName = "";
            slot.amount = 0;
            RefreshSlot(slot);
        }
    }

    // ============================================================
    // 아이템 추가 함수
    // ============================================================

    /// <summary>
    /// AddItem()은 인벤토리에 아이템을 추가하는 함수입니다.
    /// 같은 이름의 아이템이 있으면 수량만 증가시킵니다.
    /// 빈 슬롯이 있으면 새 아이템으로 추가합니다.
    ///
    /// [예시]
    /// AddItem("사과", appleSprite, 5);
    /// AddItem("검", swordSprite, 1);
    /// </summary>
    public void AddItem(string itemName, Sprite icon, int amount)
    {
        if (inventorySlots == null) return;

        // 같은 이름의 아이템이 있는지 확인합니다
        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;

            if (slot.itemName == itemName)
            {
                // 같은 아이템이 있으면 수량만 증가시킵니다
                slot.amount += amount;
                RefreshSlot(slot);
                return;
            }
        }

        // 빈 슬롯에 새 아이템을 추가합니다
        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;

            if (slot.IsEmpty())
            {
                slot.itemName = itemName;
                slot.amount = amount;
                if (slot.iconImage != null)
                {
                    slot.iconImage.sprite = icon;
                    slot.iconImage.enabled = true;
                }
                RefreshSlot(slot);
                return;
            }
        }

        Debug.LogWarning("[GameHudInventoryUI] 빈 슬롯이 없습니다.");
    }

    // ============================================================
    // 아이템 제거 함수
    // ============================================================

    /// <summary>
    /// RemoveItem()은 인벤토리에서 아이템을 제거하는 함수입니다.
    /// 수량이 0이 되면 슬롯을 비웁니다.
    ///
    /// [예시]
    /// RemoveItem("사과", 3);  // 사과 3개 제거
    /// </summary>
    public void RemoveItem(string itemName, int amount)
    {
        if (inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;

            if (slot.itemName == itemName)
            {
                slot.amount -= amount;

                if (slot.amount <= 0)
                {
                    // 수량이 0 이하면 슬롯을 비웁니다
                    ClearSlot(slot);
                }
                else
                {
                    RefreshSlot(slot);
                }
                return;
            }
        }
    }

    // ============================================================
    // 인벤토리 전체 비우기 함수
    // ============================================================

    /// <summary>
    /// ClearInventory()는 인벤토리를 모두 비우는 함수입니다.
    /// </summary>
    public void ClearInventory()
    {
        if (inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            ClearSlot(slot);
        }
    }

    // ============================================================
    // 슬롯 갱신 함수들
    // ============================================================

    /// <summary>
    /// RefreshSlot()은 슬롯의 화면 표시를 갱신하는 함수입니다.
    /// </summary>
    private void RefreshSlot(InventorySlot slot)
    {
        if (slot.nameText != null)
        {
            slot.nameText.text = slot.itemName;
        }

        if (slot.amountText != null)
        {
            slot.amountText.text = slot.amount > 0 ? $"x{slot.amount}" : "";
        }
    }

    /// <summary>
    /// ClearSlot()은 슬롯을 비우는 함수입니다.
    /// </summary>
    private void ClearSlot(InventorySlot slot)
    {
        slot.itemName = "";
        slot.amount = 0;

        if (slot.iconImage != null)
        {
            slot.iconImage.sprite = null;
            slot.iconImage.enabled = false;
        }

        RefreshSlot(slot);
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================

    /// <summary>
    /// GetItemCount()는 특정 아이템의 총 수량을 반환합니다.
    /// </summary>
    public int GetItemCount(string itemName)
    {
        if (inventorySlots == null) return 0;

        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            if (slot.itemName == itemName)
            {
                return slot.amount;
            }
        }

        return 0;
    }

    /// <summary>
    /// HasItem()은 특정 아이템이 있는지 확인하는 함수입니다.
    /// </summary>
    public bool HasItem(string itemName)
    {
        return GetItemCount(itemName) > 0;
    }
}

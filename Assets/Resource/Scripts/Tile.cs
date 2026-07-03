// Version: v1.1
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    private Board board;
    private Image tileImage;
    private TextMeshProUGUI tileText;

    public int Numeric { get; private set; }
    public Vector3 CorrectPosition { get; private set; }

    public bool IsCorrected => GetComponent<RectTransform>().localPosition == CorrectPosition;

    /// <summary>
    /// 원본 Board.cs의 SpawnTiles에서 호출하는 초기화 메서드
    /// </summary>
    public void Setup(Board gameBoard, int totalTiles, int tileNumber)
    {
        board = gameBoard;
        Numeric = tileNumber;

        tileImage = GetComponent<Image>();
        tileText = GetComponentInChildren<TextMeshProUGUI>();

        if (tileText != null)
        {
            tileText.text = Numeric.ToString();
            tileText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 원본 Board.cs의 Start에서 호출하는 정답 좌표 저장 메서드
    /// </summary>
    public void SetCorrectPosition()
    {
        CorrectPosition = GetComponent<RectTransform>().localPosition;
    }

    /// <summary>
    /// 원본 Board.cs의 IsMoveTile에서 호출하는 물리 좌표 이동 메서드
    /// </summary>
    public void OnMoveTo(Vector3 goalPosition)
    {
        GetComponent<RectTransform>().localPosition = goalPosition;
    }

    /// <summary>
    /// 타일 클릭 시 Board의 이동 검사 메서드를 호출합니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (board != null)
        {
            board.IsMoveTile(this);
        }
    }
}
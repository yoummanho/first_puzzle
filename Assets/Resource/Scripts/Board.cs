// Version: v1.1
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    [SerializeField]
    private GameObject tilePrefab;                              // 숫자 타일 프리팹
    [SerializeField]
    private Transform tilesParent;                          // 타일이 배치되는 "Board" 오브젝트의 Transform

    [Header("Added for Pre-sliced Sprites & UI Resize")]
    [SerializeField]
    private Sprite[] puzzleSprites;                         // 에디터에서 미리 슬라이싱된 스프라이트 조각 배열
    [SerializeField]
    private Vector2 cellSize = new Vector2(100f, 100f);     // 타일 하나의 크기
    [SerializeField]
    private Vector2 spacing = new Vector2(5f, 5f);          // 타일 간 간격

    private List<Tile> tileList;                                // 생성한 타일 정보 저장

    [SerializeField]
    private Vector2Int puzzleSize = new Vector2Int(5, 9);       // 사용자가 지정할 가로(5), 세로(9) 퍼즐 크기
    private float neighborTileDistance = 105;               // 인접한 타일 사이의 거리 (자동 계산)

    public Vector3 EmptyTilePosition { set; get; }          // 빈 타일의 위치
    public int Playtime { private set; get; } = 0;      // 게임 플레이 시간
    public int MoveCount { private set; get; } = 0; // 이동 횟수

    private bool isShuffling = false;                   // 셔플 중 클릭 방지 플래그

    private IEnumerator Start()
    {
        tileList = new List<Tile>();

        // 1. 격자 칸수에 맞춰 Board(tilesParent) 크기 및 GridLayoutGroup 동적 조절
        RectTransform parentRect = tilesParent.GetComponent<RectTransform>();
        GridLayoutGroup gridLayout = tilesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = tilesParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = puzzleSize.x;

        // 가로, 세로 칸수에 맞춰 tilesParent의 전체 가로/세로 폭(Width, Height) 계산
        float boardWidth = (puzzleSize.x * cellSize.x) + ((puzzleSize.x - 1) * spacing.x);
        float boardHeight = (puzzleSize.y * cellSize.y) + ((puzzleSize.y - 1) * spacing.y);
        parentRect.sizeDelta = new Vector2(boardWidth, boardHeight);

        // 타일 셀 크기와 간격을 바탕으로 기준 이동 거리(상하좌우 인접 거리) 자동 보정
        neighborTileDistance = cellSize.x + spacing.x;

        // 2. 타일 동적 생성 및 에디터 슬라이싱 스프라이트 주입 진행
        SpawnTiles();

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tilesParent.GetComponent<RectTransform>());

        // 현재 프레임이 종료될 때까지 대기
        yield return new WaitForEndOfFrame();

        // tileList에 있는 모든 요소의 SetCorrectPosition() 메소드 호출
        tileList.ForEach(x => x.SetCorrectPosition());

        // 3. 안전한 시뮬레이션 기반 셔플 작동
        yield return StartCoroutine("OnSuffle");

        // 게임시작과 동시에 플레이시간 초 단위 연산
        StartCoroutine("CalculatePlaytime");
    }

    private void SpawnTiles()
    {
        int totalTiles = puzzleSize.x * puzzleSize.y;

        for (int y = 0; y < puzzleSize.y; ++y)
        {
            for (int x = 0; x < puzzleSize.x; ++x)
            {
                GameObject clone = Instantiate(tilePrefab, tilesParent);
                Tile tile = clone.GetComponent<Tile>();

                int index = y * puzzleSize.x + x;
                int tileNumber = index + 1;

                tile.Setup(this, totalTiles, tileNumber);

                UnityEngine.UI.Image image = clone.GetComponent<UnityEngine.UI.Image>();
                if (image != null && puzzleSprites != null && index < puzzleSprites.Length)
                {
                    image.sprite = puzzleSprites[index];
                }

                tileList.Add(tile);

                if (tileNumber == totalTiles)
                {
                    if (image != null) image.enabled = false;
                }
            }
        }
    }

    private IEnumerator OnSuffle()
    {
        RectTransform emptyTileRect = tileList[tileList.Count - 1].GetComponent<RectTransform>();
        EmptyTilePosition = emptyTileRect.localPosition;

        float current = 0;
        float percent = 0;
        float time = 1.5f;

        isShuffling = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / time;

            List<Tile> validMoves = new List<Tile>();
            foreach (Tile tile in tileList)
            {
                if (tile == tileList[tileList.Count - 1]) continue;

                float dist = Vector3.Distance(EmptyTilePosition, tile.GetComponent<RectTransform>().localPosition);
                if (Mathf.Abs(dist - neighborTileDistance) < 0.5f)
                {
                    validMoves.Add(tile);
                }
            }

            if (validMoves.Count > 0)
            {
                Tile randomTile = validMoves[Random.Range(0, validMoves.Count)];
                RectTransform tileRect = randomTile.GetComponent<RectTransform>();

                Vector3 tempPos = emptyTileRect.localPosition;
                emptyTileRect.localPosition = tileRect.localPosition;
                tileRect.localPosition = tempPos;

                EmptyTilePosition = emptyTileRect.localPosition;
            }

            yield return null;
        }

        isShuffling = false;
    }

    public void IsMoveTile(Tile tile)
    {
        if (isShuffling) return;

        if (Mathf.Abs(Vector3.Distance(EmptyTilePosition, tile.GetComponent<RectTransform>().localPosition) - neighborTileDistance) < 0.5f)
        {
            Vector3 goalPosition = EmptyTilePosition;

            EmptyTilePosition = tile.GetComponent<RectTransform>().localPosition;

            tile.OnMoveTo(goalPosition);

            MoveCount++;

            IsGameOver();
        }
    }

    public void IsGameOver()
    {
        List<Tile> tiles = tileList.FindAll(x => x.IsCorrected == true);

        Debug.Log("Correct Count : " + tiles.Count);
        if (tiles.Count == puzzleSize.x * puzzleSize.y - 1)
        {
            Debug.Log("GameClear");
            StopCoroutine("CalculatePlaytime");

            GetComponent<UIController>().OnResultPanel();
        }
    }

    private IEnumerator CalculatePlaytime()
    {
        while (true)
        {
            Playtime++;

            yield return new WaitForSeconds(1);
        }
    }
}
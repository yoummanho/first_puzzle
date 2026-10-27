# 프로젝트 작업 규칙

## 프로젝트 별칭
- `퍼즐게임`, `first_puzzle`: `E:\Unity\Puzzle\first_puzzle`에서만 Git 및 Unity 작업을 수행한다.
- `염_스크립트`: `C:\Users\EthanHome\OneDrive\Desktop\A\automatic`에서만 Git 및 스크립트 작업을 수행한다.
- 두 프로젝트의 변경을 섞지 않는다.

## 커밋 및 일일 진행 기록
- **커밋 메시지(코멘트)는 가능하면 한글로 작성한다** (예: `feat: Phase 3 로비·도감·화면방향 대응 및 경쾌한 배경음악 적용`).
- 이 프로젝트에서 커밋하는 날에는 **매 커밋 전에 이 파일의 작업 기록을 갱신**하고 해당 커밋에 함께 포함한다.
- 기록에는 `YYYY-MM-DD HH:mm KST`, 대상 페이즈, 실제 변경 내용, 커밋 제목, 검증 결과, 남은 작업을 적는다. 같은 날 여러 커밋이면 항목을 추가한다.
- 현재 커밋의 해시는 자기 자신에 포함할 수 없으므로 커밋 제목으로 식별한다. 후속 기록에서 직전 커밋 해시 및 확인된 푸시 결과를 보완한다. 성공 전 푸시 완료로 기록하지 않는다.
- 실제 테스트 결과와 미검증 항목을 구분한다. 요청 없이 커밋/푸시하지 않으며, 요청된 경우 일반 커밋과 푸시를 사용한다.
- 상세 기능 명세는 [Assets/SPEC.md](Assets/SPEC.md)를 따른다.

## 현재 페이즈
| 페이즈 | 범위 | 상태 |
|---|---|---|
| Phase 1 | StageData, 정사각형 타일, 결정론적 셔플, 다중 밀기, Undo, 클리어 연출 | 구현 및 자동 테스트 완료 |
| Phase 2 | 타이머/이동 수, 일시정지 가림막, 미리보기 제한, 3-2-1 재개, 설정 4종, 오디오, 로컬 최고 기록 | 구현 및 자동 테스트 완료 |
| Phase 3 | 타이틀→로비→인게임 흐름, 스테이지 해금, 도감/원본 감상, 세로·가로 선택, Safe Area 및 캐주얼 배경·사운드 분리 | 코어 및 런타임 UI 구현 완료, 자동 테스트 통과 (실기기 및 고도화 마감 진행 중) |
| Phase 4 | 온라인 계정/랭킹, 광고 SDK 및 보상 연동 | 보류(일시 중단). Phase 3 인게임 완성도 마감 우선 |

## 다른 PC / 다른 에이전트 인계 가이드 (Handoff Guide)
1. **브랜치 현황:**
   - 원격 기본 브랜치 `origin/master`(및 작업 브랜치 `origin/wip/home-phase1`)에 Phase 1 ~ Phase 3 작업분이 동기화되어 있다.
   - 작업을 시작할 때 `git pull origin master` (또는 작업 브랜치 확인) 후 `AGENTS.md`와 `Assets/SPEC.md`를 먼저 읽는다.
2. **씬 및 에셋 구조:**
   - 실제 플레이 씬: `Assets/Scenes/PuzzleGame.unity` (`PuzzleGameSceneBuilder.cs`로 생성, 중복을 제거한 24개 고유 스테이지가 3x3부터 칸수 오름차순으로 연결 완료).
   - Phase 1 코어 검증용 씬: `Assets/Scenes/PhaseOneTest.unity` (`PhaseOneSceneBuilder.cs`).
   - 일러스트 원본 경로: `Assets/Resource.art/Puzzle_images` (24종 정리 완료, GIF/WebP는 첫 프레임 PNG와 함께 보존).
   - 캐주얼 배경 경로: `Assets/Resource.art/Backgrounds/bg_puzzle_misty_forest_temp.png` (기존 어두운 안개 숲 배경을 밝고 화사한 파스텔 하늘·초원 캐주얼 배경으로 교체, GUID 유지).
3. **핵심 런타임 스크립트 (`Assets/Resource/Scripts/Puzzle/`):**
   - `StageData.cs`, `PuzzleBoardState.cs`, `PuzzleTile.cs`, `BoardManager.cs`: Phase 1 퍼즐 코어 (정사각형 타일 유지, 중앙 크롭 UV, 결정론적 역방향 셔플, 다중 밀기, Undo 3회 및 보상 티켓 콜백, 타일이 정답 칸에 들어갈 때 `TilePlacedCorrectly` 이벤트 발생, 클리어 페이드인).
   - `SettingsManager.cs`: 5종 설정(`PREF_BGM`, `PREF_SFX`, `PREF_MULTIPUSH`, `PREF_SLIDE_ANIM`, `PREF_ORIENTATION` 세로/가로) 저장/복원 및 `Screen.orientation` 반영.
   - `PuzzleAudio.cs`: `PuzzleGame` 루트 오브젝트에 직렬화 부착되어 Inspector에서 7종 오디오 클립(`titleMusicClip`, `lobbyMusicClip`, `musicClip` 인게임 루프, `gameStartClip`, `moveClip`, `tileCorrectClip` 타일 정답 위치 안착 시 짧고 맑은 벨 소리, `clearClip`)을 직접 교체·편집 가능. 미할당 시 각각 구분되는 기본 합성 사운드 자동 재생.
   - `PuzzleProgress.cs`: 스테이지 해금 규칙(첫 스테이지, 각 난이도 첫 스테이지, 직전 클리어 스테이지 다음 단계 해금) 및 최소 이동/최단 시간(`PUZZLE_CLEAR_`, `PUZZLE_BEST_`, `PUZZLE_BEST_TIME_`) 관리.
   - `PuzzleLobbyGalleryUI.cs`: **타이틀(`TitlePanel`) -> 스테이지 로비(`LobbyPanel`) -> 인게임** 화면 흐름, 난이도 탭(`ALL`, `3x3`, `3x4`, `3x5`, `3x7`, `4x3`, `6x3`)별 스테이지 선택 로비, 클리어 일러스트 도감(미해금 시 불투명 자물쇠 가림막으로 원본 이미지 완전 차단, 첫/마지막 페이지에서 Prev/Next 무한 루프 방지), 크롭 없는 원본 전체화면 감상 모달(`uvRect = 0,0,1,1`).
   - `PuzzleResponsiveLayout.cs`: 모바일 Safe Area(`Screen.safeArea`) 적용 및 세로(`Portrait`) / 가로(`Landscape`) 전용 HUD·보드·일시정지·타이틀·로비·도감 레이아웃 재배치 (인게임 상단 Lobby 중복 버튼 제거).
   - `PauseSettingsUI.cs`, `PuzzleGame.cs`: 타이틀→로비→인게임 진입 흐름(`OpenTitleScreen` → `EnterLobbyFromTitle` → `Load`), 인게임 진입 시 게임 시작 SFX 및 인게임 루프 BGM 전환, 햄버거 아이콘 일시정지 버튼, `GAME` 탭과 `SYSTEM` 탭으로 분리된 일시정지 메뉴 및 `X` 닫기 버튼, 3-2-1 재개 카운트다운, 에디터 창 전환 시 자동 일시정지 방지 처리.
4. **이어서 진행할 작업:**
   - Phase 4(온라인/광고 SDK 연동)는 한동안 보류하고 Phase 3 인게임 완성도 및 UI/사운드 디테일 마감에 집중.
   - 사용자가 Inspector의 `PuzzleAudio` 슬롯(`titleMusicClip`, `lobbyMusicClip`, `musicClip`, `gameStartClip`, `moveClip`, `tileCorrectClip`, `clearClip`)에 원하는 커스텀 오디오 에셋을 연결하여 테스트 가능.

## 작업 기록

### 2026-10-27 00:26 KST — Phase 2 및 이미지 등록 체크포인트
- 커밋 제목: `feat: complete phase two and register puzzle illustrations`
- 기준 커밋: `28f6f27` (2026-10-20 16:23 KST, 실제 플레이용 퍼즐 씬 및 오프라인 게임 흐름 구현).
- 변경: 일시정지/설정/오디오 및 샘플 프레임 적용 작업을 정리. 기존 7개에 새 이미지 21개를 추가하여 플레이 씬에 총 28개 스테이지 연결.
- GIF 3개와 WebP 1개는 첫 프레임 PNG로 연결하고 원본 보존. GIF 중복 스테이지 에셋은 보존하되 플레이 목록에서 제외.
- 이미지 등록 메뉴의 중복 방지 및 테스트 도메인 재로드 후 람다 지역 변수 초기화 문제 수정.
- 검증: Unity 테스트 18/18 통과(2026-10-27 00:18:58 KST), 28개 스테이지 로딩 및 등록 재실행 시 씬 불변 확인.
- 다음 작업: 이미지 실제 비율에 따른 보드 구성, 이미지 파일명 정리, 임시 배경 추가. 이후 Phase 3.
- 푸시: 이 커밋 생성 후 요청된 원격 브랜치로 푸시 예정.

### 2026-10-27 00:33 KST — 이미지 구성 및 배경 보완
- 직전 커밋: `62431ea` (`feat: complete phase two and register puzzle illustrations`, 00:26:29 KST). 00:32 KST에 `origin/wip/home-phase1`로 푸시 성공 확인.
- 커밋 제목: `feat: fit puzzle grids to artwork and add temporary background`
- 페이즈: Phase 1/2 콘텐츠 보완. Phase 3/4는 예정 유지.
- 변경: 이미지 24종을 직접 확인하고 `puzzle_번호_내용`으로 이름 정리. GIF/WebP 원본과 변환 PNG 이름을 통일하고 GUID를 보존.
- 원본 종횡비에 따라 보드 구성 및 신규 등록 정책 변경. 플레이 스테이지 28개: 3x3 4개, 3x4 17개, 3x5 3개, 3x7 1개, 4x3 2개, 6x3 1개. 이미지별 원본 해상도/보드는 [이미지 목록](Assets/Resource.art/Puzzle_images/README.md)에 기록.
- 임시 배경: [bg_puzzle_misty_forest_temp.png](Assets/Resource.art/Backgrounds/bg_puzzle_misty_forest_temp.png). 내장 이미지 생성 도구 사용. 생성 프롬프트는 [배경 설명](Assets/Resource.art/Backgrounds/README.md)에 기록. 화면을 비율 유지로 채우며 터치 차단 해제.
- 검증: Unity 테스트 31/31 통과(00:31:56 KST), 이미지 재등록 중복 없음. 플레이 프레임 진행(7280→9693) 및 720x1280 화면에서 배경/보드/버튼 표시 확인. 에디터 플레이 종료 및 검증용 백그라운드 실행 설정 원복.
- 남은 작업: Phase 3 로비/도감/방향 선택/모바일 레이아웃, Phase 4 온라인/광고 SDK. 실제 기기 화면 검증은 미실시.
- 푸시: 이 커밋 생성 후 동일 원격 브랜치로 푸시 예정. 실제 결과는 작업 완료 응답과 다음 커밋 기록에서 확인.

### 2026-10-27 02:42 KST — Phase 3 로비/도감/방향 전환 구현 및 경쾌한 배경음악 적용
- 직전 커밋: `bac2461` (`feat: fit puzzle grids to artwork and add temporary background`, 00:34:03 KST) 및 `646ae34` (`wip: codex 한도 도달 전 작업분 백업`, 01:29:41 KST).
- 커밋 제목: `feat: Phase 3 로비·도감·화면방향 대응 및 경쾌한 배경음악 적용`
- 페이즈: Phase 2 오디오 개선 + Phase 3 코어/UI 구현.
- 변경:
  - `PuzzleAudio.cs`: 기존 잔잔한 4음 앰비언스를 뿌요뿌요 메인 테마 풍의 밝고 경쾌한 150 BPM 칩튠 아케이드 루프(리드 멜로디 + 스타카토 화음 + 옥타브 워킹 베이스 + 오프비트 하이햇)로 교체하고 타일 이동음/클리어음 개선.
  - `SettingsManager.cs`, `PauseSettingsUI.cs`, `PuzzleGame.cs`: 세로/가로 플레이 선택(`PREF_ORIENTATION`) 및 인게임/일시정지/클리어 결과창의 로비 이동 버튼 연결. 플레이 중 방향 전환 시 일시정지 상태로 보드·이동 수·Undo·시간을 보존하고 3-2-1 카운트다운 후 재개.
  - `PuzzleProgress.cs`, `PuzzleLobbyGalleryUI.cs`, `PuzzleResponsiveLayout.cs`: 스테이지 해금 흐름, 난이도 탭별 스테이지 카드 로비, 클리어 일러스트 도감 및 무크롭 원본 전체화면 뷰어, Safe Area 및 세로/가로 전용 반응형 레이아웃 구현.
  - `AGENTS.md`: 커밋 코멘트 한글 작성 지침 추가 및 타 PC/에이전트 인계 가이드 상세 문서화.
- 검증: Unity 에디터 재컴파일 에러 0건 확인(`pipeline_recompile_status.json`), `PhaseThreeTests` 3/3 통과 확인(2026-10-27 02:15 KST). 경쾌한 BGM 합성 코드 리컴파일 완료(02:42 KST). 실제 모바일 기기 빌드는 미검증.
- 남은 작업: 에디터/실기기에서 세로·가로 UI 시각적 디테일 점검, Phase 4 온라인 리더보드(UGS) 및 광고 SDK 연동.
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`에 푸시 예정.

### 2026-10-27 10:47 KST — 중복 스테이지 정리·난이도순 배치 및 일시정지 탭 UI·포커스 자동일시정지 개선
- 직전 커밋: `deaeea8` (`feat: Phase 3 로비·도감·화면방향 대응 및 경쾌한 배경음악 적용`, 02:42 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: 중복 스테이지 정리·난이도순 정렬 및 일시정지 탭 UI·포커스 개선`
- 페이즈: Phase 2/3 스테이지 구성 및 인게임/일시정지 UI 개편.
- 변경:
  - `Assets/Scenes/PuzzleGame.unity`, `PuzzleStageImporter.cs`: 동일 일러스트를 중복 참조하던 스테이지(`Stage03`, `Stage05`, `Stage06`, `Stage07`)를 플레이 목록에서 제외해 24개 고유 일러스트 스테이지로 정리하고, 기본 입문 난이도인 `3x3`(9칸)부터 `3x4`·`4x3`(12칸), `3x5`(15칸), `6x3`(18칸), `3x7`(21칸)까지 칸수가 많을수록 난이도가 높아지도록 오름차순 배치.
  - `PauseSettingsUI.cs`, `PuzzleResponsiveLayout.cs`, `HoldPreview.cs`: 인게임 하단 컨트롤(Undo, Hold to preview, Restart, Number ON/OFF)을 일시정지 메뉴 내부로 이동하고 보드 영역을 확장. HUD 일시정지 버튼을 글로벌 표준 햄버거 아이콘(가로 선 3개 상하 배치)으로 변경하고, 일시정지 메뉴를 `GAME` 탭(Restart, Undo, Hold to preview, Number ON/OFF, Back to lobby)과 `SYSTEM` 탭(Music, Sound, Line slide, Animation, Orientation)으로 분리, Continue 버튼을 우측 상단 `X` 버튼으로 구성.
  - `PuzzleGame.cs`: 일시정지 버튼을 누르기 전에 유니티 에디터에서 다른 창으로 포커스를 옮겼을 때 무조건 `PAUSED` 되던 현상을 방지(`OnApplicationFocus`에서 일반 플레이 중 자동 일시정지 제거, 3-2-1 재개 카운트다운 중이거나 모바일 백그라운드 전환 시에만 보호 동작).
  - `PhaseTwoTests.cs`, `PhaseThreeTests.cs`: 24개 고유 스테이지 및 칸수 오름차순 검증, 포커스 변경 시 비일시정지 검증, 햄버거 아이콘 및 `GAME`/`SYSTEM` 탭·`X` 버튼 검증 반영.
- 검증: Unity 자동 리컴파일 완료(`Temp/pipeline_recompile_status.json`: `failed: false, errors: []`, 10:46 KST) 및 콘솔 에러 0건 확인. 에디터 PlayMode 통합 테스트는 에디터 창에서 직접 실행 가능.
- 남은 작업: 에디터/실기기 플레이 확인 및 Phase 4 온라인 랭킹/광고 SDK 연동.
- 푸시: 이 커밋 생성 후 `origin/master`(및 `origin/wip/home-phase1`)로 푸시 예정.

### 2026-10-27 11:43 KST — 인게임 Lobby 버튼 제거·로비 페이지 무한 루프 방지·잠금 스테이지 자물쇠 가림막 추가
- 직전 커밋: `301f45d` (`feat: 중복 스테이지 정리·난이도순 정렬 및 일시정지 탭 UI·포커스 개선`, 10:47 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: 인게임 로비 버튼 제거·페이지 무한루프 방지 및 잠금 스테이지 자물쇠 가림막 적용`
- 페이즈: Phase 3 인게임 HUD 및 스테이지 로비/도감 UI 개선.
- 변경:
  - `PuzzleResponsiveLayout.cs`: 일시정지(`PAUSED`) 메뉴의 `GAME` 탭 안에 `Back to lobby`가 있으므로 인게임 HUD 상단에 중복 표시되던 `Lobby` 버튼을 제거하고 상단 타이틀 레이아웃을 정리.
  - `PuzzleLobbyGalleryUI.cs`:
    - 스테이지 로비 및 도감에서 `Prev` / `Next` 페이지 이동 시 모듈러(`%`) 순환을 제거하여 첫 페이지에서는 `Prev` 비활성화, 마지막 페이지에서는 `Next` 비활성화 처리로 무한 루프 방지.
    - `LOCKED` 상태인 스테이지 카드 및 미해금 도감 카드에서 원본 일러스트 텍스처가 노출되지 않도록 `Thumbnail.texture = null` 처리하고, 썸네일 앞면을 완전히 덮는 불투명 가림막(`LockOverlay`)과 중앙 자물쇠 아이콘(`PadlockIcon`)을 추가.
  - `PhaseThreeTests.cs`: 인게임 상단 `Lobby` 버튼 부재 확인, 페이지 첫/끝 경계에서 무한 루프 방지 확인, 잠금 카드의 `LockOverlay` 활성화 및 원본 텍스처 비노출 검증 추가.
- 검증: Unity 자동 리컴파일 완료(`Temp/pipeline_recompile_status.json`: `failed: false, errors: []`, 11:43 KST) 및 콘솔 에러 0건 확인.
- 남은 작업: 에디터/실기기 플레이 확인 및 Phase 4 온라인 랭킹/광고 SDK 연동.
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

### 2026-10-27 16:33 KST — 타이틀→로비→인게임 구조 전환·캐주얼 배경 교체 및 상황별 BGM/SFX 인스펙터 연동
- 직전 커밋: `ab3af13` (`feat: 인게임 로비 버튼 제거·페이지 무한루프 방지 및 잠금 스테이지 자물쇠 가림막 적용`, 11:43 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: 타이틀·로비·인게임 흐름 전환 및 캐주얼 배경·상황별 사운드 분리`
- 페이즈: Phase 3 인게임/흐름/배경/사운드 고도화 (Phase 4 온라인/광고 SDK 연동은 일시 보류).
- 변경:
  - `PuzzleLobbyGalleryUI.cs`, `PuzzleGame.cs`: 게임 시작 시 바로 인게임으로 들어가지 않고 **타이틀(`TitlePanel`) -> 스테이지 로비(`LobbyPanel`) -> 인게임** 순서로 진입하도록 구조 전환.
  - `Assets/Resource.art/Backgrounds/bg_puzzle_misty_forest_temp.png`, `README.md`: 기존 어둡고 칙칙한 안개 숲 배경을 밝고 화사한 파스텔 하늘·구름·초원 풍의 캐주얼 배경 이미지(1024×1536)로 교체하고 타이틀/로비 패널 배경 투명도를 조정해 밝은 배경이 비치도록 개선.
  - `PuzzleAudio.cs`, `BoardManager.cs`, `Assets/Scenes/PuzzleGame.unity`:
    - `PuzzleGame` 루트 오브젝트에 `PuzzleAudio` 컴포넌트를 직접 직렬화 연결하여 에디터 Inspector에서 7종 사운드(`titleMusicClip`, `lobbyMusicClip`, `musicClip` 인게임 루프, `gameStartClip`, `moveClip`, `tileCorrectClip`, `clearClip`)를 직접 편집/할당할 수 있도록 구성.
    - 타이틀 음악, 로비 음악, 인게임 루프 음악을 구분하고, 로비에서 스테이지를 선택해 인게임에 진입할 때 게임 시작 효과음(`gameStartClip`)이 재생된 뒤 인게임 루프 음악(`musicClip`)이 나오도록 구현.
    - 타일을 이동할 때마다 원래 정답 칸에 새로 맞춰진 타일이 있으면(`BoardManager.TilePlacedCorrectly`) 짧고 맑은 벨 소리(`tileCorrectClip`)가 재생되도록 구현.
  - `PhaseThreeTests.cs`: 타이틀→로비→인게임 전환, 타이틀/로비/인게임 음악 분리 및 게임 시작 SFX, 타일 정답 위치 안착 시 맑은 효과음 재생 검증 추가.
- 검증: 코드 및 씬 직렬화 점검 완료.
- 남은 작업: Phase 3 인게임 UI/연출 추가 다듬기 (Phase 4는 보류 유지).
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

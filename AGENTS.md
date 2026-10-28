# 프로젝트 작업 규칙

## 프로젝트 별칭
- `퍼즐게임`, `first_puzzle`: `E:\Unity\Puzzle\first_puzzle`에서만 Git 및 Unity 작업을 수행한다.
- `염_스크립트`: `C:\Users\EthanHome\OneDrive\Desktop\A\automatic`에서만 Git 및 스크립트 작업을 수행한다.
- 두 프로젝트의 변경을 섞지 않는다.

## 작업 종료 프로세스 (기본 규약) 및 일일 진행 기록
- **컨셉 디자인 작업 예외 (2026-10-28 사용자 지시 우선):** 컨셉 디자인 및 관련 후속 변경은 전용 브랜치 `feat/concept-image-ui`에만 커밋하고 `origin/feat/concept-image-ui`에만 푸시한다. 아래 기본 종료 규약의 `origin/master`, `origin/wip/home-phase1` 동시 푸시를 이 작업에 적용하지 않는다. `main`/`master`에 커밋·푸시·병합하지 않는다.
- **작업 종료 프로세스 정규화 (필수 기본 규약):**
  1. 모든 수정 또는 기능 개발 작업이 완료되면, 별도 지시가 없더라도 작업 종료 단계에서 **반드시 이 파일(`AGENTS.md`)에 요약 내용을 먼저 작성·갱신한 뒤 Git 커밋 및 원격 푸시(`origin/master`, `origin/wip/home-phase1`)를 수행**한다.
  2. **기존 `AGENTS.md` 내용 변경 명시 규칙:** 새로 지시한 사안이 기존 `AGENTS.md`에 기술된 설계·기능·수치(예: UI 버튼 구성, BGM BPM, 사운드 재생 타이밍 등)에서 변경된 경우, `AGENTS.md` 본문(현재 페이즈/인계 가이드 등)의 기존 설명을 최신 상태로 수정하고, 하단 작업 기록에도 **`기존 AGENTS.md 내용 대비 변경 사항`** 항목을 두어 무엇이 어떻게 변경되었는지 명확히 명시한다.
  3. **커밋 메시지(코멘트)는 가능하면 한글로 작성한다** (예: `feat: 일시정지 방향버튼 제거·인게임 BGM 템포 완화 및 딜레이 페이드인·작업종료 규약 정립`).
  4. 기록에는 `YYYY-MM-DD HH:mm KST`, 직전 커밋 해시 및 푸시 확인 결과, 대상 페이즈, 기존 `AGENTS.md` 대비 변경 사항(해당 시), 실제 변경 내용, 커밋 제목, 검증 결과, 남은 작업을 적는다. 같은 날 여러 커밋이면 항목을 추가한다.
  5. 현재 커밋의 해시는 자기 자신에 포함할 수 없으므로 커밋 제목으로 식별한다. 후속 기록에서 직전 커밋 해시 및 확인된 푸시 결과를 보완한다. 성공 전 푸시 완료로 기록하지 않는다.
  6. 실제 테스트 결과와 미검증 항목을 구분한다. 상세 기능 명세는 [Assets/SPEC.md](Assets/SPEC.md)를 따른다.

## 현재 페이즈
| 페이즈 | 범위 | 상태 |
|---|---|---|
| Phase 1 | StageData, 정사각형 타일, 결정론적 셔플, 다중 밀기, Undo, 클리어 연출 | 구현 및 자동 테스트 완료 |
| Phase 2 | 타이머/이동 수, 일시정지 가림막, 미리보기 제한, 3-2-1 재개, 설정 4종, 오디오, 로컬 최고 기록 | 구현 및 자동 테스트 완료 |
| Phase 3 | 타이틀→로비→인게임 흐름, 스테이지 해금, 도감/원본 감상, 세로·가로 선택, Safe Area 및 캐주얼 배경·사운드 분리 | 코어 및 런타임 UI 구현 완료, 자동 테스트 통과 (실기기 및 고도화 마감 진행 중) |
| Phase 4 | 온라인 계정/랭킹, 광고 SDK 및 보상 연동 | 보류(일시 중단). Phase 3 인게임 완성도 마감 우선 |

## 다른 PC / 다른 에이전트 인계 가이드 (Handoff Guide)
1. **브랜치 현황:**
   - 컨셉 디자인은 `feat/concept-image-ui`에서 별도 진행한다. 작업 재개 시 현재 브랜치와 변경 사항을 먼저 확인하고, 기본 브랜치로 전환하거나 기본 브랜치를 무조건 pull하지 않는다.
   - 원격 기본 브랜치 `origin/master`(및 작업 브랜치 `origin/wip/home-phase1`)에 Phase 1 ~ Phase 3 작업분이 동기화되어 있다.
   - 작업을 시작할 때 `git pull origin master` (또는 작업 브랜치 확인) 후 `AGENTS.md`와 `Assets/SPEC.md`를 먼저 읽는다.
2. **씬 및 에셋 구조:**
   - 실제 플레이 씬: `Assets/Scenes/PuzzleGame.unity` (`PuzzleGameSceneBuilder.cs`로 생성, 중복을 제거한 30개 고유 스테이지가 3x3부터 칸수 오름차순으로 연결 완료).
   - Phase 1 코어 검증용 씬: `Assets/Scenes/PhaseOneTest.unity` (`PhaseOneSceneBuilder.cs`).
   - 일러스트 원본 경로: `Assets/Resource.art/Puzzle_images` (30종 정리 완료, GIF/WebP는 첫 프레임 PNG와 함께 보존).
   - 캐주얼 배경 경로: `Assets/Resource.art/Backgrounds/bg_puzzle_misty_forest_temp.png` (기존 어두운 안개 숲 배경을 밝고 화사한 파스텔 하늘·초원 캐주얼 배경으로 교체, GUID 유지).
   - 오디오 에셋 경로: `Assets/Resource/Audio/` (`bgm_title.wav`, `bgm_lobby.wav`, `bgm_ingame_loop.wav`[일반 플레이용 120 BPM 44.1 kHz 스튜디오 어쿠스틱 피아노·비브라폰·스트링 패드·업라이트 베이스 루프, 8.0초], `sfx_game_start.wav`, `sfx_tile_move.wav`, `sfx_tile_correct.wav`, `sfx_stage_clear.wav` 총 7종이 실제 WAV 파일로 저장되어 `PuzzleGame` 루트의 `PuzzleAudio` 인스펙터에 직접 연결됨).
3. **핵심 런타임 스크립트 (`Assets/Resource/Scripts/Puzzle/`):**
   - `StageData.cs`, `PuzzleBoardState.cs`, `PuzzleTile.cs`, `BoardManager.cs`: Phase 1 퍼즐 코어 (정사각형 타일 유지, 중앙 크롭 UV, 결정론적 역방향 셔플, 다중 밀기, Undo 3회 및 보상 티켓 콜백, `Numbers: ON` 상태에서 타일 숫자 텍스트에 굵은 외곽선(`LiberationSans SDF - Outline` + `OUTLINE_ON`, `OutlineWidth = 0.24`, 짙은 네이비 외곽선, `Bold`) 적용, 타일이 정답 칸에 들어갈 때 `TilePlacedCorrectly` 이벤트 발생, 클리어 페이드인).
   - `SettingsManager.cs`: 5종 설정(`PREF_BGM`, `PREF_SFX`, `PREF_MULTIPUSH`, `PREF_SLIDE_ANIM`, `PREF_ORIENTATION` 세로/가로) 저장/복원 및 `Screen.orientation` 반영.
   - `PuzzleAudio.cs`: `PuzzleGame` 루트 오브젝트에 직렬화 부착되어 Inspector에서 7종 오디오 클립(`titleMusicClip`, `lobbyMusicClip`, `musicClip` 인게임 루프[120 BPM 44.1 kHz 스튜디오 어쿠스틱 피아노·비브라폰·스트링 패드·업라이트 베이스·브러시 퍼커션 편곡으로 퀄리티 업그레이드, 기존 150 BPM은 추후 시간제한 스테이지용으로 보류], `gameStartClip`, `moveClip`, `tileCorrectClip` 타일 정답 위치 안착 시 짧고 맑은 벨 소리, `clearClip`)을 직접 교체·편집 가능. 인게임 시작(`PlayGameStartThenInGameMusic`) 시 `gameStartClip` 효과음이 끝난 뒤 **0.5초 대기 후 볼륨 0에서 0.8초 동안 천천히 페이드인**되며 인게임 루프 BGM이 재생됨.
   - `PuzzleProgress.cs`: 스테이지 해금 규칙(첫 스테이지, 각 난이도 첫 스테이지, 직전 클리어 스테이지 다음 단계 해금) 및 최소 이동/최단 시간(`PUZZLE_CLEAR_`, `PUZZLE_BEST_`, `PUZZLE_BEST_TIME_`) 관리.
   - `PuzzleLobbyGalleryUI.cs`: **타이틀(`TitlePanel`) -> 스테이지 로비(`LobbyPanel`) -> 인게임** 화면 흐름, 난이도 탭(`ALL`, `3x3`, `3x4`, `3x5`, `3x6`, `3x7`, `4x3`, `6x3`)별 스테이지 선택 로비, 클리어 일러스트 도감(미해금 시 불투명 자물쇠 가림막으로 원본 이미지 완전 차단, 첫/마지막 페이지에서 Prev/Next 무한 루프 방지), 크롭 없는 원본 전체화면 감상 모달(`uvRect = 0,0,1,1`).
   - `PuzzleResponsiveLayout.cs`: 모바일 Safe Area(`Screen.safeArea`) 적용 및 세로(`Portrait`) / 가로(`Landscape`) 전용 HUD·보드·일시정지·타이틀·로비·도감 레이아웃 재배치 (인게임 상단 Lobby 중복 버튼 제거).
   - `PauseSettingsUI.cs`, `PuzzleGame.cs`: 타이틀→로비→인게임 진입 흐름(`OpenTitleScreen` → `EnterLobbyFromTitle` → `Load`), 인게임 진입 시 게임 시작 SFX 재생 및 0.5초 후 인게임 루프 BGM 페이드인 전환, 햄버거 아이콘 일시정지 버튼, `GAME` 탭(Restart, Undo, Hold to preview, Number ON/OFF, Back to lobby)과 `SYSTEM` 탭(Music, Sound, Line slide, Animation 4종 — 기존 `Orientation: PORTRAIT` 버튼은 제거됨)으로 분리된 일시정지 메뉴 및 `X` 닫기 버튼, 3-2-1 재개 카운트다운, 에디터 창 전환 시 자동 일시정지 방지 처리.
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
  - `PhaseTwoTests.cs`, `PhaseThreeTests.cs`: 30개 고유 스테이지 및 칸수 오름차순 검증, 포커스 변경 시 비일시정지 검증, 햄버거 아이콘 및 `GAME`/`SYSTEM` 탭·`X` 버튼 검증 반영.
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

### 2026-10-27 17:12 KST — PortraitFrame/Content 하단 버튼 정리 및 7종 오디오 WAV 에셋 생성·인스펙터 등록
- 직전 커밋: `c807d41` (`feat: 타이틀·로비·인게임 흐름 전환 및 캐주얼 배경·상황별 사운드 분리`, 16:33 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: Content 하단 중복 컨트롤 정리 및 오디오 에셋 7종 인스펙터 등록`
- 페이즈: Phase 3 씬 하이어라키 정리 및 사운드 에셋 직렬화 마감.
- 변경:
  - `Assets/Scenes/PuzzleGame.unity`, `PuzzleSceneAudioAndHierarchySync.cs`, `PauseSettingsUI.cs`, `PuzzleResponsiveLayout.cs`:
    - 에디트 모드(`PortraitFrame/Content`)에 남아 있던 `Undo`, `Hold to preview`, `Numbers: ON`을 `PauseAndResult/PauseControls` 내부로 이동하여 씬 파일에 직접 저장하고, 중복이던 `Content/Restart` 오브젝트를 제거(`PauseControls/Restart puzzle` 사용).
    - `Content/Pause` 버튼을 에디트 모드에서도 좌측 상단(`anchorMin: 0.00, 0.905`, `anchorMax: 0.16, 0.985`) 햄버거 아이콘(`HamburgerIcon`, 가로 3선)으로 배치하고 `BoardFrame` 확장 및 밝은 캐주얼 배경 위 텍스트 가독성 색상(짙은 네이비) 반영.
  - `Assets/Resource/Audio/*.wav`, `PuzzleAudio.cs`:
    - 런타임에만 메모리 합성되던 7종 사운드를 `Assets/Resource/Audio/` 폴더에 실제 WAV 파일(`bgm_title.wav`, `bgm_lobby.wav`, `bgm_ingame_loop.wav`, `sfx_game_start.wav`, `sfx_tile_move.wav`, `sfx_tile_correct.wav`, `sfx_stage_clear.wav`)로 생성·저장하고, `PuzzleGame` 루트의 `PuzzleAudio` 컴포넌트 인스펙터 슬롯 7개에 모두 직렬화 등록 완료.
  - `PhaseThreeTests.cs`: `PortraitFrame/Content` 하단 구 버튼 부재 확인 및 `PuzzleAudio` 인스펙터 클립 7종 할당 검증 추가.
- 검증: Unity 자동 리컴파일 완료(`Temp/pipeline_recompile_status.json`: `status: up_to_date, failed: false, errors: []`, 17:11 KST) 및 열린 씬 동기화·저장 확인.
- 남은 작업: Phase 3 인게임 세부 연출 점검 (Phase 4 온라인/광고 SDK는 보류 유지).
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

### 2026-10-27 17:58 KST — 타이틀 전체화면 적용 및 타이틀·로비 진입 시 인게임/PAUSED 화면 비침 현상 수정
- 직전 커밋: `5a91fdf` (`feat: Content 하단 중복 컨트롤 정리 및 오디오 에셋 7종 인스펙터 등록`, 17:12 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `fix: 타이틀 전체화면 전환 및 타이틀·로비 뒤 인게임·PAUSED 화면 노출 수정`
- 페이즈: Phase 3 타이틀·로비·인게임 화면 전환 표시 분리 수정.
- 변경:
  - `PuzzleLobbyGalleryUI.cs`, `PuzzleResponsiveLayout.cs`:
    - 타이틀 화면(`TitlePanel/TitleCard`)을 작은 중앙 카드에서 전체화면(`anchorMin: 0,0`, `anchorMax: 1,1`) 레이아웃으로 변경.
    - 타이틀, 스테이지 로비, 갤러리 화면이 열릴 때(`OpenTitle`, `OpenLobby`, `OpenGallery`) 뒤쪽 인게임 보드/HUD 컨테이너(`PortraitFrame/Content`)와 `PauseAndResult`(`curtain`)를 비활성화(`SetInGameBackdropVisible(false)`)하여 뒤로 게임 화면이나 `PAUSED` 화면이 반투명하게 비치지 않도록 수정하고, 스테이지를 선택해 인게임에 들어갈 때(`CloseAll`) 다시 활성화하도록 처리.
  - `PuzzleGame.cs`:
    - `OpenLobby()` 호출 시 `curtain.SetActive(true)`로 `PAUSED` 화면을 켜던 코드를 `curtain.SetActive(false)`로 변경하여 스테이지 셀렉트(로비) 뒤에 `PAUSED` 창이 겹쳐 보이던 버그를 해결.
  - `PhaseThreeTests.cs`: 타이틀 전체화면 앵커(`0,0`~`1,1`), 타이틀 및 로비 오픈 중 `Content`와 `curtain`(`PAUSED`) 비활성화, 인게임 진입 시 `Content` 재활성화 검증 추가.
- 검증: Unity 자동 리컴파일 완료(`Temp/pipeline_recompile_status.json`: `status: completed, failed: false, errors: []`, 17:58 KST).
- 남은 작업: Phase 3 인게임 세부 연출 점검 (Phase 4 온라인/광고 SDK는 보류 유지).
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

### 2026-10-27 18:31 KST — 일시정지 방향버튼 제거·인게임 BGM 템포 완화 및 딜레이 페이드인·작업종료 규약 정립
- 직전 커밋: `3ada396` (`fix: 타이틀 전체화면 전환 및 타이틀·로비 뒤 인게임·PAUSED 화면 노출 수정`, 17:58 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: 일시정지 방향버튼 제거·인게임 BGM 템포 완화 및 딜레이 페이드인·작업종료 규약 정립`
- 페이즈: Phase 3 인게임 일시정지 UI 및 사운드 연출 개선 + 프로젝트 작업 종료 기본 규약 정립.
- **기존 `AGENTS.md` 내용 대비 변경 사항:**
  1. **`PAUSED` 메뉴 `SYSTEM` 탭 구성 변경:** 기존 `AGENTS.md`(10:47 KST 기록 및 인계 가이드)에서는 `SYSTEM` 탭이 `Music, Sound, Line slide, Animation, Orientation` 5종으로 기술되어 있었으나, 새 지시에 따라 **`PAUSED -> SYSTEM` 메뉴 내 `Orientation: PORTRAIT` 버튼을 삭제**하여 `Music`, `Sound`, `Line slide`, `Animation` 4종으로 변경함.
  2. **인게임 플레이 BGM(`bgm_ingame_loop.wav`) BPM 변경:** 기존 `AGENTS.md`(02:42 KST 기록)에서는 인게임 루프 BGM을 `150 BPM`(6.4초 루프)으로 기술했으나, 일반 스테이지에서는 템포가 너무 빠르다는 지시에 따라 **120 BPM(16분음표 0.125초, 8.0초 루프)으로 템포를 완화**함 (기존 150 BPM은 향후 시간제한 스테이지 추가 시 활용하도록 보류).
  3. **인게임 진입 시 BGM 시작 타이밍 변경:** 기존 `AGENTS.md`(16:33 KST 기록 및 인계 가이드)에서는 인게임 진입 시 게임 시작 효과음과 함께 인게임 루프 BGM으로 전환된다고만 기술되어 있었으나, 새 지시에 따라 **게임 시작 효과음(`sfx_game_start.wav`)이 끝난 뒤 0.5초 후부터 볼륨 0에서 0.8초간 천천히 페이드인되며 시작**하도록 변경함.
  4. **작업 종료 프로세스 기본 규약 신설:** 기존 `AGENTS.md` 상단의 `커밋 및 일일 진행 기록`에 "요청 없이 커밋/푸시하지 않으며"라고 되어 있던 문구를 개편하여, **모든 수정/개발 완료 시 `AGENTS.md` 요약 갱신(기존 내용 대비 변경점 명시 포함) 및 Git 커밋·푸시를 수행하는 정규화된 작업 종료 프로세스**를 기본 규약으로 명문화함.
- 변경:
  - `PauseSettingsUI.cs`: `PAUSED` 메뉴의 `SYSTEM` 탭에서 `Orientation` 버튼을 생성하지 않도록 제거(기존 씬에 남아 있을 경우 제거)하고 4개 항목(`Music`, `Sound`, `Line slide`, `Animation`)에 맞춰 세로(1열 4행)/가로(2×2 그리드) 레이아웃 정렬.
  - `PuzzleAudio.cs`, `PuzzleSceneAudioAndHierarchySync.cs`, `Assets/Resource/Audio/bgm_ingame_loop.wav`:
    - 인게임 루프 BGM 합성 템포를 150 BPM(`0.10f` step)에서 **120 BPM(`0.125f` step, 8.0초 루프)**으로 늦추고 `Assets/Resource/Audio/bgm_ingame_loop.wav` 파일을 재생성(`352,812` bytes, GUID 보존).
    - `PlayGameStartThenInGameMusic(delayAfterSfx: 0.5f, fadeInDuration: 0.8f)` 및 `DelayedInGameMusicRoutine` 코루틴을 추가하여 스테이지 시작 시 기존 BGM을 즉시 멈추고 `gameStartClip` 효과음 재생 종료 후 0.5초 대기한 뒤 `musicClip`을 볼륨 `0`에서 `0.16`까지 0.8초 동안 부드럽게 페이드인하도록 구현.
  - `PuzzleGame.cs`: `LoadStageInternal(..., enterInGame: true)`에서 `Audio.PlayGameStartThenInGameMusic()`을 호출하도록 연결.
  - `PhaseTwoTests.cs`, `PhaseThreeTests.cs`: `SystemTab/Orientation` 부재 확인, 인게임 진입 직후 `CurrentMusicVolume == 0f` 및 `IsMusicTransitioning == true` 검증, `InGameMusicClip.length` 8.0초(120 BPM) 검증 반영.
- 검증: Unity 에디터 파이프라인 리컴파일 완료(`Temp/pipeline_recompile_status.json`: `status: up_to_date, failed: false, errors: []`, 18:29 KST) 및 `bgm_ingame_loop.wav` 갱신 확인.
- 남은 작업: Phase 3 추가 피드백 대응 (Phase 4 온라인/광고 SDK는 보류 유지).
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

### 2026-10-27 22:24 KST — 유니티 Game 뷰 모바일 세로·갤럭시 폴드·Duo 해상도 프리셋 13종 자동 등록
- 직전 커밋: 4e6658f (feat: 일시정지 방향버튼 제거·인게임 BGM 템포 완화 및 딜레이 페이드인·작업종료 규약 정립, 18:31 KST). origin/master 및 origin/wip/home-phase1 푸시 완료 확인.
- 커밋 제목: feat: 유니티 Game 뷰 모바일 세로·폴드·Duo 해상도 프리셋 자동 등록
- 페이즈: Phase 3 모바일·폴더블·듀얼스크린 해상도 테스트 환경 구축.
- 변경:
  - Assets/Resource/Scripts/Puzzle/Editor/PuzzleGameViewSizeRegistrar.cs:
    - 에디터 로드 시([InitializeOnLoad]) 및 상단 메뉴(Puzzle/Register Mobile, Fold & Duo Game View Resolutions)를 통해 Unity GameViewSizes(Standalone, Android, iOS 그룹 모두)에 총 13종의 해상도 프리셋을 자동 등록하고 SaveToHDD()로 저장하도록 구현.
    - **대표 휴대폰 세로 해상도 (7종):** Phone Portrait HD+ (720×1600), Phone Portrait 16:9 FHD (1080×1920), Phone Portrait 19.5:9 Galaxy/iPhone (1080×2340), Phone Portrait 20:9 Android FHD+ (1080×2400), iPhone 15/16 Pro Portrait (1179×2556), iPhone 15/16 Pro Max Portrait (1290×2796), Galaxy S24 Ultra QHD+ Portrait (1440×3120).
    - **갤럭시 Z 폴드 해상도 (3종):** Galaxy Z Fold Cover Portrait (904×2316), Galaxy Z Fold Unfolded Portrait (1812×2176), Galaxy Z Fold Unfolded Landscape (2176×1812).
    - **iPhone / Surface Duo 해상도 (3종):** iPhone/Surface Duo Single Portrait (1350×1800), iPhone/Surface Duo Dual Portrait (1800×2700), iPhone/Surface Duo Dual Landscape (2700×1800).
    - Unity GameViewSize.baseText의 40자 길이 제한 및 해상도((WxH)) 자동 표기 규칙에 맞춰 라벨이 잘리지 않도록 38자 이내로 구성하고, 이전 잘린 중복 항목은 자동 정리하도록 처리.
  - Assets/Tests/Editor/PhaseThreeTests.cs: GameViewResolutionsIncludeMobilePortraitFoldAndDuoPresets 단위 테스트 추가.
- 검증: Unity 자동 리컴파일 완료(Temp/pipeline_recompile_status.json: status: completed, failed: false, errors: [], 22:22 KST) 및 에디터 GameViewSizes.asset에 13종 프리셋 등록·저장 확인.
- 남은 작업: Phase 3 추가 UI/연출 피드백 대응 (Phase 4 온라인/광고 SDK는 보류 유지).
- 푸시: 이 커밋 생성 후 origin/master 및 origin/wip/home-phase1로 푸시 예정.

### 2026-10-27 23:56 KST — Gemini 작업 인계 및 신규 일러스트 6종 등록
- 직전 커밋: `e19e9a3` (유니티 Game 뷰 모바일 세로·폴드·Duo 해상도 프리셋 자동 등록). 작업 시작 시 원격 조회로 `origin/master`, `origin/wip/home-phase1` 모두 동일 해시임을 확인. `git pull --ff-only origin master`: Already up to date.
- 커밋 제목: `feat: 신규 퍼즐 이미지 6종 등록 및 비율별 스테이지 배정`
- 대상 페이즈: Phase 3 콘텐츠 확장 및 에이전트 인계 기록 정비.
- 기존 AGENTS.md 내용 대비 변경 사항:
  - 고유 이미지 및 플레이 스테이지 수 24개 → 30개. 신규 3x6 난이도 포함(탭은 스테이지 목록에서 자동 구성).
  - 깨진 한글 본문은 정상 커밋 `4e6658f`에서 복원하고 Gemini의 22:24 해상도 프리셋 기록은 보존. 해당 기록의 제어문자 및 잘린 마지막 문구 수정.
- 실제 변경:
  - 신규 JPG 6장을 직접 확인하여 `puzzle_025_blonde_dark_archer`부터 `puzzle_030_blonde_white_dress`까지 내용 기반 이름으로 변경. Unity AssetDatabase로 이름 변경 및 등록하여 GUID 유지.
  - 025 금발 어둠의 궁수(710x900), 026 드래곤 라이더(750x900), 028 은발 노란 리본(761x900): 3x4.
  - 029 바람에 흩날리는 붉은 망토(600x900): 3x5.
  - 027 분홍 머리 방패 기사(418x900), 030 금발 흰 드레스(2212x4093): 3x6.
  - Stage32~Stage37 생성, 기존 스테이지 ID 보존 및 칸수 오름차순 정렬. 원본 이미지 픽셀은 변경하지 않음.
  - 에디터에 남아 있던 미저장 씬 Image 색상 변경(네이비 → 흰색)을 저장하여 보존.
  - 이미지 README, SPEC의 등록 현황 및 기존 테스트의 스테이지 수/마지막 페이지 검증 갱신.
- 검증: Unity 리컴파일 오류 0건, 에디터 자동 테스트 35/35 통과(실패 0). 전체 스테이지 로딩·비율·중복·정렬 및 로비 페이지 이동 검증. 등록 재실행 시 씬 불변 확인. git diff --check 통과.
- 미검증/남은 작업: 실제 모바일 기기의 새 이미지 시각 검수. Phase 4는 보류 유지.
- 푸시: 본 커밋 생성 후 `origin/master`, `origin/wip/home-phase1`로 푸시 예정. 성공 여부는 완료 응답 및 다음 기록에 반영.

### 2026-10-28 01:51 KST — 타일 숫자 외곽선 텍스트 적용 및 메인 스테이지 BGM 44.1kHz 어쿠스틱 스튜디오 사운드 퀄업
- 직전 커밋: `ac7c301` (`feat: 신규 퍼즐 이미지 6종 등록 및 비율별 스테이지 배정`, 2026-10-27 23:56 KST). `origin/master` 및 `origin/wip/home-phase1` 푸시 완료 확인.
- 커밋 제목: `feat: 타일 숫자 외곽선 텍스트 적용 및 메인 스테이지 BGM 어쿠스틱 퀄업`
- 대상 페이즈: Phase 3 인게임 가독성(타일 숫자 외곽선) 및 BGM 사운드 퀄리티 고도화.
- **기존 `AGENTS.md` 내용 대비 변경 사항:**
  1. **타일 숫자(`Numbers: ON`) 텍스트 스타일 변경:** 기존에는 `PuzzleTile.prefab`의 `Number` 텍스트가 `m_sharedMaterial: {fileID: 0}`(기본 SDF 머티리얼)로 직렬화되어 외곽선이 렌더링되지 않았으나, **`LiberationSans SDF - Outline` 머티리얼 연결 및 런타임 `OUTLINE_ON`(`OutlineWidth = 0.24`, 짙은 네이비 외곽선, `Bold`) 강제 적용**으로 밝은 일러스트 위에서도 숫자가 뚜렷하게 보이도록 변경함.
  2. **메인 스테이지 BGM(`bgm_ingame_loop.wav`) 및 타이틀/로비 BGM 음원 퀄리티 변경:** 기존 `AGENTS.md`에서는 22,050 Hz 단음 스텝 칩튠/미디(MIDI) 방식(`352,812` bytes)으로 기술되어 있었으나, 미디 느낌을 없애라는 지시에 따라 **44,100 Hz 폴리포닉(스텝 간 자연스러운 서스테인/릴리즈 오버랩) 어쿠스틱 그랜드 피아노(해머 어택 + 디튠 코러스 + 고차 배음 감쇠) + 일렉트릭 피아노/스트링 패드 8코드 진행(`Cmaj7 -> G/B -> Am7 -> Em7 -> Fmaj7 -> C/E -> Dm7 -> G13`) + 비브라폰 대선율 + 업라이트 베이스 + 브러시 스네어/셰이커 + 심리스 룸 리버브·소프트 새추레이션 마스터링**으로 전면 재합성(`705,644` bytes, 120 BPM 8.0초 루프 유지)함.
- 실제 변경:
  - `Assets/Resource/PuzzlePhaseOne/PuzzleTile.prefab`, `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat`, `Assets/Resource/Scripts/Puzzle/PuzzleTile.cs`:
    - `PuzzleTile.prefab`의 `Number` 텍스트에 `LiberationSans SDF - Outline` 머티리얼(`guid: 79459efec17a4d00a321bdcc27bbc385`), `Bold`(`m_fontStyle: 1`), 불투명도 `0.96`을 연결.
    - `PuzzleTile.Initialize` 시점에 `ApplyNumberOutlineStyle()`을 호출하여 `ShaderUtilities.Keyword_Outline`(`OUTLINE_ON`), `OutlineWidth = 0.24`, `OutlineSoftness = 0.04`, `FaceDilate = 0.12`, 짙은 네이비 외곽선(`0.04, 0.07, 0.12, 0.96`)을 보장.
  - `Assets/Resource/Scripts/Puzzle/PuzzleAudio.cs`, `Assets/Resource/Scripts/Puzzle/Editor/PuzzleSceneAudioAndHierarchySync.cs`, `Assets/Resource/Audio/bgm_ingame_loop.wav`, `bgm_title.wav`, `bgm_lobby.wav`:
    - 메인 스테이지 인게임 BGM(`SynthesizeInGameLoopSamples`) 및 타이틀/로비 BGM을 44.1 kHz 폴리포닉 어쿠스틱 편곡 및 스튜디오 리버브/새추레이션 마스터링으로 업그레이드하고 실제 WAV 에셋 3종을 갱신(GUID 보존).
  - `Assets/Tests/Editor/PhaseThreeTests.cs`: `TileNumberLabelUsesOutlinedBoldMaterialAndStudioBgmIs44kHz` 단위 테스트 추가.
- 검증: WAV 에셋 3종(`bgm_ingame_loop.wav` 705,644 bytes, `bgm_lobby.wav` 378,246 bytes, `bgm_title.wav` 352,844 bytes) 생성 및 `git diff --check` 통과 확인.
- 남은 작업: 에디터/실기기 청음 및 시각 확인 (Phase 4 온라인/광고 SDK는 보류 유지).
- 푸시: 이 커밋 생성 후 `origin/master` 및 `origin/wip/home-phase1`로 푸시 예정.

### 2026-10-28 09:09 KST — 컨셉 디자인 전용 브랜치 작업 재개 및 체크포인트
- 직전 커밋: `a8c60c4` (`feat: 타일 숫자 외곽선 텍스트 적용 및 메인 스테이지 BGM 어쿠스틱 퀄업`). 원격 조회로 `origin/master`, `origin/wip/home-phase1` 모두 동일 해시 확인.
- 커밋 제목: `wip: 컨셉 디자인 작업 보존 및 전용 브랜치 푸시 규약 적용`
- 대상 페이즈: Phase 3 컨셉 UI 디자인 실험.
- 기존 AGENTS.md 내용 대비 변경 사항: 컨셉 작업은 전용 브랜치 `feat/concept-image-ui`에만 커밋·푸시하도록 기본 종료 규약의 예외 추가. 기존 파스텔 UI에서 크림색 배경·검은 외곽선·이미지 라벨 및 아이콘 테마를 실험하고, 인게임 Undo/미리보기 버튼을 추가한 미완료 작업분 보존.
- 실제 변경: 이전 작업에서 남은 ConceptTheme, ConceptImageLabel, ConceptPlate와 UI 연결 변경, 컨셉 원본·배경 백업·ConceptUI 리소스 및 에셋 생성 도구를 체크포인트로 저장. 임시 화면 캡처(Assets/Temp)는 로컬에 유지하고 커밋에서 제외.
- 검증: 편집기 스크립트 진단 오류 없음. 기존 추적 코드의 git diff --check는 통과했으나, 신규 파일을 포함한 staged 검사에서는 Unity 생성 .meta의 후행 공백 경고가 확인됨(원본 보존). 이번 작업에서 Unity 컴파일·플레이·자동 테스트 및 실기기 시각 검증은 미실시. 디자인 완성으로 간주하지 않음.
- 남은 작업: 이전 대화 본문 조회 결과가 비어 마지막 세부 디자인 지시는 복구하지 못함. 사용자 추가 피드백 및 컨셉 대비 화면 검증 후 디자인 마감. Phase 4 보류 유지.
- 푸시: 이 커밋 생성 후 `origin/feat/concept-image-ui`에만 최초 푸시 예정. 실제 결과는 완료 응답 또는 후속 기록에서 확인.

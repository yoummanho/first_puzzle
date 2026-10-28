# 컨셉 UI 에셋 편집 안내

이 디자인 작업은 **feat/concept-image-ui** 브랜치 전용이다.

## 개별 이미지 교체

| 폴더 | 용도 / 예시 |
|---|---|
| [Buttons/Icons](Buttons/Icons) | 버튼 아이콘: `icon_pause.png`, `icon_undo.png`, `icon_preview.png`, `icon_prev.png`, `icon_next.png`, `icon_lock.png` |
| [Buttons/Labels](Buttons/Labels) | 버튼 문구: `label_tap_to_start.png`, `label_play_stage.png`, `label_music_on.png` 등. ON/OFF는 별도 파일 |
| [Surfaces](Surfaces) | 색을 입히는 흰색 바탕 마스크: `panel.png`, `pill.png`, `circle.png`, `tile.png` |
| [Backgrounds](Backgrounds) | `background.png`: 오프화이트 + 규칙적으로 배치한 퍼즐/별/번개 두들 |
| [Brand](Brand) | `logo.png`: Lilita One 단색 앰버 로고 |
| [Fonts](Fonts) | Lilita One / 주아체 원본 TTF와 각각의 OFL 라이선스 |

PNG를 같은 이름으로 덮어쓰면 다음 플레이부터 반영된다. **옆의 `.meta` 파일은 지우지 않는다.** 아이콘과 문구는 투명 배경을 유지한다. PNG의 가로/세로 비율은 그대로 표시되며, 잘리지 않도록 이미지 내부에 여백을 둔다.

버튼의 아이콘, 문구, 바탕은 각각 분리되어 있다. 아이콘만 바꾸거나 ON/OFF 문구만 바꾸어도 된다. `Surfaces`는 흰색 마스크이며, 실제 팔레트 색은 [ConceptTheme.cs](../../Resource/Scripts/Puzzle/ConceptTheme.cs)의 `Skin`에서 지정한다. 마스크에 외곽선/그림자를 직접 넣으면 런타임 효과와 중복되므로 넣지 않는다.

기본 그래픽을 다시 생성할 때는 [GenerateConceptAssets.ps1](../../../Tools/GenerateConceptAssets.ps1)을 Windows PowerShell에서 실행한다. **재생성은 위 PNG의 수동 편집을 덮어쓴다.** 사용자 교체 이미지는 먼저 백업하거나 생성 도구에서 해당 항목을 제외한다. 글자는 프로젝트의 Lilita One TTF를 직접 읽으므로 시스템 폰트 설치가 필요 없다.

## Core Style Guide

- 배경 `#F8F9FA`, 두들 `#E5E7EB` / alpha 46/255 ≈ 18%. 노이즈·그라데이션·블러 없음.
- 외곽선 `#1E1E24`, 3px. 수직 하드 섀도우 6px. Canvas 기준 해상도(세로 1080×1920 / 가로 1920×1080)의 UI 단위이며 화면 배율에 따라 함께 조절된다.
- Primary: `#38E54D`, `#FF3366`. Active: `#00D2FF`, `#FFBE0B`.
- Surface: `#FFFFFF`, `#F3F4F6`. Locked/Inactive: `#D1D5DB`.
- 영문 Lilita One, 한글 주아체 fallback. 흰색/앰버 채움 + 차콜 외곽선 및 단색 그림자.
- 퍼즐 원본은 수정하지 않는다. 타이틀·도감은 원본 전체를 비율 유지로 넣고, 플레이 보드/스테이지 썸네일은 기존 중앙 크롭을 비율 유지로 사용한다. 가로/세로를 별도로 늘리는 변형 금지.

폰트 출처: [Google Fonts Lilita One](https://github.com/google/fonts/tree/main/ofl/lilitaone), [Google Fonts Jua](https://github.com/google/fonts/tree/main/ofl/jua). 배포 시 Fonts 폴더의 두 OFL 문서를 함께 보존한다.

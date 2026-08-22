# ShoulderView UI 인계 및 릴리스 기록

작성일: 2026-08-22
담당: 박한솔
Jira: SBSL-8

## 완료 범위

- 3D 숄더뷰 카메라, 이동, 중앙 조준 상호작용
- 역 상점 열기/닫기, 볼트 통화, 3개 업그레이드 카드와 구매 후 갱신
- Railway Field Workshop HUD, 카메라 옵션 패널, 의미 기반 UI 테마
- 봄·여름 팔레트 전환 및 동일 조건 실제 플레이 캡처
- 프로젝트 고유 생성 아틀라스와 라이선스 출처 기록
- Tiny Swords 기어 아이콘 로컬 설치 옵션

## 검증

- Unity: 6000.5.2f1
- Windows 플레이어 빌드 성공
- PlayMode: 10/10 통과
- 실제 상점 시나리오: `opened=True`, `purchased=True`
- 실제 증거 해상도: 1024×720
- 봄·여름 각각 월드/상점 열림/구매 후 캡처 완료

## 변경 경계

- 소유 경로: `Assets/01.hansol/ShoulderView`
- 팀원 기능과 공용 맵·Enemy 구현은 수정하지 않는다.
- Unity 빌드가 자동 변경한 공용 렌더·PlayerSettings는 모두 복구했다.
- 기존 미추적 중첩 폴더 `railgame/`은 작업 및 커밋 대상에서 제외했다.
- 박한솔 임시 적 시스템은 제거된 상태를 유지한다.

## 교체와 제거

- UI 외형은 `ShoulderUiTheme`와 `ShoulderUiRole`로 교체한다.
- 기본 아틀라스는 `UI/Original/RailwayWorkshopAtlas.png`다.
- Tiny Swords 원본은 공개 저장소에 넣지 않는다. 로컬 설치 폴더를 지우면 관련 옵션이 제거된다.
- 전체 기능 제거 시 `Assets/01.hansol/ShoulderView` 폴더와 데모 씬 참조만 제거하면 된다.

## 커밋

- `6fd7dab` 모듈형 워크숍 UI 테마 구조
- `fba511f` 봄·여름 UI 검증
- `03aa17b` Tiny Swords 로컬 테마 옵션

## 연동

- Jira: <https://hansola1234.atlassian.net/browse/SBSL-8>
- GitHub PR: 병합 절차에서 확정 후 기록


# 봄·여름 시각 검증

검증 일자: 2026-08-22

## 참고 이미지에서 확인한 기준

- 한눈에 읽히는 밝은 잔디, 파란 물, 갈색 흙의 제한된 팔레트
- 나무·바위는 작은 노이즈보다 덩어리진 블록 실루엣으로 구분
- 자원 군집 사이에 플레이어와 철로가 지나갈 수 있는 열린 통로 유지
- 핵심 통화·거리·속도는 화면 가장자리의 간결한 HUD로 고정
- 계절 차이는 색상만이 아니라 식생 밀도, 흙 노출, 물길 폭의 차이로 표현

참고: [Unrailed Steam](https://store.steampowered.com/app/1016920/Unrailed/), [PlayStation 공식 스토어 이미지](https://store.playstation.com/en-us/product/UP1440-CUSA20431_00-DAENAUNRAILED001/), [사용자 제공 나무위키 페이지](https://namu.wiki/w/Unrailed!)

## 봄 장면

- 캡처: `Logs/Spring_Visual_Before.png`
- 연두 잔디와 밝은 청록 물이 명확해 초반 지역 인상이 잘 전달된다.
- 중앙의 큰 갈색 산악 덩어리가 시선을 과하게 차지하지만 진행 통로 구분에는 도움이 된다.
- 나무 텍스처가 멀리서 점 노이즈처럼 보이는 구간은 추후 공용 맵 담당자와 블록 실루엣 강화 여부를 협의한다.
- 우측 상단 `SHOP TEST`는 검증용 표시이므로 최종 플레이 HUD에서는 제거 대상이다.

## 여름 장면

- 캡처: `Logs/Summer_Visual_Before.png`
- 봄보다 짙은 녹색, 넓은 흙 경계, 어두운 물로 계절 구분이 가능하다.
- 바위와 흙 자원이 녹지에서 더 잘 분리되고 양쪽 경계가 진행 방향을 명확히 한다.
- 일부 나무·바위 군집은 실루엣이 겹쳐 숄더뷰 높이에서 가려질 수 있으므로 실제 플레이 카메라 검증이 추가로 필요하다.

## 이번 작업 반영

- 공용 봄·여름 맵과 생성기는 수정하지 않았다.
- 박한솔 전용 상점 데모에 밝은 잔디·물·흙 팔레트, 블록형 나무 군집, 볼트 HUD를 반영했다.
- 상점 카드에는 통화, 3개 제안, 비용, 현재/다음 수치, 구매 후 피드백을 유지했다.

## 새 UI 재검증 절차

- 독립 데모 실행 인자 `-evidence-season Spring|Summer`로 동일한 카메라와 UI에서 팔레트만 교체한다.
- 계절 전환은 `ShoulderSeasonPreview` 컴포넌트 하나에 격리되어 팀 맵이나 공용 생성기를 수정하지 않는다.
- 결과 파일은 `ShoulderView_*_Spring_Evidence.png`와 `ShoulderView_*_Summer_Evidence.png`로 분리한다.
- 월드 HUD, 상점 열림, 구매 후 상태를 각 계절에서 모두 캡처해 배경 대비와 상호작용 상태를 함께 확인한다.

## 새 UI 재검증 결과

- 봄: `Logs/ShoulderView_World_Evidence_Spring.png`, `Logs/ShoulderView_Shop_Open_Evidence_Spring.png`, `Logs/ShoulderView_Shop_Purchased_Evidence_Spring.png`
- 여름: `Logs/ShoulderView_World_Evidence_Summer.png`, `Logs/ShoulderView_Shop_Open_Evidence_Summer.png`, `Logs/ShoulderView_Shop_Purchased_Evidence_Summer.png`
- 두 계절 모두 상점 열기와 첫 업그레이드 구매가 성공했으며 플레이어 로그에 `opened=True`, `purchased=True`가 기록됐다.
- 봄의 밝은 녹지와 여름의 짙은 녹지 양쪽에서 네이비 HUD, 양피지 카드, 청록 버튼의 계층과 텍스트 대비가 유지됐다.
- 실행 세션의 창 크기 제한으로 실제 캡처는 `1024x720`이다. 요청 해상도 `1280x720`과 별개로 증거 크기를 과장하지 않는다.

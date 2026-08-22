# 숄더뷰 역 상점 프로토타입 설계

## 목표

플레이어가 3D 숄더뷰로 역 상점 단말기에 접근하고, 화면 중앙으로 조준한 뒤 `E`로 상점을 열어 볼트를 소비하고 업그레이드 결과를 즉시 확인한다.

## 자료에서 가져온 원칙

- Unrailed는 역에서 열차·객차를 업그레이드하며, 업그레이드 비용과 개선 내용을 보여 준다.
- 상점은 제한된 수의 제안을 제시하고, 같은 계열을 반복 강화할수록 비용이 증가한다.
- 볼트는 역 도달·미션·스테이지 수집으로 얻고 업그레이드에 소비하는 핵심 통화다.

참고: [Unrailed Steam 페이지](https://store.steampowered.com/app/1016920/Unrailed/), [공식 업데이트 공지](https://store.steampowered.com/news/posts/?appids=1016920&enddate=1584118806&feed=steam_community_announcements), [Unrailed Wiki - Bolts](https://unrailed-wiki.com/page/Bolts), [Unrailed Wiki - Wagons](https://unrailed-wiki.com/page/Wagons)

## 숄더뷰 적응

1. 플레이어가 월드 단말기를 중앙 조준한다.
2. 상호작용 가능 거리에서는 `E OPEN STATION SHOP` 프롬프트가 나타난다.
3. 상점을 열면 이동·카메라 입력을 잠시 끄고 커서를 해제한다.
4. 한 화면에 3개 제안, 현재 볼트, 현재/다음 수치, 구매 비용을 함께 표시한다.
5. 구매 즉시 볼트·티어·수치·다음 비용이 갱신된다.
6. 닫으면 이동·카메라 입력과 커서 잠금을 복구한다.

## 현재 독립 경계

- 모든 코드·장면·재질은 `Assets/01.hansol/ShoulderView` 안에만 둔다.
- 팀원의 열차, 적, 아이템, 인벤토리 구현을 참조하거나 수정하지 않는다.
- `CRAFT DRIVE`, `CARGO RACK`, `COOLANT LOOP`는 연결 전 UX 검증용 프로토타입 수치다.
- 추후 팀 시스템은 `ShoulderShopOffer.TryUpgrade` 성공 뒤 어댑터/이벤트를 통해 연결한다.

## 완료 기준

- 중앙 조준으로 단말기 탐색 및 상호작용 성공.
- 볼트 부족·최대 티어에서 구매 차단.
- 구매 시 비용 상승, 티어 및 수치 갱신.
- PlayMode 전체 테스트 통과.
- Windows 플레이어에서 상점 열기와 구매 성공 로그 및 전·후 스크린샷 확보.

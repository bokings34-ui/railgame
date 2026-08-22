# Tiny Swords 로컬 UI 테마

Tiny Swords는 게임 프로젝트에서 사용·수정할 수 있지만 원본 또는 수정 파일의 재배포·재포장은 금지된다. 따라서 공개 저장소에는 원본 PNG를 커밋하지 않는다.

공식 조건: <https://pixelfrog-assets.itch.io/tiny-swords>

## 설치

기본 보유 경로가 `D:\Downloads\Tiny Swords (Free Pack)`이면 Unity 메뉴 `Railgame/Hansol/Install Local Tiny Swords UI Theme`를 한 번 실행한다.

다른 경로에서는 Unity 명령행에 `-tiny-swords-path "절대 경로"`를 추가하고 `ShoulderTinySwordsLocalThemeInstaller.Install`을 실행한다.

설치기는 업그레이드 기어 아이콘만 로컬 폴더로 복사한다. 큰 버튼 원본은 조립용 조각 시트여서 현재 UGUI 단일 스프라이트 적용 시 깨지는 것을 실제 캡처로 확인했다. 버튼·패널·카드·헤더는 저장소에 포함된 원본 Railway Workshop 아틀라스를 유지해 철도 정체성과 읽기 쉬운 레이아웃을 보존한다.

생성 경로 `UI/ThirdParty/TinySwordsLocal`은 폴더 내부 `.gitignore`로 제외된다. 비교 빌드에서만 `-use-local-tiny-swords-theme` 플래그로 켠다. 기본 데모는 실제 캡처에서 안정성이 확인된 Railway Workshop 테마를 유지하며, 로컬 폴더를 지우면 옵션도 완전히 제거된다.

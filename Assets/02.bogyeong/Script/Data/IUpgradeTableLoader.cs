// 업그레이드 테이블 로더 추상화. 텍스트(파일 내용)를 받아 포맷을 해석해
// 공통 UpgradeTable로 반환한다. 포맷 교체(CSV→JSON)는 구현 클래스 추가만으로 끝난다.
// 경로가 아닌 "텍스트"를 받으므로 TextAsset/파일/네트워크 등 소스에 독립적이다.
public interface IUpgradeTableLoader
{
    UpgradeTable Load(string text);
}

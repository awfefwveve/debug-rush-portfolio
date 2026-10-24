using System.Collections.Generic;

/// <summary>스토리 기획서 (10/24 B 전달) 그대로: 스테이지별 제목·오프닝·엔딩 대사 (주인공). 괄호 지문은 연출이라 대사에서 뺌</summary>
public static class StoryData
{
    public class Stage { public string title; public string[] intro, outro; }

    public static readonly Dictionary<int, Stage> Chapter1 = new Dictionary<int, Stage>
    {
        { 1, new Stage { title = "첫 번째 예외 발생",
            intro = new[] {
                "으… 머리야. 나… 과방에서 졸고 있었나? 분명 프로젝트 마무리를 하고있었는데…",
                "잠깐만, 저건… 내가 만들고 있는 게임에서 나오는 몬스터?! 어떻게 된거야?",
                "저 NPC 버그에 물들여져 실제로 게임에서 나온건가? 그런거 가능할리가…!",
                "금방 공격해올거야, 이럴 시간 없어 빨리 버그를 막고 고쳐야해!",
                "좋아, 당황하지말고…",
                "버그 단말기만 제대로 수리해서 데이터만 복구하면 탈출할 수 있을 거야. 조합 재료부터 빠르게 찾아서 단말기에 꽂자!" },
            outro = new[] {
                "후우… 일단, 복구를 끝냈는데… 아직 게임이 고쳐지지 않은걸보니 아마 더 남아있는 것 같네.",
                "얼른 버그들을 다 고쳐야지!" } } },
        { 2, new Stage { title = "끝나지 않은 버그",
            intro = new[] {
                "어라? 분명 저 버그 코드를 고쳤는데 왜 아직도 버그들이 남아있는거지?",
                "이런, 여기를 고치는 사이에 다른 쪽을 망가트렸잖아?!",
                "더 빠르게 움직여야겠어!" },
            outro = new[] {
                "드디어 하나는 끝냈네!",
                "얼마나 더 남아있을진 모르겠지만 끝까지 달려보자고!" } } },
        { 3, new Stage { title = "과부하된 네트워크",
            intro = new[] {
                "다른 버그들은 어디로간거지…",
                "도대체 나가는 길이… 아! 저건 도움 NPC!",
                "이쪽이야! 너도 다시 게임에 들어가야지!",
                "어…?",
                "잠깐 잠깐!!" },
            outro = new[] {
                "도움 NPC까지 버그에 당했다니…",
                "플레이어한테 도움주라고 만든건데! 공격이나 하고 말이야!",
                "그래도 어두워진게 많이 없어진걸 보니 거의 다 고쳐진거같아. 얼마 안남았어." } } },
        { 4, new Stage { title = "미완성 네트워크",
            intro = new[] {
                "이런, 다시 살아났잖아?",
                "저번 버그보다 더 어려운 버그인가본데…",
                "이럴시간 없지, 마감도 얼마 남지 않았다고!" },
            outro = new[] {
                "아이고… 저번보다 더 힘들었다…",
                "이랬는데도 아직 멀었다니 거짓말!",
                "그래도 하나씩 해나가면 분명 끝은 있겠지!" } } },
        { 5, new Stage { title = "",   // 기획서에 제목 없음
            intro = new[] {
                "저건… 보스 몬스터인데…",
                "하지만 나갈려면… 부딪힐 수 밖에 없어!" },
            outro = new[] {
                "성공했다… 드디어…",
                "게임이 안정화되고있어.",
                "좋아 좋아, 다시 돌아가자!" } } },
    };

    public static DialogueLine[] Lines(string[] texts)
    {
        var a = new DialogueLine[texts.Length];
        for (int i = 0; i < texts.Length; i++) a[i] = new DialogueLine { speaker = "주인공", text = texts[i] };
        return a;
    }
}

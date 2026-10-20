using UnityEngine;

/// <summary>효과음 (Resources/Sfx). 파일만 같은 이름으로 바꿔 넣으면 교체됨. 음량은 설정창 효과음 슬라이더.</summary>
public static class Sfx
{
    public static void Pickup() => Play("item_pickup");   // 재료·결과물 집기
    public static void ItemOut() => Play("item_out");     // 손에서 내보냄 (제작대·단말기에 넣기, 버리기)
    public static void Craft() => Play("craft");          // 제작 완료
    public static void Hit() => Play("hit");              // 피해받음

    static void Play(string name, float volume = 1f)
    {
        var clip = Resources.Load<AudioClip>("Sfx/" + name);
        if (clip == null) return;
        SoundEffectManager.EnsureInstance();
        SoundEffectManager.Play(clip, volume);
    }
}

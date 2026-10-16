using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 기획자용 스테이지 제작 툴. 메뉴: DebugRush > Stage Maker
/// 사용법: Assets/Editor/StageMaker_사용법.md
/// </summary>
public class StageMakerWindow : EditorWindow
{
    const string DataFolder = "Assets/DebugRush/Data";
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const int Chapters = 3;

    StageData selected;
    string selectedPath;
    SerializedObject so;
    Vector2 leftScroll, rightScroll;
    int newChapter = 1, newStage = 1;

    [MenuItem("DebugRush/Stage Maker")]
    public static void Open() => GetWindow<StageMakerWindow>("Stage Maker");

    void OnGUI()
    {
        // 씬 열기/닫기 후 에셋이 언로드되면 경로로 다시 로드
        if (selected == null && !string.IsNullOrEmpty(selectedPath))
        {
            selected = AssetDatabase.LoadAssetAtPath<StageData>(selectedPath);
            so = null;
            if (selected == null) selectedPath = null;
        }
        var all = LoadAllStages();
        EditorGUILayout.BeginHorizontal();
        DrawLeft(all);
        DrawRight(all);
        EditorGUILayout.EndHorizontal();
    }

    // ---------------- 왼쪽: 3챕터 x 10스테이지 그리드 ----------------
    void DrawLeft(StageData[] all)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        var registered = RegisteredStages();

        for (int c = 1; c <= Chapters; c++)
        {
            EditorGUILayout.LabelField($"챕터 {c} ({c}학년)", EditorStyles.boldLabel);
            int max = Mathf.Max(StageManager.StagesPerChapter, all.Where(s => s.chapter == c).Select(s => s.stage).DefaultIfEmpty(0).Max());
            for (int row = 0; row < Mathf.CeilToInt(max / 5f); row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 1; col <= 5; col++)
                {
                    int n = row * 5 + col;
                    if (n > max) break;
                    var s = all.FirstOrDefault(x => x.chapter == c && x.stage == n);
                    var prev = GUI.backgroundColor;
                    if (s == null) GUI.backgroundColor = new Color(0.55f, 0.55f, 0.55f);
                    else if (!registered.Contains(s)) GUI.backgroundColor = new Color(1f, 0.75f, 0.3f);
                    else GUI.backgroundColor = s == selected ? new Color(0.4f, 0.8f, 1f) : new Color(0.5f, 0.95f, 0.5f);
                    if (GUILayout.Button($"{c}-{n}", GUILayout.Width(45), GUILayout.Height(28)))
                    {
                        if (s != null) Select(s);
                        else { Select(null); newChapter = c; newStage = n; }
                    }
                    GUI.backgroundColor = prev;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(6);
        }
        EditorGUILayout.HelpBox("초록: 플레이 가능\n주황: 에셋은 있지만 게임에 미등록\n회색: 없음(맵에 '준비 중'으로 잠김)", MessageType.None);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ---------------- 오른쪽: 편집 ----------------
    void DrawRight(StageData[] all)
    {
        EditorGUILayout.BeginVertical();
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        if (selected == null)
        {
            DrawCreate(all);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        if (so == null || so.targetObject != selected) so = new SerializedObject(selected);
        so.Update();

        EditorGUILayout.LabelField($"스테이지 {selected.chapter}-{selected.stage}", EditorStyles.largeLabel);
        EditorGUILayout.ObjectField("에셋", selected, typeof(StageData), false);

        Section("기본 정보");
        Prop("chapter", "챕터");
        Prop("stage", "스테이지 번호");
        Prop("displayName", "표시 이름 (카드 제목)");
        Prop("summary", "요약 (카드 설명)");
        Prop("sceneName", "씬 이름");

        Section("난이도");
        Prop("timeLimit", "제한시간 (초, Normal 기준)");
        Prop("timeMultiplier", "난이도별 시간 배율 (Easy/Normal/Hard)");
        Prop("terminalGoal", "난이도별 단말기 목표 (2~8)");

        Section("몬스터 (스테이지당 1~2마리)");
        Prop("monsters", "몬스터 목록");

        Section("스토리");
        Prop("intro", "시작 대사");
        Prop("outro", "종료 대사");

        Section("맵 배치");
        Prop("layout", "배치 데이터");
        so.ApplyModifiedProperties();
        DrawLayoutButtons();

        Section("확인");
        DrawWarnings();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        bool registered = RegisteredStages().Contains(selected);
        if (!registered && GUILayout.Button("게임에 등록 (잠금 해제)", GUILayout.Height(30))) Register(selected);
        if (GUILayout.Button("저장", GUILayout.Height(30))) Save(selected);
        if (GUILayout.Button("▶ 이 스테이지 플레이", GUILayout.Height(30))) Play(selected);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("새 스테이지 만들기로 돌아가기")) Select(null);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawCreate(StageData[] all)
    {
        EditorGUILayout.LabelField("새 스테이지 만들기", EditorStyles.largeLabel);
        newChapter = EditorGUILayout.IntSlider("챕터", newChapter, 1, Chapters);
        newStage = EditorGUILayout.IntSlider("스테이지 번호", newStage, 1, StageManager.StagesPerChapter);

        var exists = all.FirstOrDefault(s => s.chapter == newChapter && s.stage == newStage);
        if (exists != null)
        {
            EditorGUILayout.HelpBox($"{newChapter}-{newStage}는 이미 있습니다.", MessageType.Info);
            if (GUILayout.Button("열기")) Select(exists);
            return;
        }

        var template = all.Where(s => s.chapter == newChapter && s.stage < newStage).OrderByDescending(s => s.stage).FirstOrDefault()
                       ?? all.OrderBy(s => s.chapter).ThenBy(s => s.stage).FirstOrDefault();
        EditorGUILayout.HelpBox(template != null
            ? $"수치·몬스터·배치는 {template.displayName}에서 복사해서 시작합니다. 대사는 비어 있습니다."
            : "기본값으로 시작합니다.", MessageType.None);

        if (GUILayout.Button("새 스테이지 만들기", GUILayout.Height(32)))
            Select(Create(newChapter, newStage, template));
    }

    void DrawLayoutButtons()
    {
        EditorGUILayout.HelpBox("1) [Stage 씬 열기]  2) 씬에서 재료대·제작대·단말기·플레이어를 드래그로 배치  3) [씬 배치를 이 스테이지에 저장]\n" +
                                "단말기를 늘리려면 씬에서 BugTerminal을 복제(Ctrl+D)하고 StageManager의 terminals 목록에 추가한 뒤 저장하세요. " +
                                "저장 후 Stage 씬 자체는 저장하지 않아도 됩니다 (배치는 스테이지 데이터에 들어감).", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stage 씬 열기")) OpenStageScene(selected);
        if (GUILayout.Button("씬에 미리보기")) Preview(selected);
        if (GUILayout.Button("씬 배치를 이 스테이지에 저장")) CaptureLayout(selected);
        if (GUILayout.Button("씬 되돌리기")) RevertScene(selected);
        EditorGUILayout.EndHorizontal();
    }

    void DrawWarnings()
    {
        var s = selected;
        bool any = false;
        void Warn(string msg) { EditorGUILayout.HelpBox(msg, MessageType.Warning); any = true; }

        if (s.monsters == null || s.monsters.Length == 0) Warn("몬스터가 없습니다. (기획서: 스테이지마다 1~2마리)");
        else if (s.monsters.Length > 2) Warn("몬스터가 3마리 이상입니다. (기획서: 1~2마리)");
        else if (s.monsters.Any(m => m.prefab == null)) Warn("프리팹이 비어 있는 몬스터가 있습니다.");
        if (s.intro == null || s.intro.Length == 0) Warn("시작 대사가 없습니다. (기획서: 스테이지 시작과 끝에 스토리)");
        if (s.outro == null || s.outro.Length == 0) Warn("종료 대사가 없습니다.");
        if (s.layout != null && s.layout.use && (s.layout.terminals == null || s.layout.terminals.Length == 0 || s.layout.terminals.Length > 4))
            Warn("단말기는 1~4개로 배치하세요.");
        if (s.terminalGoal == null || s.terminalGoal.Length < 3 || s.timeMultiplier == null || s.timeMultiplier.Length < 3)
            Warn("난이도별 값은 3개(Easy/Normal/Hard)가 필요합니다.");
        if (!RegisteredStages().Contains(s)) Warn("아직 게임에 등록되지 않아 맵에서 '준비 중'으로 잠겨 있습니다. [게임에 등록]을 누르세요.");
        if (!any) EditorGUILayout.HelpBox("문제 없음", MessageType.Info);
    }

    // ---------------- 동작 ----------------
    void Select(StageData s)
    {
        selected = s;
        selectedPath = s != null ? AssetDatabase.GetAssetPath(s) : null;
        so = s != null ? new SerializedObject(s) : null;
        GUI.FocusControl(null);
    }

    static StageData Create(int chapter, int stage, StageData template)
    {
        StageData s = template != null ? Instantiate(template) : CreateInstance<StageData>();
        s.chapter = chapter;
        s.stage = stage;
        s.displayName = $"{chapter}-{stage}";
        s.summary = "";
        s.intro = new DialogueLine[0];
        s.outro = new DialogueLine[0];
        if (s.monsters == null) s.monsters = new MonsterSpawn[0];

        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/DebugRush", "Data");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/Stage_{chapter}_{stage}.asset");
        AssetDatabase.CreateAsset(s, path);
        AssetDatabase.SaveAssets();
        Register(s);
        return s;
    }

    static void Save(StageData s)
    {
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssetIfDirty(s);
        if (!RegisteredStages().Contains(s)) Register(s);   // 저장하면 자동으로 잠금 해제
    }

    static StageData[] LoadAllStages()
        => AssetDatabase.FindAssets("t:StageData")
            .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).OrderBy(s => s.chapter).ThenBy(s => s.stage).ToArray();

    /// <summary>Title 씬 GameManager.allStages에 들어있는 스테이지</summary>
    static StageData[] RegisteredStages()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null && gm.allStages != null) return gm.allStages;
        return ReadTitleSceneStages();
    }

    static StageData[] ReadTitleSceneStages()
    {
        // 씬을 열지 않고 YAML에서 GUID 목록만 읽음 (그리기마다 씬을 열면 느려서)
        var text = System.IO.File.Exists(TitleScenePath) ? System.IO.File.ReadAllText(TitleScenePath) : "";
        int i = text.IndexOf("allStages:");
        if (i < 0) return new StageData[0];
        var list = new System.Collections.Generic.List<StageData>();
        foreach (var line in text.Substring(i).Split('\n').Skip(1))
        {
            var t = line.Trim();
            if (!t.StartsWith("- {fileID")) break;
            int g = t.IndexOf("guid: ");
            if (g < 0) continue;
            string guid = t.Substring(g + 6, 32);
            var s = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid));
            if (s != null) list.Add(s);
        }
        return list.ToArray();
    }

    /// <summary>Title 씬의 GameManager.allStages에 추가 → 맵에 나타나고 진행 조건에 따라 해금</summary>
    static void Register(StageData s)
    {
        var scene = SceneManager.GetSceneByPath(TitleScenePath);
        bool opened = false;
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Additive);
            opened = true;
        }

        GameManager gm = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<GameManager>(true)).FirstOrDefault(x => x != null);
        if (gm == null)
        {
            Debug.LogError("[Stage Maker] Title 씬에서 GameManager를 찾지 못했습니다.");
        }
        else
        {
            var gso = new SerializedObject(gm);
            var arr = gso.FindProperty("allStages");
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == s) has = true;
            if (!has)
            {
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = s;
                gso.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Stage Maker] {s.displayName} 등록 완료");
            }
        }
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    static string ScenePath(StageData s) => $"Assets/Scenes/{(string.IsNullOrEmpty(s.sceneName) ? "Stage" : s.sceneName)}.unity";

    static StageManager OpenStageScene(StageData s)
    {
        string path = ScenePath(s);
        var active = SceneManager.GetActiveScene();
        if (active.path != path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
        var sm = Object.FindAnyObjectByType<StageManager>();
        if (sm == null) Debug.LogError($"[Stage Maker] {path}에서 StageManager를 찾지 못했습니다.");
        return sm;
    }

    static void Preview(StageData s)
    {
        var sm = OpenStageScene(s);
        if (sm == null || s.layout == null) return;
        StageLayoutApplier.Apply(s.layout, sm, false);
        if (s.layout.terminals != null && sm.terminals != null && s.layout.terminals.Length > sm.terminals.Length)
            Debug.Log($"[Stage Maker] 단말기 {s.layout.terminals.Length}개 중 씬에 있는 {sm.terminals.Length}개만 미리보기됩니다. 나머지는 플레이 시 자동 생성됩니다.");
        if (!s.layout.use) Debug.Log("[Stage Maker] 배치 데이터의 'use'가 꺼져 있어 플레이 시에는 씬 기본 배치가 쓰입니다.");
    }

    static void CaptureLayout(StageData s)
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        if (sm == null) { Debug.LogError("[Stage Maker] 먼저 [Stage 씬 열기]를 누르세요."); return; }
        Undo.RecordObject(s, "Capture Stage Layout");
        if (s.layout == null) s.layout = new StageLayout();
        StageLayoutApplier.Capture(s.layout, sm);
        Save(s);
        Debug.Log($"[Stage Maker] {s.displayName} 배치 저장 (단말기 {s.layout.terminals.Length}개)");
    }

    static void RevertScene(StageData s)
    {
        string path = ScenePath(s);
        if (SceneManager.GetActiveScene().path == path) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    static void Play(StageData s)
    {
        Save(s);
        if (OpenStageScene(s) == null) return;
        SessionState.SetString(StageManager.EditorPlayStageKey, AssetDatabase.GetAssetPath(s));
        EditorApplication.EnterPlaymode();
    }

    // ---------------- GUI 헬퍼 ----------------
    static void Section(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    void Prop(string name, string label)
    {
        var p = so.FindProperty(name);
        if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label), true);
    }
}

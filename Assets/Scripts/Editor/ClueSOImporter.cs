using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 线索 SO 批量导入工具（Editor 菜单 → BOOOM → 导入线索CSV）。
///
/// CSV 格式（第一行为表头，后续每行一条线索）：
///   clueId,clueName,suspectId,time,summary
///
/// 示例：
///   CLUE_JESS_01,咖啡杯,SUSPECT_JESS,00:05,桌上遗留的咖啡杯，上面有口红印记。
///   CLUE_JESS_02,日记本,SUSPECT_JESS,00:15,翻开的日记写着"再也回不去了"。
///
/// 导入后会在指定文件夹创建 ClueDataSO 资产，重复ID自动覆盖更新。
/// </summary>
public class ClueSOImporter : EditorWindow
{
    private string _csvPath = "";
    private string _outputFolder = "Assets/ArtAssets/Clues";
    private Vector2 _scroll;
    private string _log = "";

    [MenuItem("BOOOM/导入线索CSV")]
    public static void ShowWindow()
    {
        GetWindow<ClueSOImporter>("线索CSV导入").minSize = new Vector2(480, 320);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("线索 ScriptableObject 批量导入", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // CSV 文件路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CSV 文件", GUILayout.Width(70));
        _csvPath = EditorGUILayout.TextField(_csvPath);
        if (GUILayout.Button("浏览", GUILayout.Width(52)))
        {
            string path = EditorUtility.OpenFilePanel("选择线索CSV文件", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
                _csvPath = path;
        }
        EditorGUILayout.EndHorizontal();

        // 输出目录
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出目录", GUILayout.Width(70));
        _outputFolder = EditorGUILayout.TextField(_outputFolder);
        if (GUILayout.Button("浏览", GUILayout.Width(52)))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转为相对路径
                if (path.StartsWith(Application.dataPath))
                    _outputFolder = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
                else
                    _outputFolder = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // CSV格式说明
        EditorGUILayout.HelpBox(
            "CSV表头（首行）：clueId,clueName,suspectId,time,summary\n" +
            "每行一条线索，逗号分隔，summary列可含中文逗号（建议用英文逗号只出现在前4列分隔处）。\n" +
            "重复clueId会覆盖更新已有资产。",
            MessageType.Info);

        EditorGUILayout.Space(6);

        if (GUILayout.Button("开始导入", GUILayout.Height(32)))
        {
            RunImport();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunImport()
    {
        _log = "";

        if (string.IsNullOrEmpty(_csvPath) || !File.Exists(_csvPath))
        {
            _log = "错误：CSV 文件路径无效，请先选择文件。";
            return;
        }

        // 确保输出目录存在
        if (!AssetDatabase.IsValidFolder(_outputFolder))
        {
            CreateFolderRecursive(_outputFolder);
        }

        string[] lines = File.ReadAllLines(_csvPath, System.Text.Encoding.UTF8);
        if (lines.Length < 2)
        {
            _log = "错误：CSV 文件内容为空或仅含表头。";
            return;
        }

        int created = 0, updated = 0, skipped = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // 最多分割为5列（summary可能含逗号，只切前4刀）
            string[] cols = line.Split(new char[] { ',' }, 5);
            if (cols.Length < 2)
            {
                sb.AppendLine($"行 {i + 1}：列数不足，已跳过 → {line}");
                skipped++;
                continue;
            }

            string clueId   = cols.Length > 0 ? cols[0].Trim() : "";
            string clueName = cols.Length > 1 ? cols[1].Trim() : "";
            string suspectId= cols.Length > 2 ? cols[2].Trim() : "";
            string time     = cols.Length > 3 ? cols[3].Trim() : "";
            string summary  = cols.Length > 4 ? cols[4].Trim() : "";

            if (string.IsNullOrEmpty(clueId))
            {
                sb.AppendLine($"行 {i + 1}：clueId 为空，已跳过。");
                skipped++;
                continue;
            }

            // 查找已有资产
            string assetPath = $"{_outputFolder}/{clueId}.asset";
            ClueDataSO so = AssetDatabase.LoadAssetAtPath<ClueDataSO>(assetPath);
            bool isNew = so == null;

            if (isNew)
            {
                so = CreateInstance<ClueDataSO>();
            }

            so.clueId    = clueId;
            so.clueName  = clueName;
            so.suspectId = suspectId;
            so.time      = time;
            so.summary   = summary;

            if (isNew)
            {
                AssetDatabase.CreateAsset(so, assetPath);
                sb.AppendLine($"[新建] {assetPath}");
                created++;
            }
            else
            {
                EditorUtility.SetDirty(so);
                sb.AppendLine($"[更新] {assetPath}");
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.Insert(0, $"导入完成：新建 {created} 个，更新 {updated} 个，跳过 {skipped} 行。\n\n");
        _log = sb.ToString();
    }

    /// <summary>
    /// 递归创建 Assets 内的多级文件夹。
    /// </summary>
    private static void CreateFolderRecursive(string folderPath)
    {
        string[] parts = folderPath.Replace('\\', '/').Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

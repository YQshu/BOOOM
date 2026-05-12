using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 线索 SO 批量导入工具。
/// 菜单：BOOOM → 导入线索 CSV
///
/// CSV 格式（UTF-8，首行为表头）：
/// clueId, clueName, suspectId, time, summary
///
/// - clueId   : 线索唯一ID，如 CLUE_JESS_01（必填）
/// - clueName : 线索名称（必填）
/// - suspectId: 所属嫌疑人ID，与 SuspectEntry.loopId 一致（必填）
/// - time     : 时间戳，如 00:05（可留空）
/// - summary  : 线索摘要（可留空）
///
/// 生成路径：Assets/Resources/Clues/（自动创建）
/// 已存在同名 SO 时跳过（不覆盖），避免误删已有数据。
/// </summary>
public class ClueCSVImporter : EditorWindow
{
    private string _csvPath = "";
    private string _outputFolder = "Assets/Resources/Clues";
    private string _lastLog = "";
    private Vector2 _scroll;

    [MenuItem("BOOOM/导入线索 CSV")]
    public static void OpenWindow()
    {
        GetWindow<ClueCSVImporter>("线索 CSV 导入").minSize = new Vector2(480, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("线索 CSV 批量导入", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // CSV 路径选择
        EditorGUILayout.BeginHorizontal();
        _csvPath = EditorGUILayout.TextField("CSV 文件路径", _csvPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择线索 CSV", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path)) _csvPath = path;
        }
        EditorGUILayout.EndHorizontal();

        // 输出目录
        _outputFolder = EditorGUILayout.TextField("SO 输出目录", _outputFolder);

        EditorGUILayout.Space();

        // 模板路径提示
        string templatePath = Path.Combine(Application.dataPath, "Editor/ClueImportTemplate.csv");
        EditorGUILayout.HelpBox($"CSV 模板位置：{templatePath}\n列顺序：clueId, clueName, suspectId, time, summary", MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = !string.IsNullOrEmpty(_csvPath);
        if (GUILayout.Button("开始导入", GUILayout.Height(36)))
            Import();
        GUI.enabled = true;

        // 日志显示
        if (!string.IsNullOrEmpty(_lastLog))
        {
            EditorGUILayout.Space();
            GUILayout.Label("导入日志：", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(150));
            EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void Import()
    {
        if (!File.Exists(_csvPath))
        {
            _lastLog = $"[错误] 文件不存在：{_csvPath}";
            return;
        }

        // 确保输出目录存在
        if (!AssetDatabase.IsValidFolder(_outputFolder))
            CreateFolderRecursive(_outputFolder);

        string[] lines = File.ReadAllLines(_csvPath, System.Text.Encoding.UTF8);
        if (lines.Length < 2)
        {
            _lastLog = "[错误] CSV 文件为空或只有表头。";
            return;
        }

        var log = new System.Text.StringBuilder();
        int created = 0, skipped = 0, error = 0;

        // 解析表头，确定列索引（允许列顺序不固定）
        string[] headers = ParseRow(lines[0]);
        int iId      = FindHeader(headers, "clueId");
        int iName    = FindHeader(headers, "clueName");
        int iSuspect = FindHeader(headers, "suspectId");
        int iTime    = FindHeader(headers, "time");
        int iSummary = FindHeader(headers, "summary");

        if (iId < 0 || iName < 0 || iSuspect < 0)
        {
            _lastLog = "[错误] CSV 表头缺少必填列（clueId / clueName / suspectId）。";
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseRow(line);

            string clueId   = SafeGet(cols, iId).Trim();
            string clueName = SafeGet(cols, iName).Trim();
            string suspectId = SafeGet(cols, iSuspect).Trim();
            string time      = iTime    >= 0 ? SafeGet(cols, iTime).Trim()    : "";
            string summary   = iSummary >= 0 ? SafeGet(cols, iSummary).Trim() : "";

            if (string.IsNullOrEmpty(clueId) || string.IsNullOrEmpty(clueName) || string.IsNullOrEmpty(suspectId))
            {
                log.AppendLine($"[第{i+1}行] 跳过：clueId/clueName/suspectId 不能为空");
                error++;
                continue;
            }

            string assetPath = $"{_outputFolder}/{clueId}.asset";

            // 已存在则跳过
            if (AssetDatabase.LoadAssetAtPath<ClueDataSO>(assetPath) != null)
            {
                log.AppendLine($"[跳过] 已存在：{clueId}");
                skipped++;
                continue;
            }

            ClueDataSO so = ScriptableObject.CreateInstance<ClueDataSO>();
            so.clueId    = clueId;
            so.clueName  = clueName;
            so.suspectId = suspectId;
            so.time      = time;
            so.summary   = summary;

            AssetDatabase.CreateAsset(so, assetPath);
            log.AppendLine($"[创建] {clueId} → {assetPath}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.Insert(0, $"完成：创建 {created} 个，跳过 {skipped} 个，错误 {error} 个\n\n");
        _lastLog = log.ToString();
        Debug.Log($"[ClueImporter] {_lastLog}");
    }

    // ─── 工具方法 ─────────────────────────────────────────────

    private static int FindHeader(string[] headers, string name)
    {
        for (int i = 0; i < headers.Length; i++)
            if (headers[i].Trim().ToLower() == name.ToLower()) return i;
        return -1;
    }

    private static string SafeGet(string[] arr, int index)
        => index >= 0 && index < arr.Length ? arr[index] : "";

    /// <summary>解析 CSV 一行，支持带引号的字段（字段内含逗号时用引号包裹）。</summary>
    private static string[] ParseRow(string line)
    {
        var fields = new List<string>();
        bool inQuote = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }   // 转义双引号
                else
                { inQuote = !inQuote; }
            }
            else if (c == ',' && !inQuote)
            { fields.Add(current.ToString()); current.Clear(); }
            else
            { current.Append(c); }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private static void CreateFolderRecursive(string folderPath)
    {
        string[] parts = folderPath.Replace('\\', '/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

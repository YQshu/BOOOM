using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 对话导表工具：将 Excel 导出的 CSV 文件转换为 Ink 故事文件。
/// 菜单：Tools → 对话导表 → CSV 转 Ink
///
/// CSV 列名约定（第一行为表头）：
///   对话序号  — 段落ID，同一序号合并为一个 knot
///   角色      — 说话人（对应 # speaker tag）
///   对话内容  — 对话文字
///   文本状态  — begin/normal/end（可选，忽略）
/// </summary>
public static class CsvToInkConverter
{
    [MenuItem("Tools/对话导表/CSV 转 Ink")]
    static void ConvertCsvToInk()
    {
        // 选择输入 CSV
        string csvPath = EditorUtility.OpenFilePanel("选择对话 CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        // 选择输出 .ink 路径，默认指向 Assets/Ink/Stories/
        string defaultDir = Path.Combine(Application.dataPath, "Ink/Stories");
        string defaultName = Path.GetFileNameWithoutExtension(csvPath);
        string inkPath = EditorUtility.SaveFilePanel("保存 .ink 文件", defaultDir, defaultName, "ink");
        if (string.IsNullOrEmpty(inkPath)) return;

        try
        {
            string content = GenerateInk(csvPath);
            File.WriteAllText(inkPath, content, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            // 在 Project 窗口高亮生成的文件
            string relative = "Assets" + inkPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            var obj = AssetDatabase.LoadAssetAtPath<Object>(relative);
            if (obj != null) EditorGUIUtility.PingObject(obj);

            Debug.Log($"[CsvToInk] 转换完成：{inkPath}");
            EditorUtility.DisplayDialog("完成", $"已生成 {Path.GetFileName(inkPath)}", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("转换失败", e.Message, "OK");
            Debug.LogError($"[CsvToInk] {e}");
        }
    }

    static string GenerateInk(string csvPath)
    {
        string[] rawLines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (rawLines.Length < 2) throw new System.Exception("CSV 文件为空或只有表头");

        string[] headers = ParseCsvLine(rawLines[0]);
        int idxKnot     = FindColumn(headers, "Knot");
        int idxSpeaker = FindColumn(headers, "角色");
        int idxContent = FindColumn(headers, "对话内容");

        if (idxKnot < 0 || idxContent < 0)
            throw new System.Exception("找不到必要列：Knot / 对话内容\n请检查 CSV 表头名称是否正确。");

        var order  = new List<string>();
        var groups = new Dictionary<string, List<(string speaker, string content)>>();

        for (int i = 1; i < rawLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rawLines[i])) continue;
            string[] cols = ParseCsvLine(rawLines[i]);

            string knot     = GetCol(cols, idxKnot).Trim();
            string speaker = idxSpeaker >= 0 ? GetCol(cols, idxSpeaker).Trim() : "";
            string content = GetCol(cols, idxContent).Trim();

            if (string.IsNullOrEmpty(knot) || string.IsNullOrEmpty(content)) continue;

            if (!groups.ContainsKey(knot))
            {
                order.Add(knot);
                groups[knot] = new List<(string, string)>();
            }
            groups[knot].Add((speaker, content));
        }

        if (order.Count == 0)
            throw new System.Exception("未读取到任何对话数据，请检查 CSV 内容。");

        var sb = new StringBuilder();
        sb.AppendLine("// 由 CsvToInkConverter 自动生成，请勿手动修改");
        sb.AppendLine($"// 源文件：{Path.GetFileName(csvPath)}");
        sb.AppendLine();

        for (int i = 0; i < order.Count; i++)
        {
            string seq      = order[i];
            string knotName = SanitizeKnotName(seq, i);

            sb.AppendLine($"// {seq}");
            sb.AppendLine($"=== {knotName} ===");

            foreach (var (speaker, content) in groups[seq])
            {
                if (!string.IsNullOrEmpty(speaker))
                    sb.AppendLine($"# speaker: {speaker}");
                sb.AppendLine(content);
            }

            sb.AppendLine("-> END");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static string SanitizeKnotName(string name, int index)
    {
        // 基于你的要求：把 CSV 中的 Knot 名称原样使用，但将短横线 '-' 替换为字母 'n'
        var s = (name ?? "").Trim();
        if (!string.IsNullOrEmpty(s))
        {
            s = s.Replace("-", "n");
        }
        else s = $"knot_{index}";
        return s;
    }

    static int FindColumn(string[] headers, string name)
    {
        for (int i = 0; i < headers.Length; i++)
            if (headers[i].Trim() == name) return i;
        return -1;
    }

    static string GetCol(string[] cols, int idx)
        => idx < cols.Length ? cols[idx] : "";

    /// <summary>简单 CSV 解析，支持带引号的字段（含逗号或换行）。</summary>
    static string[] ParseCsvLine(string line)
    {
        var result   = new List<string>();
        var current  = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                // 双引号转义：""
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}

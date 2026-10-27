using System;
using System.Collections.Generic;
using System.Drawing;

namespace PowerSimBuilder
{
    /// <summary>
    /// CanvasModel (v1, index-based) &lt;-&gt; ProjectFileV2 (v2, name-based) 変換。
    /// Save: index → 名前ベース（環境非依存、ZIP 共有可能）
    /// Load: 名前ベース → index（後方互換のため CanvasModel に戻す）
    /// </summary>
    public static class ProjectFileV2Extensions
    {
        public static ProjectFileV2 FromCanvasModel(
            this CanvasModel model,
            string name = "",
            string description = "")
        {
            var v2 = new ProjectFileV2
            {
                Metadata = new ProjectMetadata
                {
                    Name = name,
                    Description = description,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    CreatedBy = "PowerSimBuilder",
                },
            };

            // Components
            foreach (var c in model.Components)
            {
                v2.Components.Add(new ComponentInstanceV2
                {
                    InstanceName = c.InstanceName ?? "",
                    Descriptor = c.Descriptor ?? new ComponentDescriptor(),
                    Parameters = c.Parameters != null
                        ? (Dictionary<string, object?>)c.Parameters
                        : new Dictionary<string, object?>(),
                    Location = new PointData { X = c.Location.X, Y = c.Location.Y },
                    PhysicsModelPath = c.Descriptor?.PhysicsModelPath,
                    FmiType = c.Descriptor?.FmiType,
                });
            }

            // Connections: index → "instanceName.port"
            foreach (var conn in model.Connections)
            {
                if (conn.StartComponentIndex < 0 || conn.StartComponentIndex >= model.Components.Count)
                    continue;
                if (conn.EndComponentIndex < 0 || conn.EndComponentIndex >= model.Components.Count)
                    continue;

                var fromName = model.Components[conn.StartComponentIndex].InstanceName;
                var toName = model.Components[conn.EndComponentIndex].InstanceName;
                v2.Connections.Add(new ConnectionV2
                {
                    From = $"{fromName}.{conn.StartPort}",
                    To = $"{toName}.{conn.EndPort}",
                });
            }

            return v2;
        }

        public static CanvasModel ToCanvasModel(this ProjectFileV2 v2)
        {
            var model = new CanvasModel();

            // Components
            foreach (var c in v2.Components)
            {
                var desc = c.Descriptor ?? new ComponentDescriptor();

                // v1.8.0 fix: copy instance-level physics info into descriptor
                // so that round-trip (FromCanvasModel) preserves it.
                if (!string.IsNullOrEmpty(c.PhysicsModelPath))
                    desc.PhysicsModelPath = c.PhysicsModelPath;
                if (!string.IsNullOrEmpty(c.FmiType))
                    desc.FmiType = c.FmiType;

                model.Components.Add(new ComponentModel
                {
                    InstanceName = c.InstanceName ?? "",
                    Descriptor = desc,
                    Parameters = c.Parameters != null
                        ? (Dictionary<string, object>)c.Parameters
                        : new Dictionary<string, object>(),
                    Location = new Point(c.Location?.X ?? 0, c.Location?.Y ?? 0),
                });
            }

            // name → index マップ
            var nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < model.Components.Count; i++)
            {
                var instName = model.Components[i].InstanceName ?? "";
                if (!string.IsNullOrEmpty(instName) && !nameToIndex.ContainsKey(instName))
                    nameToIndex[instName] = i;
            }

            // Connections: "instanceName.port" → index
            foreach (var conn in v2.Connections)
            {
                if (string.IsNullOrEmpty(conn.From) || string.IsNullOrEmpty(conn.To)) continue;

                var dotFrom = conn.From.IndexOf('.');
                var dotTo = conn.To.IndexOf('.');
                if (dotFrom < 0 || dotTo < 0) continue;

                var fromName = conn.From.Substring(0, dotFrom);
                var fromPort = conn.From.Substring(dotFrom + 1);
                var toName = conn.To.Substring(0, dotTo);
                var toPort = conn.To.Substring(dotTo + 1);

                if (!nameToIndex.TryGetValue(fromName, out var fromIdx)) continue;
                if (!nameToIndex.TryGetValue(toName, out var toIdx)) continue;

                model.Connections.Add(new ConnectionModel
                {
                    StartComponentIndex = fromIdx,
                    StartPort = fromPort,
                    EndComponentIndex = toIdx,
                    EndPort = toPort,
                });
            }

            return model;
        }
    }
}
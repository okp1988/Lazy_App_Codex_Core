namespace Lazy_App_Codex_Core
{
    internal static class DeviceMetadataMerge
    {
        // Start with the latest saved records, then apply only fields edited in this dialog.
        internal static Dictionary<string, DeviceInfo> Merge(
            IDictionary<string, DeviceInfo> saved,
            IDictionary<string, DeviceInfo> baseline,
            IDictionary<string, DeviceInfo> edited,
            ISet<string> deleted)
        {
            var result = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in saved)
            {
                if (!deleted.Contains(item.Key)) result[item.Key] = Copy(item.Value);
            }

            foreach (var item in edited)
            {
                if (deleted.Contains(item.Key)) continue;
                baseline.TryGetValue(item.Key, out var original);
                if (!result.TryGetValue(item.Key, out var current))
                {
                    // Do not resurrect an externally deleted, otherwise untouched record.
                    if (original != null && Equal(original, item.Value)) continue;
                    result[item.Key] = Copy(item.Value);
                    continue;
                }

                var local = item.Value;
                if (original == null || local.Name != original.Name) current.Name = local.Name;
                if (original == null || local.Manufacturer != original.Manufacturer) current.Manufacturer = local.Manufacturer;
                if (original == null || local.Model != original.Model) current.Model = local.Model;
                if (original == null || local.LastSerial != original.LastSerial) current.LastSerial = local.LastSerial;
                if (original == null || local.LastSeen != original.LastSeen) current.LastSeen = local.LastSeen;
            }

            return result;
        }

        internal static bool Equal(DeviceInfo left, DeviceInfo right) =>
            left.Name == right.Name && left.Manufacturer == right.Manufacturer &&
            left.Model == right.Model && left.LastSerial == right.LastSerial && left.LastSeen == right.LastSeen;

        private static DeviceInfo Copy(DeviceInfo value) => new()
        {
            Name = value.Name,
            Manufacturer = value.Manufacturer,
            Model = value.Model,
            LastSerial = value.LastSerial,
            LastSeen = value.LastSeen
        };
    }
}
